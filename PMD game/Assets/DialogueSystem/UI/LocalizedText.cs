using DialogueSystem.Localization;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
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
            if (_text == null) return;
            _text.text = LocalizationSystem.Get(_key);
        }
    }
}

