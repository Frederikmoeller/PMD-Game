using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Save
{
    public static class DialogueSaveManager
    {
        private static IDialogueSaveBridge _saveBridge;
        private static DialogueSaveData _pendingSaveData = new();

        public static bool HasSaveBridge => _saveBridge != null;

        public static void RegisterSaveBridge(IDialogueSaveBridge bridge)
        {
            _saveBridge = bridge;
            Debug.Log("Dialogue Save Bridge registered");
            LoadFromBridge();
        }
        
        public static void UnregisterSaveBridge()
        {
            _saveBridge = null;
            Debug.Log("Dialogue Save Bridge unregistered");
        }

        public static void SaveVariable(string key, object value)
        {
            _pendingSaveData.Variables[key] = value;
            TryFlushToBridge();
        }

        public static void SaveDialogueState(string dialogueId, string nodeId)
        {
            _pendingSaveData.DialogueStates[dialogueId] = nodeId;
            TryFlushToBridge();
        }

        public static void SaveChoice(string dialogueId, string choiceId)
        {
            string choiceKey = $"{dialogueId}_{choiceId}";
            _pendingSaveData.ChoicesMade[choiceKey] = true;
            TryFlushToBridge();
        }

        public static void SaveAllVariables(Dictionary<string, object> variables)
        {
            foreach (var kvp in variables)
            {
                _pendingSaveData.Variables[kvp.Key] = kvp.Value;
            }
            TryFlushToBridge();
        }

        public static object LoadVariable(string key)
        {
            return _pendingSaveData.Variables.ContainsKey(key) ? _pendingSaveData.Variables[key] : null;
        }

        public static string LoadDialogueState(string dialogueId)
        {
            return _pendingSaveData.DialogueStates.ContainsKey(dialogueId)
                ? _pendingSaveData.DialogueStates[dialogueId]
                : "";
        }

        public static bool WasChoiceMade(string dialogueId, string choiceId)
        {
            string choiceKey = $"{dialogueId}_{choiceId}";
            return _pendingSaveData.ChoicesMade.ContainsKey(choiceKey) && _pendingSaveData.ChoicesMade[choiceKey];
        }

        public static Dictionary<string, object> LoadAllVariables()
        {
            return new Dictionary<string, object>(_pendingSaveData.Variables);
        }

        public static void ClearAllData()
        {
            _pendingSaveData = new DialogueSaveData();
            _saveBridge.OnDialogueClearRequested();
            Debug.Log("Dialogue data cleared");
        }

        public static void ManualSave() => TryFlushToBridge(force: true);
        public static void ManualLoad() => LoadFromBridge();


        private static void TryFlushToBridge(bool force = false)
        {
            if (_saveBridge != null && (force || !_pendingSaveData.IsEmpty))
            {
                _saveBridge.OnDialogueDataReady(_pendingSaveData);
            }
        }

        private static void LoadFromBridge()
        {
            if (_saveBridge != null)
            {
                var loadedData = _saveBridge.OnDialogueDataRequested();
                if (loadedData != null) _pendingSaveData = loadedData;
            }
        }
    }
}
