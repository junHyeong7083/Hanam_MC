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
    [SerializeField] private string[] keywords;  // 각 키워드 (인덱스로 구분)
    [SerializeField] private float matchThreshold = 0.3f;

    /// <summary>키워드 매칭 시 발생 (매칭된 키워드의 인덱스)</summary>
    public event Action<int> OnKeywordMatched;
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
