using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace STT
{
    /// <summary>
    /// STTButton - MicRecordingIndicator의 경량 대체 버전 STT 버튼 컴포넌트
    ///
    /// 【역할】
    ///   마이크 버튼 UI를 제공하며, 클릭 시 STT 녹음/인식을 자동 처리한다.
    ///   MicRecordingIndicator와 동일한 공개 인터페이스(ToggleRecording, SetRecording)를 제공하여
    ///   기존 코드와 호환된다. MicRecordingIndicator보다 간단한 구현으로,
    ///   스프라이트 교체 대신 색상 변경 + 펄스 애니메이션으로 상태를 표시한다.
    ///
    /// 【참조하는 곳】
    ///   - 각 Problem Step Logic/Binder : ToggleRecording() / SetRecording() 호출
    ///   - UI Button onClick 이벤트 : ToggleRecording() 연결
    ///
    /// 【참조되는 곳】
    ///   - STTManager : 녹음/인식 기능 (OnPartialResult, OnFinalResult 이벤트)
    ///
    /// 【흐름】
    ///   1. OnEnable → SubscribeWhenReady() 코루틴으로 STTManager 연결 대기
    ///   2. ToggleRecording() → 녹음 시작/중지 토글
    ///   3. 녹음 시작 → STTManager.StartRecording() + RecordingTimeout() 코루틴
    ///   4. 녹음 중 → Update()에서 펄스 애니메이션 (Sin 웨이브 스케일)
    ///   5. 녹음 중지 → STTManager.StopRecording() → HandleFinalResult() → OnRecognitionComplete 이벤트
    ///
    /// 사용법:
    /// - 기존 MicRecordingIndicator 대신 이 컴포넌트를 사용
    /// - 기존 코드에서 MicIndicator.ToggleRecording() → 그대로 동작
    /// </summary>
    public class STTButton : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Image backgroundImage;            // 배경 이미지 (색상 변경 대상)

        [Header("색상 설정")]
        [SerializeField] private Color idleColor = new Color(0.49f, 0.35f, 0.27f);      // 대기 상태 색상 (#7C5A46 갈색)
        [SerializeField] private Color recordingColor = new Color(1f, 0.54f, 0.24f);    // 녹음 중 색상 (#FF8A3D 주황)

        [Header("펄스 애니메이션")]
        [SerializeField] private float pulseAmplitude = 0.05f;     // 펄스 진폭 (스케일 변화량, 0.05 = 5%)
        [SerializeField] private float pulseSpeed = 3f;            // 펄스 속도 (Sin 주기)

        [Header("녹음 설정")]
        [SerializeField] private float maxRecordingDuration = 10f; // 최대 녹음 시간 (초). 초과 시 자동 중지

        [Header("결과 표시 (선택)")]
        [SerializeField] private TextMeshProUGUI resultText;       // 인식 결과를 표시할 텍스트 UI (없으면 무시)

        [Header("키워드 제한 (선택)")]
        [Tooltip("특정 키워드만 인식 (비워두면 자유 인식)")]
        [SerializeField] private string[] recognitionKeywords;     // 인식 키워드 배열 (Vosk 호환, Whisper에서는 실제 제한 안됨)

        // ===== 이벤트 (외부 구독용) =====

        /// <summary>Whisper 최종 인식 완료 시 발생 (인식된 텍스트 전달)</summary>
        public event Action<string> OnRecognitionComplete;

        /// <summary>실시간 중간 인식 결과 발생 시</summary>
        public event Action<string> OnPartialResult;

        /// <summary>녹음이 시작되었을 때 발생</summary>
        public event Action OnRecordingStarted;

        /// <summary>녹음이 종료되었을 때 발생</summary>
        public event Action OnRecordingStopped;

        // ===== 내부 상태 =====

        private bool _recording;                                   // 현재 녹음 중 여부
        private Vector3 _baseScale;                                // 원래 스케일 (펄스 애니메이션 기준)
        private Coroutine _recordingCoroutine;                     // RecordingTimeout 코루틴 참조 (중지용)

        /// <summary>현재 녹음 중인지 (읽기 전용)</summary>
        public bool IsRecording => _recording;

        /// <summary>마지막 Whisper 인식 결과 텍스트</summary>
        public string LastResult { get; private set; }

        // ===== Unity 생명주기 =====

        /// <summary>
        /// 초기 스케일 저장 및 Image 컴포넌트 자동 참조
        /// </summary>
        private void Awake()
        {
            _baseScale = transform.localScale;

            // backgroundImage가 인스펙터에서 미설정이면 같은 오브젝트의 Image 사용
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            ApplyColor();
        }

        /// <summary>
        /// 활성화 시 STTManager 연결 (비동기 대기 코루틴)
        /// </summary>
        private void OnEnable()
        {
            StartCoroutine(SubscribeWhenReady());
        }

        /// <summary>
        /// 비활성화 시 STTManager 이벤트 구독 해제 및 녹음 정리
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeFromSTT();

            // 녹음 중이면 결과 없이 중지 (getResult=false)
            if (_recording)
                StopRecordingInternal(false);
        }

        /// <summary>
        /// 매 프레임 펄스 애니메이션 처리
        /// 녹음 중이면 Sin 웨이브로 스케일을 변화시키고,
        /// 녹음 중이 아니면 원래 크기로 부드럽게 복귀한다.
        /// </summary>
        private void Update()
        {
            if (!_recording)
            {
                // 원래 크기로 Lerp 복귀 (펄스 종료 후 부드러운 전환)
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 10f * Time.deltaTime);
                return;
            }

            // 녹음 중 펄스 애니메이션: 1.0 ± pulseAmplitude 범위에서 Sin 웨이브
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

        /// <summary>
        /// 녹음 상태에 따라 배경 색상을 적용한다.
        /// 녹음 중이면 recordingColor(주황), 아니면 idleColor(갈색)
        /// </summary>
        private void ApplyColor()
        {
            if (backgroundImage != null)
                backgroundImage.color = _recording ? recordingColor : idleColor;
        }

        /// <summary>
        /// STTManager 싱글톤이 준비될 때까지 대기한 후 이벤트를 구독하는 코루틴.
        /// STTManager는 DontDestroyOnLoad이므로 씬 전환 직후에는 아직 없을 수 있다.
        /// 최대 10초 대기 후 연결을 시도한다.
        /// </summary>
        private IEnumerator SubscribeWhenReady()
        {
            float timeout = 10f;
            float elapsed = 0f;

            // STTManager 인스턴스가 생성될 때까지 대기
            while (STTManager.Instance == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (STTManager.Instance != null)
            {
                // 실시간 + 최종 결과 이벤트 구독
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

        /// <summary>
        /// STTManager 이벤트 구독을 해제한다 (OnDisable에서 호출).
        /// </summary>
        private void UnsubscribeFromSTT()
        {
            if (STTManager.Instance != null)
            {
                STTManager.Instance.OnPartialResult -= HandlePartialResult;
                STTManager.Instance.OnFinalResult -= HandleFinalResult;
            }
        }

        /// <summary>
        /// 내부 녹음 시작 처리.
        /// STTManager가 없어도 UI(펄스, 색상)는 동작하도록 설계되어 있다.
        /// </summary>
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

            // STTManager를 통해 마이크 녹음 시작
            STTManager.Instance.StartRecording();

            // 최대 녹음 시간 후 자동 중지 코루틴 시작
            if (_recordingCoroutine != null)
                StopCoroutine(_recordingCoroutine);
            _recordingCoroutine = StartCoroutine(RecordingTimeout());

            OnRecordingStarted?.Invoke();
            Debug.Log("[STTButton] 녹음 시작");
        }

        /// <summary>
        /// 내부 녹음 중지 처리.
        /// </summary>
        /// <param name="getResult">true이면 STTManager.StopRecording()을 호출하여 Whisper 인식 수행</param>
        private void StopRecordingInternal(bool getResult)
        {
            // 타임아웃 코루틴 중지
            if (_recordingCoroutine != null)
            {
                StopCoroutine(_recordingCoroutine);
                _recordingCoroutine = null;
            }

            // STT 중지 (getResult=true이면 Whisper 인식도 수행)
            if (getResult && STTManager.Instance != null && STTManager.Instance.IsRecording)
            {
                STTManager.Instance.StopRecording();
            }

            OnRecordingStopped?.Invoke();
            Debug.Log("[STTButton] 녹음 중지");
        }

        /// <summary>
        /// 최대 녹음 시간 초과 시 자동으로 녹음을 중지하는 코루틴.
        /// </summary>
        private IEnumerator RecordingTimeout()
        {
            yield return new WaitForSeconds(maxRecordingDuration);

            if (_recording)
            {
                Debug.Log("[STTButton] 최대 녹음 시간 초과 - 자동 중지");
                SetRecording(false);
            }
        }

        /// <summary>
        /// STTManager의 실시간 부분 인식 결과 핸들러.
        /// 녹음 중일 때만 결과 텍스트를 UI에 표시하고 이벤트를 전달한다.
        /// </summary>
        private void HandlePartialResult(string text)
        {
            if (!_recording) return;

            if (resultText != null)
                resultText.text = text;

            OnPartialResult?.Invoke(text);
        }

        /// <summary>
        /// STTManager의 최종 인식 결과 핸들러.
        /// LastResult를 업데이트하고, UI에 표시한 후, OnRecognitionComplete 이벤트를 발생시킨다.
        /// </summary>
        private void HandleFinalResult(string text)
        {
            LastResult = text;

            if (resultText != null)
                resultText.text = text;

            Debug.Log($"[STTButton] 인식 결과: {text}");
            OnRecognitionComplete?.Invoke(text);
        }

        /// <summary>
        /// 인식 키워드를 STTManager에 적용한다 (Vosk 호환 — Whisper에서는 실제 제한되지 않음).
        /// </summary>
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
