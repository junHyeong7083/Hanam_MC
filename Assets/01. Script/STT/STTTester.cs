using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace STT
{
    /// <summary>
    /// STTTester - 개발/디버그용 STT 테스트 컴포넌트
    ///
    /// 【역할】
    ///   Whisper 음성 인식 및 KeywordMatcher 키워드 매칭을 실시간으로 테스트할 수 있는
    ///   독립적인 디버그 UI 컴포넌트. 빈 GameObject에 붙이고 UI 요소를 연결하면
    ///   별도의 코드 작성 없이 STT 기능을 테스트할 수 있다.
    ///
    /// 【참조하는 곳】
    ///   - 에디터 테스트 씬에서 사용 (프로덕션에서는 사용하지 않음)
    ///
    /// 【참조되는 곳】
    ///   - STTManager   : 녹음/인식 기능
    ///   - KeywordMatcher : FindBestMatch()로 키워드 매칭 테스트
    ///
    /// 【흐름】
    ///   1. Start() → STTManager 자동 생성 (없으면) + 버튼 리스너 + 이벤트 구독
    ///   2. 마이크 버튼 클릭 → OnClickMic() → StartRecording() / StopRecording() 토글
    ///   3. 녹음 중 → Update()에서 버튼 색상 펄스 애니메이션
    ///   4. 인식 완료 → OnFinalResult() → KeywordMatcher.FindBestMatch()로 키워드 매칭
    ///   5. 결과를 UI에 표시 (인식 텍스트, 매칭 키워드, 유사도 %)
    /// </summary>
    public class STTTester : MonoBehaviour
    {
        [Header("UI 연결")]
        [SerializeField] private Button micButton;                 // 마이크 토글 버튼
        [SerializeField] private TextMeshProUGUI statusText;       // 상태 표시 텍스트 ("대기 중", "녹음 중", "처리 중" 등)
        [SerializeField] private TextMeshProUGUI resultText;       // 인식 결과 + 매칭 결과 표시 텍스트
        [SerializeField] private Image micButtonImage;             // 마이크 버튼 이미지 (색상 변경용)

        [Header("녹음 설정")]
        [SerializeField] private float maxRecordingTime = 5f;      // 최대 녹음 시간 (초). 초과 시 자동 중지

        [Header("시각 피드백")]
        [SerializeField] private Color idleColor = new Color(0.3f, 0.3f, 0.3f);     // 대기 상태 색상 (어두운 회색)
        [SerializeField] private Color recordingColor = new Color(1f, 0.3f, 0.3f);   // 녹음 중 색상 (빨간색)

        [Header("키워드 매칭 테스트")]
        [SerializeField] private string[] testKeywords = { "위스퍼", "가나다라", "유니티" };  // 테스트용 키워드 배열
        [SerializeField] private float matchThreshold = 0.5f;      // 매칭 성공 임계값 (0.5 = 50% 이상 유사도)

        private bool _isRecording;  // 현재 녹음 중 플래그

        /// <summary>
        /// 초기화: STTManager 자동 생성, 버튼 리스너 등록, STT 이벤트 구독
        /// </summary>
        private void Start()
        {
            // STTManager 인스턴스가 없으면 새 GameObject에 자동 생성
            // (테스트 씬에서 Bootstrap 없이 독립 실행 시 필요)
            if (STTManager.Instance == null)
            {
                var go = new GameObject("STTManager");
                go.AddComponent<STTManager>();
            }

            // 마이크 버튼 클릭 리스너 등록
            if (micButton != null)
            {
                micButton.onClick.AddListener(OnClickMic);
            }

            // STTManager 이벤트 구독 (실시간 결과, 최종 결과, 에러)
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult += OnPartialResult;
                STTManager.Instance.OnFinalResult += OnFinalResult;
                STTManager.Instance.OnError += OnError;
            }

            UpdateUI("대기 중... 마이크 버튼을 누르세요", "");
        }

        /// <summary>
        /// 파괴 시 STTManager 이벤트 구독 해제 (메모리 누수 방지)
        /// </summary>
        private void OnDestroy()
        {
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult -= OnPartialResult;
                STTManager.Instance.OnFinalResult -= OnFinalResult;
                STTManager.Instance.OnError -= OnError;
            }
        }

        /// <summary>
        /// 매 프레임 녹음 중 시각 피드백 (버튼 색상 펄스)
        /// Sin 웨이브로 0.8~1.0 범위에서 recordingColor 밝기를 변화시킨다.
        /// </summary>
        private void Update()
        {
            // 녹음 중 펄스 애니메이션
            if (_isRecording && micButtonImage != null)
            {
                float pulse = 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
                micButtonImage.color = recordingColor * pulse;
            }
        }

        /// <summary>
        /// 마이크 버튼 클릭 핸들러. 녹음 시작/중지를 토글한다.
        /// STTManager 상태를 먼저 확인하여, 미초기화 시 에러 메시지를 표시한다.
        /// </summary>
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
                StopRecording();     // 녹음 중이면 중지
            }
            else
            {
                StartRecording();    // 대기 중이면 녹음 시작
            }
        }

        /// <summary>
        /// 녹음 시작. STTManager.StartRecording() 호출 + 자동 중지 타이머 설정
        /// </summary>
        private void StartRecording()
        {
            _isRecording = true;
            STTManager.Instance.StartRecording();

            if (micButtonImage != null)
                micButtonImage.color = recordingColor;

            UpdateUI("녹음 중... (다시 누르면 중지)", "");

            // maxRecordingTime 후 자동 중지 (Invoke 사용)
            Invoke(nameof(AutoStopRecording), maxRecordingTime);
        }

        /// <summary>
        /// 녹음 중지. 자동 중지 타이머 취소 + STTManager.StopRecording() 호출
        /// </summary>
        private void StopRecording()
        {
            CancelInvoke(nameof(AutoStopRecording));
            _isRecording = false;
            STTManager.Instance.StopRecording();

            if (micButtonImage != null)
                micButtonImage.color = idleColor;

            UpdateUI("처리 중...", "");
        }

        /// <summary>
        /// 최대 녹음 시간 초과 시 Invoke로 호출되는 자동 중지 메서드
        /// </summary>
        private void AutoStopRecording()
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }

        /// <summary>
        /// 실시간 부분 인식 결과 핸들러. 인식 진행 중 텍스트를 UI에 표시한다.
        /// </summary>
        private void OnPartialResult(string text)
        {
            if (resultText != null)
                resultText.text = $"(인식 중) {text}";
        }

        /// <summary>
        /// 최종 인식 결과 핸들러.
        /// Whisper 인식 텍스트를 testKeywords 배열과 KeywordMatcher.FindBestMatch()로 매칭하고,
        /// 매칭 결과(키워드, 유사도 %)를 UI에 표시한다.
        /// </summary>
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
                // 키워드 매칭: testKeywords 중 가장 유사한 키워드와 유사도를 반환
                var (matchedKeyword, similarity) = KeywordMatcher.FindBestMatch(text, testKeywords);

                string matchResult = $"인식: \"{text}\"\n" +
                    $"결과: \"{matchedKeyword}\" ({similarity * 100:F0}%)";

                UpdateUI("인식 완료!", matchResult);

                Debug.Log($"[STT 테스트] 인식: {text} → 결과: {matchedKeyword} ({similarity * 100:F0}%)");
            }
        }

        /// <summary>
        /// STT 에러 핸들러. 에러 메시지를 UI에 표시하고 녹음 상태를 초기화한다.
        /// </summary>
        private void OnError(string error)
        {
            _isRecording = false;

            if (micButtonImage != null)
                micButtonImage.color = idleColor;

            UpdateUI($"오류: {error}", "");
            Debug.LogError($"[STT 테스트] {error}");
        }

        /// <summary>
        /// 상태 텍스트와 결과 텍스트를 업데이트하는 유틸리티 메서드
        /// </summary>
        /// <param name="status">상태 텍스트 ("대기 중", "녹음 중", "처리 중" 등)</param>
        /// <param name="result">결과 텍스트 (비어있으면 기존 유지)</param>
        private void UpdateUI(string status, string result)
        {
            if (statusText != null)
                statusText.text = status;

            if (resultText != null && !string.IsNullOrEmpty(result))
                resultText.text = result;
        }
    }
}
