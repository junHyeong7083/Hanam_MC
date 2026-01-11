using System;
using UnityEngine;
using UnityEngine.UI;
using STT;

/// <summary>
/// 마이크 버튼 + STT 통합 컴포넌트
/// - 녹음 시각 피드백 (Sprite 교체)
/// - STT 녹음/인식
/// - 키워드 매칭 후 이벤트 발생
/// </summary>
public class MicRecordingIndicator : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite recordingSprite;
    [SerializeField] private GameObject micPulseImage;

    [Header("텍스트 피드백")]
    [SerializeField] private Text statusText;
    [SerializeField] private string idleText = "마이크를 눌러주세요";
    [SerializeField] private string recordingText = "녹음 중...";

    [Header("자동 종료 설정")]
    [SerializeField] private bool enableAutoStop = true;
    [SerializeField] private float silenceDuration = 3f;  // 무음 지속 시간 (초)
    [SerializeField] private float volumeThreshold = 0.02f;  // 음성 감지 임계값 (높을수록 큰 소리만 인식)

    [Header("STT 키워드")]
    [SerializeField] private string[] keywords;
    [SerializeField] private float matchThreshold = 0.3f;

    /// <summary>
    /// 키워드를 동적으로 설정 (문제별로 다른 키워드 사용 시)
    /// </summary>
    public void SetKeywords(string[] newKeywords)
    {
        keywords = newKeywords;
    }

    /// <summary>키워드 매칭 시 발생 (매칭된 키워드의 인덱스)</summary>
    public event Action<int> OnKeywordMatched;
    /// <summary>매칭 실패 시 발생</summary>
    public event Action<string> OnNoMatch;

    private bool _recording;
    private bool _isSTTRecording;

    // 실시간 매칭 캐시
    private int _cachedMatchIndex = -1;
    private float _cachedMatchScore = 0f;

    // 자동 종료
    private Coroutine _silenceCheckCoroutine;
    private float _silenceTimer = 0f;
    private bool _hasDetectedVoice = false;  // 의미있는 음성이 감지되었는지 여부

    private void OnEnable()
    {
        // 상태 초기화 (혹시 이전 상태가 남아있을 경우 대비)
        _recording = false;
        _isSTTRecording = false;
        ApplyVisual();
    }

    public void ToggleRecording()
    {
       // Debug.Log($"[MicRecordingIndicator] ToggleRecording 호출 - 현재 상태: _recording={_recording}, _isSTTRecording={_isSTTRecording}");

        // 녹음 시작/중지 결정
        bool startRecording = !_isSTTRecording;

        // 상태 업데이트 및 비주얼 즉시 반영
        _recording = startRecording;
        _isSTTRecording = startRecording;
     //   Debug.Log($"[MicRecordingIndicator] 상태 변경 후: _recording={_recording}, startRecording={startRecording}");
        ApplyVisual();

        // STT 사용 불가능하면 비주얼만 토글하고 종료
        if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
        {
          //  Debug.LogWarning("[MicRecordingIndicator] STTManager가 초기화되지 않았습니다");
            return;
        }

        if (startRecording)
        {
            // 녹음 시작
            _cachedMatchIndex = -1;
            _cachedMatchScore = 0f;
            _silenceTimer = 0f;
            _hasDetectedVoice = false;

            STTManager.Instance.OnPartialResult -= HandlePartialResult;
            STTManager.Instance.OnPartialResult += HandlePartialResult;
            STTManager.Instance.StartRecording();

            // 자동 종료 코루틴 시작
            if (enableAutoStop)
            {
                if (_silenceCheckCoroutine != null)
                    StopCoroutine(_silenceCheckCoroutine);
                _silenceCheckCoroutine = StartCoroutine(CheckSilence());
            }
        }
        else
        {
            // 녹음 중지
            STTManager.Instance.OnPartialResult -= HandlePartialResult;

            // 자동 종료 코루틴 중지
            if (_silenceCheckCoroutine != null)
            {
                StopCoroutine(_silenceCheckCoroutine);
                _silenceCheckCoroutine = null;
            }

            // 자동 종료 모드일 때만 음성 감지 체크
            if (enableAutoStop && !_hasDetectedVoice)
            {
               // Debug.Log("[MicRecordingIndicator] 음성이 감지되지 않아 STT를 실행하지 않습니다");
                STTManager.Instance.StopRecording();
                return;
            }

            // 캐시된 실시간 결과가 있으면 즉시 사용
            if (_cachedMatchIndex >= 0)
            {
              //  Debug.Log($"[MicRecordingIndicator] 캐시된 실시간 결과 사용: [{_cachedMatchIndex}] {keywords[_cachedMatchIndex]} ({_cachedMatchScore:F2})");
                int matchIndex = _cachedMatchIndex;
                _cachedMatchIndex = -1;
                _cachedMatchScore = 0f;
                STTManager.Instance.StopRecording();
                OnKeywordMatched?.Invoke(matchIndex);
            }
            else
            {
                // 캐시된 결과가 없으면 최종 결과 대기
                STTManager.Instance.OnFinalResult -= HandleSTTResult;
                STTManager.Instance.OnFinalResult += HandleSTTResult;
                STTManager.Instance.StopRecording();
            }
        }
    }

    public void SetRecording(bool value)
    {
        _recording = value;
        _isSTTRecording = value;
        ApplyVisual();
    }

    /// <summary>
    /// 실시간 부분 결과 처리 - 키워드 매칭하여 캐시
    /// </summary>
    private void HandlePartialResult(string result)
    {
        if (string.IsNullOrEmpty(result)) return;

    //    Debug.Log($"[MicRecordingIndicator] 실시간 결과 수신: {result}");

        if (keywords == null || keywords.Length == 0) return;

        // 각 키워드와 비교해서 가장 높은 점수 찾기
        int bestIndex = -1;
        float bestScore = 0f;

        for (int i = 0; i < keywords.Length; i++)
        {
            float score = KeywordMatcher.CalculateSimilarity(result, keywords[i]);
          //  Debug.Log($"[MicRecordingIndicator] 실시간 [{i}] {keywords[i]}: {score:F2}");

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // 임계값 이상이고, 이전 캐시보다 점수가 높으면 업데이트
        if (bestIndex >= 0 && bestScore >= matchThreshold && bestScore > _cachedMatchScore)
        {
            _cachedMatchIndex = bestIndex;
            _cachedMatchScore = bestScore;
          //  Debug.Log($"[MicRecordingIndicator] 실시간 매칭 캐시 업데이트: [{bestIndex}] {keywords[bestIndex]} ({bestScore:F2})");
        }
    }

    private void HandleSTTResult(string result)
    {
        STTManager.Instance.OnFinalResult -= HandleSTTResult;

        if (string.IsNullOrEmpty(result))
        {
          //  Debug.Log("[MicRecordingIndicator] STT 결과가 비어있습니다");
            OnNoMatch?.Invoke("");
            return;
        }

       // Debug.Log($"[MicRecordingIndicator] STT 인식 결과: {result}");

        if (keywords == null || keywords.Length == 0)
        {
          //  Debug.LogWarning("[MicRecordingIndicator] 키워드가 설정되지 않았습니다");
            OnNoMatch?.Invoke(result);
            return;
        }

        // 각 키워드와 비교해서 가장 높은 점수 찾기
        int bestIndex = -1;
        float bestScore = 0f;

        for (int i = 0; i < keywords.Length; i++)
        {
            float score = KeywordMatcher.CalculateSimilarity(result, keywords[i]);
         //   Debug.Log($"[MicRecordingIndicator] [{i}] {keywords[i]}: {score:F2}");

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // 임계값 이상이면 매칭 성공
        if (bestIndex >= 0 && bestScore >= matchThreshold)
        {
         //   Debug.Log($"[MicRecordingIndicator] → 매칭 성공: [{bestIndex}] {keywords[bestIndex]} ({bestScore:F2})");
            OnKeywordMatched?.Invoke(bestIndex);
        }
        else
        {
           // Debug.Log($"[MicRecordingIndicator] 매칭 실패 (최고 점수: {bestScore:F2})");
            OnNoMatch?.Invoke(result);
        }
    }

    /// <summary>
    /// 무음 감지 코루틴 - 일정 시간 동안 음성이 없으면 자동 종료
    /// </summary>
    private System.Collections.IEnumerator CheckSilence()
    {
        while (_isSTTRecording)
        {
            yield return new WaitForSeconds(0.1f);

            // STTManager의 현재 볼륨 가져오기 (마이크 충돌 방지)
            float averageVolume = STTManager.Instance.GetCurrentVolume();

            // 음성 감지 여부
            if (averageVolume > volumeThreshold)
            {
                // 음성 감지됨 - 타이머 리셋
                _silenceTimer = 0f;
                _hasDetectedVoice = true;  // 의미있는 음성 감지
            }
            else
            {
                // 무음 - 타이머 증가 (단, 음성이 한 번이라도 감지된 경우에만)
                if (_hasDetectedVoice)
                {
                    _silenceTimer += 0.1f;

                    // 무음 지속 시간 초과 시 자동 종료
                    if (_silenceTimer >= silenceDuration)
                    {
                        Debug.Log($"[MicRecordingIndicator] {silenceDuration}초 동안 음성 없음 - 자동 종료");
                        ToggleRecording();  // 녹음 종료
                        yield break;
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        // 코루틴 정리
        if (_silenceCheckCoroutine != null)
        {
            StopCoroutine(_silenceCheckCoroutine);
            _silenceCheckCoroutine = null;
        }

        // 정리
        if (STTManager.Instance != null)
        {
            if (_isSTTRecording)
            {
                STTManager.Instance.StopRecording();
            }
            STTManager.Instance.OnFinalResult -= HandleSTTResult;
            STTManager.Instance.OnPartialResult -= HandlePartialResult;
        }
        _isSTTRecording = false;
        _recording = false;
        _cachedMatchIndex = -1;
        _cachedMatchScore = 0f;
    }

    private void ApplyVisual()
    {
        if (targetImage != null)
        {
            targetImage.sprite = _recording ? recordingSprite : idleSprite;
        }

        if (micPulseImage != null)
        {
            micPulseImage.SetActive(_recording);
        }

        if (statusText != null)
        {
            statusText.text = _recording ? recordingText : idleText;
        }
    }
}
