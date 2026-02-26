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

        // �÷��� �ĺ��� ���� �� �θ� ��Ʈ�� ���� �ٲ� �� ����
        int idxId = CsvHeader.GetIndex(headerMap, "index", "id", "textId");
        int idxKo = CsvHeader.GetIndex(headerMap, "ko", "�ѱ���");
        int idxEn = CsvHeader.GetIndex(headerMap, "en", "����");

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
        string text = korean ? (v.ko ?? "") : (v.en ?? "");
        return text.Replace("\\n", "\n");
    }
}