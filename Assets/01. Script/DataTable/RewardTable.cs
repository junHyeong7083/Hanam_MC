using System.Collections.Generic;
using UnityEngine;

public class RewardRow
{
    public int rewardId;
    public string fileName;
    public int nameTextId;
    public int descTextId;
}

public class RewardTable
{
    private readonly Dictionary<int, RewardRow> _map = new Dictionary<int, RewardRow>();

    public void Load(TextAsset rewardCsv)
    {
        var rows = CsvUtil.Parse(rewardCsv);
        if (rows.Count <= 1) return;

        var headerMap = CsvHeader.Build(rows[0]);

        int idxId = CsvHeader.GetIndex(headerMap, "index", "rewardId", "id");
        int idxFile = CsvHeader.GetIndex(headerMap, "fileName", "filename", "icon", "iconFile");
        int idxName = CsvHeader.GetIndex(headerMap, "nameTextId", "name", "nameId");
        int idxDesc = CsvHeader.GetIndex(headerMap, "descTextId", "desc", "description", "descId");

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            int id = CsvHeader.GetInt(r, idxId);
            if (id == 0) continue;

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

    public bool TryGet(int rewardId, out RewardRow row) => _map.TryGetValue(rewardId, out row);
}