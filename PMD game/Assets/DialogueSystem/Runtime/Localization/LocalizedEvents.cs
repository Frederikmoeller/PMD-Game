using System;

namespace DialogueSystem.Localization
{
    public static class LocalizationEvents
    {
        public static Action OnLanguageChanged;

        public static void RaiseLanguageChanged()
        {
            OnLanguageChanged?.Invoke();
        }
    }
}
