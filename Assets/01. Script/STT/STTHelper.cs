using System;
using System.Collections;
using UnityEngine;

namespace STT
{
    /// <summary>
    /// 기존 Problem 로직에서 STT를 쉽게 사용할 수 있는 헬퍼
    ///
    /// 사용법:
    /// 1. 기존 VoiceFlow() 코루틴에서 STTHelper.RecognizeSpeech() 호출
    /// 2. 콜백으로 결과 받아서 처리
    ///
    /// 예시:
    /// private IEnumerator VoiceFlow()
    /// {
    ///     yield return STTHelper.RecognizeSpeech(
    ///         maxDuration: 5f,
    ///         onResult: (text) => {
    ///             // 인식 결과 처리
    ///             if (KeywordMatcher.ContainsKeyword(text, "사실"))
    ///                 OnSelectFact();
    ///         },
    ///         onPartial: (text) => {
    ///             // 실시간 표시 (선택)
    ///         }
    ///     );
    /// }
    /// </summary>
    public static class STTHelper
    {
        /// <summary>
        /// 음성 인식 수행 (코루틴용)
        /// </summary>
        /// <param name="maxDuration">최대 녹음 시간 (초)</param>
        /// <param name="onResult">최종 결과 콜백</param>
        /// <param name="onPartial">중간 결과 콜백 (선택)</param>
        /// <param name="keywords">인식할 키워드 제한 (선택, 빈 배열이면 자유 인식)</param>
        public static IEnumerator RecognizeSpeech(
            float maxDuration,
            Action<string> onResult,
            Action<string> onPartial = null,
            string[] keywords = null)
        {
            // STTManager 체크
            if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[STTHelper] STTManager가 초기화되지 않음 - 빈 결과 반환");
                onResult?.Invoke("");
                yield break;
            }

            string finalResult = "";
            bool isComplete = false;

            // 이벤트 핸들러
            void HandlePartial(string text)
            {
                onPartial?.Invoke(text);
            }

            void HandleFinal(string text)
            {
                finalResult = text;
                isComplete = true;
            }

            // 키워드 설정
            if (keywords != null && keywords.Length > 0)
            {
                STTManager.Instance.SetGrammar(keywords);
            }

            // 이벤트 구독
            STTManager.Instance.OnPartialResult += HandlePartial;
            STTManager.Instance.OnFinalResult += HandleFinal;

            // 녹음 시작
            STTManager.Instance.StartRecording();

            // 타임아웃 대기
            float elapsed = 0f;
            while (!isComplete && elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 녹음 중지 (아직 안 끝났으면)
            if (!isComplete && STTManager.Instance.IsRecording)
            {
                STTManager.Instance.StopRecording();

                // 결과 대기 (최대 0.5초)
                float waitTime = 0f;
                while (!isComplete && waitTime < 0.5f)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }
            }

            // 이벤트 구독 해제
            STTManager.Instance.OnPartialResult -= HandlePartial;
            STTManager.Instance.OnFinalResult -= HandleFinal;

            // 키워드 제한 해제
            if (keywords != null && keywords.Length > 0)
            {
                STTManager.Instance.ClearGrammar();
            }

            // 결과 반환
            onResult?.Invoke(finalResult);
        }

        /// <summary>
        /// 간단 버전: 결과만 받기
        /// </summary>
        public static IEnumerator RecognizeSpeech(float maxDuration, Action<string> onResult)
        {
            yield return RecognizeSpeech(maxDuration, onResult, null, null);
        }

        /// <summary>
        /// STT가 사용 가능한지 확인
        /// </summary>
        public static bool IsAvailable()
        {
            return STTManager.Instance != null && STTManager.Instance.IsInitialized;
        }

        /// <summary>
        /// STT 초기화 대기 (씬 시작 시 사용)
        /// </summary>
        public static IEnumerator WaitForInitialization(float timeout = 10f)
        {
            float elapsed = 0f;

            while (!IsAvailable() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!IsAvailable())
            {
                Debug.LogWarning($"[STTHelper] STT 초기화 타임아웃 ({timeout}초)");
            }
        }
    }
}
