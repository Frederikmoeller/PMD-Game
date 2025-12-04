using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueSystem.Data
{
    [CreateAssetMenu(fileName = "DialogueAsset", menuName = "Scriptable Objects/DialogueAsset")]
    public class DialogueAsset : ScriptableObject
    {
        public List<DialogueLine> nodes;
        public string startNodeId;
        
        [Header("End Behavior")]
        public UnityEvent onDialogueEnd; // For custom Inspector setup
    
        [Header("Auto Actions")]
        public string endSceneName;
        public DialogueAsset nextDialogue;
        public bool giveControlBack = true;
        public string cutsceneToPlay;
    }
}

