using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueSystem.Data
{
    [CreateAssetMenu(fileName = "DialogueAsset", menuName = "Scriptable Objects/DialogueAsset")]
    public class DialogueAsset : ScriptableObject
    {
        public List<DialogueLine> Nodes;
        public string StartNodeId;
        
        [Header("End Behavior")]
        public UnityEvent OnDialogueEnd; // For custom Inspector setup
    
        [Header("Auto Actions")]
        public string EndSceneName;
        public DialogueAsset NextDialogue;
        public bool GiveControlBack = true;
        public string CutsceneToPlay;
    }
}

