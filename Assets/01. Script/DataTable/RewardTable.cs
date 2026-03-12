using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RewardRow - 보상 아이템 한 행의 데이터를 담는 클래스
///
/// 【역할】 보상 테이블 CSV의 한 행에 해당하는 데이터를 저장한다.
///          CommonRewardStep에서 보상 화면에 아이콘, 이름, 설명을 표시할 때 사용된다.
/// </summary>
public class RewardRow
{
    /// <summary>보상 아이템의 고유 ID</summary>
    public int rewardId;

    /// <summary>보상 아이콘 이미지 파일명 (Assets/03. Resource/Meinblock/Item/ 하위)</summary>
    public string fileName;

    /// <summary>보상 이름 텍스트의 textId (LocalizedTable에서 조회)</summary>
    public int nameTextId;

    /// <summary>보상 설명 텍스트의 textId (LocalizedTable에서 조회)</summary>
    public int descTextId;
}

/// <summary>
/// RewardTable - 보상 아이템 정보 테이블
///
/// 【역할】 보상 CSV 파일을 파싱하여 rewardId → RewardRow 매핑을 관리한다.
///          각 스테이지 완료 시 보상 화면에서 아이콘, 이름, 설명을 표시하기 위한 데이터를 제공한다.
///
/// 【참조하는 곳】 CommonRewardStep — 보상 화면 표시 시 TryGet()으로 보상 정보 조회,
///                ProblemRuntime — 보상 테이블 로드 및 관리
/// 【참조되는 곳】 CsvUtil.Parse() — CSV 파싱,
///                CsvHeader — 헤더 매핑 및 셀 데이터 추출
///
/// 【흐름】
///   1. 앱 초기화 시 Load(TextAsset)로 보상 CSV 파싱 → _map에 저장
///   2. 스테이지 완료 시 TryGet(rewardId)로 보상 정보 조회
///   3. RewardRow.fileName으로 아이콘 이미지 로드
///   4. RewardRow.nameTextId/descTextId로 ProblemRuntime.L()을 통해 텍스트 표시
/// </summary>
public class RewardTable
{
    /// <summary>rewardId → RewardRow 매핑. CSV 로드 시 채워진다.</summary>
    private readonly Dictionary<int, RewardRow> _map = new Dictionary<int, RewardRow>();

    /// <summary>
    /// 보상 CSV TextAsset을 파싱하여 내부 매핑(_map)을 구성한다.
    /// 컬럼 헤더 변경에 대비하여 여러 후보 이름으로 검색한다.
    /// </summary>
    /// <param name="rewardCsv">보상 테이블 CSV에 대응하는 TextAsset</param>
    public void Load(TextAsset rewardCsv)
    {
        var rows = CsvUtil.Parse(rewardCsv);
        if (rows.Count <= 1) return;  // 헤더만 있거나 빈 파일이면 무시

        var headerMap = CsvHeader.Build(rows[0]);

        // 각 컬럼의 후보 이름들 (CSV 헤더가 변경되어도 호환되도록)
        int idxId = CsvHeader.GetIndex(headerMap, "index", "rewardId", "id");
        int idxFile = CsvHeader.GetIndex(headerMap, "fileName", "filename", "icon", "iconFile");
        int idxName = CsvHeader.GetIndex(headerMap, "nameTextId", "name", "nameId");
        int idxDesc = CsvHeader.GetIndex(headerMap, "descTextId", "desc", "description", "descId");

        // 데이터 행 순회 (i=1부터: 헤더 행 건너뛰기)
        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            int id = CsvHeader.GetInt(r, idxId);
            if (id == 0) continue;  // id가 0이면 빈 행으로 간주

            var row = new RewardRow
            {
                rewardId = id,
                fileName = CsvHeader.GetString(r, idxFile),
                nameTextId = CsvHeader.GetInt(r, idxName),
                descTextId = CsvHeader.GetInt(r, idxDesc),
            };

            _map[id] = row;
        }
    }

    /// <summary>
    /// rewardId로 보상 정보를 조회한다.
    /// </summary>
    /// <param name="rewardId">보상 고유 ID</param>
    /// <param name="row">조회된 보상 데이터 (못 찾으면 null)</param>
    /// <returns>조회 성공 여부</returns>
    public bool TryGet(int rewardId, out RewardRow row) => _map.TryGetValue(rewardId, out row);
}