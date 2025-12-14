using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace STT
{
    /// <summary>
    /// 마이크 버튼에 부착하여 STT 기능을 쉽게 연동하는 컴포넌트
    /// </summary>
    public class STTButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button micButton;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Recording Settings")]
        [SerializeField] private float maxRecordingDuration = 5f;
        [SerializeField] private float silenceTimeout = 2f;

        [Header("Visual Feedback")]
        [SerializeField] private Color idleColor = new Color(0.49f, 0.35f, 0.27f);
        [SerializeField] private Color recordingColor = new Color(1f, 0.54f, 0.24f);
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.1f;

        [Header("Keywords (Optional)")]
        [SerializeField] private string[] recognitionKeywords;

        // 이벤트
        public event Action<string> OnRecognitionComplete;
        public event Action<string> OnPartialResult;
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;

        // 상태
        private bool _isRecording;
        private float _recordingTimer;
        private Vector3 _baseScale;
        private Coroutine _recordingCoroutine;

        public bool IsRecording => _isRecording;
        public string LastResult { get; private set; }

        private void Awake()
        {
            _baseScale = transform.localScale;

            if (micButton == null)
                micButton = GetComponent<Button>();

            if (buttonImage == null)
                buttonImage = GetComponent<Image>();

            if (micButton != null)
                micButton.onClick.AddListener(ToggleRecording);
        }

        private void OnEnable()
        {
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult += HandlePartialResult;
                STTManager.Instance.OnFinalResult += HandleFinalResult;

                // 키워드 설정 (옵션)
                if (recognitionKeywords != null && recognitionKeywords.Length > 0)
                {
                    STTManager.Instance.SetGrammar(recognitionKeywords);
                }
            }
        }

        private void OnDisable()
        {
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult -= HandlePartialResult;
                STTManager.Instance.OnFinalResult -= HandleFinalResult;
            }

            if (_isRecording)
                StopRecordingInternal();
        }

        private void Update()
        {
            if (_isRecording)
            {
                // 펄스 애니메이션
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                transform.localScale = _baseScale * pulse;
            }
            else
            {
                // 원래 크기로 복귀
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, Time.deltaTime * 10f);
            }
        }

        /// <summary>
        /// 녹음 토글
        /// </summary>
        public void ToggleRecording()
        {
            if (_isRecording)
                StopRecording();
            else
                StartRecording();
        }

        /// <summary>
        /// 녹음 시작
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording) return;

            if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[STTButton] STTManager가 초기화되지 않았습니다");
                return;
            }

            _isRecording = true;
            _recordingTimer = 0f;

            // 색상 변경
            if (buttonImage != null)
                buttonImage.color = recordingColor;

            // STT 시작
            STTManager.Instance.StartRecording();

            // 타임아웃 코루틴 시작
            _recordingCoroutine = StartCoroutine(RecordingTimeout());

            OnRecordingStarted?.Invoke();
        }

        /// <summary>
        /// 녹음 중지
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording) return;
            StopRecordingInternal();
        }

        private void StopRecordingInternal()
        {
            _isRecording = false;

            // 코루틴 중지
            if (_recordingCoroutine != null)
            {
                StopCoroutine(_recordingCoroutine);
                _recordingCoroutine = null;
            }

            // 색상 복귀
            if (buttonImage != null)
                buttonImage.color = idleColor;

            // STT 중지
            if (STTManager.Instance != null)
                STTManager.Instance.StopRecording();

            OnRecordingStopped?.Invoke();
        }

        private IEnumerator RecordingTimeout()
        {
            yield return new WaitForSeconds(maxRecordingDuration);

            if (_isRecording)
            {
                Debug.Log("[STTButton] 최대 녹음 시간 초과");
                StopRecordingInternal();
            }
        }

        private void HandlePartialResult(string text)
        {
            if (!_isRecording) return;

            if (resultText != null)
                resultText.text = text;

            OnPartialResult?.Invoke(text);
        }

        private void HandleFinalResult(string text)
        {
            LastResult = text;

            if (resultText != null)
                resultText.text = text;

            OnRecognitionComplete?.Invoke(text);
        }

        /// <summary>
        /// 인식할 키워드 설정 (런타임)
        /// </summary>
        public void SetKeywords(params string[] keywords)
        {
            recognitionKeywords = keywords;

            if (STTManager.Instance != null && keywords.Length > 0)
            {
                STTManager.Instance.SetGrammar(keywords);
            }
            else if (STTManager.Instance != null)
            {
                STTManager.Instance.ClearGrammar();
            }
        }
    }
}
