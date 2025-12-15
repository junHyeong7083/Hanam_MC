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
    /// Whisper 기반 음성 인식 매니저 (싱글톤)
    ///
    /// 사용법:
    /// 1. StartRecording() - 녹음 시작
    /// 2. StopRecording() - 녹음 종료 + 음성 인식
    /// 3. OnFinalResult 이벤트로 결과 수신
    /// </summary>
    public class STTManager : MonoBehaviour
    {
        public static STTManager Instance { get; private set; }

        [Header("모델 설정")]
        [SerializeField] private string modelFileName = "ggml-tiny.bin";
        [SerializeField] private string language = "ko";

        [Header("녹음 설정")]
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private float maxRecordingTime = 30f;

        [Header("Whisper 설정")]
        [SerializeField] private int numThreads = 4;
        [SerializeField] private bool translate = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        [Header("실시간 처리 설정")]
        [SerializeField] private bool enableRealtimeProcessing = true;
        [SerializeField] private float realtimeProcessInterval = 1.0f;  // 청크 처리 간격 (초)
        [SerializeField] private int minSamplesForProcessing = 8000;    // 최소 샘플 수 (0.5초)

        // Whisper 핸들
        private IntPtr _ctx = IntPtr.Zero;

        // 마이크 관련
        private AudioClip _micClip;
        private string _micDevice;
        private bool _isRecording;
        private List<float> _recordedSamples = new List<float>();

        // 실시간 처리 관련
        private int _lastProcessedSampleCount;
        private bool _isRealtimeProcessing;
        private string _lastPartialResult = "";
        private Coroutine _realtimeProcessCoroutine;

        // 처리 상태
        private bool _isProcessing;

        // 스레드 동기화 (Whisper context 동시 접근 방지)
        private readonly object _whisperLock = new object();

        // 이벤트
        public event Action<string> OnPartialResult;  // 실시간 처리 결과
        public event Action<string> OnFinalResult;
        public event Action<string> OnError;

        // 상태
        public bool IsInitialized => _ctx != IntPtr.Zero;
        public bool IsRecording => _isRecording;
        public bool IsProcessing => _isProcessing;

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

        private IEnumerator Start()
        {
            yield return StartCoroutine(InitializeWhisper());
        }

        private void OnDestroy()
        {
            Cleanup();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Whisper 모델 초기화
        /// </summary>
        private IEnumerator InitializeWhisper()
        {
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
            bool loadComplete = false;
            string loadError = null;

            Thread loadThread = new Thread(() =>
            {
                try
                {
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

            // 로딩 대기
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

            // 마이크 디바이스 확인
            if (Microphone.devices.Length == 0)
            {
                string error = "마이크를 찾을 수 없습니다";
                Debug.LogError($"[STT] {error}");
                OnError?.Invoke(error);
                yield break;
            }

            _micDevice = Microphone.devices[0];
            Log($"초기화 완료! 마이크: {_micDevice}");
        }

        /// <summary>
        /// 녹음 시작
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

            // 녹음 데이터 초기화
            _recordedSamples.Clear();
            _lastProcessedSampleCount = 0;
            _lastPartialResult = "";

            // 마이크 녹음 시작
            _micClip = Microphone.Start(_micDevice, false, Mathf.CeilToInt(maxRecordingTime), sampleRate);
            _isRecording = true;

            StartCoroutine(RecordAudio());

            // 실시간 처리 시작
            if (enableRealtimeProcessing)
            {
                _realtimeProcessCoroutine = StartCoroutine(RealtimeProcessAudio());
            }

            Log("녹음 시작");
        }

        /// <summary>
        /// 녹음 중지 및 음성 인식 시작
        /// </summary>
        public void StopRecording()
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

            // 녹음 중지
            int lastPos = Microphone.GetPosition(_micDevice);
            Microphone.End(_micDevice);

            // 녹음된 데이터 가져오기
            if (_micClip != null && lastPos > 0)
            {
                float[] samples = new float[lastPos];
                _micClip.GetData(samples, 0);
                _recordedSamples.AddRange(samples);
            }

            Log($"녹음 종료. 샘플 수: {_recordedSamples.Count}");

            // 실시간 처리에서 이미 결과가 있으면 즉시 반환
            if (enableRealtimeProcessing && !string.IsNullOrEmpty(_lastPartialResult))
            {
                Log($"실시간 처리 결과 사용: {_lastPartialResult}");
                _isProcessing = false;
                OnFinalResult?.Invoke(_lastPartialResult);
                return;
            }

            // 음성 인식 시작
            StartCoroutine(ProcessAudio());
        }

        /// <summary>
        /// 녹음 중 오디오 데이터 수집
        /// </summary>
        private IEnumerator RecordAudio()
        {
            int lastPos = 0;

            while (_isRecording)
            {
                int currentPos = Microphone.GetPosition(_micDevice);

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
        /// 실시간 청크 기반 음성 인식
        /// </summary>
        private IEnumerator RealtimeProcessAudio()
        {
            Log("실시간 처리 시작");

            while (_isRecording)
            {
                yield return new WaitForSeconds(realtimeProcessInterval);

                // 녹음 중이 아니면 종료
                if (!_isRecording) break;

                // 이미 처리 중이면 스킵
                if (_isRealtimeProcessing) continue;

                // 처리할 샘플이 충분한지 확인
                int currentSampleCount = _recordedSamples.Count;
                if (currentSampleCount < minSamplesForProcessing) continue;

                // 새로운 샘플이 없으면 스킵
                if (currentSampleCount <= _lastProcessedSampleCount) continue;

                // 현재까지의 샘플 복사
                float[] samplesToProcess = _recordedSamples.ToArray();
                _lastProcessedSampleCount = currentSampleCount;

                // 별도 스레드에서 처리
                _isRealtimeProcessing = true;
                string result = "";
                bool processComplete = false;

                Thread processThread = new Thread(() =>
                {
                    try
                    {
                        result = RunWhisper(samplesToProcess);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[STT] 실시간 처리 오류: {e.Message}");
                        result = "";
                    }
                    processComplete = true;
                });

                processThread.Start();

                // 처리 완료 대기 (녹음 중인 동안만)
                while (!processComplete && _isRecording)
                {
                    yield return null;
                }

                _isRealtimeProcessing = false;

                // 결과가 있으면 이벤트 발생
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
        /// Whisper로 음성 인식
        /// </summary>
        private IEnumerator ProcessAudio()
        {
            if (_recordedSamples.Count == 0)
            {
                OnFinalResult?.Invoke("");
                yield break;
            }

            _isProcessing = true;
            Log("음성 인식 처리 중...");

            string result = "";
            bool processComplete = false;

            // 별도 스레드에서 Whisper 처리
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

            // 처리 대기
            while (!processComplete)
            {
                yield return null;
            }

            _isProcessing = false;

            Log($"인식 결과: {result}");
            OnFinalResult?.Invoke(result);
        }

        /// <summary>
        /// Whisper API 호출 (스레드 안전)
        /// </summary>
        private string RunWhisper(float[] samples)
        {
            // 동시 접근 방지 락
            lock (_whisperLock)
            {
                if (_ctx == IntPtr.Zero)
                {
                    Debug.LogWarning("[STT] Whisper context가 유효하지 않습니다");
                    return "";
                }

                // Whisper 파라미터 설정
                WhisperFullParams wparams = WhisperWrapper.whisper_full_default_params(
                    WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY);

                wparams.n_threads = numThreads;
                wparams.translate = translate;
                wparams.print_special = false;
                wparams.print_progress = false;
                wparams.print_realtime = false;
                wparams.print_timestamps = false;
                wparams.single_segment = false;

                // 언어 설정
                IntPtr langPtr = Marshal.StringToHGlobalAnsi(language);
                wparams.language = langPtr;

                try
                {
                    // float 배열을 네이티브 메모리로 복사
                    int size = samples.Length * sizeof(float);
                    IntPtr samplesPtr = Marshal.AllocHGlobal(size);
                    Marshal.Copy(samples, 0, samplesPtr, samples.Length);

                    try
                    {
                        // Whisper 실행
                        int ret = WhisperWrapper.whisper_full(_ctx, wparams, samplesPtr, samples.Length);

                        if (ret != 0)
                        {
                            Debug.LogError($"[STT] whisper_full 실패: {ret}");
                            return "";
                        }

                        // 결과 가져오기
                        int nSegments = WhisperWrapper.whisper_full_n_segments(_ctx);
                        string fullText = "";

                        for (int i = 0; i < nSegments; i++)
                        {
                            IntPtr textPtr = WhisperWrapper.whisper_full_get_segment_text(_ctx, i);
                            if (textPtr != IntPtr.Zero)
                            {
                                string segment = Marshal.PtrToStringAnsi(textPtr);
                                fullText += segment;
                            }
                        }

                        return fullText.Trim();
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(samplesPtr);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(langPtr);
                }
            }
        }

        /// <summary>
        /// 리소스 정리
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

            // 처리 완료 대기 (최대 2초)
            int waitCount = 0;
            while ((_isProcessing || _isRealtimeProcessing) && waitCount < 20)
            {
                Thread.Sleep(100);
                waitCount++;
            }

            // Whisper context 정리 (락 사용)
            lock (_whisperLock)
            {
                if (_ctx != IntPtr.Zero)
                {
                    WhisperWrapper.whisper_free(_ctx);
                    _ctx = IntPtr.Zero;
                }
            }
        }

        private void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log($"[STT] {message}");
        }

        // ===== 호환성 메서드 (Vosk API 호환) =====

        /// <summary>
        /// 문법 설정 (Whisper는 지원 안함 - 무시)
        /// </summary>
        public void SetGrammar(string[] keywords)
        {
            Log("Whisper는 키워드 제한을 지원하지 않습니다");
        }

        /// <summary>
        /// 문법 해제 (Whisper는 지원 안함 - 무시)
        /// </summary>
        public void ClearGrammar()
        {
            // 무시
        }
    }
}
