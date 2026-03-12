using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// CsvUtil - CSV 텍스트를 파싱하여 2차원 문자열 배열로 변환하는 유틸리티 클래스
///
/// 【역할】 RFC 4180 호환 CSV 파서. 쌍따옴표(") 이스케이프, 셀 내 줄바꿈, \r\n 등을 정확히 처리한다.
///          Unity TextAsset 또는 raw 문자열을 입력으로 받아 List&lt;string[]&gt; 형태의 행 데이터를 반환한다.
///
/// 【참조하는 곳】 LocalizedTable.Load() — MC_DataTable_v01.csv 파싱,
///                RewardTable.Load() — 보상 테이블 CSV 파싱
/// 【참조되는 곳】 없음 (독립적인 유틸리티 클래스)
///
/// 【흐름】
///   1. Resources.Load&lt;TextAsset&gt;("CSV/MC_DataTable_v01") 등으로 CSV 로드
///   2. CsvUtil.Parse(textAsset) 호출 → List&lt;string[]&gt; 반환
///   3. 반환된 rows[0]이 헤더, rows[1~]이 데이터 행
///   4. CsvHeader.Build(rows[0])로 헤더 매핑 생성 후 데이터 접근
/// </summary>
public static class CsvUtil
{
    /// <summary>
    /// Unity TextAsset에서 CSV를 파싱한다.
    /// </summary>
    /// <param name="csv">CSV 파일을 담은 TextAsset (null이면 빈 리스트 반환)</param>
    /// <returns>각 행이 string[] 인 2차원 리스트. [0]이 헤더 행.</returns>
    public static List<string[]> Parse(TextAsset csv)
    {
        return Parse(csv != null ? csv.text : "");
    }

    /// <summary>
    /// CSV 문자열을 파싱하여 2차원 문자열 배열로 변환한다.
    /// RFC 4180 호환: 쌍따옴표 이스케이프(""), 셀 내 줄바꿈, \r\n 처리를 지원한다.
    /// </summary>
    /// <param name="csvText">CSV 원본 문자열</param>
    /// <returns>각 행이 string[] 인 2차원 리스트</returns>
    public static List<string[]> Parse(string csvText)
    {
        var result = new List<string[]>();
        if (string.IsNullOrEmpty(csvText))
            return result;

        var row = new List<string>();    // 현재 행의 셀들을 임시 저장
        var cell = new StringBuilder();  // 현재 셀 내용을 조립

        bool inQuotes = false;  // 현재 쌍따옴표 안에 있는지 여부

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            // 쌍따옴표 처리: "" → 리터럴 ", 단일 " → 인용 모드 토글
            if (c == '"')
            {
                if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                {
                    // 이스케이프된 쌍따옴표: "" → 리터럴 " 하나로 변환
                    cell.Append('"');
                    i++;
                }
                else
                {
                    // 인용 모드 진입/탈출 토글
                    inQuotes = !inQuotes;
                }
                continue;
            }

            // 쉼표: 인용 모드 밖에서만 셀 구분자로 인식
            if (c == ',' && !inQuotes)
            {
                row.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            // 줄바꿈: 인용 모드 밖에서만 행 구분자로 인식 (\r\n도 단일 줄바꿈으로 처리)
            if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    i++;  // \r\n을 하나의 줄바꿈으로 처리

                row.Add(cell.ToString());
                cell.Clear();

                result.Add(row.ToArray());
                row.Clear();
                continue;
            }

            // 일반 문자: 셀에 추가
            cell.Append(c);
        }

        // 마지막 행 처리 (파일 끝에 줄바꿈이 없는 경우)
        row.Add(cell.ToString());
        result.Add(row.ToArray());

        return result;
    }
}