using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace STT
{
    /// <summary>
    /// STTManager - Whisper.cpp 기반 음성인식(STT) 싱글톤 매니저
    ///
    /// 【역할】
    ///   마이크 녹음 → PCM 오디오 데이터 수집 → Whisper FFI(네이티브 DLL)로 음성 인식 수행.
    ///   실시간(Partial) 인식과 최종(Final) 인식을 모두 지원한다.
    ///   DontDestroyOnLoad로 씬 전환 시에도 유지된다.
    ///
    /// 【참조하는 곳】
    ///   - MicRecordingIndicator : 마이크 버튼 UI에서 녹음 시작/종료 호출
    ///   - STTButton             : 간편 STT 버튼에서 녹음 시작/종료 호출
    ///   - STTHelper             : 코루틴 기반 헬퍼에서 녹음/결과 처리
    ///   - STTTester             : 디버그 테스트 UI에서 사용
    ///   - 각 Problem Step Logic : SetPromptHint()로 키워드 힌트 설정
    ///
    /// 【참조되는 곳】
    ///   - WhisperWrapper : 네이티브 whisper.cpp DLL의 P/Invoke 바인딩
    ///   - Unity Microphone API : 마이크 녹음 제어
    ///
    /// 【흐름】
    ///   1. Awake() → 싱글톤 초기화 + DontDestroyOnLoad
    ///   2. Start() → InitializeWhisper() 코루틴으로 모델 로드 (별도 스레드)
    ///   3. StartRecording() → Microphone.Start() + RecordAudio() 코루틴 (샘플 수집)
    ///      → enableRealtimeProcessing이면 RealtimeProcessAudio() 코루틴도 시작
    ///   4. StopRecording() → Microphone.End() + ProcessAudio() 코루틴 (최종 인식)
    ///   5. RunWhisper() → 네이티브 Whisper API 호출 (스레드 안전, _whisperLock 사용)
    ///   6. 결과 이벤트: OnPartialResult(실시간), OnFinalResult(최종), OnError(오류)
    ///
    /// 사용법:
    /// 1. StartRecording() - 녹음 시작
    /// 2. StopRecording() - 녹음 종료 + 음성 인식
    /// 3. OnFinalResult 이벤트로 결과 수신
    /// </summary>
    public class STTManager : MonoBehaviour
    {
        /// <summary>싱글톤 인스턴스 (DontDestroyOnLoad)</summary>
        public static STTManager Instance { get; private set; }

        [Header("모델 설정")]
        [SerializeField] private string modelFileName = "ggml-tiny.bin";   // StreamingAssets/WhisperModels/ 하위의 모델 파일명
        [SerializeField] private string language = "ko";                   // 인식 대상 언어 (ko = 한국어)

        [Header("녹음 설정")]
        [SerializeField] private int sampleRate = 16000;                   // Whisper가 요구하는 샘플레이트 (16kHz 고정)
        [SerializeField] private float maxRecordingTime = 30f;             // 최대 녹음 시간 (초)

        [Header("Whisper 설정")]
        [SerializeField] private int numThreads = 4;                       // Whisper 추론 시 사용할 CPU 스레드 수
        [SerializeField] private bool translate = false;                   // true이면 영어로 번역 출력 (사용하지 않음)

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;                // 디버그 로그 출력 여부

        [Header("실시간 처리 설정")]
        [SerializeField] private bool enableRealtimeProcessing = true;     // 녹음 중 실시간 부분 인식 활성화 여부
        [SerializeField] private float realtimeProcessInterval = 2.0f;     // 실시간 청크 처리 간격 (초)
        [SerializeField] private int minSamplesForProcessing = 8000;       // 실시간 처리에 필요한 최소 샘플 수 (0.5초 = 8000 @16kHz)
        [SerializeField] private float realtimeWindowSeconds = 3.0f;       // 실시간 처리 시 최근 N초 윈도우만 Whisper에 전달 (전체 누적 방지 → 속도 향상)

        // ===== Whisper 네이티브 핸들 =====
        private IntPtr _ctx = IntPtr.Zero;  // whisper_init_from_file()이 반환하는 네이티브 컨텍스트 포인터. Zero이면 미초기화 상태

        // ===== 마이크 녹음 관련 =====
        private AudioClip _micClip;                                        // Microphone.Start()가 반환하는 AudioClip (순환 버퍼)
        private string _micDevice;                                         // 사용 중인 마이크 디바이스 이름 (Microphone.devices[0])
        private bool _isRecording;                                         // 현재 녹음 진행 중 플래그
        private List<float> _recordedSamples = new List<float>();          // 녹음된 PCM float 샘플 누적 리스트 (-1.0 ~ 1.0)

        // ===== 실시간 처리 관련 =====
        private int _lastProcessedSampleCount;                             // 마지막으로 실시간 처리한 시점의 총 샘플 수 (중복 처리 방지)
        private bool _isRealtimeProcessing;                                // 현재 실시간 Whisper 추론 진행 중 플래그
        private string _lastPartialResult = "";                            // 마지막 실시간 인식 결과 텍스트
        private Coroutine _realtimeProcessCoroutine;                       // RealtimeProcessAudio() 코루틴 참조 (중지용)

        // ===== 처리 상태 =====
        private bool _isProcessing;                                        // 최종 Whisper 추론 진행 중 플래그

        // ===== 스레드 동기화 =====
        private readonly object _whisperLock = new object();               // Whisper context(_ctx) 동시 접근 방지용 락 (실시간 + 최종 처리가 겹칠 수 있음)

        // ===== 인식 힌트 =====
        private string _promptHint = "";                                   // Whisper initial_prompt로 전달할 키워드 힌트 문자열 (환각 방지 + 인식률 향상)

        // ===== 이벤트 (외부 구독용) =====
        /// <summary>실시간 부분 인식 결과 이벤트 (녹음 도중 주기적으로 발생)</summary>
        public event Action<string> OnPartialResult;
        /// <summary>최종 인식 결과 이벤트 (녹음 종료 후 전체 오디오 인식 완료 시 발생)</summary>
        public event Action<string> OnFinalResult;
        /// <summary>오류 발생 이벤트 (초기화 실패, 마이크 없음 등)</summary>
        public event Action<string> OnError;

        // ===== 읽기 전용 상태 프로퍼티 =====
        /// <summary>Whisper 모델이 정상 로드되어 사용 가능한 상태인지</summary>
        public bool IsInitialized => _ctx != IntPtr.Zero;
        /// <summary>현재 마이크 녹음 중인지</summary>
        public bool IsRecording => _isRecording;
        /// <summary>최종 Whisper 추론 처리 중인지</summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 현재 마이크 입력 볼륨 가져오기 (0.0 ~ 1.0)
        /// MicRecordingIndicator의 무음 감지(CheckSilence)에서 주기적으로 호출하여
        /// 사용자가 말하고 있는지 판단하는 데 사용된다.
        /// 최근 128 샘플의 평균 절대값을 반환한다.
        /// </summary>
        public float GetCurrentVolume()
        {
            if (!_isRecording || _micClip == null)
                return 0f;

            // 최근 128 샘플(약 8ms @16kHz)을 읽어서 평균 볼륨 계산
            int sampleWindow = 128;
            float[] samples = new float[sampleWindow];

            // 현재 마이크 쓰기 위치에서 샘플 윈도우만큼 뒤로 이동
            int micPosition = Microphone.GetPosition(_micDevice) - (sampleWindow + 1);
            if (micPosition < 0)
                return 0f;

            _micClip.GetData(samples, micPosition);

            // 각 샘플의 절대값 합산 → 평균 = RMS가 아닌 간이 볼륨 측정
            float sum = 0f;
            for (int i = 0; i < sampleWindow; i++)
            {
                sum += Mathf.Abs(samples[i]);
            }

            return sum / sampleWindow;
        }

        /// <summary>
        /// 싱글톤 초기화. 중복 인스턴스가 있으면 자신을 파괴한다.
        /// ※ STTManager는 Bootstrap 씬에서 생성되므로 Awake() 사용이 허용됨
        ///    (일반 ProblemScene 스텝에서는 Awake 대신 OnEnable 사용 규칙)
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Start에서 Whisper 모델 비동기 초기화를 시작한다.
        /// IEnumerator 반환으로 코루틴 자동 실행.
        /// </summary>
        private IEnumerator Start()
        {
            yield return StartCoroutine(InitializeWhisper());
        }

        /// <summary>
        /// 오브젝트 파괴 시 네이티브 리소스(Whisper context) 및 마이크를 정리한다.
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Whisper 모델 초기화 (코루틴)
        /// StreamingAssets/WhisperModels/ 에서 .bin 모델 파일을 로드한다.
        /// 모델 로드는 수초 소요되므로 별도 스레드에서 수행하고, 메인 스레드에서 완료를 대기한다.
        /// 로드 완료 후 마이크 디바이스를 탐색하여 첫 번째 마이크를 선택한다.
        /// </summary>
        private IEnumerator InitializeWhisper()
        {
            // 모델 파일 경로 조합 (예: StreamingAssets/WhisperModels/ggml-tiny.bin)
            string modelPath = Path.Combine(Application.streamingAssetsPath, "WhisperModels", modelFileName);

            if (!File.Exists(modelPath))
            {
                string error = $"[STT] 모델 파일을 찾을 수 없습니다: {modelPath}";
                Debug.LogError(error);
                OnError?.Invoke(error);
                yield break;
            }

            Log($"모델 로딩 중: {modelPath}");

            // 별도 스레드에서 모델 로드 (메인 스레드 블로킹 방지)
            // ※ whisper_init_from_file()은 수초 소요되므로 메인 스레드에서 호출하면 프레임 멈춤
            bool loadComplete = false;
            string loadError = null;

            Thread loadThread = new Thread(() =>
            {
                try
                {
                    // 네이티브 Whisper 컨텍스트 생성 (모델 파일 로드 + 메모리 할당)
                    _ctx = WhisperWrapper.whisper_init_from_file(modelPath);
                    if (_ctx == IntPtr.Zero)
                    {
                        loadError = "모델 로드 실패";
                    }
                }
                catch (Exception e)
                {
                    loadError = e.Message;
                }
                loadComplete = true;
            });

            loadThread.Start();

            // 로딩 완료까지 매 프레임 대기 (메인 스레드는 자유롭게 렌더링)
            while (!loadComplete)
            {
                yield return null;
            }

            if (loadError != null)
            {
                Debug.LogError($"[STT] {loadError}");
                OnError?.Invoke(loadError);
                yield break;
            }

            // 마이크 디바이스 확인 및 로그 출력
            Log($"=== 감지된 마이크 목록 ({Microphone.devices.Length}개) ===");
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                Log($"  [{i}] {Microphone.devices[i]}");
            }
            Log($"==========================================");

            if (Microphone.devices.Length == 0)
            {
                string error = "마이크를 찾을 수 없습니다";
                Debug.LogError($"[STT] {error}");
                OnError?.Invoke(error);
                yield break;
            }

            // 첫 번째 마이크를 기본 디바이스로 사용
            _micDevice = Microphone.devices[0];
            Log($"초기화 완료! 선택된 마이크: {_micDevice}");
        }

        /// <summary>
        /// 녹음 시작
        /// 마이크 녹음을 시작하고, RecordAudio() 코루틴으로 매 프레임 PCM 샘플을 수집한다.
        /// enableRealtimeProcessing이 true이면 RealtimeProcessAudio() 코루틴도 병렬 시작하여
        /// 녹음 도중 주기적으로 Whisper 부분 인식을 수행한다.
        /// </summary>
        public void StartRecording()
        {
            if (!IsInitialized)
            {
                OnError?.Invoke("STT가 초기화되지 않았습니다");
                return;
            }

            if (_isRecording)
            {
                Log("이미 녹음 중입니다");
                return;
            }

            if (_isProcessing)
            {
                Log("이전 녹음 처리 중입니다");
                return;
            }

            // 이전 녹음 데이터 초기화
            _recordedSamples.Clear();
            _lastProcessedSampleCount = 0;
            _lastPartialResult = "";

            // Unity Microphone API로 녹음 시작 (loop=false, 최대 시간만큼 한번만 녹음)
            _micClip = Microphone.Start(_micDevice, false, Mathf.CeilToInt(maxRecordingTime), sampleRate);
            _isRecording = true;

            // 매 프레임 마이크 데이터를 _recordedSamples에 수집하는 코루틴
            StartCoroutine(RecordAudio());

            // 실시간 부분 인식 코루틴 (녹음 도중 Whisper 추론)
            if (enableRealtimeProcessing)
            {
                _realtimeProcessCoroutine = StartCoroutine(RealtimeProcessAudio());
            }

            Log("녹음 시작");
        }

        /// <summary>
        /// 녹음 중지 및 음성 인식 시작
        /// skipProcessing=true이면 녹음만 중지하고 Whisper 인식은 수행하지 않는다.
        /// 이는 MicRecordingIndicator에서 캐시된 실시간 결과를 사용하거나,
        /// 음성 미감지 시 불필요한 Whisper 추론을 방지하기 위해 사용된다.
        /// </summary>
        /// <param name="skipProcessing">true면 녹음만 중지하고 음성 인식은 수행하지 않음</param>
        public void StopRecording(bool skipProcessing = false)
        {
            if (!_isRecording)
                return;

            _isRecording = false;

            // 실시간 처리 코루틴 중지
            if (_realtimeProcessCoroutine != null)
            {
                StopCoroutine(_realtimeProcessCoroutine);
                _realtimeProcessCoroutine = null;
            }

            // 마이크 녹음 중지 및 마지막 위치 기록
            int lastPos = Microphone.GetPosition(_micDevice);
            Microphone.End(_micDevice);

            // RecordAudio() 코루틴이 아직 수집하지 못한 마지막 구간의 데이터를 추가 수집
            if (_micClip != null && lastPos > 0)
            {
                float[] samples = new float[lastPos];
                _micClip.GetData(samples, 0);
                _recordedSamples.AddRange(samples);
            }

            Log($"녹음 종료. 샘플 수: {_recordedSamples.Count}");

            if (skipProcessing)
            {
                Log("처리 스킵 (캐시 사용 또는 음성 미감지)");
                return;
            }

            // 전체 녹음 데이터로 최종 Whisper 인식 수행
            StartCoroutine(ProcessAudio());
        }

        /// <summary>
        /// 녹음 중 매 프레임 마이크 오디오 데이터를 _recordedSamples에 수집하는 코루틴
        /// Microphone.GetPosition()으로 현재 쓰기 위치를 추적하며,
        /// 이전 프레임 이후 새로 녹음된 구간의 PCM 샘플을 읽어서 리스트에 추가한다.
        /// </summary>
        private IEnumerator RecordAudio()
        {
            int lastPos = 0;  // 이전 프레임에서 읽은 마지막 위치

            while (_isRecording)
            {
                int currentPos = Microphone.GetPosition(_micDevice);

                // 새로 녹음된 샘플이 있으면 수집
                if (currentPos > lastPos)
                {
                    int samplesToRead = currentPos - lastPos;
                    float[] samples = new float[samplesToRead];
                    _micClip.GetData(samples, lastPos);
                    _recordedSamples.AddRange(samples);
                    lastPos = currentPos;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 실시간 청크 기반 음성 인식 코루틴
        /// realtimeProcessInterval 간격으로 최근 realtimeWindowSeconds만큼의 오디오를 잘라서
        /// 별도 스레드에서 Whisper 추론을 수행한다.
        /// 결과는 OnPartialResult 이벤트로 전달되며, MicRecordingIndicator가 구독하여
        /// 실시간 키워드 매칭(캐시)에 활용한다.
        ///
        /// ※ 실시간 처리에서는 singleSegment=true, max_tokens=4로 제한하여
        ///   빠른 응답 + 환각 방지를 우선시한다.
        /// </summary>
        private IEnumerator RealtimeProcessAudio()
        {
            Log("실시간 처리 시작");

            while (_isRecording)
            {
                // 설정된 간격만큼 대기
                yield return new WaitForSeconds(realtimeProcessInterval);

                // 녹음 중이 아니면 종료
                if (!_isRecording) break;

                // 이전 실시간 추론이 아직 진행 중이면 이번 사이클 스킵
                if (_isRealtimeProcessing) continue;

                // 처리할 샘플이 충분한지 확인 (너무 짧은 오디오는 Whisper가 정확히 인식 못함)
                int currentSampleCount = _recordedSamples.Count;
                if (currentSampleCount < minSamplesForProcessing) continue;

                // 이전 처리 이후 새로운 샘플이 없으면 스킵
                if (currentSampleCount <= _lastProcessedSampleCount) continue;

                // 실시간 처리: 최근 N초 윈도우만 사용 (전체 누적 오디오를 보내면 느려짐)
                int windowSamples = Mathf.Min(currentSampleCount, (int)(realtimeWindowSeconds * sampleRate));
                float[] samplesToProcess = new float[windowSamples];
                _recordedSamples.CopyTo(currentSampleCount - windowSamples, samplesToProcess, 0, windowSamples);
                _lastProcessedSampleCount = currentSampleCount;

                // 별도 스레드에서 Whisper 추론 수행 (메인 스레드 블로킹 방지)
                _isRealtimeProcessing = true;
                string result = "";
                bool processComplete = false;

                Thread processThread = new Thread(() =>
                {
                    try
                    {
                        result = RunWhisper(samplesToProcess, singleSegment: true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[STT] 실시간 처리 오류: {e.Message}");
                        result = "";
                    }
                    processComplete = true;
                });

                processThread.Start();

                // 처리 완료 대기 (녹음 중인 동안만, 최대 5초 타임아웃)
                float waitTimer = 0f;
                while (!processComplete && _isRecording && waitTimer < 5f)
                {
                    yield return null;
                    waitTimer += Time.deltaTime;
                }

                // 타임아웃 시 결과 무시하고 계속 진행 (다음 사이클에서 재시도)
                if (!processComplete)
                {
                    Log("실시간 처리 타임아웃 - 스킵");
                }

                _isRealtimeProcessing = false;

                // 결과가 있으면 OnPartialResult 이벤트 발생
                if (!string.IsNullOrEmpty(result))
                {
                    _lastPartialResult = result;
                    Log($"실시간 결과: {result}");
                    OnPartialResult?.Invoke(result);
                }
            }

            Log("실시간 처리 종료");
        }

        /// <summary>
        /// 최종 음성 인식 수행 (코루틴)
        /// 녹음 종료 후 수집된 전체 오디오 샘플(_recordedSamples)을
        /// 별도 스레드에서 Whisper로 인식하고, OnFinalResult 이벤트로 결과를 전달한다.
        /// 실시간 처리와 달리 singleSegment=false, max_tokens=16으로 설정하여
        /// 더 정확한 전체 문맥 인식을 수행한다.
        /// </summary>
        private IEnumerator ProcessAudio()
        {
            // 녹음 데이터가 없으면 빈 결과 반환
            if (_recordedSamples.Count == 0)
            {
                OnFinalResult?.Invoke("");
                yield break;
            }

            _isProcessing = true;
            Log("음성 인식 처리 중...");

            string result = "";
            bool processComplete = false;

            // 별도 스레드에서 Whisper 추론 수행 (메인 스레드 블로킹 방지)
            Thread processThread = new Thread(() =>
            {
                try
                {
                    result = RunWhisper(_recordedSamples.ToArray());
                }
                catch (Exception e)
                {
                    Debug.LogError($"[STT] Whisper 처리 오류: {e.Message}");
                    result = "";
                }
                processComplete = true;
            });

            processThread.Start();

            // 추론 완료까지 매 프레임 대기
            while (!processComplete)
            {
                yield return null;
            }

            _isProcessing = false;

            Log($"인식 결과: {result}");
            OnFinalResult?.Invoke(result);
        }

        /// <summary>
        /// Whisper 네이티브 API 호출 (스레드 안전)
        /// _whisperLock으로 동시 접근을 방지하여, 실시간 처리와 최종 처리가 겹치지 않도록 한다.
        ///
        /// 주요 흐름:
        ///   1. WhisperFullParams 구조체 설정 (Greedy 전략, 한국어, 토큰 제한 등)
        ///   2. float[] 오디오 샘플을 네이티브 메모리(IntPtr)에 복사
        ///   3. whisper_full() 호출로 추론 실행
        ///   4. whisper_full_n_segments() / whisper_full_get_segment_text()로 결과 추출
        ///   5. 네이티브 메모리 해제 (Marshal.FreeHGlobal)
        /// </summary>
        /// <param name="samples">PCM float 오디오 샘플 배열 (16kHz, mono)</param>
        /// <param name="singleSegment">true이면 단일 세그먼트 모드 (실시간용, 더 빠름)</param>
        /// <returns>인식된 텍스트 (빈 문자열이면 인식 실패)</returns>
        private string RunWhisper(float[] samples, bool singleSegment = false)
        {
            // 동시 접근 방지 락 (실시간 처리 스레드 + 최종 처리 스레드가 동시에 _ctx에 접근하면 크래시)
            lock (_whisperLock)
            {
                if (_ctx == IntPtr.Zero)
                {
                    Debug.LogWarning("[STT] Whisper context가 유효하지 않습니다");
                    return "";
                }

                // Greedy 샘플링 전략의 기본 파라미터로 시작
                WhisperFullParams wparams = WhisperWrapper.whisper_full_default_params(
                    WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY);

                wparams.n_threads = numThreads;            // CPU 스레드 수
                wparams.translate = translate;              // 번역 모드 (false = 원어 그대로 출력)
                wparams.print_special = false;              // 특수 토큰 출력 비활성화
                wparams.print_progress = false;             // 진행률 출력 비활성화
                wparams.print_realtime = false;             // 실시간 출력 비활성화
                wparams.print_timestamps = false;           // 타임스탬프 출력 비활성화
                wparams.single_segment = singleSegment;     // 단일 세그먼트 모드 (실시간 처리 시 true)

                // ===== 인식률 강화 설정 =====
                wparams.suppress_blank = true;              // 빈 결과 억제
                wparams.suppress_non_speech_tokens = true;   // 비음성 토큰(음악, 잡음 기호 등) 억제
                wparams.no_context = false;                  // initial_prompt 컨텍스트 사용 허용
                wparams.temperature = 0f;                    // 0 = 가장 확신있는 결과만 (deterministic, 랜덤성 제거)
                wparams.temperature_inc = 0f;                // 온도 증가 비활성화 (실패 시 재시도 방지 → 속도 향상)
                wparams.no_speech_thold = 0.6f;              // 무음 판정 임계값 (이 값 이상이면 "음성 없음"으로 판정)
                wparams.max_tokens = singleSegment ? 4 : 16;  // 실시간은 4토큰(짧게), 최종은 16토큰(정확하게) — 환각 방지

                // 언어 설정 (C 문자열로 변환하여 네이티브에 전달)
                IntPtr langPtr = Marshal.StringToHGlobalAnsi(language);
                wparams.language = langPtr;

                // 인식 힌트(initial_prompt) 설정
                // ※ 최종 인식에서만 사용. 실시간에서 사용하면 힌트 자체를 환각으로 반복 출력하는 문제 발생
                IntPtr promptPtr = IntPtr.Zero;
                if (!singleSegment && !string.IsNullOrEmpty(_promptHint))
                {
                    promptPtr = Marshal.StringToHGlobalAnsi(_promptHint);
                    wparams.initial_prompt = promptPtr;
                }

                try
                {
                    // C#의 float[]를 네이티브 메모리로 복사 (Whisper C API는 네이티브 포인터를 요구)
                    int size = samples.Length * sizeof(float);
                    IntPtr samplesPtr = Marshal.AllocHGlobal(size);
                    Marshal.Copy(samples, 0, samplesPtr, samples.Length);

                    try
                    {
                        // Whisper 추론 실행 (가장 시간이 오래 걸리는 부분)
                        int ret = WhisperWrapper.whisper_full(_ctx, wparams, samplesPtr, samples.Length);

                        if (ret != 0)
                        {
                            Debug.LogError($"[STT] whisper_full 실패: {ret}");
                            return "";
                        }

                        // 인식 결과 세그먼트들을 연결하여 전체 텍스트 구성
                        int nSegments = WhisperWrapper.whisper_full_n_segments(_ctx);
                        string fullText = "";

                        for (int i = 0; i < nSegments; i++)
                        {
                            IntPtr textPtr = WhisperWrapper.whisper_full_get_segment_text(_ctx, i);
                            if (textPtr != IntPtr.Zero)
                            {
                                // 네이티브 C 문자열 → C# string 변환
                                string segment = Marshal.PtrToStringAnsi(textPtr);
                                fullText += segment;
                            }
                        }

                        return fullText.Trim();
                    }
                    finally
                    {
                        // 네이티브 오디오 메모리 해제
                        Marshal.FreeHGlobal(samplesPtr);
                    }
                }
                finally
                {
                    // 네이티브 문자열 메모리 해제 (언어, 프롬프트)
                    Marshal.FreeHGlobal(langPtr);
                    if (promptPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(promptPtr);
                }
            }
        }

        /// <summary>
        /// 리소스 정리 (OnDestroy에서 호출)
        /// 마이크 녹음 중지 → 코루틴 정리 → 진행 중인 Whisper 추론 완료 대기 → 네이티브 컨텍스트 해제
        /// </summary>
        private void Cleanup()
        {
            // 녹음 중지
            if (_isRecording)
            {
                _isRecording = false;
                Microphone.End(_micDevice);
            }

            // 실시간 처리 코루틴 중지
            if (_realtimeProcessCoroutine != null)
            {
                StopCoroutine(_realtimeProcessCoroutine);
                _realtimeProcessCoroutine = null;
            }

            // 진행 중인 Whisper 추론 스레드 완료 대기 (최대 2초, 100ms 간격 폴링)
            // ※ 스레드가 _ctx에 접근 중인 상태에서 whisper_free()를 호출하면 크래시 발생
            int waitCount = 0;
            while ((_isProcessing || _isRealtimeProcessing) && waitCount < 20)
            {
                Thread.Sleep(100);
                waitCount++;
            }

            // Whisper 네이티브 컨텍스트 해제 (락 사용하여 안전하게)
            lock (_whisperLock)
            {
                if (_ctx != IntPtr.Zero)
                {
                    WhisperWrapper.whisper_free(_ctx);
                    _ctx = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// 조건부 디버그 로그 출력 (showDebugLogs가 true일 때만)
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log($"[STT] {message}");
        }

        // ===== 호환성 메서드 (구 Vosk API 호환) =====
        // 초기에 Vosk STT를 사용하다가 Whisper로 전환했으나,
        // 기존 코드와의 호환성을 위해 인터페이스를 유지한다.

        /// <summary>
        /// 인식 힌트 설정 - 예상되는 키워드를 Whisper의 initial_prompt에 전달하여 인식률을 향상시킨다.
        /// MicRecordingIndicator가 녹음 시작 시 현재 스텝의 정답 키워드를 전달한다.
        /// 예: SetPromptHint(new[] { "생각", "사실" }) → _promptHint = "생각, 사실"
        /// </summary>
        public void SetPromptHint(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                _promptHint = "";
                return;
            }
            // 키워드를 쉼표로 연결한 문자열로 구성 (Whisper가 문맥 힌트로 활용)
            _promptHint = string.Join(", ", keywords);
            Log($"인식 힌트 설정: {_promptHint}");
        }

        /// <summary>
        /// 인식 힌트 해제 (스텝 종료 시 호출)
        /// </summary>
        public void ClearPromptHint()
        {
            _promptHint = "";
        }

        /// <summary>
        /// 문법 기반 키워드 제한 설정 (Vosk 호환 - Whisper는 문법 제한을 지원하지 않아 무시됨)
        /// STTButton, STTHelper에서 호출하지만 실제 동작하지 않는다.
        /// 대신 SetPromptHint()를 사용하여 인식률을 향상시킨다.
        /// </summary>
        public void SetGrammar(string[] keywords)
        {
            Log("Whisper는 키워드 제한을 지원하지 않습니다");
        }

        /// <summary>
        /// 문법 해제 (Vosk 호환 - Whisper에서는 무시됨)
        /// </summary>
        public void ClearGrammar()
        {
            // Whisper는 문법 기능이 없으므로 무시
        }
    }
}
