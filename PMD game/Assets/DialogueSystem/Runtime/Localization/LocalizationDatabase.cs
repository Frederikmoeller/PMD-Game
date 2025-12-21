using System.Collections.Generic;

namespace DialogueSystem.Localization
{
    public class LocalizationDatabase
    {
        public Dictionary<string, Dictionary<string, string>> Data = new Dictionary<string, Dictionary<string, string>>();

        public string CurrentLanguage { get; private set; } = "English";

        public void SetLanguage(string lang)
        {
            CurrentLanguage = lang;
        }

        public string Get(string key)
        {
            if (!Data.ContainsKey(key)) return $"Missing Key: {key}";
            if (!Data[key].ContainsKey(CurrentLanguage)) return $"[No {CurrentLanguage} For {key}";
            return Data[key][CurrentLanguage];
        }
    }
}
