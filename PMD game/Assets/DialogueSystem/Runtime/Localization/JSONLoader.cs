using System;
using System.Collections.Generic;
using System.IO;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Localization
{
    public class JsonLoader : IDialogueDataLoader
    {
        [Serializable]
        private class JsonLocalizationData
        {
            public List<JsonLocalizationEntry> Entries = new();
        }
        
        [Serializable]
        private class JsonLocalizationEntry
        {
            public string Key;
            public List<JsonTranslation> Translations = new List<JsonTranslation>();
        }

        [Serializable]
        private class JsonTranslation
        {
            public string Language;
            public string Text;
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
                JsonLocalizationData jsonData = JsonUtility.FromJson<JsonLocalizationData>(jsonContent);

                if (jsonData?.Entries == null)
                {
                    Debug.LogError("Invalid JSON format: entries array is null");
                    return db;
                }

                foreach (var entry in jsonData.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Key))
                    {
                        Debug.LogWarning("Skipping entry with null key in JSON");
                        continue;
                    }

                    db.Data[entry.Key] = new Dictionary<string, string>();

                    foreach (var translation in entry.Translations)
                    {
                        if (!string.IsNullOrEmpty(translation.Language))
                        {
                            db.Data[entry.Key][translation.Language] = translation.Text ?? "";
                        }
                    }
                }
                Debug.Log($"JSON loaded successfully: {db.Data.Count} keys");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse JSON file '{filePath}': {e.Message}");
            }

            return db;
        }
    }
}


