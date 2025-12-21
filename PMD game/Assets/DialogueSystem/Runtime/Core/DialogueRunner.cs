using System;
using System.Collections.Generic;
using System.Linq;
using DialogueSystem.Data;
using DialogueSystem.Save;
using UnityEngine;

namespace DialogueSystem
{
    public class DialogueRunner
    {
        private DialogueAsset _asset;
        private DialogueLine _currentNode;

        public event Action<DialogueLine> OnLineDisplayed;
        public event Action<List<DialogueChoice>> OnChoicesDisplayed;
        public event Action OnDialogueEnd;

        public void StartDialogue(DialogueAsset asset, bool useSavedState = true)
        {
            _asset = asset;

            if (useSavedState)
            {
                LoadState();
            }
            else
            {
                _currentNode = FindNode(asset.StartNodeId);
                DisplayNode();
            }
        }

        void DisplayNode()
        {
            OnLineDisplayed?.Invoke(_currentNode);

            if (_currentNode.Choices is { Length: > 0 })
            {
                OnChoicesDisplayed?.Invoke(GetValidChoices(_currentNode));
            }
        }

        public void Continue()
        {
            if (_currentNode == null || _asset == null)
            {
                OnDialogueEnd?.Invoke();
                return;
            }

            // If there are choices, don't auto-continue
            if (_currentNode.Choices is { Length: > 0 })
            {
                return;
            }

            // Move to next node if specified
            if (!string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                Debug.Log(_currentNode.TextKey);
                _currentNode = FindNode(_currentNode.NextNodeId);
                Debug.Log(_currentNode.TextKey);
                DisplayNode();
            }
            else
            {
                OnDialogueEnd?.Invoke();
            }
        }

        public void Choose(int index)
        {
            if (_currentNode == null || _currentNode.Choices == null || index < 0 || index >= _currentNode.Choices.Length)
            {
                Debug.LogError("Invalid choice index");
                return;
            }
            
            var choice = _currentNode.Choices[index];
            if (_asset != null)
            {
                string choiceId = choice.TextKey ?? $"Choice_{index}";
                DialogueSaveManager.SaveChoice(_asset.name, choiceId);
            }
            RunActions(choice.Actions);
            _currentNode = FindNode(choice.NextNodeId);
            CheckForEndOrContinue();
        }

        DialogueLine FindNode(string id)
        {
            if (string.IsNullOrEmpty(id) || _asset == null || _asset.Nodes == null)
                return null;
    
            // Always search by primary ID first
            var node = _asset.Nodes.Find(n => n.Guid == id);

            return node;
        }

        List<DialogueChoice> GetValidChoices(DialogueLine line) =>
            line.Choices.Where(c => AreConditionsMet(c.Conditions)).ToList();

        bool AreConditionsMet(Condition[] conditions)
        {
            if (conditions == null) return true;
            foreach (var condition in conditions)
            {
                if (!DialogueConditionSystem.Check(condition)) return false;
            }
            return true;
        }

        void RunActions(ActionEvent[] actions)
        {
            if (actions == null) return;
            foreach (var action in actions)
            {
                DialogueActionSystem.Run(action);
            }
        }

        public void CheckForEndOrContinue()
        {
            if (_currentNode == null)
            {
                OnDialogueEnd?.Invoke();
                return;
            }
            DisplayNode();
        }

        public void SaveState()
        {
            if (_currentNode != null && _asset != null)
            {
                DialogueSaveManager.SaveDialogueState(_asset.name, _currentNode.Guid);
            }
        }

        public void LoadState()
        {
            if (_asset != null)
            {
                string savedNodeId = DialogueSaveManager.LoadDialogueState(_asset.name);
                if (!string.IsNullOrEmpty(savedNodeId))
                {
                    _currentNode = FindNode(savedNodeId);
                    if (_currentNode != null)
                    {
                        Debug.Log($"Loaded saved state for {_asset.name}");
                        DisplayNode();
                        return;
                    }
                }
                // No saved state, start normally
                _currentNode = FindNode(_asset.StartNodeId);
                DisplayNode();
            }
        }
        
        // Helper methods for external access
        public bool IsDialogueActive => _currentNode != null;
        public string GetCurrentSpeaker => _currentNode?.SpeakerId;
        public DialogueLine GetCurrentNode => _currentNode;
    }
}