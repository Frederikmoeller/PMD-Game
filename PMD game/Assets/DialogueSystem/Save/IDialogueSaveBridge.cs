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
        public Dictionary<string, object> variables = new();
        public Dictionary<string, string> dialogueStates = new();
        public Dictionary<string, bool> choicesMade = new();

        public bool IsEmpty => variables.Count == 0 && dialogueStates.Count == 0 && choicesMade.Count == 0;
    }
}
