using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace STT
{
    /// <summary>
    /// Vosk 기반 음성 인식 매니저 (싱글톤)
    /// </summary>
    public class STTManager : MonoBehaviour
    {
        public static STTManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string modelFolderName = "vosk-model-small-ko-0.22";
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private float maxRecordingTime = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Vosk 핸들
        private IntPtr _model = IntPtr.Zero;
        private IntPtr _recognizer = IntPtr.Zero;

        // 마이크 관련
        private AudioClip _micClip;
        private string _micDevice;
        private bool _isRecording;
        private int _lastSamplePos;

        // 이벤트
        public event Action<string> OnPartialResult;
        public event Action<string> OnFinalResult;
        public event Action<string> OnError;

        // 상태
        public bool IsInitialized => _model != IntPtr.Zero && _recognizer != IntPtr.Zero;
        public bool IsRecording => _isRecording;

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
            yield return StartCoroutine(InitializeVosk());
        }

        private void OnDestroy()
        {
            Cleanup();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Vosk 모델 초기화
        /// </summary>
        private IEnumerator InitializeVosk()
        {
            // 로그 레벨 설정 (0: 에러만)
            VoskWrapper.vosk_set_log_level(showDebugLogs ? 0 : -1);

            string modelPath = Path.Combine(Application.streamingAssetsPath, modelFolderName);

            if (!Directory.Exists(modelPath))
            {
                string error = $"[STT] 모델 폴더를 찾을 수 없습니다: {modelPath}";
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
                    _model = VoskWrapper.vosk_model_new(modelPath);
                    if (_model == IntPtr.Zero)
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

            // Recognizer 생성
            _recognizer = VoskWrapper.vosk_recognizer_new(_model, sampleRate);
            if (_recognizer == IntPtr.Zero)
            {
                string error = "Recognizer 생성 실패";
                Debug.LogError($"[STT] {error}");
                OnError?.Invoke(error);
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
        /// 음성 인식 시작
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

            // Recognizer 리셋
            VoskWrapper.vosk_recognizer_reset(_recognizer);

            // 마이크 녹음 시작
            _micClip = Microphone.Start(_micDevice, true, Mathf.CeilToInt(maxRecordingTime), sampleRate);
            _lastSamplePos = 0;
            _isRecording = true;

            StartCoroutine(ProcessAudio());
            Log("녹음 시작");
        }

        /// <summary>
        /// 음성 인식 중지 및 최종 결과 반환
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording)
                return;

            _isRecording = false;
            Microphone.End(_micDevice);

            // 최종 결과 가져오기
            IntPtr resultPtr = VoskWrapper.vosk_recognizer_final_result(_recognizer);
            string resultJson = Marshal.PtrToStringAnsi(resultPtr);
            string text = ParseResultText(resultJson);

            Log($"최종 결과: {text}");
            OnFinalResult?.Invoke(text);
        }

        /// <summary>
        /// 실시간 오디오 처리 코루틴
        /// </summary>
        private IEnumerator ProcessAudio()
        {
            float[] floatBuffer = new float[1024];
            short[] shortBuffer = new short[1024];

            while (_isRecording)
            {
                int currentPos = Microphone.GetPosition(_micDevice);
                if (currentPos < _lastSamplePos)
                    currentPos += _micClip.samples;

                int samplesToRead = currentPos - _lastSamplePos;
                if (samplesToRead > 0)
                {
                    // 버퍼 크기 조정
                    if (samplesToRead > floatBuffer.Length)
                    {
                        floatBuffer = new float[samplesToRead];
                        shortBuffer = new short[samplesToRead];
                    }

                    // 오디오 데이터 읽기
                    _micClip.GetData(floatBuffer, _lastSamplePos % _micClip.samples);

                    // float -> short 변환 (16-bit PCM)
                    for (int i = 0; i < samplesToRead; i++)
                    {
                        shortBuffer[i] = (short)(floatBuffer[i] * 32767f);
                    }

                    // Vosk에 전달
                    int result = VoskWrapper.vosk_recognizer_accept_waveform_s(_recognizer, shortBuffer, samplesToRead);

                    // 중간 결과 가져오기
                    if (result == 0)
                    {
                        IntPtr partialPtr = VoskWrapper.vosk_recognizer_partial_result(_recognizer);
                        string partialJson = Marshal.PtrToStringAnsi(partialPtr);
                        string partialText = ParsePartialText(partialJson);

                        if (!string.IsNullOrEmpty(partialText))
                        {
                            OnPartialResult?.Invoke(partialText);
                        }
                    }

                    _lastSamplePos = currentPos % _micClip.samples;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 최종 결과 JSON 파싱
        /// </summary>
        private string ParseResultText(string json)
        {
            // {"text" : "인식된 텍스트"}
            try
            {
                int startIndex = json.IndexOf("\"text\"") + 9;
                int endIndex = json.LastIndexOf("\"");
                if (startIndex > 8 && endIndex > startIndex)
                {
                    return json.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 중간 결과 JSON 파싱
        /// </summary>
        private string ParsePartialText(string json)
        {
            // {"partial" : "인식 중인 텍스트"}
            try
            {
                int startIndex = json.IndexOf("\"partial\"") + 12;
                int endIndex = json.LastIndexOf("\"");
                if (startIndex > 11 && endIndex > startIndex)
                {
                    return json.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 특정 키워드만 인식하도록 문법 설정 (선택적)
        /// </summary>
        public void SetGrammar(string[] keywords)
        {
            if (_recognizer != IntPtr.Zero)
            {
                VoskWrapper.vosk_recognizer_free(_recognizer);
            }

            string grammar = "[\"" + string.Join("\", \"", keywords) + "\", \"[unk]\"]";
            _recognizer = VoskWrapper.vosk_recognizer_new_grm(_model, sampleRate, grammar);

            Log($"문법 설정: {grammar}");
        }

        /// <summary>
        /// 문법 해제 (자유 인식 모드)
        /// </summary>
        public void ClearGrammar()
        {
            if (_recognizer != IntPtr.Zero)
            {
                VoskWrapper.vosk_recognizer_free(_recognizer);
            }

            _recognizer = VoskWrapper.vosk_recognizer_new(_model, sampleRate);
            Log("자유 인식 모드로 전환");
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        private void Cleanup()
        {
            if (_isRecording)
            {
                _isRecording = false;
                Microphone.End(_micDevice);
            }

            if (_recognizer != IntPtr.Zero)
            {
                VoskWrapper.vosk_recognizer_free(_recognizer);
                _recognizer = IntPtr.Zero;
            }

            if (_model != IntPtr.Zero)
            {
                VoskWrapper.vosk_model_free(_model);
                _model = IntPtr.Zero;
            }
        }

        private void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log($"[STT] {message}");
        }
    }
}
