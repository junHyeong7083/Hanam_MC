using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace STT
{
    /// <summary>
    /// STT 테스트용 컴포넌트
    /// 빈 GameObject에 붙이고 UI 연결하면 바로 테스트 가능
    /// </summary>
    public class STTTester : MonoBehaviour
    {
        [Header("UI 연결")]
        [SerializeField] private Button micButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Image micButtonImage;

        [Header("녹음 설정")]
        [SerializeField] private float maxRecordingTime = 5f;

        [Header("시각 피드백")]
        [SerializeField] private Color idleColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color recordingColor = new Color(1f, 0.3f, 0.3f);

        private bool _isRecording;

        private void Start()
        {
            // STTManager 자동 생성 (없으면)
            if (STTManager.Instance == null)
            {
                var go = new GameObject("STTManager");
                go.AddComponent<STTManager>();
            }

            // 버튼 리스너
            if (micButton != null)
            {
                micButton.onClick.AddListener(OnClickMic);
            }

            // 이벤트 구독
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult += OnPartialResult;
                STTManager.Instance.OnFinalResult += OnFinalResult;
                STTManager.Instance.OnError += OnError;
            }

            UpdateUI("대기 중... 마이크 버튼을 누르세요", "");
        }

        private void OnDestroy()
        {
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult -= OnPartialResult;
                STTManager.Instance.OnFinalResult -= OnFinalResult;
                STTManager.Instance.OnError -= OnError;
            }
        }

        private void Update()
        {
            // 녹음 중 펄스 애니메이션
            if (_isRecording && micButtonImage != null)
            {
                float pulse = 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
                micButtonImage.color = recordingColor * pulse;
            }
        }

        public void OnClickMic()
        {
            if (STTManager.Instance == null)
            {
                UpdateUI("오류: STTManager가 없습니다", "");
                return;
            }

            if (!STTManager.Instance.IsInitialized)
            {
                UpdateUI("오류: STT 초기화 중...", "모델 로딩을 기다려주세요");
                return;
            }

            if (_isRecording)
            {
                // 녹음 중지
                StopRecording();
            }
            else
            {
                // 녹음 시작
                StartRecording();
            }
        }

        private void StartRecording()
        {
            _isRecording = true;
            STTManager.Instance.StartRecording();

            if (micButtonImage != null)
                micButtonImage.color = recordingColor;

            UpdateUI("녹음 중... (다시 누르면 중지)", "");

            // 최대 녹음 시간 후 자동 중지
            Invoke(nameof(AutoStopRecording), maxRecordingTime);
        }

        private void StopRecording()
        {
            CancelInvoke(nameof(AutoStopRecording));
            _isRecording = false;
            STTManager.Instance.StopRecording();

            if (micButtonImage != null)
                micButtonImage.color = idleColor;

            UpdateUI("처리 중...", "");
        }

        private void AutoStopRecording()
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }

        private void OnPartialResult(string text)
        {
            if (resultText != null)
                resultText.text = $"(인식 중) {text}";
        }

        private void OnFinalResult(string text)
        {
            _isRecording = false;

            if (micButtonImage != null)
                micButtonImage.color = idleColor;

            if (string.IsNullOrEmpty(text))
            {
                UpdateUI("인식 결과 없음", "다시 시도해주세요");
            }
            else
            {
                UpdateUI("인식 완료!", text);
                Debug.Log($"[STT 테스트] 최종 결과: {text}");
            }
        }

        private void OnError(string error)
        {
            _isRecording = false;

            if (micButtonImage != null)
                micButtonImage.color = idleColor;

            UpdateUI($"오류: {error}", "");
            Debug.LogError($"[STT 테스트] {error}");
        }

        private void UpdateUI(string status, string result)
        {
            if (statusText != null)
                statusText.text = status;

            if (resultText != null && !string.IsNullOrEmpty(result))
                resultText.text = result;
        }
    }
}
