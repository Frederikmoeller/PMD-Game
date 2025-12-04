using System.Collections.Generic;
using System.IO;
using System.Linq;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Localization
{
    public class CSVLoader : IDialogueDataLoader
    {
        public bool CanLoad(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".csv";
        }

        public LocalizationDatabase Load(string filePath)
        {
            var db = new LocalizationDatabase();
            
            string fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            /*if (Path.IsPathRooted(filePath))
            {
                fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            }*/
            
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"CSV file not found: {fullPath}");
                return db;
            }

            string[] lines = File.ReadAllLines(fullPath);

            if (lines.Length < 2)
            {
                Debug.LogError("Csv has no content!");
                return db;
            }

            string[] header = lines[0].Split(',');

            string[] languages = header.Skip(1).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = ParseCSVLine(lines[i]);

                if (cols.Length < 2 || string.IsNullOrWhiteSpace(cols[0])) continue;

                string key = cols[0].Trim();
                db.data[key] = new Dictionary<string, string>();

                for (int langIndex = 0; langIndex < languages.Length; langIndex++)
                {
                    string lang = languages[langIndex];

                    if (langIndex + 1 < cols.Length)
                    {
                        db.data[key][lang] = cols[langIndex + 1];
                    }
                    else
                    {
                        db.data[key][lang] = "";
                    }
                }
            }
            return db;
        }

        private string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(UnescapedCSVField(line.Substring(start, i - start)));
                    start = i + 1;
                }
            }
            
            //Add the last field
            result.Add(UnescapedCSVField(line.Substring(start)));
            
            return result.ToArray();
        }

        private string UnescapedCSVField(string field)
        { 
            field = field.Trim();
            // Remove Surrounding quotes and handle escaped quotes
            if (field.Length >= 2 && field.StartsWith("\"") && field.EndsWith("\""))
            {
                field = field.Substring(1, field.Length - 2);
                field = field.Replace("\"\"", "\"");
            }

            return field;
        }
    }
}
