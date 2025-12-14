using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace STT
{
    /// <summary>
    /// MicRecordingIndicator를 대체하는 통합 STT 버튼
    ///
    /// 기능:
    /// 1. 기존 MicRecordingIndicator와 동일한 인터페이스 (ToggleRecording, SetRecording)
    /// 2. 색상 변경 + 펄스 애니메이션
    /// 3. STT 음성 인식 자동 처리
    ///
    /// 사용법:
    /// - 기존 MicRecordingIndicator 대신 이 컴포넌트 사용
    /// - 기존 코드에서 MicIndicator.ToggleRecording() → 그대로 동작
    /// </summary>
    /// 
    public class STTButton : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Image backgroundImage;

        [Header("색상 설정")]
        [SerializeField] private Color idleColor = new Color(0.49f, 0.35f, 0.27f);      // #7C5A46
        [SerializeField] private Color recordingColor = new Color(1f, 0.54f, 0.24f);    // #FF8A3D

        [Header("펄스 애니메이션")]
        [SerializeField] private float pulseAmplitude = 0.05f;
        [SerializeField] private float pulseSpeed = 3f;

        [Header("녹음 설정")]
        [SerializeField] private float maxRecordingDuration = 10f;

        [Header("결과 표시 (선택)")]
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("키워드 제한 (선택)")]
        [Tooltip("특정 키워드만 인식 (비워두면 자유 인식)")]
        [SerializeField] private string[] recognitionKeywords;

        // ===== 이벤트 =====

        /// <summary>음성 인식 완료 시</summary>
        public event Action<string> OnRecognitionComplete;

        /// <summary>실시간 중간 결과</summary>
        public event Action<string> OnPartialResult;

        /// <summary>녹음 시작됨</summary>
        public event Action OnRecordingStarted;

        /// <summary>녹음 종료됨</summary>
        public event Action OnRecordingStopped;

        // ===== 상태 =====

        private bool _recording;
        private Vector3 _baseScale;
        private Coroutine _recordingCoroutine;

        /// <summary>현재 녹음 중인지</summary>
        public bool IsRecording => _recording;

        /// <summary>마지막 인식 결과</summary>
        public string LastResult { get; private set; }

        // ===== Unity 생명주기 =====

        private void Awake()
        {
            _baseScale = transform.localScale;

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            ApplyColor();
        }

        private void OnEnable()
        {
            StartCoroutine(SubscribeWhenReady());
        }

        private void OnDisable()
        {
            UnsubscribeFromSTT();

            if (_recording)
                StopRecordingInternal(false);
        }

        private void Update()
        {
            if (!_recording)
            {
                // 원래 크기로 천천히 복귀
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 10f * Time.deltaTime);
                return;
            }

            // 녹음 중 펄스 애니메이션
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            transform.localScale = _baseScale * s;
        }

        // ===== 공개 메서드 (MicRecordingIndicator 호환) =====

        /// <summary>
        /// 녹음 토글 (기존 MicRecordingIndicator.ToggleRecording()과 동일)
        /// </summary>
        public void ToggleRecording()
        {
            _recording = !_recording;
            ApplyColor();

            if (_recording)
                StartRecordingInternal();
            else
                StopRecordingInternal(true);
        }

        /// <summary>
        /// 녹음 상태 직접 설정 (기존 MicRecordingIndicator.SetRecording()과 동일)
        /// </summary>
        public void SetRecording(bool value)
        {
            if (_recording == value) return;

            _recording = value;
            ApplyColor();

            if (_recording)
                StartRecordingInternal();
            else
                StopRecordingInternal(true);
        }

        /// <summary>
        /// 인식할 키워드 설정 (런타임)
        /// </summary>
        public void SetKeywords(params string[] keywords)
        {
            recognitionKeywords = keywords;
            ApplyKeywords();
        }

        // ===== 내부 메서드 =====

        private void ApplyColor()
        {
            if (backgroundImage != null)
                backgroundImage.color = _recording ? recordingColor : idleColor;
        }

        private IEnumerator SubscribeWhenReady()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (STTManager.Instance == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult += HandlePartialResult;
                STTManager.Instance.OnFinalResult += HandleFinalResult;
                ApplyKeywords();
                Debug.Log("[STTButton] STTManager 연결됨");
            }
            else
            {
                Debug.LogWarning("[STTButton] STTManager를 찾을 수 없습니다. STT 기능이 비활성화됩니다.");
            }
        }

        private void UnsubscribeFromSTT()
        {
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult -= HandlePartialResult;
                STTManager.Instance.OnFinalResult -= HandleFinalResult;
            }
        }

        private void StartRecordingInternal()
        {
            // STTManager가 없어도 UI는 동작 (펄스, 색상)
            if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[STTButton] STTManager 미초기화 - UI만 동작");
                OnRecordingStarted?.Invoke();
                return;
            }

            LastResult = "";

            // STT 시작
            STTManager.Instance.StartRecording();

            // 타임아웃 코루틴
            if (_recordingCoroutine != null)
                StopCoroutine(_recordingCoroutine);
            _recordingCoroutine = StartCoroutine(RecordingTimeout());

            OnRecordingStarted?.Invoke();
            Debug.Log("[STTButton] 녹음 시작");
        }

        private void StopRecordingInternal(bool getResult)
        {
            // 타임아웃 코루틴 중지
            if (_recordingCoroutine != null)
            {
                StopCoroutine(_recordingCoroutine);
                _recordingCoroutine = null;
            }

            // STT 중지
            if (getResult && STTManager.Instance != null && STTManager.Instance.IsRecording)
            {
                STTManager.Instance.StopRecording();
            }

            OnRecordingStopped?.Invoke();
            Debug.Log("[STTButton] 녹음 중지");
        }

        private IEnumerator RecordingTimeout()
        {
            yield return new WaitForSeconds(maxRecordingDuration);

            if (_recording)
            {
                Debug.Log("[STTButton] 최대 녹음 시간 초과 - 자동 중지");
                SetRecording(false);
            }
        }

        private void HandlePartialResult(string text)
        {
            if (!_recording) return;

            if (resultText != null)
                resultText.text = text;

            OnPartialResult?.Invoke(text);
        }

        private void HandleFinalResult(string text)
        {
            LastResult = text;

            if (resultText != null)
                resultText.text = text;

            Debug.Log($"[STTButton] 인식 결과: {text}");
            OnRecognitionComplete?.Invoke(text);
        }

        private void ApplyKeywords()
        {
            if (STTManager.Instance == null) return;

            if (recognitionKeywords != null && recognitionKeywords.Length > 0)
            {
                STTManager.Instance.SetGrammar(recognitionKeywords);
            }
            else
            {
                STTManager.Instance.ClearGrammar();
            }
        }
    }
}
