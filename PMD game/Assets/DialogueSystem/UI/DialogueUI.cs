using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Data;
using DialogueSystem.Localization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DialogueSystem.UI
{
    public class DialogueUi : MonoBehaviour, IDialogueUi
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;
        [SerializeField] private GameObject _continueIndicator;
        
        [Header("Typewriter Settings")]
        [SerializeField] private bool _useTypewriterEffect = true;
        [SerializeField] private TypewriterEffect _typewriterEffect;

        private void Awake()
        { 
            // Setup TypewriterEffect if needed
            if (_useTypewriterEffect)
            {
                InitializeTypewriterEffect();
            }
            
            // Hide continue indicator initially
            if (_continueIndicator != null)
                _continueIndicator.SetActive(false);
        }
        
        private void InitializeTypewriterEffect()
        {
            if (_typewriterEffect == null)
            {
                _typewriterEffect = _dialogueText.GetComponent<TypewriterEffect>();
                if (_typewriterEffect == null)
                    _typewriterEffect = _dialogueText.gameObject.AddComponent<TypewriterEffect>();
            }
            
            // Subscribe to typewriter events
            _typewriterEffect.OnTypingCompleted += OnTypingComplete;
        }

        public void ShowLine(DialogueLine line)
        {
            if (line == null)
            {
                Debug.LogError("DialogueUI: Cannot show null line");
                return;
            }

            string speakerString = LocalizationSystem.Get(line.SpeakerId); 
            // Get the localized text
            string dialogueString = LocalizationSystem.Get(line.TextKey);
            
            // Ensure text is active
            _dialogueText.gameObject.SetActive(true);
            _speakerText.gameObject.SetActive(true);
            
            if (_useTypewriterEffect && _typewriterEffect != null)
            {
                // Hide continue indicator while typing
                if (_continueIndicator != null)
                    _continueIndicator.SetActive(false);
                
                // Start typing the text
                _typewriterEffect.StartTyping(dialogueString);
            }
            else
            {
                // Show text immediately
                _dialogueText.text = dialogueString;
                _speakerText.text = speakerString;
                
                // Show continue indicator immediately
                OnTypingComplete();
            }
        }
        
        private void OnTypingComplete()
        {
            // Show continue indicator when typing is done
            if (_continueIndicator != null)
                _continueIndicator.SetActive(true);
            StartCoroutine(ContinueIndicatorBlinking());
        }

        IEnumerator ContinueIndicatorBlinking()
        {
            var imageColor = _continueIndicator.GetComponent<Image>().color;
            while (_continueIndicator.activeSelf)
            {
                yield return new WaitForSeconds(.7f);
                imageColor.a = 0;
                yield return new WaitForSeconds(.7f);
                imageColor.a = 255;
            }
        }

        public void ShowChoices(List<DialogueChoice> choices)
        {
            if (_choicesContainer == null || _choiceButtonPrefab == null)
            {
                Debug.LogWarning("DialogueUI: Choices container or button prefab not set up");
                return;
            }
    
            // Clear existing choices
            foreach (Transform child in _choicesContainer)
            {
                Destroy(child.gameObject);
            }
    
            // Show container
            _choicesContainer.gameObject.SetActive(true);
    
            // Hide continue indicator during choice selection
            if (_continueIndicator != null)
                _continueIndicator.SetActive(false);
    
            // Create choice buttons
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var buttonGo = Instantiate(_choiceButtonPrefab, _choicesContainer);
        
                // Get the Button component (UnityEngine.UI.Button)
                var button = buttonGo.GetComponent<Button>();
                var textComponent = buttonGo.GetComponentInChildren<TextMeshProUGUI>();
        
                if (textComponent != null)
                {
                    string choiceText = LocalizationSystem.Get(choice.TextKey);
                    textComponent.text = choiceText;
                }
        
                // Store the index in a local variable to avoid closure issues
                int choiceIndex = i;
        
                // Add click event
                button.onClick.AddListener(() =>
                {
                    // Inform dialogue runner of choice selection
                    if (DialogueManager.Instance != null && DialogueManager.Instance.GetRunner() != null)
                    {
                        var runner = DialogueManager.Instance.GetRunner();
                        runner.Choose(choiceIndex);
                    }
            
                    // Hide choices
                    _choicesContainer.gameObject.SetActive(false);
                });
            }
        }

        public void HideAll()
        {
            _dialogueText.gameObject.SetActive(false);
            if (_choicesContainer != null)
            {
                _choicesContainer.gameObject.SetActive(false);
            }
            
            if (_continueIndicator != null)
                _continueIndicator.SetActive(false);
        }

        public void EndDialogue()
        {
            HideAll();
        }
        
        // Public method for external scripts to skip typing
        public void SkipTyping()
        {
            if (_useTypewriterEffect && _typewriterEffect != null && _typewriterEffect.IsTyping)
            {
                _typewriterEffect.Finish();
            }
        }
        
        // Check if typing is in progress
        public bool IsTyping()
        {
            return _useTypewriterEffect && _typewriterEffect != null && _typewriterEffect.IsTyping;
        }
    }
}
