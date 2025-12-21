using System;
using System.Collections.Generic;

namespace DialogueSystem.Save
{
    public interface IDialogueSaveBridge
    {
        void OnDialogueDataReady(DialogueSaveData saveData);
        DialogueSaveData OnDialogueDataRequested();
        void OnDialogueClearRequested();
    }

    [Serializable]
    public class DialogueSaveData
    {
        public Dictionary<string, object> Variables = new();
        public Dictionary<string, string> DialogueStates = new();
        public Dictionary<string, bool> ChoicesMade = new();

        public bool IsEmpty => Variables.Count == 0 && DialogueStates.Count == 0 && ChoicesMade.Count == 0;
    }
}
