using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LocalizedTable - CSV 기반 다국어 텍스트 테이블
///
/// 【역할】 MC_DataTable_v01.csv 파일을 파싱하여 textId → (한국어, 영어) 텍스트 매핑을 관리한다.
///          ProblemRuntime.L(textId) 호출 시 내부적으로 이 클래스의 Get() 메서드가 사용된다.
///          프로젝트의 모든 UI 텍스트는 하드코딩 대신 이 테이블을 통해 textId로 접근해야 한다.
///
/// 【참조하는 곳】 ProblemRuntime — L(textId) 메서드에서 Get() 호출,
///                SoundManager.BuildReverseMap() — 텍스트 → textId 역방향 매핑 생성 시
/// 【참조되는 곳】 CsvUtil.Parse() — CSV 파싱,
///                CsvHeader — 헤더 매핑 및 셀 데이터 추출
///
/// 【흐름】
///   1. 앱 초기화 시 Load(TextAsset)로 CSV 파싱 → _map에 저장
///   2. 런타임에 Get(textId, korean)로 텍스트 조회
///   3. "\\n" 문자열을 실제 줄바꿈(\n)으로 변환하여 반환
///   4. textId가 없으면 "<missing textId:N>" 디버그 문자열 반환 (누락 감지용)
/// </summary>
public class LocalizedTable
{
    /// <summary>textId → (한국어 텍스트, 영어 텍스트) 매핑. CSV 로드 시 채워진다.</summary>
    private readonly Dictionary<int, (string ko, string en)> _map
        = new Dictionary<int, (string ko, string en)>();

    /// <summary>
    /// CSV TextAsset을 파싱하여 내부 매핑(_map)을 구성한다.
    /// CSV 컬럼 헤더가 변경될 수 있으므로 여러 후보 이름으로 검색한다.
    /// 예: "index" 또는 "id" 또는 "textId" → textId 컬럼
    /// </summary>
    /// <param name="localizedCsv">MC_DataTable_v01.csv에 대응하는 TextAsset</param>
    public void Load(TextAsset localizedCsv)
    {
        var rows = CsvUtil.Parse(localizedCsv);
        if (rows.Count <= 1) return;  // 헤더만 있거나 빈 파일이면 무시

        var headerMap = CsvHeader.Build(rows[0]);

        // 컬럼 식별자 여러 개 등록 (헤더 문자열이 바뀔 수 있으므로 후보를 나열)
        int idxId = CsvHeader.GetIndex(headerMap, "index", "id", "textId");
        int idxKo = CsvHeader.GetIndex(headerMap, "ko", "한국어");
        int idxEn = CsvHeader.GetIndex(headerMap, "en", "영어");

        // 데이터 행 순회 (i=1부터: 헤더 행 건너뛰기)
        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            int id = CsvHeader.GetInt(r, idxId);
            if (id == 0) continue;  // id가 0이면 빈 행으로 간주하여 건너뜀

            string ko = CsvHeader.GetString(r, idxKo);
            string en = CsvHeader.GetString(r, idxEn);

            _map[id] = (ko, en);
        }
    }

    /// <summary>
    /// textId에 해당하는 로컬라이즈 텍스트를 반환한다.
    /// CSV에서 "\\n"으로 기록된 줄바꿈을 실제 \n으로 변환한다.
    /// </summary>
    /// <param name="textId">CSV DataTable의 고유 텍스트 ID</param>
    /// <param name="korean">true이면 한국어, false이면 영어 반환</param>
    /// <returns>해당 텍스트. 미등록이면 "&lt;missing textId:N&gt;" 반환 (디버깅용)</returns>
    public string Get(int textId, bool korean)
    {
        if (textId == 0) return "";
        if (!_map.TryGetValue(textId, out var v)) return $"<missing textId:{textId}>";
        string text = korean ? (v.ko ?? "") : (v.en ?? "");
        // CSV에서 줄바꿈을 "\\n" 리터럴로 저장하므로 실제 줄바꿈 문자로 변환
        return text.Replace("\\n", "\n");
    }
}
