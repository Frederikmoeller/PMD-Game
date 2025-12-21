using DialogueSystem.UI;
using DialogueSystem.Data;
using DialogueSystem.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        // Static instance for easy access
        public static DialogueManager Instance { get; private set; }
        
        [Header("UI Reference")]
        public DialogueUi Ui;
        public string Language = "English";
        
        [Header("Events")]
        public UnityEvent<DialogueAsset> OnDialogueEnded;
        
        [Header("Start Settings")]
        public DialogueAsset StartAsset;
        
        private DialogueRunner _runner;
        private bool _isDialogueActive = false;
        private DialogueAsset _currentAsset;
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Initialize()
        {
            LocalizationSystem.Load("Dialogue.csv");
            LocalizationSystem.SetLanguage(Language);
            
            _runner = new DialogueRunner();
            _runner.OnLineDisplayed += Ui.ShowLine;
            _runner.OnChoicesDisplayed += Ui.ShowChoices;
            _runner.OnDialogueEnd += OnDialogueEnd;
            
            // Hide UI initially
            if (Ui != null)
            {
                Ui.gameObject.SetActive(false);
            }
        }
        
        void Update()
        {
            if (!_isDialogueActive) return;
            
            if (ShouldContinueInput())
            {
                if (Ui.IsTyping())
                {
                    Ui.SkipTyping();
                }
                else
                {
                    ContinueDialogue();
                }
            }
        }
        
        public void StartDialogue(DialogueAsset asset, bool useSavedState = true)
        {
            if (asset == null)
            {
                Debug.LogError("DialogueManager: Cannot start dialogue - asset is null!");
                return;
            }
            
            _isDialogueActive = true;
            _currentAsset = asset;
            _runner.StartDialogue(asset, useSavedState);
        }
        
        public void StartDialogue(bool useSavedState = true)
        {
            if (StartAsset == null)
            {
                Debug.LogError("DialogueManager: Cannot start dialogue - StartAsset is null!");
                return;
            }
            
            StartDialogue(StartAsset, useSavedState);
        }

        public void ContinueDialogue()
        {
            if (_runner != null)
            {
                _runner.Continue();
            }
        }
        
        protected virtual bool ShouldContinueInput()
        {
            return Input.GetKeyDown(KeyCode.Space) || 
                   Input.GetKeyDown(KeyCode.Return) || 
                   Input.GetMouseButtonDown(0);
        }
        
        private void OnDialogueEnd()
        {
            _isDialogueActive = false;
    
            if (Ui != null)
                Ui.HideAll();
    
            // Let the asset handle its own ending
            if (_currentAsset != null)
            {
                _currentAsset.OnDialogueEnd?.Invoke();
        
                // OR handle specific fields
                //if (_currentAsset.giveControlBack)
                    //PlayerController.Instance.EnableControl();
            
                /*if (!string.IsNullOrEmpty(_currentAsset.cutsceneToPlay))
                    CutsceneManager.Play(_currentAsset.cutsceneToPlay);*/
            
                if (!string.IsNullOrEmpty(_currentAsset.EndSceneName))
                    SceneManager.LoadScene(_currentAsset.EndSceneName);
            }
    
            // Still fire the generic event for other listeners
            OnDialogueEnded?.Invoke(_currentAsset);
    
            _currentAsset = null;
        }
        public DialogueRunner GetRunner()
        {
            return _runner;
        }
        
        // Helper property
        public bool IsDialogueActive => _isDialogueActive;
    }
}