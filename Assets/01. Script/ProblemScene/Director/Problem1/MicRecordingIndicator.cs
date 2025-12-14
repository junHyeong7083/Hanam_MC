using System;
using UnityEngine;
using UnityEngine.UI;
using STT;

/// <summary>
/// 마이크 버튼 + STT 통합 컴포넌트
/// - 녹음 시각 피드백 (색상 변경, 펄스)
/// - STT 녹음/인식
/// - 키워드 매칭 후 이벤트 발생
/// </summary>
public class MicRecordingIndicator : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color idleColor = new Color(0.49f, 0.35f, 0.27f);
    [SerializeField] private Color recordingColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private float pulseAmplitude = 0.05f;
    [SerializeField] private float pulseSpeed = 3f;

    [Header("STT 키워드")]
    [SerializeField] private string[] keywordsA = { "생각", "의견", "느낌", "추측" };
    [SerializeField] private string[] keywordsB = { "사실", "팩트", "실제", "진짜" };

    /// <summary>키워드A 매칭 시 발생 (예: "생각")</summary>
    public event Action OnKeywordAMatched;
    /// <summary>키워드B 매칭 시 발생 (예: "사실")</summary>
    public event Action OnKeywordBMatched;
    /// <summary>매칭 실패 시 발생</summary>
    public event Action<string> OnNoMatch;

    private bool _recording;
    private Vector3 _baseScale;
    private bool _isSTTRecording;

    private void Awake()
    {
        _baseScale = transform.localScale;
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        ApplyColor();
    }

    private void Update()
    {
        if (!_recording)
        {
            // ���� ũ��� õõ�� ����
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 10f * Time.deltaTime);
            return;
        }

        // ���� ���� �� �޽�
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        transform.localScale = _baseScale * s;
    }

    public void ToggleRecording()
    {
        if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[MicRecordingIndicator] STTManager가 초기화되지 않았습니다");
            return;
        }

        if (_isSTTRecording)
        {
            // 녹음 중지 및 인식 시작
            _isSTTRecording = false;
            _recording = false;
            STTManager.Instance.OnFinalResult -= HandleSTTResult;
            STTManager.Instance.OnFinalResult += HandleSTTResult;
            STTManager.Instance.StopRecording();
        }
        else
        {
            // 녹음 시작
            _isSTTRecording = true;
            _recording = true;
            STTManager.Instance.StartRecording();
        }

        ApplyColor();
    }

    public void SetRecording(bool value)
    {
        _recording = value;
        _isSTTRecording = value;
        ApplyColor();
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

        // 키워드 매칭
        var (keywordA, scoreA) = KeywordMatcher.FindBestMatch(result, keywordsA);
        var (keywordB, scoreB) = KeywordMatcher.FindBestMatch(result, keywordsB);

        Debug.Log($"[MicRecordingIndicator] A점수: {scoreA:F2} ({keywordA}), B점수: {scoreB:F2} ({keywordB})");

        // 더 높은 점수를 가진 쪽으로 분류
        if (scoreA > scoreB && scoreA >= 0.3f)
        {
            Debug.Log($"[MicRecordingIndicator] → 키워드A 매칭: {keywordA}");
            OnKeywordAMatched?.Invoke();
        }
        else if (scoreB >= 0.3f)
        {
            Debug.Log($"[MicRecordingIndicator] → 키워드B 매칭: {keywordB}");
            OnKeywordBMatched?.Invoke();
        }
        else
        {
            Debug.Log($"[MicRecordingIndicator] 매칭 실패 (A: {scoreA:F2}, B: {scoreB:F2})");
            OnNoMatch?.Invoke(result);
        }
    }

    private void OnDisable()
    {
        // 정리
        if (_isSTTRecording && STTManager.Instance != null)
        {
            STTManager.Instance.StopRecording();
            STTManager.Instance.OnFinalResult -= HandleSTTResult;
        }
        _isSTTRecording = false;
        _recording = false;
    }

    private void ApplyColor()
    {
        if (backgroundImage != null)
            backgroundImage.color = _recording ? recordingColor : idleColor;
    }
}
