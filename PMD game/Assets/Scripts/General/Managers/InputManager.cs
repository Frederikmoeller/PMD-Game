using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GameSystem
{
    public class InputManager : MonoBehaviour, IGameManagerListener
    {
        // ===== INPUT ACTION ASSETS =====
        [Header("Input Assets")]
        [SerializeField] private InputActionAsset _playerControls;
        
        // ===== INPUT ACTION MAPS =====
        private InputActionMap _uiActionMap;
        private InputActionMap _gameplayActionMap;
        private InputActionMap _dialogueActionMap;
        
        // ===== INPUT ACTIONS =====
        // UI Actions
        private InputAction _uiNavigateAction;
        private InputAction _uiSubmitAction;
        private InputAction _uiCancelAction;
        private InputAction _uiPauseAction;
        
        // Gameplay Actions
        private InputAction _moveAction;
        private InputAction _interactAction;
        private InputAction _attackAction;
        private InputAction _inventoryAction;
        private InputAction _questLogAction;
        private InputAction _minimapAction;
        private InputAction _gameplayPauseAction;
        
        // Dialogue Actions
        private InputAction _dialogueContinueAction;
        private InputAction _dialogueSkipAction;
        private InputAction _dialogueChoiceSelectAction;
        
        // ===== STATE =====
        private bool _isInputEnabled = true;
        private bool _isCombatLocked = false;
        private Vector2 _lastMoveInput = Vector2.zero;
        
        // ===== EVENTS =====
        // UI Events
        public event Action<Vector2> OnUiNavigate;
        public event Action OnUiSubmit;
        public event Action OnUiCancel;
        
        // Gameplay Events
        public event Action<Vector2Int> OnMoveInput;
        public event Action OnInteractInput;
        public event Action OnAttackInput;
        public event Action OnInventoryInput;
        public event Action OnQuestLogInput;
        public event Action OnMinimapToggleInput;
        
        // Dialogue Events
        public event Action OnDialogueContinue;
        public event Action OnDialogueSkip;
        public event Action<int> OnDialogueChoiceSelected;
        
        // Global Events
        public event Action OnPauseInput;
        
        // ===== PROPERTIES =====
        public bool IsInputEnabled => _isInputEnabled && !_isCombatLocked;
        public Vector2 LastMoveInput => _lastMoveInput;
        
        public void Initialize()
        {
            Debug.Log("InputManager Initializing with New Input System");
            
            if (_playerControls == null)
            {
                Debug.LogError("Player Controls Input Action Asset is not assigned!");
                return;
            }
            
            SetupActionMaps();
            EnableUiInput();
            
            Debug.Log("InputManager initialized successfully");
        }
        
        private void SetupActionMaps()
        {
            // Get or create action maps
            _uiActionMap = _playerControls.FindActionMap("UI") ?? _playerControls.AddActionMap("UI");
            _gameplayActionMap = _playerControls.FindActionMap("Gameplay") ?? _playerControls.AddActionMap("Gameplay");

            SetupUiActions();
            SetupGameplayActions();
            SetupDialogueActions();
        }
        
        private void SetupUiActions()
        {
            // UI Navigation
            _uiNavigateAction = _uiActionMap.FindAction("Navigate") ?? _uiActionMap.AddAction("Navigate", InputActionType.Value, "<Gamepad>/leftStick,<Gamepad>/dpad,<Keyboard>/arrowKeys,<Keyboard>/wasd");
            _uiNavigateAction.performed += OnUINavigatePerformed;
            _uiNavigateAction.canceled += OnUINavigateCanceled;
            
            // UI Submit
            _uiSubmitAction = _uiActionMap.FindAction("Confirm") ?? _uiActionMap.AddAction("Submit", InputActionType.Button, "<Gamepad>/buttonSouth,<Keyboard>/enter,<Keyboard>/space");
            _uiSubmitAction.performed += OnUISubmitPerformed;
            
            // UI Cancel
            _uiCancelAction = _uiActionMap.FindAction("Cancel") ?? _uiActionMap.AddAction("Cancel", InputActionType.Button, "<Gamepad>/buttonEast,<Keyboard>/escape");
            _uiCancelAction.performed += OnUICancelPerformed;
            
            // UI Pause
            _uiPauseAction = _uiActionMap.FindAction("Pause") ?? _uiActionMap.AddAction("Pause", InputActionType.Button, "<Gamepad>/start,<Keyboard>/escape");
            _uiPauseAction.performed += OnPausePerformed;
        }
        
        private void SetupGameplayActions()
        {
            // Movement
            _moveAction = _gameplayActionMap.FindAction("Move") ?? _gameplayActionMap.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick,<Gamepad>/dpad,<Keyboard>/arrowKeys,<Keyboard>/wasd");
            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMoveCanceled;
            
            // Interact
            _interactAction = _gameplayActionMap.FindAction("Interact") ?? _gameplayActionMap.AddAction("Interact", InputActionType.Button, "<Gamepad>/buttonWest,<Keyboard>/e,<Keyboard>/space");
            _interactAction.performed += OnInteractPerformed;

            // Gameplay Pause
            _gameplayPauseAction = _gameplayActionMap.FindAction("Pause") ?? _gameplayActionMap.AddAction("Pause", InputActionType.Button, "<Gamepad>/start,<Keyboard>/escape");
            _gameplayPauseAction.performed += OnPausePerformed;
        }
        
        private void SetupDialogueActions()
        {
            // Dialogue Continue
            _dialogueContinueAction = _dialogueActionMap.FindAction("Continue") ?? _dialogueActionMap.AddAction("Continue", InputActionType.Button, "<Gamepad>/buttonSouth,<Keyboard>/space,<Keyboard>/enter,<Mouse>/leftButton");
            _dialogueContinueAction.performed += OnDialogueContinuePerformed;
            
            // Dialogue Skip
            _dialogueSkipAction = _dialogueActionMap.FindAction("Skip") ?? _dialogueActionMap.AddAction("Skip", InputActionType.Button, "<Keyboard>/escape,<Gamepad>/buttonEast");
            _dialogueSkipAction.performed += OnDialogueSkipPerformed;
            
            // Dialogue Choice Select (1-4)
            _dialogueChoiceSelectAction = _dialogueActionMap.FindAction("ChoiceSelect") ?? _dialogueActionMap.AddAction("ChoiceSelect", InputActionType.Button, "<Keyboard>/1,<Keyboard>/2,<Keyboard>/3,<Keyboard>/4");
            _dialogueChoiceSelectAction.performed += OnDialogueChoiceSelectPerformed;
        }
        
        // ===== INPUT MODE MANAGEMENT =====
        public void EnableUiInput()
        {
            DisableAllInput();
            _uiActionMap.Enable();
            Debug.Log("UI input enabled");
        }
        
        public void EnableGameplayInput()
        {
            DisableAllInput();
            _gameplayActionMap.Enable();
            Debug.Log("Gameplay input enabled");
        }
        
        public void EnableDialogueInput()
        {
            DisableAllInput();
            _dialogueActionMap.Enable();
            Debug.Log("Dialogue input enabled");
        }
        
        public void DisableAllInput()
        {
            _uiActionMap.Disable();
            _gameplayActionMap.Disable();
            _dialogueActionMap.Disable();
        }
        
        // ===== INPUT EVENT HANDLERS =====
        // UI Events
        private void OnUINavigatePerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            Vector2 input = context.ReadValue<Vector2>();
            OnUiNavigate?.Invoke(input);
        }
        
        private void OnUINavigateCanceled(InputAction.CallbackContext context)
        {
            OnUiNavigate?.Invoke(Vector2.zero);
        }
        
        private void OnUISubmitPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnUiSubmit?.Invoke();
            GameManager.Instance.Audio.PlayButtonClick();
        }
        
        private void OnUICancelPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnUiCancel?.Invoke();
            GameManager.Instance.Audio.PlayButtonClick();
        }
        
        // Gameplay Events
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            Vector2 input = context.ReadValue<Vector2>();
            _lastMoveInput = input;
            
            // Convert to grid movement
            Vector2Int gridDirection = GetGridDirectionFromInput(input);
            if (gridDirection != Vector2Int.zero)
            {
                OnMoveInput?.Invoke(gridDirection);
            }
        }
        
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _lastMoveInput = Vector2.zero;
        }
        
        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnInteractInput?.Invoke();
        }
        
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnAttackInput?.Invoke();
            //GameManager.Instance.Audio.PlayPlayerAttack();
        }
        
        private void OnInventoryPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnInventoryInput?.Invoke();
            //GameManager.Instance.Audio.PlayButtonClick();
        }
        
        private void OnQuestLogPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnQuestLogInput?.Invoke();
            //GameManager.Instance.Audio.PlayButtonClick();
        }

        // Dialogue Events
        private void OnDialogueContinuePerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnDialogueContinue?.Invoke();
            //GameManager.Instance.Audio.PlayButtonClick();
        }
        
        private void OnDialogueSkipPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            OnDialogueSkip?.Invoke();
            //GameManager.Instance.Audio.PlayButtonClick();
        }
        
        private void OnDialogueChoiceSelectPerformed(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled) return;
            
            // Determine which number key was pressed
            if (context.control is KeyControl keyControl)
            {
                int choiceIndex = keyControl.keyCode switch
                {
                    Key.Digit1 => 0,
                    Key.Digit2 => 1,
                    Key.Digit3 => 2,
                    Key.Digit4 => 3,
                    Key.Numpad1 => 0,
                    Key.Numpad2 => 1,
                    Key.Numpad3 => 2,
                    Key.Numpad4 => 3,
                    _ => -1
                };
                
                if (choiceIndex >= 0)
                {
                    OnDialogueChoiceSelected?.Invoke(choiceIndex);
                    //GameManager.Instance.Audio.PlayButtonClick();
                }
            }
        }
        
        // Global Events
        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            // Pause should always work regardless of input lock
            OnPauseInput?.Invoke();
            //GameManager.Instance.Audio.PlayButtonClick();
        }
        
        // ===== HELPER METHODS =====
        private Vector2Int GetGridDirectionFromInput(Vector2 input)
        {
            // Deadzone to prevent drift
            float deadzone = 0.5f;
            
            if (input.magnitude < deadzone)
                return Vector2Int.zero;
            
            // Determine primary direction
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                // Horizontal movement
                return input.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                // Vertical movement
                return input.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
        }
        
        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.TitleScreen:
                    EnableUiInput();
                    break;
                case GameState.InTown:
                case GameState.InDungeon:
                    EnableGameplayInput();
                    break;
                case GameState.InDialogue:
                    EnableDialogueInput();
                    break;
                case GameState.Paused:
                    EnableUiInput();
                    break;
            }
            
            Debug.Log($"Input mode changed for state: {newState}");
        }
        
        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {
            // Input doesn't need scene-specific setup
        }
        
        public void OnPauseStateChanged(bool paused)
        {
            SetInputEnabled(!paused);
        }
        
        // ===== PUBLIC API =====
        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;
            Debug.Log($"Input {(enabled ? "enabled" : "disabled")}");
        }
        
        public void SetCombatLock(bool locked)
        {
            _isCombatLocked = locked;
            Debug.Log($"Combat lock {(locked ? "enabled" : "disabled")}");
        }
        
        public Vector2 GetMousePosition()
        {
            return Mouse.current.position.ReadValue();
        }
        
        public Vector2 GetMouseWorldPosition()
        {
            Vector2 mousePos = GetMousePosition();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane));
            return worldPos;
        }
        
        public Vector2Int GetMouseGridPosition()
        {
            Vector2 worldPos = GetMouseWorldPosition();
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.y)
            );
        }
        
        public bool IsMouseOverUi()
        {
            // This would use your UI system's event system
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
        
        public void VibrateController(float lowFrequency, float highFrequency, float duration = 0.1f)
        {
            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
                
                // Stop vibration after duration
                StartCoroutine(StopVibrationAfterDelay(duration));
            }
        }
        
        private System.Collections.IEnumerator StopVibrationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }
        
        // ===== INPUT QUERIES =====
        public bool IsActionPressed(string actionName)
        {
            InputAction action = FindActionByName(actionName);
            return action?.IsPressed() ?? false;
        }
        
        public bool WasActionPressedThisFrame(string actionName)
        {
            InputAction action = FindActionByName(actionName);
            return action?.WasPressedThisFrame() ?? false;
        }
        
        public float GetActionValue(string actionName)
        {
            InputAction action = FindActionByName(actionName);
            return action?.ReadValue<float>() ?? 0f;
        }
        
        public Vector2 GetActionVector2(string actionName)
        {
            InputAction action = FindActionByName(actionName);
            return action?.ReadValue<Vector2>() ?? Vector2.zero;
        }
        
        private InputAction FindActionByName(string actionName)
        {
            // Search all action maps
            InputAction action = _uiActionMap.FindAction(actionName);
            action ??= _gameplayActionMap.FindAction(actionName);
            action ??= _dialogueActionMap.FindAction(actionName);
            
            return action;
        }
        
        // ===== CLEANUP =====
        private void OnDestroy()
        {
            // Unsubscribe from all events
            if (_uiNavigateAction != null)
            {
                _uiNavigateAction.performed -= OnUINavigatePerformed;
                _uiNavigateAction.canceled -= OnUINavigateCanceled;
            }
            
            if (_uiSubmitAction != null)
                _uiSubmitAction.performed -= OnUISubmitPerformed;
            
            if (_uiCancelAction != null)
                _uiCancelAction.performed -= OnUICancelPerformed;
            
            if (_uiPauseAction != null)
                _uiPauseAction.performed -= OnPausePerformed;
            
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }
            
            if (_interactAction != null)
                _interactAction.performed -= OnInteractPerformed;
            
            if (_attackAction != null)
                _attackAction.performed -= OnAttackPerformed;
            
            if (_inventoryAction != null)
                _inventoryAction.performed -= OnInventoryPerformed;
            
            if (_questLogAction != null)
                _questLogAction.performed -= OnQuestLogPerformed;

            if (_gameplayPauseAction != null)
                _gameplayPauseAction.performed -= OnPausePerformed;
            
            if (_dialogueContinueAction != null)
                _dialogueContinueAction.performed -= OnDialogueContinuePerformed;
            
            if (_dialogueSkipAction != null)
                _dialogueSkipAction.performed -= OnDialogueSkipPerformed;
            
            if (_dialogueChoiceSelectAction != null)
                _dialogueChoiceSelectAction.performed -= OnDialogueChoiceSelectPerformed;
        }
    }
}