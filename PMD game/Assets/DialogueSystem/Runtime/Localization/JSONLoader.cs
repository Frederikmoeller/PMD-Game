using System;
using System.Collections.Generic;
using System.IO;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Localization
{
    public class JSONLoader : IDialogueDataLoader
    {
        [Serializable]
        private class JSONLocalizationData
        {
            public List<JSONLocalizationEntry> entries = new();
        }
        
        [Serializable]
        private class JSONLocalizationEntry
        {
            public string key;
            public List<JSONTranslation> translations = new List<JSONTranslation>();
        }

        [Serializable]
        private class JSONTranslation
        {
            public string language;
            public string text;
        }

        public bool CanLoad(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".json";
        }

        public LocalizationDatabase Load(string filePath)
        {
            var db = new LocalizationDatabase();
            
            // Handle both absolute paths and StreamingAssets paths
            string fullPath = filePath;
            if (!Path.IsPathRooted(filePath))
            {
                fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            }

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"JSON file not found: {fullPath}");
                return db;
            }

            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                JSONLocalizationData jsonData = JsonUtility.FromJson<JSONLocalizationData>(jsonContent);

                if (jsonData?.entries == null)
                {
                    Debug.LogError("Invalid JSON format: entries array is null");
                    return db;
                }

                foreach (var entry in jsonData.entries)
                {
                    if (string.IsNullOrEmpty(entry.key))
                    {
                        Debug.LogWarning("Skipping entry with null key in JSON");
                        continue;
                    }

                    db.data[entry.key] = new Dictionary<string, string>();

                    foreach (var translation in entry.translations)
                    {
                        if (!string.IsNullOrEmpty(translation.language))
                        {
                            db.data[entry.key][translation.language] = translation.text ?? "";
                        }
                    }
                }
                Debug.Log($"JSON loaded successfully: {db.data.Count} keys");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse JSON file '{filePath}': {e.Message}");
            }

            return db;
        }
    }
}


