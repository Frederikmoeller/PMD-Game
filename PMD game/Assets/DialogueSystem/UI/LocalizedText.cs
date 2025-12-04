using DialogueSystem.Localization;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text text;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            LocalizationEvents.OnLanguageChanged += RefreshText;
        }

        private void Start()
        {
            RefreshText();
        }

        private void OnDestroy()
        {
            LocalizationEvents.OnLanguageChanged -= RefreshText;
        }

        public void RefreshText()
        {
            if (text == null) return;
            text.text = LocalizationSystem.Get(key);
        }
    }
}

