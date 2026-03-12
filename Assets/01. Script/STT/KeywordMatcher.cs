using System;
using System.Collections.Generic;
using UnityEngine;

namespace STT
{
    /// <summary>
    /// KeywordMatcher - STT 인식 결과 텍스트와 정답 키워드를 매칭하는 정적 유틸리티 클래스
    ///
    /// 【역할】
    ///   Whisper가 반환한 인식 텍스트를 미리 정의된 키워드와 비교하여
    ///   유사도를 계산하고, 가장 일치하는 키워드를 찾아낸다.
    ///   한글 음성 인식의 부정확성을 보완하기 위해 자모 분해 + Levenshtein 거리 알고리즘을 사용한다.
    ///
    /// 【참조하는 곳】
    ///   - MicRecordingIndicator : HandlePartialResult(), HandleSTTResult()에서 CalculateSimilarity() 호출
    ///   - STTTester             : 테스트 UI에서 FindBestMatch() 호출
    ///   - STTHelper             : 유틸리티 래퍼에서 참조
    ///   ※ ProblemKeywords는 이 파일 내부의 중첩 클래스로, 사실/생각 판정에서 FindBestMatch() 사용
    ///
    /// 【참조되는 곳】 (의존하는 외부 클래스 없음 - 독립적인 유틸리티)
    ///
    /// 【흐름】
    ///   1. STT 인식 결과(text) + 정답 키워드 배열(keywords) 입력
    ///   2. NormalizeText()로 텍스트 정규화 (소문자, 구두점 제거, 공백 정리)
    ///   3. 완전일치 → 포함 → 단어 단위 일치 순으로 빠른 판정 시도
    ///   4. 위 조건에 해당하지 않으면 DecomposeToJamo()로 한글 자모 분해
    ///   5. LevenshteinDistance()로 편집 거리 계산 → 유사도(0.0~1.0) 반환
    /// </summary>
    public static class KeywordMatcher
    {
        /// <summary>
        /// 텍스트에 키워드가 포함되어 있는지 확인 (대소문자 무시)
        /// 가장 단순한 매칭 방법으로, text.Contains(keyword)를 사용한다.
        /// </summary>
        public static bool ContainsKeyword(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return false;

            return text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 텍스트에 키워드 배열 중 하나라도 포함되어 있는지 확인
        /// 순서대로 검사하며, 첫 번째 매칭에서 즉시 true 반환
        /// </summary>
        public static bool ContainsAnyKeyword(string text, params string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (ContainsKeyword(text, keyword))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 텍스트에서 가장 먼저 포함된 키워드를 반환 (없으면 null)
        /// ContainsKeyword()를 순차 적용하여 첫 번째 매칭 키워드를 찾는다.
        /// </summary>
        public static string FindMatchedKeyword(string text, params string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (ContainsKeyword(text, keyword))
                    return keyword;
            }
            return null;
        }

        /// <summary>
        /// 텍스트와 키워드의 유사도 계산 (0.0 ~ 1.0)
        /// 한글 음성 인식의 부정확성을 보완하기 위해 다단계 매칭 전략을 사용한다.
        ///
        /// 매칭 우선순위 (빠른 것부터):
        ///   1.00 = 정규화 후 완전 일치
        ///   0.95 = 단어 단위 완전 일치 (예: "나는 사실이라고 생각해" 안의 "사실")
        ///   0.90 = text가 keyword를 포함
        ///   0.85 = 단어 안에 keyword 포함
        ///   0.80 = keyword가 text를 포함 (짧은 인식 결과)
        ///   ~0.x = 자모 분해 후 Levenshtein 거리 기반 유사도
        /// </summary>
        public static float CalculateSimilarity(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return 0f;

            // 정규화: 소문자 + 구두점 제거 + 공백 정리
            text = NormalizeText(text);
            keyword = NormalizeText(keyword);

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return 0f;

            // 1단계: 완전 일치 (정규화 후)
            if (text == keyword) return 1f;

            // 2단계: 부분 포함 검사
            if (text.Contains(keyword)) return 0.9f;   // text 안에 keyword가 들어있음
            if (keyword.Contains(text)) return 0.8f;    // keyword가 text를 감싸고 있음 (STT가 짧게 인식한 경우)

            // 3단계: 단어 단위로 분리해서 키워드와 일치하는 단어가 있는지 확인
            //   예: text="나는 사실 이라고 생각해", keyword="사실" → 단어 "사실" 완전 일치 = 0.95
            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (word == keyword) return 0.95f;      // 단어 완전 일치
                if (word.Contains(keyword)) return 0.85f; // 단어 안에 키워드 포함
            }

            // 4단계: 한글 자모 분해 후 Levenshtein 편집 거리 기반 유사도
            //   예: "사실" → "ㅅㅏㅅㅣㄹ", "사실" vs "산실" → 자모 레벨에서 거리 계산
            //   Whisper가 "사실"을 "사씰"로 인식해도 자모 레벨에서 유사도가 높게 나옴
            string textJamo = DecomposeToJamo(text);
            string keywordJamo = DecomposeToJamo(keyword);

            int distance = LevenshteinDistance(textJamo, keywordJamo);
            int maxLength = Math.Max(textJamo.Length, keywordJamo.Length);

            // 유사도 = 1 - (편집거리 / 최대길이). 편집거리가 0이면 1.0, 완전히 다르면 0.0
            return 1f - (float)distance / maxLength;
        }

        /// <summary>
        /// 텍스트 정규화: 소문자 변환, 구두점 제거, 공백 정리
        /// Whisper 출력에는 구두점, 특수문자가 포함될 수 있으므로
        /// 비교 전에 통일된 형태로 정규화한다.
        ///
        /// 보존 대상: 한글 음절(가~힣), 한글 자모(ㄱ~ㅎ,ㅏ~ㅣ), 영문, 숫자, 공백
        /// 제거/변환 대상: 구두점 → 공백, 그 외 특수문자 → 제거
        /// </summary>
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var sb = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                // 한글, 영문, 숫자, 공백만 유지
                if ((c >= 0xAC00 && c <= 0xD7A3) ||  // 한글 완성형 음절 (가 ~ 힣)
                    (c >= 0x3131 && c <= 0x318E) ||  // 한글 호환 자모 (ㄱ ~ ㅣ)
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == ' ')
                {
                    sb.Append(c);
                }
                else if (c == '.' || c == ',' || c == '!' || c == '?' || c == '\n' || c == '\r')
                {
                    // 구두점/줄바꿈은 공백으로 대체 (단어 분리 유지)
                    sb.Append(' ');
                }
                // 그 외 문자 (괄호, 따옴표, 특수기호 등)는 무시
            }

            // 소문자 변환 + 연속 공백 제거 + 양쪽 공백 제거
            string result = sb.ToString().ToLower();
            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }

            return result.Trim();
        }

        /// <summary>
        /// 한글 완성형 음절을 초성+중성+종성 자모로 분해한다.
        /// 예: "유니티" → "ㅇㅠㄴㅣㅌㅣ"
        ///     "사실" → "ㅅㅏㅅㅣㄹ"
        ///
        /// 한글 유니코드 분해 공식:
        ///   음절코드 = (c - 0xAC00)
        ///   초성 인덱스 = 음절코드 / (21 * 28)
        ///   중성 인덱스 = (음절코드 % (21 * 28)) / 28
        ///   종성 인덱스 = 음절코드 % 28  (0이면 종성 없음)
        ///
        /// 자모 분해를 통해 "사실" vs "사씰"처럼 발음이 비슷한 글자의
        /// Levenshtein 거리가 글자 단위보다 더 작게 나와서 유사도가 높아진다.
        /// </summary>
        private static string DecomposeToJamo(string text)
        {
            var result = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                if (c >= 0xAC00 && c <= 0xD7A3) // 한글 완성형 음절 범위 (가 ~ 힣)
                {
                    int syllable = c - 0xAC00;
                    int cho = syllable / (21 * 28);        // 초성 인덱스 (0~18)
                    int jung = (syllable % (21 * 28)) / 28; // 중성 인덱스 (0~20)
                    int jong = syllable % 28;               // 종성 인덱스 (0~27, 0=종성없음)

                    result.Append(CHO[cho]);    // 초성 추가
                    result.Append(JUNG[jung]);  // 중성 추가
                    if (jong > 0)
                        result.Append(JONG[jong]); // 종성 추가 (있는 경우만)
                }
                else
                {
                    // 한글이 아닌 문자(영문, 숫자, 공백 등)는 그대로 유지
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        // ===== 한글 자모 테이블 =====
        // 유니코드 한글 완성형 분해에 사용되는 초성/중성/종성 배열

        /// <summary>초성 자모 테이블 (19개, 유니코드 순서)</summary>
        private static readonly char[] CHO = {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        /// <summary>중성 자모 테이블 (21개, 유니코드 순서)</summary>
        private static readonly char[] JUNG = {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        };

        /// <summary>종성 자모 테이블 (28개, 0번은 종성 없음을 의미)</summary>
        private static readonly char[] JONG = {
            '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ',
            'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        /// <summary>
        /// 가장 유사한 키워드를 찾아 (키워드, 유사도) 튜플로 반환한다.
        /// 모든 키워드에 대해 CalculateSimilarity()를 호출하고, 최고 유사도의 키워드를 선택한다.
        /// 매칭되는 것이 없으면 (null, 0f)를 반환한다.
        /// </summary>
        public static (string keyword, float similarity) FindBestMatch(string text, params string[] keywords)
        {
            string bestKeyword = null;
            float bestSimilarity = 0f;

            foreach (var keyword in keywords)
            {
                float similarity = CalculateSimilarity(text, keyword);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestKeyword = keyword;
                }
            }

            return (bestKeyword, bestSimilarity);
        }

        /// <summary>
        /// 임계값(threshold) 이상의 유사도를 가진 키워드가 있는지 확인
        /// FindBestMatch()를 내부 호출하여 최고 유사도가 threshold 이상인지 판정한다.
        /// </summary>
        public static bool HasSimilarKeyword(string text, float threshold, params string[] keywords)
        {
            var (_, similarity) = FindBestMatch(text, keywords);
            return similarity >= threshold;
        }

        /// <summary>
        /// 제네릭 옵션 배열에서 STT 텍스트와 가장 일치하는 옵션의 인덱스를 반환한다.
        /// 각 옵션에서 키워드 배열 또는 텍스트를 추출하는 함수를 인자로 받아
        /// 유연하게 다양한 데이터 구조와 함께 사용할 수 있다.
        ///
        /// 사용 예:
        ///   int idx = FindBestMatchingOptionIndex(sttText, choices,
        ///       c => c.keywords, c => c.displayText, threshold: 0.5f);
        /// </summary>
        /// <param name="sttText">STT 인식 결과 텍스트</param>
        /// <param name="options">옵션 배열 (각 옵션은 keywords 배열 또는 text를 가짐)</param>
        /// <param name="getKeywords">옵션에서 키워드 배열을 가져오는 함수 (null이면 getText 사용)</param>
        /// <param name="getText">옵션에서 텍스트를 가져오는 함수 (키워드가 없을 때 폴백)</param>
        /// <param name="threshold">최소 유사도 임계값 (기본 0.5, 이 값 미만이면 -1 반환)</param>
        /// <returns>매칭된 옵션의 인덱스 (-1이면 매칭 실패)</returns>
        public static int FindBestMatchingOptionIndex<T>(
            string sttText,
            T[] options,
            Func<T, string[]> getKeywords,
            Func<T, string> getText,
            float threshold = 0.5f)
        {
            if (string.IsNullOrEmpty(sttText) || options == null || options.Length == 0)
                return -1;

            int bestIndex = -1;
            float bestSimilarity = 0f;

            // 모든 옵션에 대해 유사도 계산
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var keywords = getKeywords(option);

                // 키워드 배열이 있으면 키워드로, 없으면 텍스트 자체를 매칭 대상으로 사용
                string[] matchTargets = (keywords != null && keywords.Length > 0)
                    ? keywords
                    : new[] { getText(option) };

                var (_, similarity) = FindBestMatch(sttText, matchTargets);

                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestIndex = i;
                }
            }

            // 최고 유사도가 임계값 이상일 때만 인덱스 반환, 미만이면 -1 (매칭 실패)
            return bestSimilarity >= threshold ? bestIndex : -1;
        }


        /// <summary>
        /// Levenshtein Distance (편집 거리) 계산
        /// 두 문자열을 같게 만들기 위해 필요한 최소 편집 연산(삽입/삭제/치환) 횟수를 반환한다.
        /// 동적 프로그래밍(DP) 방식으로 O(n*m) 시간복잡도.
        /// 자모 분해된 한글 문자열에 적용하여 발음 유사도를 측정하는 데 사용된다.
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }

    /// <summary>
    /// ProblemKeywords - 문제별 정답 키워드 정의 및 판정 유틸리티
    ///
    /// 【역할】
    ///   각 Problem/Step에서 사용하는 정답 키워드 배열을 중앙에서 관리하고,
    ///   STT 인식 결과가 어떤 카테고리에 해당하는지 판정하는 헬퍼 메서드를 제공한다.
    ///
    /// 【참조하는 곳】
    ///   - Problem1 Step3 Logic : 사실/생각 분류 시 IsFactResponse(), IsThoughtResponse() 사용
    ///   - Problem5 Step3 Logic : 건강한 반응/회피 반응 판정
    ///
    /// 【참조되는 곳】
    ///   - KeywordMatcher : FindBestMatch()로 유사도 계산
    /// </summary>
    public static class ProblemKeywords
    {
        // ===== Problem1 Step3: 사실 vs 생각 분류 =====
        /// <summary>"사실"에 해당하는 동의어 키워드 배열</summary>
        public static readonly string[] FactKeywords = { "사실", "팩트", "실제", "진짜" };
        /// <summary>"생각"에 해당하는 동의어 키워드 배열</summary>
        public static readonly string[] ThoughtKeywords = { "생각", "의견", "느낌", "추측" };

        // ===== Problem5 Step3: 대사 선택 관련 =====
        /// <summary>건강한/긍정적 반응 키워드</summary>
        public static readonly string[] HealthyResponseKeywords = { "건강", "좋은", "긍정", "올바른" };
        /// <summary>회피 반응 키워드</summary>
        public static readonly string[] AvoidanceKeywords = { "회피", "피하다", "안해", "싫어" };

        /// <summary>
        /// STT 인식 결과가 "사실" 카테고리에 해당하는지 판정
        /// 사실 키워드 유사도가 생각 키워드보다 높고, 임계값(0.5) 이상이면 true
        /// </summary>
        public static bool IsFactResponse(string text)
        {
            float factScore = KeywordMatcher.FindBestMatch(text, FactKeywords).similarity;
            float thoughtScore = KeywordMatcher.FindBestMatch(text, ThoughtKeywords).similarity;

            return factScore > thoughtScore && factScore >= 0.5f;
        }

        /// <summary>
        /// STT 인식 결과가 "생각" 카테고리에 해당하는지 판정
        /// 생각 키워드 유사도가 사실 키워드보다 높고, 임계값(0.5) 이상이면 true
        /// </summary>
        public static bool IsThoughtResponse(string text)
        {
            float factScore = KeywordMatcher.FindBestMatch(text, FactKeywords).similarity;
            float thoughtScore = KeywordMatcher.FindBestMatch(text, ThoughtKeywords).similarity;

            return thoughtScore > factScore && thoughtScore >= 0.5f;
        }
    }
}
