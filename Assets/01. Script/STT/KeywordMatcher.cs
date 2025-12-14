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
        /// </summary>
        public static float CalculateSimilarity(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return 0f;

            text = text.ToLower().Trim();
            keyword = keyword.ToLower().Trim();

            // 완전 일치
            if (text == keyword) return 1f;

            // 포함 여부
            if (text.Contains(keyword)) return 0.9f;
            if (keyword.Contains(text)) return 0.8f;

            // Levenshtein Distance 기반 유사도
            int distance = LevenshteinDistance(text, keyword);
            int maxLength = Math.Max(text.Length, keyword.Length);

            return 1f - (float)distance / maxLength;
        }

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
