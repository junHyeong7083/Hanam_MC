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

    private void OnEnable()
    {
        // 상태 초기화 (혹시 이전 상태가 남아있을 경우 대비)
        _recording = false;
        _isSTTRecording = false;
        ApplyVisual();
    }

    public void ToggleRecording()
    {
        Debug.Log($"[MicRecordingIndicator] ToggleRecording 호출 - 현재 상태: _recording={_recording}, _isSTTRecording={_isSTTRecording}");

        // 녹음 시작/중지 결정
        bool startRecording = !_isSTTRecording;

        // 상태 업데이트 및 비주얼 즉시 반영
        _recording = startRecording;
        _isSTTRecording = startRecording;
        Debug.Log($"[MicRecordingIndicator] 상태 변경 후: _recording={_recording}, startRecording={startRecording}");
        ApplyVisual();

        // STT 사용 불가능하면 비주얼만 토글하고 종료
        if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[MicRecordingIndicator] STTManager가 초기화되지 않았습니다");
            return;
        }

        if (startRecording)
        {
            // 녹음 시작
            _cachedMatchIndex = -1;
            _cachedMatchScore = 0f;

            STTManager.Instance.OnPartialResult -= HandlePartialResult;
            STTManager.Instance.OnPartialResult += HandlePartialResult;
            STTManager.Instance.StartRecording();
        }
        else
        {
            // 녹음 중지
            STTManager.Instance.OnPartialResult -= HandlePartialResult;

            // 캐시된 실시간 결과가 있으면 즉시 사용
            if (_cachedMatchIndex >= 0)
            {
                Debug.Log($"[MicRecordingIndicator] 캐시된 실시간 결과 사용: [{_cachedMatchIndex}] {keywords[_cachedMatchIndex]} ({_cachedMatchScore:F2})");
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

        Debug.Log($"[MicRecordingIndicator] 실시간 결과 수신: {result}");

        if (keywords == null || keywords.Length == 0) return;

        // 각 키워드와 비교해서 가장 높은 점수 찾기
        int bestIndex = -1;
        float bestScore = 0f;

        for (int i = 0; i < keywords.Length; i++)
        {
            float score = KeywordMatcher.CalculateSimilarity(result, keywords[i]);
            Debug.Log($"[MicRecordingIndicator] 실시간 [{i}] {keywords[i]}: {score:F2}");

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
            Debug.Log($"[MicRecordingIndicator] 실시간 매칭 캐시 업데이트: [{bestIndex}] {keywords[bestIndex]} ({bestScore:F2})");
        }
    }

    private void HandleSTTResult(string result)
    {
        STTManager.Instance.OnFinalResult -= HandleSTTResult;

        if (string.IsNullOrEmpty(result))
        {
            Debug.Log("[MicRecordingIndicator] STT 결과가 비어있습니다");
            OnNoMatch?.Invoke("");
            return;
        }

        Debug.Log($"[MicRecordingIndicator] STT 인식 결과: {result}");

        if (keywords == null || keywords.Length == 0)
        {
            Debug.LogWarning("[MicRecordingIndicator] 키워드가 설정되지 않았습니다");
            OnNoMatch?.Invoke(result);
            return;
        }

        // 각 키워드와 비교해서 가장 높은 점수 찾기
        int bestIndex = -1;
        float bestScore = 0f;

        for (int i = 0; i < keywords.Length; i++)
        {
            float score = KeywordMatcher.CalculateSimilarity(result, keywords[i]);
            Debug.Log($"[MicRecordingIndicator] [{i}] {keywords[i]}: {score:F2}");

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // 임계값 이상이면 매칭 성공
        if (bestIndex >= 0 && bestScore >= matchThreshold)
        {
            Debug.Log($"[MicRecordingIndicator] → 매칭 성공: [{bestIndex}] {keywords[bestIndex]} ({bestScore:F2})");
            OnKeywordMatched?.Invoke(bestIndex);
        }
        else
        {
            Debug.Log($"[MicRecordingIndicator] 매칭 실패 (최고 점수: {bestScore:F2})");
            OnNoMatch?.Invoke(result);
        }
    }

    private void OnDisable()
    {
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
    }
}
