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
    public class DialogueUI : MonoBehaviour, IDialogueUI
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private GameObject continueIndicator;
        
        [Header("Typewriter Settings")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private TypewriterEffect typewriterEffect;

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

            string speakerString = LocalizationSystem.Get(line.SpeakerId); 
            // Get the localized text
            string dialogueString = LocalizationSystem.Get(line.TextKey);
            
            // Ensure text is active
            dialogueText.gameObject.SetActive(true);
            speakerText.gameObject.SetActive(true);
            
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
                speakerText.text = speakerString;
                
                // Show continue indicator immediately
                OnTypingComplete();
            }
        }
        
        private void OnTypingComplete()
        {
            // Show continue indicator when typing is done
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
            StartCoroutine(ContinueIndicatorBlinking());
        }

        IEnumerator ContinueIndicatorBlinking()
        {
            var imageColor = continueIndicator.GetComponent<Image>().color;
            while (continueIndicator.activeSelf)
            {
                yield return new WaitForSeconds(.7f);
                imageColor.a = 0;
                yield return new WaitForSeconds(.7f);
                imageColor.a = 255;
            }
        }

        public void ShowChoices(List<DialogueChoice> choices)
        {
            if (choicesContainer == null || choiceButtonPrefab == null)
            {
                Debug.LogWarning("DialogueUI: Choices container or button prefab not set up");
                return;
            }
    
            // Clear existing choices
            foreach (Transform child in choicesContainer)
            {
                Destroy(child.gameObject);
            }
    
            // Show container
            choicesContainer.gameObject.SetActive(true);
    
            // Hide continue indicator during choice selection
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
    
            // Create choice buttons
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var buttonGO = Instantiate(choiceButtonPrefab, choicesContainer);
        
                // Get the Button component (UnityEngine.UI.Button)
                var button = buttonGO.GetComponent<Button>();
                var textComponent = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
        
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
                    choicesContainer.gameObject.SetActive(false);
                });
            }
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
