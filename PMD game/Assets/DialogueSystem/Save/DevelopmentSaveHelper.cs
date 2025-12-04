using UnityEngine;

namespace DialogueSystem.Save
{
    public class DevelopmentSaveHelper : IDialogueSaveBridge
    {
        private DialogueSaveData _memoryData = new();

        public void OnDialogueDataReady(DialogueSaveData saveData)
        {
            _memoryData = saveData;
            Debug.Log($"Development: Dialogue data stored ({saveData.variables.Count} variables in memory)");
        }

        public DialogueSaveData OnDialogueDataRequested() => _memoryData;
        public void OnDialogueClearRequested() => _memoryData = new DialogueSaveData();

        public static void UseForDevelopment()
        {
            DialogueSaveManager.RegisterSaveBridge(new DevelopmentSaveHelper());
            Debug.Log("Development save helper activated");
        }
    }
}
