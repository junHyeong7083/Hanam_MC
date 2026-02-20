using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizedTable
{
    private readonly Dictionary<int, (string ko, string en)> _map
        = new Dictionary<int, (string ko, string en)>();

    public void Load(TextAsset localizedCsv)
    {
        var rows = CsvUtil.Parse(localizedCsv);
        if (rows.Count <= 1) return;

        var headerMap = CsvHeader.Build(rows[0]);

        // 컬럼명 후보를 여러 개 두면 시트가 조금 바뀌어도 덜 깨짐
        int idxId = CsvHeader.GetIndex(headerMap, "index", "id", "textId");
        int idxKo = CsvHeader.GetIndex(headerMap, "ko", "한국어");
        int idxEn = CsvHeader.GetIndex(headerMap, "en", "영어");

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            int id = CsvHeader.GetInt(r, idxId);
            if (id == 0) continue;

            string ko = CsvHeader.GetString(r, idxKo);
            string en = CsvHeader.GetString(r, idxEn);

            _map[id] = (ko, en);
        }
    }

    public string Get(int textId, bool korean)
    {
        if (textId == 0) return "";
        if (!_map.TryGetValue(textId, out var v)) return $"<missing textId:{textId}>";
        return korean ? (v.ko ?? "") : (v.en ?? "");
    }
}