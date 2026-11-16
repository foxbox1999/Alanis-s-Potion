using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class CsvParser
{
    //通过ResourceManaeger加载CSV文本，并解析成DialogueRow列表
    public static List<DialogueRow> ParseDialogueCsv(string csvText)
    {
        var rows = new List<DialogueRow>();
        if (string.IsNullOrEmpty(csvText)) return rows;

        string[] lines = csvText.Split('\n');
        if (lines.Length < 2) return rows; // 至少需要表头 + 一行数据

        // 从第二行开始，跳过表头
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(line)) continue;

            List<string> fields = ParseCsvLine(line);
            if (fields.Count < 6) // 最少需要标志、ID、人物、表情、内容、跳转
            {
                Debug.LogWarning($"第{i + 1}行字段不足，已跳过: {line}");
                continue;
            }

            var row = new DialogueRow
            {
                flag = fields[0].Trim(),
                id = int.TryParse(fields[1].Trim(), out int id) ? id : -1,
                characterId = fields[2].Trim(),
                expression = fields[3].Trim(),
                content = fields[4].Trim(),
                nextId = int.TryParse(fields[5].Trim(), out int next) ? next : -1,
                effect = fields.Count > 6 ? fields[6].Trim() : "",
                target = fields.Count > 7 ? fields[7].Trim() : ""
            };

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// 解析一行 CSV，正确处理引号内的逗号和换行符
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 双引号转义，添加一个引号
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 字段结束
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // 添加最后一个字段
        fields.Add(currentField.ToString());

        return fields;
    }
}