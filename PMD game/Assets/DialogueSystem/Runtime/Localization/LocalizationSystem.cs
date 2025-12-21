using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Localization
{
    public static class LocalizationSystem
    {
        public static LocalizationDatabase Db { get; private set; }

        public static void Load(string filePath)
        {
            Db = DataLoaderManager.Load(filePath);

            if (Db != null)
            {
                Debug.Log($"Localization system loaded: {Db.Data.Count} entries");
            }
        }

        public static void SetLanguage(string lang)
        {
            Db?.SetLanguage(lang);
            Debug.Log($"Localization language set to: {lang}");
        }

        public static string Get(string key)
        {
            return Db.Get(key);
        }

        public static void RegisterCustomLoader(IDialogueDataLoader loader)
        {
            DataLoaderManager.RegisterLoader(loader);
        }
    }
}
