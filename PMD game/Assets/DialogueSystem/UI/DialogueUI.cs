using System.Collections.Generic;
using DialogueSystem.Data;
using DialogueSystem.Localization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace DialogueSystem.UI
{
    public class DialogueUI : MonoBehaviour, IDialogueUI
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private GameObject continueIndicator;
        
        [Header("Typewriter Settings")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private TypewriterEffect typewriterEffect;
        
        private DialogueManager _dialogueManager;
        
        private void Awake()
        { 
            // Setup TypewriterEffect if needed
            if (useTypewriterEffect)
            {
                InitializeTypewriterEffect();
            }
            
            // Hide continue indicator initially
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
        }
        
        private void InitializeTypewriterEffect()
        {
            if (typewriterEffect == null)
            {
                typewriterEffect = dialogueText.GetComponent<TypewriterEffect>();
                if (typewriterEffect == null)
                    typewriterEffect = dialogueText.gameObject.AddComponent<TypewriterEffect>();
            }
            
            // Subscribe to typewriter events
            typewriterEffect.OnTypingCompleted += OnTypingComplete;
        }

        public void ShowLine(DialogueLine line)
        {
            if (line == null)
            {
                Debug.LogError("DialogueUI: Cannot show null line");
                return;
            }
            
            // Get the localized text
            string dialogueString = LocalizationSystem.Get(line.TextKey);
            
            // Ensure text is active
            dialogueText.gameObject.SetActive(true);
            
            if (useTypewriterEffect && typewriterEffect != null)
            {
                // Hide continue indicator while typing
                if (continueIndicator != null)
                    continueIndicator.SetActive(false);
                
                // Start typing the text
                typewriterEffect.StartTyping(dialogueString);
            }
            else
            {
                // Show text immediately
                dialogueText.text = dialogueString;
                
                // Show continue indicator immediately
                if (continueIndicator != null)
                    continueIndicator.SetActive(true);
            }
        }
        
        private void OnTypingComplete()
        {
            // Show continue indicator when typing is done
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        public void ShowChoices(List<DialogueChoice> choices)
        {
            
        }

        public void HideAll()
        {
            dialogueText.gameObject.SetActive(false);
            if (choicesContainer != null)
            {
                choicesContainer.gameObject.SetActive(false);
            }
            
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
        }

        public void EndDialogue()
        {
            HideAll();
        }
        
        // Public method for external scripts to skip typing
        public void SkipTyping()
        {
            if (useTypewriterEffect && typewriterEffect != null && typewriterEffect.IsTyping)
            {
                typewriterEffect.Finish();
            }
        }
        
        // Check if typing is in progress
        public bool IsTyping()
        {
            return useTypewriterEffect && typewriterEffect != null && typewriterEffect.IsTyping;
        }
    }
}
