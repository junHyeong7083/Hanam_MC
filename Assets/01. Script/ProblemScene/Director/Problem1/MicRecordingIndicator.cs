using System;
using UnityEngine;
using UnityEngine.UI;
using STT;

/// <summary>
/// MicRecordingIndicator - 마이크 버튼 UI + STT 음성 인식 + 키워드 매칭 통합 컴포넌트
///
/// 【역할】
///   마이크 버튼의 시각적 상태 관리(idle/recording/recognizing 스프라이트 교체)와
///   STT 녹음/인식/키워드 매칭까지 전체 음성 인식 플로우를 담당하는 핵심 컴포넌트.
///   각 Problem Step의 음성 입력 UI에 부착되어 사용된다.
///
/// 【참조하는 곳】
///   - Problem1~10의 Step3 Logic (Director_ProblemN_Step3_Logic) : SetKeywords()로 정답 키워드 설정,
///     OnKeywordMatched/OnNoMatch 이벤트 구독하여 매칭 결과 처리
///   - Problem2~3 Step2/Step3 Logic : 사실/생각 판정 등에서 사용
///   - Problem1~10의 Step3 Binder : ToggleRecording() 호출 (버튼 onClick)
///   - STTButton : STT 버튼 래퍼에서 MicRecordingIndicator 참조
///
/// 【참조되는 곳】
///   - STTManager     : 녹음 시작/중지, GetCurrentVolume(), OnPartialResult/OnFinalResult 이벤트
///   - KeywordMatcher : CalculateSimilarity()로 키워드 매칭
///   - ButtonHover    : 같은 GameObject의 ButtonHover 컴포넌트 (스프라이트 교체 위임)
///
/// 【흐름】
///   1. Step Logic에서 SetKeywords(["사실", "생각"]) 호출
///   2. 사용자가 마이크 버튼 클릭 → ToggleRecording()
///   3. 녹음 시작:
///      - STTManager.SetPromptHint(keywords)로 Whisper 힌트 설정
///      - STTManager.StartRecording()으로 마이크 녹음 시작
///      - CheckSilence() 코루틴으로 무음 감지 시작
///      - OnPartialResult 구독 → HandlePartialResult()에서 실시간 키워드 매칭 → 캐시
///   4. 녹음 중지 (사용자 클릭 또는 무음 자동 종료):
///      - 캐시된 실시간 결과가 있으면 즉시 사용 (STT 최종 처리 스킵)
///      - 음성 미감지 시 STT 스킵하고 OnNoMatch 발생
///      - 그 외: STTManager.StopRecording() → HandleSTTResult()에서 최종 매칭
///   5. 매칭 성공 → OnKeywordMatched(index) 이벤트 → Step Logic에서 처리
///   6. 매칭 실패 → OnNoMatch(rawText) 이벤트
///
/// 【3가지 상태와 시각 피드백】
///   - idle(대기):      idleSprite 표시, statusText = idleText
///   - recording(녹음): recordingSprite 표시, pulseRoot 활성화, statusText = "녹음 중..."
///   - recognizing(인식): recognizingSprite 표시, statusText = "인식 중..."
/// </summary>
public class MicRecordingIndicator : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private Image targetImage;              // 스프라이트를 적용할 이미지 (ButtonHover가 없을 때 폴백)
    [SerializeField] private Sprite idleSprite;              // 대기 상태 스프라이트 (마이크 아이콘)
    [SerializeField] private Sprite recordingSprite;         // 녹음 중 스프라이트 (빨간 마이크 아이콘)
    [SerializeField] private Sprite recognizingSprite;       // 인식 중 스프라이트 (로딩 아이콘)

    [Header("펄스 효과")]
    [SerializeField] private GameObject pulseRoot;           // 녹음 중 펄스 효과 루트 (활성/비활성 토글)

    /// <summary>같은 GameObject의 ButtonHover 컴포넌트 (스프라이트 교체를 위임)</summary>
    private ButtonHover buttonHover;

    [Header("텍스트 피드백")]
    [SerializeField] private Text statusText;                // 상태 텍스트 UI
    [SerializeField] private string idleText = "마이크를 눌러주세요";     // 대기 상태 텍스트 (인스펙터 기본값)
    [SerializeField] private string recordingText = "녹음 중...";        // 녹음 중 텍스트
    [SerializeField] private string recognizingText = "인식 중...";      // 인식 중 텍스트

    [Header("자동 종료 설정")]
    [SerializeField] private bool enableAutoStop = true;                 // 무음 감지 기반 자동 녹음 종료 활성화
    [SerializeField] private float silenceDuration = 1.0f;               // 무음이 이 시간(초) 동안 지속되면 자동 종료
    [SerializeField] private float volumeThreshold = 0.005f;             // 이 값 이하 볼륨을 무음으로 판정 (낮게 설정하여 작은 마이크도 감지)
    [SerializeField] private float recognizingMinDuration = 0.5f;        // "인식 중" 상태 최소 표시 시간 (UX를 위해 즉시 결과 전달하지 않음)

    [Header("STT 키워드")]
    [SerializeField] private string[] keywords;              // 매칭 대상 키워드 배열 (각 Step에서 SetKeywords()로 동적 설정)
    [SerializeField] private float matchThreshold = 0.3f;    // 키워드 매칭 최소 유사도 임계값 (0.3 = 30% 이상이면 매칭 성공)

    /// <summary>
    /// 키워드를 동적으로 설정 (문제별로 다른 키워드 사용 시)
    /// </summary>
    public void SetKeywords(string[] newKeywords)
    {
        keywords = newKeywords;
    }

    // ===== 이벤트 (Step Logic에서 구독) =====

    /// <summary>키워드 매칭 성공 시 발생. 매칭된 키워드의 인덱스(keywords 배열 기준)를 전달한다.</summary>
    public event Action<int> OnKeywordMatched;
    /// <summary>매칭 실패 시 발생. STT 인식 원본 텍스트를 전달한다 (빈 문자열일 수 있음).</summary>
    public event Action<string> OnNoMatch;
    /// <summary>녹음 상태 변경 시 발생. true=녹음 시작, false=녹음 종료.</summary>
    public event Action<bool> OnRecordingChanged;

    // ===== 내부 상태 =====
    private bool _recording;                   // UI 표시용 녹음 상태 (녹음 + 인식 중 모두 포함)
    private bool _isSTTRecording;              // 실제 STTManager 녹음 진행 중 여부
    private bool _recognizing;                 // "인식 중" 상태 (녹음 종료 ~ 결과 수신 사이)

    // ===== 실시간 매칭 캐시 =====
    // 녹음 도중 OnPartialResult에서 키워드 매칭을 수행하고, 가장 좋은 결과를 캐시한다.
    // 녹음 종료 시 캐시된 결과가 있으면 최종 Whisper 추론을 스킵하고 캐시 결과를 즉시 사용한다.
    // → 응답 시간 대폭 단축 (사용자가 말을 마치자마자 바로 결과 전달)
    private int _cachedMatchIndex = -1;        // 캐시된 최고 매칭 키워드 인덱스 (-1 = 캐시 없음)
    private float _cachedMatchScore = 0f;      // 캐시된 최고 매칭 유사도 점수

    // ===== 자동 종료 관련 =====
    private Coroutine _silenceCheckCoroutine;  // CheckSilence() 코루틴 참조 (중지용)
    private float _silenceTimer = 0f;          // 무음 지속 시간 누적 카운터 (초)
    private bool _hasDetectedVoice = false;    // 녹음 시작 이후 의미있는 음성이 한 번이라도 감지되었는지

    // ===== 인식 중 딜레이 =====
    private Coroutine _recognizingDelayCoroutine;  // DelayedResult() 코루틴 참조 (중지용)

    // ===== 대기 텍스트 =====
    // idleText(SerializeField)는 인스펙터 기본값으로 런타임에 절대 수정하지 않음
    // 런타임에 동적으로 변경되는 표시용 텍스트는 _displayIdleText 사용
    private string _displayIdleText;

    /// <summary>
    /// 아이들 텍스트를 동적으로 변경 (재시도 시 "다시 말하기" 등)
    /// </summary>
    public void SetIdleText(string text)
    {
        _displayIdleText = text;
        if (!_recording && statusText != null)
            statusText.text = text;
    }

    /// <summary>
    /// 아이들 텍스트를 원래 값(인스펙터 값)으로 복원
    /// </summary>
    public void ResetIdleText()
    {
        _displayIdleText = idleText;
        if (!_recording && statusText != null)
            statusText.text = idleText;
    }

    /// <summary>
    /// ButtonHover 컴포넌트 자동 참조 (같은 GameObject에서)
    /// </summary>
    private void Awake()
    {
        buttonHover = GetComponent<ButtonHover>();
    }

    /// <summary>
    /// 스텝 활성화 시 모든 상태를 초기화하고 대기 상태 비주얼을 적용한다.
    /// ※ 프로젝트 규칙: Awake 대신 OnEnable에서 초기화 (스텝 활성화 순서 문제)
    /// </summary>
    private void OnEnable()
    {
        _displayIdleText = idleText;    // 인스펙터 기본값으로 초기화
        _recording = false;
        _isSTTRecording = false;
        _recognizing = false;
        ApplyVisual();
    }

    /// <summary>
    /// 녹음 시작/중지 토글 (마이크 버튼 onClick에서 호출)
    ///
    /// 【녹음 시작 시 처리】
    ///   1. 캐시 초기화 + 무음 감지 초기화
    ///   2. SetPromptHint(keywords)로 Whisper에 키워드 힌트 전달
    ///   3. STTManager.StartRecording()으로 마이크 녹음 시작
    ///   4. OnPartialResult 구독 → 실시간 키워드 매칭
    ///   5. CheckSilence() 코루틴 시작 (무음 감지 기반 자동 종료)
    ///
    /// 【녹음 중지 시 3가지 분기】
    ///   A. 캐시된 실시간 매칭 결과가 있으면 → Whisper 최종 인식 스킵, 캐시 결과 즉시 사용
    ///   B. 음성 미감지 + 캐시 없으면 → Whisper 인식 스킵, OnNoMatch 발생
    ///   C. 그 외 → STTManager.StopRecording()으로 최종 Whisper 인식 수행
    /// </summary>
    public void ToggleRecording()
    {
        // TTS 재생 중이면 먼저 중지 (마이크와 충돌 방지)
        if (SoundManager.Instance != null && SoundManager.Instance.IsTTSPlaying) SoundManager.Instance.StopTTS();

        // 현재 STT 녹음 상태의 반대로 토글
        bool startRecording = !_isSTTRecording;

        // 상태 업데이트 및 비주얼 즉시 반영 (사용자에게 즉각적인 피드백)
        _recording = startRecording;
        _isSTTRecording = startRecording;
        OnRecordingChanged?.Invoke(startRecording);
        Debug.Log($"[MicRecordingIndicator] 상태 변경: _recording={_recording}, startRecording={startRecording}");
        ApplyVisual();

        // STT 사용 불가능하면 비주얼만 토글하고 종료 (STTManager 없이도 UI는 동작)
        if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
        {
            return;
        }

        if (startRecording)
        {
            // ===== 녹음 시작 =====

            // 이전 캐시 및 무음 감지 상태 초기화
            _cachedMatchIndex = -1;
            _cachedMatchScore = 0f;
            _silenceTimer = 0f;
            _hasDetectedVoice = false;

            // 키워드를 Whisper의 initial_prompt에 힌트로 설정 (인식률 향상)
            if (keywords != null && keywords.Length > 0)
                STTManager.Instance.SetPromptHint(keywords);

            // 실시간 부분 결과 이벤트 구독 (중복 방지를 위해 먼저 해제 후 등록)
            STTManager.Instance.OnPartialResult -= HandlePartialResult;
            STTManager.Instance.OnPartialResult += HandlePartialResult;

            // 마이크 녹음 시작
            STTManager.Instance.StartRecording();

            // 무음 감지 기반 자동 종료 코루틴 시작
            if (enableAutoStop)
            {
                if (_silenceCheckCoroutine != null)
                    StopCoroutine(_silenceCheckCoroutine);
                _silenceCheckCoroutine = StartCoroutine(CheckSilence());
            }
        }
        else
        {
            // ===== 녹음 중지 =====

            // 실시간 결과 이벤트 구독 해제
            STTManager.Instance.OnPartialResult -= HandlePartialResult;

            // 무음 감지 코루틴 중지
            if (_silenceCheckCoroutine != null)
            {
                StopCoroutine(_silenceCheckCoroutine);
                _silenceCheckCoroutine = null;
            }

            // "인식 중" 비주얼 표시
            SetRecognizing(true);

            // 분기 A: 캐시된 실시간 결과가 있으면 최종 Whisper 인식 스킵하고 즉시 사용
            //   → 응답 시간 대폭 단축 (Whisper 최종 추론 1~3초 절약)
            if (_cachedMatchIndex >= 0)
            {
                Debug.Log($"[MicRecordingIndicator] 캐시된 실시간 결과 사용: [{_cachedMatchIndex}] {keywords[_cachedMatchIndex]} ({_cachedMatchScore:F2})");
                int matchIndex = _cachedMatchIndex;
                _cachedMatchIndex = -1;
                _cachedMatchScore = 0f;
                // skipProcessing=true: Whisper 최종 인식을 수행하지 않음
                STTManager.Instance.StopRecording(skipProcessing: true);
                // "인식 중" 최소 표시 시간 후 결과 전달 (UX 안정성)
                _recognizingDelayCoroutine = StartCoroutine(DelayedResult(() => OnKeywordMatched?.Invoke(matchIndex)));
            }
            // 분기 B: 음성이 한 번도 감지되지 않았고 캐시도 없으면 → STT 스킵
            else if (enableAutoStop && !_hasDetectedVoice)
            {
                Debug.Log("[MicRecordingIndicator] 음성 미감지 → STT 스킵");
                STTManager.Instance.StopRecording(skipProcessing: true);
                _recognizingDelayCoroutine = StartCoroutine(DelayedResult(() => OnNoMatch?.Invoke("")));
            }
            // 분기 C: 캐시 없고 음성은 감지됨 → Whisper 최종 인식 수행
            else
            {
                // 최종 결과 이벤트 구독 (중복 방지를 위해 먼저 해제 후 등록)
                STTManager.Instance.OnFinalResult -= HandleSTTResult;
                STTManager.Instance.OnFinalResult += HandleSTTResult;
                // skipProcessing=false(기본값): 전체 오디오로 최종 Whisper 인식 수행
                STTManager.Instance.StopRecording();
            }
        }
    }

    /// <summary>
    /// 녹음 상태를 직접 설정한다 (외부에서 상태를 강제로 변경할 때 사용).
    /// ToggleRecording()과 달리 STTManager 호출은 수행하지 않고 비주얼만 업데이트한다.
    /// </summary>
    public void SetRecording(bool value)
    {
        _recording = value;
        _isSTTRecording = value;
        ApplyVisual();
    }

    /// <summary>
    /// 실시간 부분 인식 결과 핸들러 - STTManager.OnPartialResult 이벤트에서 호출
    ///
    /// 녹음 도중 주기적으로 Whisper가 인식한 중간 결과를 받아서
    /// keywords 배열의 각 키워드와 유사도를 계산하고,
    /// 가장 높은 유사도의 매칭 결과를 캐시(_cachedMatchIndex, _cachedMatchScore)에 저장한다.
    ///
    /// 이 캐시는 녹음 종료 시 최종 Whisper 인식을 스킵하고 즉시 결과를 반환하는 데 사용된다.
    /// (ToggleRecording()의 분기 A 참조)
    /// </summary>
    private void HandlePartialResult(string result)
    {
        if (string.IsNullOrEmpty(result)) return;

        Debug.Log($"[MicRecordingIndicator] 실시간 결과: {result}");

        if (keywords == null || keywords.Length == 0) return;

        // 모든 키워드와 유사도 비교하여 최고 점수 찾기
        int bestIndex = -1;
        float bestScore = 0f;

        for (int i = 0; i < keywords.Length; i++)
        {
            float score = KeywordMatcher.CalculateSimilarity(result, keywords[i]);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // 임계값 이상이고, 이전 캐시보다 점수가 높으면 캐시 업데이트
        // (여러 번의 실시간 결과 중 가장 좋은 결과만 유지)
        if (bestIndex >= 0 && bestScore >= matchThreshold && bestScore > _cachedMatchScore)
        {
            _cachedMatchIndex = bestIndex;
            _cachedMatchScore = bestScore;
        }
    }

    /// <summary>
    /// Whisper 최종 인식 결과 핸들러 - STTManager.OnFinalResult 이벤트에서 호출
    /// (ToggleRecording() 분기 C에서만 도달)
    ///
    /// 최종 인식 텍스트를 keywords 배열과 매칭하여
    /// matchThreshold 이상이면 OnKeywordMatched, 미만이면 OnNoMatch 이벤트를 발생시킨다.
    /// </summary>
    private void HandleSTTResult(string result)
    {
        // 이벤트 구독 해제 (일회성 처리)
        STTManager.Instance.OnFinalResult -= HandleSTTResult;
        SetRecognizing(false);

        // 빈 결과 (무음 또는 인식 실패)
        if (string.IsNullOrEmpty(result))
        {
            OnNoMatch?.Invoke("");
            return;
        }

        Debug.Log($"[MicRecordingIndicator] STT 최종 결과: {result}");

        // 키워드가 설정되지 않았으면 매칭 불가
        if (keywords == null || keywords.Length == 0)
        {
            OnNoMatch?.Invoke(result);
            return;
        }

        // 모든 키워드와 유사도 비교하여 최고 점수 찾기
        int bestIndex = -1;
        float bestScore = 0f;

        for (int i = 0; i < keywords.Length; i++)
        {
            float score = KeywordMatcher.CalculateSimilarity(result, keywords[i]);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // 임계값 이상이면 매칭 성공 → OnKeywordMatched 이벤트 (인덱스 전달)
        if (bestIndex >= 0 && bestScore >= matchThreshold)
        {
            Debug.Log($"[MicRecordingIndicator] 매칭 성공: [{bestIndex}] {keywords[bestIndex]} (점수:{bestScore:F2})");
            OnKeywordMatched?.Invoke(bestIndex);
        }
        // 임계값 미만이면 매칭 실패 → OnNoMatch 이벤트 (원본 텍스트 전달)
        else
        {
            Debug.Log($"[MicRecordingIndicator] 매칭 실패 (STT결과: {result}, 최고점수: {bestScore:F2})");
            OnNoMatch?.Invoke(result);
        }
    }

    /// <summary>
    /// 무음 감지 코루틴 - 0.1초 간격으로 마이크 볼륨을 확인하여 자동 녹음 종료
    ///
    /// 동작 원리:
    ///   1. STTManager.GetCurrentVolume()으로 현재 마이크 볼륨 확인
    ///   2. volumeThreshold 초과이면 → 음성 감지 → 타이머 리셋 + _hasDetectedVoice = true
    ///   3. volumeThreshold 이하이면 → 무음 → 타이머 증가 (단, 음성이 한 번이라도 감지된 후에만)
    ///   4. 무음이 silenceDuration 이상 지속되면 → ToggleRecording()으로 자동 녹음 종료
    ///
    /// ※ _hasDetectedVoice가 false인 동안(한 번도 음성 감지 안 됨)에는 타이머가 증가하지 않음
    ///   → 버튼 누르자마자 바로 자동 종료되는 것을 방지
    /// </summary>
    private System.Collections.IEnumerator CheckSilence()
    {
        while (_isSTTRecording)
        {
            yield return new WaitForSeconds(0.1f);

            // STTManager에서 현재 마이크 볼륨 조회 (직접 마이크 접근하지 않음 → 충돌 방지)
            float averageVolume = STTManager.Instance.GetCurrentVolume();

            if (averageVolume > volumeThreshold)
            {
                // 음성 감지됨 → 무음 타이머 리셋
                _silenceTimer = 0f;
                _hasDetectedVoice = true;  // 의미있는 음성이 한 번이라도 감지됨
            }
            else
            {
                // 무음 감지 → 타이머 증가 (단, 음성이 한 번이라도 감지된 경우에만)
                if (_hasDetectedVoice)
                {
                    _silenceTimer += 0.1f;

                    // 무음 지속 시간 초과 시 자동 녹음 종료
                    if (_silenceTimer >= silenceDuration)
                    {
                        Debug.Log($"[MicRecordingIndicator] {silenceDuration}초 동안 음성 없음 - 자동 종료");
                        ToggleRecording();  // 녹음 종료 (ToggleRecording 내부에서 결과 처리)
                        yield break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 비활성화 시 모든 코루틴, 이벤트 구독, 상태를 정리한다.
    /// 스텝 비활성화 시 녹음 중이면 STTManager도 중지시킨다.
    /// </summary>
    private void OnDisable()
    {
        // 진행 중인 코루틴 정리
        if (_silenceCheckCoroutine != null)
        {
            StopCoroutine(_silenceCheckCoroutine);
            _silenceCheckCoroutine = null;
        }
        if (_recognizingDelayCoroutine != null)
        {
            StopCoroutine(_recognizingDelayCoroutine);
            _recognizingDelayCoroutine = null;
        }

        // STTManager 이벤트 구독 해제 및 녹음 중지
        if (STTManager.Instance != null)
        {
            if (_isSTTRecording)
            {
                STTManager.Instance.StopRecording();
            }
            STTManager.Instance.OnFinalResult -= HandleSTTResult;
            STTManager.Instance.OnPartialResult -= HandlePartialResult;
        }

        // 모든 상태 초기화
        _isSTTRecording = false;
        _recording = false;
        _recognizing = false;
        _cachedMatchIndex = -1;
        _cachedMatchScore = 0f;
    }

    /// <summary>
    /// "인식 중" 최소 표시 시간 후 결과 전달
    /// </summary>
    private System.Collections.IEnumerator DelayedResult(Action callback)
    {
        yield return new WaitForSeconds(recognizingMinDuration);
        _recognizingDelayCoroutine = null;
        SetRecognizing(false);
        callback?.Invoke();
    }

    /// <summary>
    /// "인식 중" 상태를 설정하고 비주얼을 업데이트한다.
    /// 인식 종료(value=false) 시 _recording도 함께 해제하여 상태 일관성을 보장한다.
    /// </summary>
    private void SetRecognizing(bool value)
    {
        _recognizing = value;
        if (!value) _recording = false;  // 인식 종료 시 녹음 상태도 확실히 해제
        ApplyVisual();
    }

    /// <summary>
    /// 현재 상태(_recording, _recognizing)에 따라 UI 비주얼을 일괄 업데이트한다.
    ///
    /// 스프라이트 우선순위:
    ///   1. _recording=true  → recordingSprite (녹음 중)
    ///   2. _recognizing=true → recognizingSprite (인식 중)
    ///   3. 둘 다 false      → idleSprite (대기)
    ///
    /// ButtonHover가 있으면 SetSpriteOverride()를 통해 스프라이트를 교체하고,
    /// 없으면 targetImage.sprite를 직접 변경한다.
    /// </summary>
    private void ApplyVisual()
    {
        // 스프라이트 교체 (ButtonHover 우선, 없으면 targetImage 직접 변경)
        if (buttonHover != null)
        {
            if (_recording && recordingSprite != null)
                buttonHover.SetSpriteOverride(recordingSprite);
            else if (_recognizing && recognizingSprite != null)
                buttonHover.SetSpriteOverride(recognizingSprite);
            else if (idleSprite != null)
                buttonHover.SetSpriteOverride(idleSprite);
            else
                buttonHover.ClearSpriteOverride();
        }
        else if (targetImage != null)
        {
            Sprite sprite = _recording ? recordingSprite
                          : _recognizing ? recognizingSprite
                          : idleSprite;
            if (sprite != null)
                targetImage.sprite = sprite;
        }

        // 상태 텍스트 업데이트
        if (statusText != null)
        {
            if (_recording)
                statusText.text = recordingText;
            else if (_recognizing)
                statusText.text = recognizingText;
            else
                statusText.text = _displayIdleText;    // 런타임 동적 텍스트 사용
        }

        // 펄스 효과 활성/비활성 (녹음 중에만 활성)
        if (pulseRoot != null)
            pulseRoot.SetActive(_recording);
    }
}
