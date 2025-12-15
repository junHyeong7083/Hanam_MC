using System;
using System.Collections.Generic;
using UnityEngine;

namespace STT
{
    /// <summary>
    /// 음성 인식 결과와 정답 키워드를 매칭하는 유틸리티
    /// </summary>
    public static class KeywordMatcher
    {
        /// <summary>
        /// 텍스트에 키워드가 포함되어 있는지 확인
        /// </summary>
        public static bool ContainsKeyword(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return false;

            return text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 텍스트에 키워드 배열 중 하나라도 포함되어 있는지 확인
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
        /// 텍스트에서 매칭된 키워드 반환
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
        /// 한글은 자모 분해하여 더 정확한 유사도 계산
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

            // 완전 일치
            if (text == keyword) return 1f;

            // 포함 여부
            if (text.Contains(keyword)) return 0.9f;
            if (keyword.Contains(text)) return 0.8f;

            // 단어 단위로 분리해서 키워드와 일치하는 단어가 있는지 확인
            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (word == keyword) return 0.95f;  // 단어 완전 일치
                if (word.Contains(keyword)) return 0.85f;
            }

            // 자모 분해하여 비교 (한글 음성 인식 개선)
            string textJamo = DecomposeToJamo(text);
            string keywordJamo = DecomposeToJamo(keyword);

            int distance = LevenshteinDistance(textJamo, keywordJamo);
            int maxLength = Math.Max(textJamo.Length, keywordJamo.Length);

            return 1f - (float)distance / maxLength;
        }

        /// <summary>
        /// 텍스트 정규화: 소문자 변환, 구두점 제거, 공백 정리
        /// </summary>
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var sb = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                // 한글, 영문, 숫자, 공백만 유지
                if ((c >= 0xAC00 && c <= 0xD7A3) ||  // 한글 음절
                    (c >= 0x3131 && c <= 0x318E) ||  // 한글 자모
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == ' ')
                {
                    sb.Append(c);
                }
                else if (c == '.' || c == ',' || c == '!' || c == '?' || c == '\n' || c == '\r')
                {
                    // 구두점은 공백으로 대체
                    sb.Append(' ');
                }
            }

            // 연속 공백 제거 및 트림
            string result = sb.ToString().ToLower();
            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }

            return result.Trim();
        }

        /// <summary>
        /// 한글을 자모로 분해 (예: "유니티" → "ㅇㅠㄴㅣㅌㅣ")
        /// </summary>
        private static string DecomposeToJamo(string text)
        {
            var result = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                if (c >= 0xAC00 && c <= 0xD7A3) // 한글 음절 범위
                {
                    int syllable = c - 0xAC00;
                    int cho = syllable / (21 * 28);
                    int jung = (syllable % (21 * 28)) / 28;
                    int jong = syllable % 28;

                    result.Append(CHO[cho]);
                    result.Append(JUNG[jung]);
                    if (jong > 0)
                        result.Append(JONG[jong]);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        // 초성 (19개)
        private static readonly char[] CHO = {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        // 중성 (21개)
        private static readonly char[] JUNG = {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        };

        // 종성 (28개, 0번은 없음)
        private static readonly char[] JONG = {
            '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ',
            'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        /// <summary>
        /// 가장 유사한 키워드 찾기
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
        /// 임계값 이상의 유사도를 가진 키워드가 있는지 확인
        /// </summary>
        public static bool HasSimilarKeyword(string text, float threshold, params string[] keywords)
        {
            var (_, similarity) = FindBestMatch(text, keywords);
            return similarity >= threshold;
        }

        /// <summary>
        /// 옵션 배열에서 STT 텍스트와 가장 일치하는 옵션의 인덱스 반환
        /// </summary>
        /// <param name="sttText">STT 인식 결과</param>
        /// <param name="options">옵션 배열 (각 옵션은 keywords 배열 또는 text를 가짐)</param>
        /// <param name="getKeywords">옵션에서 키워드 배열을 가져오는 함수</param>
        /// <param name="getText">옵션에서 텍스트를 가져오는 함수 (키워드가 없을 때 사용)</param>
        /// <param name="threshold">최소 유사도 임계값 (기본 0.5)</param>
        /// <returns>매칭된 인덱스 (-1이면 매칭 실패)</returns>
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

            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var keywords = getKeywords(option);

                // 키워드가 있으면 키워드로, 없으면 텍스트로 매칭
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

            // 임계값 이상일 때만 반환
            return bestSimilarity >= threshold ? bestIndex : -1;
        }

        /// <summary>
        /// IDirectorProblem2PerspectiveOption 배열에서 STT 텍스트와 가장 일치하는 옵션의 인덱스 반환
        /// </summary>
        public static int FindBestMatchingPerspectiveIndex(
            string sttText,
            IDirectorProblem2PerspectiveOption[] options,
            float threshold = 0.5f)
        {
            return FindBestMatchingOptionIndex(
                sttText,
                options,
                opt => opt.Keywords,
                opt => opt.Text,
                threshold
            );
        }

        /// <summary>
        /// Levenshtein Distance 계산
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
    /// 문제별 정답 키워드 정의
    /// </summary>
    public static class ProblemKeywords
    {
        // Problem1 Step3: 사실/생각 분류
        public static readonly string[] FactKeywords = { "사실", "팩트", "실제", "진짜" };
        public static readonly string[] ThoughtKeywords = { "생각", "의견", "느낌", "추측" };

        // Problem5 Step3: 대사 선택 관련
        public static readonly string[] HealthyResponseKeywords = { "건강", "좋은", "긍정", "올바른" };
        public static readonly string[] AvoidanceKeywords = { "회피", "피하다", "안해", "싫어" };

        /// <summary>
        /// 사실/생각 분류 판정
        /// </summary>
        public static bool IsFactResponse(string text)
        {
            float factScore = KeywordMatcher.FindBestMatch(text, FactKeywords).similarity;
            float thoughtScore = KeywordMatcher.FindBestMatch(text, ThoughtKeywords).similarity;

            return factScore > thoughtScore && factScore >= 0.5f;
        }

        /// <summary>
        /// 생각 응답인지 판정
        /// </summary>
        public static bool IsThoughtResponse(string text)
        {
            float factScore = KeywordMatcher.FindBestMatch(text, FactKeywords).similarity;
            float thoughtScore = KeywordMatcher.FindBestMatch(text, ThoughtKeywords).similarity;

            return thoughtScore > factScore && thoughtScore >= 0.5f;
        }
    }
}
