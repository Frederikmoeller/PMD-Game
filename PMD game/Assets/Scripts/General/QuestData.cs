using System;
using System.Collections.Generic;
using DialogueSystem.Data;
using UnityEngine;

namespace GameSystem
{
    public enum QuestObjectiveType
    {
        CollectItems,
        DefeatEnemies,
        RescueCivilian,
        Escort,
        ReachFloor,
        TalkToNPC,
        UseItem,
        DiscoverLocation,
        CompleteDungeon
    }
    
    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        ReadyToComplete,
        Completed,
        Failed
    }
    
    [CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
    public class QuestData : ScriptableObject
    {
        [Header("Basic Info")]
        public string QuestId;
        public string Name;
        [TextArea(3, 5)]
        public string Description;
        [TextArea(3, 5)]
        public string CompletionText;
        
        [Header("Requirements")]
        public int RequiredLevel = 1;
        public List<string> RequiredCompletedQuests = new List<string>();
        
        [Header("Objectives")]
        public List<QuestObjectiveData> Objectives = new List<QuestObjectiveData>();
        
        [Header("Rewards")]
        public int GoldReward;
        public List<ItemReward> ItemRewards = new List<ItemReward>();
        public int XPReward;
        
        [Header("Dialogue")]
        public DialogueAsset StartDialogue;
        public DialogueAsset ProgressDialogue;
        public DialogueAsset CompletionDialogue;
        
        [Header("Settings")]
        public bool AutoStart = false;
        public bool IsRepeatable = false;
        public bool IsMainQuest = false;
        public List<string> FollowUpQuests = new List<string>();
        
        [Header("Time Limit (0 = no limit)")]
        public float TimeLimitMinutes = 0f;
        
        // Editor helper
        #if UNITY_EDITOR
        [HideInInspector] public bool ShowObjectives = true;
        [HideInInspector] public bool ShowRewards = true;
        [HideInInspector] public bool ShowSettings = true;
        #endif
    }
    
    [Serializable]
    public class QuestObjectiveData
    {
        public string ObjectiveId;
        public QuestObjectiveType Type;
        public string Description;
        
        // Target information
        public string TargetId; // ItemId, EnemyType, NPCId, etc.
        public int RequiredAmount = 1;
        
        // Optional location data
        public Vector2Int Location = Vector2Int.zero;
        public string SceneName = "";
        
        // Optional dialogue
        public DialogueAsset Dialogue;
        
        public QuestObjectiveData Clone()
        {
            return new QuestObjectiveData
            {
                ObjectiveId = ObjectiveId,
                Type = Type,
                Description = Description,
                TargetId = TargetId,
                RequiredAmount = RequiredAmount,
                Location = Location,
                SceneName = SceneName,
                Dialogue = Dialogue
            };
        }
    }
    
    [Serializable]
    public class ItemReward
    {
        public ItemData Item;
        public int Quantity = 1;
        [Range(0f, 1f)]
        public float DropChance = 1f; // For random rewards
    }
}