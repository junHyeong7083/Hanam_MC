using System;
using System.Collections;
using UnityEngine;

namespace STT
{
    /// <summary>
    /// STTHelper - Problem 스텝 로직에서 STT를 코루틴으로 쉽게 사용할 수 있는 정적 헬퍼 클래스
    ///
    /// 【역할】
    ///   STTManager의 녹음/인식 과정을 코루틴(yield return)으로 감싸서,
    ///   Problem 로직 코드에서 한 줄로 음성 인식을 수행할 수 있게 한다.
    ///   녹음 시작 → 타임아웃 대기 → 녹음 중지 → 결과 콜백까지 자동 처리.
    ///
    /// 【참조하는 곳】
    ///   - 각 Problem Step Logic : VoiceFlow() 등의 코루틴에서 RecognizeSpeech() 사용
    ///
    /// 【참조되는 곳】
    ///   - STTManager : 녹음/인식/이벤트 구독
    ///
    /// 【흐름】
    ///   1. RecognizeSpeech() 호출
    ///   2. STTManager.Instance 존재 확인
    ///   3. SetGrammar()로 키워드 설정 (선택)
    ///   4. OnPartialResult / OnFinalResult 이벤트 구독
    ///   5. StartRecording() → maxDuration 동안 대기
    ///   6. StopRecording() → 결과 대기 (최대 0.5초 추가)
    ///   7. 이벤트 구독 해제 + ClearGrammar()
    ///   8. onResult 콜백으로 결과 전달
    ///
    /// 사용법:
    /// private IEnumerator VoiceFlow()
    /// {
    ///     yield return STTHelper.RecognizeSpeech(
    ///         maxDuration: 5f,
    ///         onResult: (text) => {
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
        /// 음성 인식 수행 (코루틴 — yield return으로 사용)
        /// 녹음 시작 → maxDuration 대기 → 녹음 중지 → Whisper 인식 → 콜백 호출까지
        /// 전체 과정을 자동으로 처리한다.
        /// </summary>
        /// <param name="maxDuration">최대 녹음 시간 (초). 이 시간이 지나면 자동으로 녹음 중지</param>
        /// <param name="onResult">최종 인식 결과 콜백 (인식 완료 또는 타임아웃 후 호출됨)</param>
        /// <param name="onPartial">중간(실시간) 결과 콜백 (선택, null이면 무시)</param>
        /// <param name="keywords">인식 키워드 제한 배열 (선택, Vosk 호환 — Whisper에서는 실제로 제한되지 않음)</param>
        public static IEnumerator RecognizeSpeech(
            float maxDuration,
            Action<string> onResult,
            Action<string> onPartial = null,
            string[] keywords = null)
        {
            // STTManager 인스턴스 및 초기화 상태 확인
            if (STTManager.Instance == null || !STTManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[STTHelper] STTManager가 초기화되지 않음 - 빈 결과 반환");
                onResult?.Invoke("");
                yield break;
            }

            string finalResult = "";
            bool isComplete = false;

            // 로컬 함수로 이벤트 핸들러 정의 (구독 해제 시 동일 참조 필요)
            void HandlePartial(string text)
            {
                onPartial?.Invoke(text);
            }

            void HandleFinal(string text)
            {
                finalResult = text;
                isComplete = true;
            }

            // 키워드 설정 (Vosk 호환 — Whisper에서는 SetGrammar가 무시됨)
            if (keywords != null && keywords.Length > 0)
            {
                STTManager.Instance.SetGrammar(keywords);
            }

            // STTManager 이벤트 구독
            STTManager.Instance.OnPartialResult += HandlePartial;
            STTManager.Instance.OnFinalResult += HandleFinal;

            // 녹음 시작
            STTManager.Instance.StartRecording();

            // maxDuration 동안 대기 (이 동안 실시간 결과가 HandlePartial로 전달됨)
            float elapsed = 0f;
            while (!isComplete && elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 타임아웃 시 아직 녹음 중이면 수동 중지
            if (!isComplete && STTManager.Instance.IsRecording)
            {
                STTManager.Instance.StopRecording();

                // StopRecording() 후 Whisper 추론 결과가 올 때까지 최대 0.5초 추가 대기
                float waitTime = 0f;
                while (!isComplete && waitTime < 0.5f)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }
            }

            // 이벤트 구독 해제 (메모리 누수 방지)
            STTManager.Instance.OnPartialResult -= HandlePartial;
            STTManager.Instance.OnFinalResult -= HandleFinal;

            // 키워드 제한 해제
            if (keywords != null && keywords.Length > 0)
            {
                STTManager.Instance.ClearGrammar();
            }

            // 최종 결과를 콜백으로 전달
            onResult?.Invoke(finalResult);
        }

        /// <summary>
        /// 간단 버전: 최종 결과만 콜백으로 받는다 (실시간 결과, 키워드 제한 없음).
        /// </summary>
        public static IEnumerator RecognizeSpeech(float maxDuration, Action<string> onResult)
        {
            yield return RecognizeSpeech(maxDuration, onResult, null, null);
        }

        /// <summary>
        /// STT가 현재 사용 가능한 상태인지 확인한다.
        /// STTManager 인스턴스가 존재하고 Whisper 모델이 로드 완료된 경우 true.
        /// </summary>
        public static bool IsAvailable()
        {
            return STTManager.Instance != null && STTManager.Instance.IsInitialized;
        }

        /// <summary>
        /// STT 초기화 완료까지 대기하는 코루틴 (씬 시작 시 사용).
        /// Whisper 모델 로드는 수초 소요되므로, STT 기능을 사용하기 전에
        /// 이 코루틴으로 초기화 완료를 보장할 수 있다.
        /// </summary>
        /// <param name="timeout">최대 대기 시간 (초). 초과 시 경고 로그 출력</param>
        public static IEnumerator WaitForInitialization(float timeout = 10f)
        {
            float elapsed = 0f;

            // IsAvailable()이 true가 될 때까지 매 프레임 대기
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
