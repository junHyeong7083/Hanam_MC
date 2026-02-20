using System;
using System.Collections.Generic;

public static class CsvHeader
{
    public static Dictionary<string, int> Build(string[] headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRow.Length; i++)
        {
            var h = (headerRow[i] ?? "").Trim();

            if (string.IsNullOrEmpty(h))
                continue;

            // 핵심: #로 시작하면 무시
            if (h.StartsWith("#"))
                continue;

            if (!map.ContainsKey(h))
                map.Add(h, i);
        }

        return map;
    }

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

    public static string GetString(string[] row, int idx)
    {
        if (idx < 0 || idx >= row.Length) return "";
        return row[idx] ?? "";
    }

    public static int GetInt(string[] row, int idx)
    {
        if (idx < 0 || idx >= row.Length) return 0;
        int.TryParse(row[idx], out int v);
        return v;
    }
}