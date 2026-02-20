using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class CsvUtil
{
    public static List<string[]> Parse(TextAsset csv)
    {
        return Parse(csv != null ? csv.text : "");
    }

    public static List<string[]> Parse(string csvText)
    {
        var result = new List<string[]>();
        if (string.IsNullOrEmpty(csvText))
            return result;

        var row = new List<string>();
        var cell = new StringBuilder();

        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                row.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    i++;

                row.Add(cell.ToString());
                cell.Clear();

                result.Add(row.ToArray());
                row.Clear();
                continue;
            }

            cell.Append(c);
        }

        row.Add(cell.ToString());
        result.Add(row.ToArray());

        return result;
    }
}