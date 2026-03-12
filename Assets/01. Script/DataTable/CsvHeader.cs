using System;
using System.Collections.Generic;

/// <summary>
/// CsvHeader - CSV 헤더 행을 파싱하여 컬럼명 → 인덱스 매핑을 생성하는 유틸리티 클래스
///
/// 【역할】 CSV 파일의 첫 번째 행(헤더)을 분석하여 컬럼명과 인덱스를 매핑하는 Dictionary를 생성한다.
///          이를 통해 컬럼 순서가 변경되더라도 이름으로 안전하게 데이터에 접근할 수 있다.
///          '#'으로 시작하는 주석 컬럼은 무시한다.
///
/// 【참조하는 곳】 LocalizedTable.Load() — 텍스트 CSV 파싱,
///                RewardTable.Load() — 보상 CSV 파싱
/// 【참조되는 곳】 없음 (독립적인 유틸리티 클래스)
///
/// 【흐름】
///   1. Build(headerRow)로 헤더 행 파싱 → Dictionary<string, int> 생성
///   2. GetIndex(headerMap, "name1", "name2")로 여러 후보 이름 중 매칭되는 컬럼 인덱스 조회
///   3. GetString/GetInt(row, idx)로 해당 인덱스의 셀 데이터 추출
/// </summary>
public static class CsvHeader
{
    /// <summary>
    /// CSV 헤더 행을 파싱하여 컬럼명 → 인덱스 매핑 Dictionary를 생성한다.
    /// 대소문자 무시(OrdinalIgnoreCase)로 매칭하며, '#'으로 시작하는 주석 컬럼은 건너뛴다.
    /// 같은 이름의 컬럼이 중복되면 첫 번째 것만 등록한다.
    /// </summary>
    /// <param name="headerRow">CSV 첫 번째 행의 셀 배열</param>
    /// <returns>컬럼명 → 인덱스 매핑 Dictionary</returns>
    public static Dictionary<string, int> Build(string[] headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRow.Length; i++)
        {
            var h = (headerRow[i] ?? "").Trim();

            if (string.IsNullOrEmpty(h))
                continue;

            // '#'으로 시작하는 컬럼은 주석으로 간주하여 무시
            if (h.StartsWith("#"))
                continue;

            // 중복 컬럼명 방지: 첫 번째 등장만 등록
            if (!map.ContainsKey(h))
                map.Add(h, i);
        }

        return map;
    }

    /// <summary>
    /// 여러 후보 컬럼명 중 headerMap에 존재하는 첫 번째 이름의 인덱스를 반환한다.
    /// CSV 헤더 문자열이 변경될 수 있으므로 여러 이름을 후보로 등록하여 호환성을 확보한다.
    /// 예: GetIndex(map, "index", "id", "textId") → "index", "id", "textId" 순서로 검색
    /// </summary>
    /// <param name="headerMap">Build()로 생성한 헤더 매핑</param>
    /// <param name="names">검색할 컬럼명 후보들 (우선순위 순)</param>
    /// <returns>찾은 컬럼의 인덱스. 못 찾으면 -1</returns>
    public static int GetIndex(Dictionary<string, int> headerMap, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var key = (names[i] ?? "").Trim();
            if (headerMap.TryGetValue(key, out int idx))
                return idx;
        }
        return -1;
    }

    /// <summary>
    /// CSV 행에서 지정 인덱스의 셀 값을 문자열로 반환한다.
    /// 인덱스가 범위 밖이면 빈 문자열을 반환한다.
    /// </summary>
    /// <param name="row">CSV 행의 셀 배열</param>
    /// <param name="idx">컬럼 인덱스 (Build/GetIndex에서 얻은 값)</param>
    /// <returns>셀 문자열 값</returns>
    public static string GetString(string[] row, int idx)
    {
        if (idx < 0 || idx >= row.Length) return "";
        return row[idx] ?? "";
    }

    /// <summary>
    /// CSV 행에서 지정 인덱스의 셀 값을 정수로 반환한다.
    /// 파싱 실패 또는 범위 밖이면 0을 반환한다.
    /// </summary>
    /// <param name="row">CSV 행의 셀 배열</param>
    /// <param name="idx">컬럼 인덱스</param>
    /// <returns>셀 정수 값 (파싱 실패 시 0)</returns>
    public static int GetInt(string[] row, int idx)
    {
        if (idx < 0 || idx >= row.Length) return 0;
        int.TryParse(row[idx], out int v);
        return v;
    }
}