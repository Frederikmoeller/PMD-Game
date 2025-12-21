using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystem
{
    public class QuestManager : MonoBehaviour, IGameManagerListener
    {
        [Header("Quest Settings")]
        [SerializeField] private List<QuestData> _availableQuests = new();
        [SerializeField] private bool _autoAcceptMainQuests = true;
        
        // State
        private List<ActiveQuest> _activeQuests = new();
        private List<QuestData> _completedQuests = new();
        private List<QuestData> _failedQuests = new();
        
        // Events
        public event Action<ActiveQuest> OnQuestStarted;
        public event Action<ActiveQuest> OnQuestProgressUpdated;
        public event Action<ActiveQuest> OnQuestCompleted;
        public event Action<ActiveQuest> OnQuestFailed;
        public event Action<List<ActiveQuest>> OnQuestListUpdated;
        
        public List<ActiveQuest> ActiveQuests => _activeQuests;
        public List<QuestData> CompletedQuests => _completedQuests;
        public List<QuestData> FailedQuests => _failedQuests;
        
        public void Initialize()
        {
            Debug.Log("QuestManager Initializing");
            
            // Load saved quests if any
            LoadDefaultQuests();
            
            // Subscribe to game events
            SubscribeToGameEvents();
            
            Debug.Log("QuestManager initialized successfully");
        }
        
        private void LoadDefaultQuests()
        {
            // Load any starting quests
            foreach (var quest in _availableQuests)
            {
                if (quest.AutoStart && CanStartQuest(quest))
                {
                    StartQuest(quest);
                }
            }
        }
        
        private void SubscribeToGameEvents()
        {
            // Subscribe to inventory events
            if (GameManager.Instance.Inventory != null)
            {
                GameManager.Instance.Inventory.OnItemAdded += OnItemCollected;
            }
            
            // Subscribe to combat events
            if (GameManager.Instance.Combat != null)
            {
                // Assuming CombatManager has events
                // GameManager.Instance.Combat.OnEnemyDefeated += OnEnemyDefeated;
            }
            
            // Subscribe to dungeon events
            if (GameManager.Instance.Dungeon != null)
            {
                GameManager.Instance.Dungeon.OnFloorChanged.AddListener(OnFloorChanged);
            }
        }
        
        private void UnsubscribeFromGameEvents()
        {
            if (GameManager.Instance.Inventory != null)
            {
                GameManager.Instance.Inventory.OnItemAdded -= OnItemCollected;
            }
            
            if (GameManager.Instance.Combat != null)
            {
                // GameManager.Instance.Combat.OnEnemyDefeated -= OnEnemyDefeated;
            }
            
            if (GameManager.Instance.Dungeon != null)
            {
                GameManager.Instance.Dungeon.OnFloorChanged.RemoveListener(OnFloorChanged);
            }
        }
        
        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            // Quests don't care about most game states
        }
        
        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {
            // Update quest UI when changing scenes
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
        }
        
        public void OnPauseStateChanged(bool paused)
        {
            // Pause quest timers if needed
        }
        
        // ===== QUEST OPERATIONS =====
        public bool StartQuest(QuestData questData)
        {
            if (questData == null)
            {
                Debug.LogError("Cannot start null quest!");
                return false;
            }
            
            if (!CanStartQuest(questData))
            {
                Debug.LogWarning($"Cannot start quest {questData.Name}: requirements not met");
                return false;
            }
            
            // Create active quest
            var activeQuest = new ActiveQuest(questData);
            _activeQuests.Add(activeQuest);
            
            // Subscribe to quest events
            activeQuest.OnProgressUpdated += OnQuestProgressUpdatedHandler;
            activeQuest.OnCompleted += OnQuestCompletedHandler;
            activeQuest.OnFailed += OnQuestFailedHandler;
            
            // Update UI
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
            
            // Fire events
            OnQuestStarted?.Invoke(activeQuest);
            OnQuestListUpdated?.Invoke(_activeQuests);
            
            // Log to game log
            GameManager.Instance.Ui.AddLogEntry($"Quest started: {questData.Name}", Color.cyan);
            
            // Show start dialogue if available
            if (questData.StartDialogue != null)
            {
                GameManager.Instance.Dialogue.StartDialogue(questData.StartDialogue);
            }
            
            Debug.Log($"Started quest: {questData.Name}");
            return true;
        }
        
        public bool CanStartQuest(QuestData questData)
        {
            if (questData == null) return false;
            
            // Check if already active or completed
            if (IsQuestActive(questData) || IsQuestCompleted(questData))
            {
                return false;
            }
            
            // Check level requirement
            var playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
            if (playerStats != null && playerStats.Stats.Level < questData.RequiredLevel)
            {
                return false;
            }
            
            // Check prerequisite quests
            foreach (var requiredQuestId in questData.RequiredCompletedQuests)
            {
                var requiredQuest = GetQuestById(requiredQuestId);
                if (requiredQuest != null && !IsQuestCompleted(requiredQuest))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public void CompleteQuest(ActiveQuest activeQuest)
        {
            if (activeQuest == null) return;
            
            activeQuest.Complete();
        }
        
        private void OnQuestProgressUpdatedHandler(ActiveQuest quest)
        {
            OnQuestProgressUpdated?.Invoke(quest);
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
            
            // Show progress dialogue if available
            if (quest.QuestData.ProgressDialogue != null && 
                quest.CompletedObjectives == quest.TotalObjectives / 2) // At 50% progress
            {
                GameManager.Instance.Dialogue.StartDialogue(quest.QuestData.ProgressDialogue);
            }
        }
        
        private void OnQuestCompletedHandler(ActiveQuest quest)
        {
            // Remove from active
            _activeQuests.Remove(quest);
            
            // Add to completed
            _completedQuests.Add(quest.QuestData);
            
            // Award rewards
            AwardQuestRewards(quest);
            
            // Update UI
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
            
            // Fire events
            OnQuestCompleted?.Invoke(quest);
            OnQuestListUpdated?.Invoke(_activeQuests);
            
            // Show completion dialogue if available
            if (quest.QuestData.CompletionDialogue != null)
            {
                GameManager.Instance.Dialogue.StartDialogue(quest.QuestData.CompletionDialogue);
            }
            
            // Check for follow-up quests
            CheckForFollowUpQuests(quest.QuestData);
            
            Debug.Log($"Completed quest: {quest.QuestData.Name}");
        }
        
        private void OnQuestFailedHandler(ActiveQuest quest)
        {
            // Remove from active
            _activeQuests.Remove(quest);
            
            // Add to failed
            _failedQuests.Add(quest.QuestData);
            
            // Update UI
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
            
            // Fire events
            OnQuestFailed?.Invoke(quest);
            OnQuestListUpdated?.Invoke(_activeQuests);
            
            Debug.Log($"Quest failed: {quest.QuestData.Name}");
        }
        
        // ===== QUEST EVENT HANDLING =====
        public void OnEnemyDefeated(string enemyType)
        {
            foreach (var quest in _activeQuests)
            {
                quest.CheckEnemyDefeated(enemyType);
            }
        }
        
        public void OnItemCollected(ItemData itemData)
        {
            foreach (var quest in _activeQuests)
            {
                quest.CheckItemCollected(itemData);
            }
        }
        
        public void OnFloorChanged(int floorNumber)
        {
            foreach (var quest in _activeQuests)
            {
                quest.CheckFloorReached(floorNumber);
            }
        }
        
        public void OnNPCInteraction(string npcId)
        {
            foreach (var quest in _activeQuests)
            {
                quest.CheckNPCInteraction(npcId);
            }
        }
        
        public void OnLocationDiscovered(Vector2Int location, string sceneName)
        {
            foreach (var quest in _activeQuests)
            {
                quest.CheckLocationDiscovered(location, sceneName);
            }
        }
        
        public void OnDungeonCompleted(string dungeonName)
        {
            foreach (var quest in _activeQuests)
            {
                quest.CheckDungeonCompleted(dungeonName);
            }
        }
        
        // ===== HELPER METHODS =====
        private void AwardQuestRewards(ActiveQuest quest)
        {
            // Award XP
            var playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
            if (playerStats != null && quest.QuestData.XPReward > 0)
            {
                playerStats.AddExperience(quest.QuestData.XPReward);
                GameManager.Instance.Ui.AddLogEntry(
                    $"Quest complete! Gained {quest.QuestData.XPReward} XP",
                    Color.yellow
                );
            }
            
            // Award gold
            if (quest.QuestData.GoldReward > 0)
            {
                // This would depend on your currency system
                // For now, just log it
                GameManager.Instance.Ui.AddLogEntry(
                    $"Quest complete! Gained {quest.QuestData.GoldReward} gold",
                    Color.yellow
                );
            }
            
            // Award items
            if (quest.QuestData.ItemRewards != null && GameManager.Instance.Inventory != null)
            {
                foreach (var itemReward in quest.QuestData.ItemRewards)
                {
                    // Check drop chance
                    if (UnityEngine.Random.value <= itemReward.DropChance)
                    {
                        GameManager.Instance.Inventory.AddItem(itemReward.Item, itemReward.Quantity);
                        GameManager.Instance.Ui.AddLogEntry(
                            $"Quest complete! Received {itemReward.Quantity}x {itemReward.Item.ItemName}",
                            Color.yellow
                        );
                    }
                }
            }
        }
        
        private void CheckForFollowUpQuests(QuestData completedQuest)
        {
            if (completedQuest.FollowUpQuests == null) return;
            
            foreach (var followUpId in completedQuest.FollowUpQuests)
            {
                var followUpQuest = GetQuestById(followUpId);
                if (followUpQuest != null && CanStartQuest(followUpQuest))
                {
                    // Auto-start follow-up quest
                    StartQuest(followUpQuest);
                }
            }
        }
        
        public QuestData GetQuestById(string questId)
        {
            foreach (var quest in _availableQuests)
            {
                if (quest.QuestId == questId)
                {
                    return quest;
                }
            }
            
            // Also check in resources folder
            var quests = Resources.LoadAll<QuestData>("Quests");
            foreach (var quest in quests)
            {
                if (quest.QuestId == questId)
                {
                    return quest;
                }
            }
            
            return null;
        }
        
        private ActiveQuest GetActiveQuest(QuestData questData)
        {
            foreach (var quest in _activeQuests)
            {
                if (quest.QuestData == questData)
                {
                    return quest;
                }
            }
            
            return null;
        }
        
        public bool IsQuestActive(QuestData questData)
        {
            return GetActiveQuest(questData) != null;
        }
        
        public bool IsQuestCompleted(QuestData questData)
        {
            return _completedQuests.Contains(questData);
        }
        
        public bool IsQuestFailed(QuestData questData)
        {
            return _failedQuests.Contains(questData);
        }

        public void Reset()
        {
            _activeQuests.Clear();
            _completedQuests.Clear();
            _failedQuests.Clear();
            
            // Update UI
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
            OnQuestListUpdated?.Invoke(_activeQuests);
        }
        
        // ===== UPDATE LOOP =====
        private void Update()
        {
            // Update quest timers
            foreach (var quest in _activeQuests)
            {
                quest.Update();
            }
        }
        
        // ===== SAVE/LOAD =====
        [Serializable]
        public class QuestSaveData
        {
            public ActiveQuest[] ActiveQuests;
            public string[] CompletedQuestIds;
            public string[] FailedQuestIds;
        }
        
        public QuestSaveData GetSaveData()
        {
            return new QuestSaveData
            {
                ActiveQuests = _activeQuests.ToArray(),
                CompletedQuestIds = GetCompletedQuestIds(),
                FailedQuestIds = GetFailedQuestIds()
            };
        }
        
        public void LoadSaveData(QuestSaveData data)
        {
            if (data == null) return;
            
            // Clear current state
            _activeQuests.Clear();
            _completedQuests.Clear();
            _failedQuests.Clear();
            
            // Load active quests
            if (data.ActiveQuests != null)
            {
                foreach (var quest in data.ActiveQuests)
                {
                    if (quest != null)
                    {
                        _activeQuests.Add(quest);
                        
                        // Resubscribe to events
                        quest.OnProgressUpdated += OnQuestProgressUpdatedHandler;
                        quest.OnCompleted += OnQuestCompletedHandler;
                        quest.OnFailed += OnQuestFailedHandler;
                    }
                }
            }
            
            // Load completed quests
            if (data.CompletedQuestIds != null)
            {
                foreach (var questId in data.CompletedQuestIds)
                {
                    var quest = GetQuestById(questId);
                    if (quest != null)
                    {
                        _completedQuests.Add(quest);
                    }
                }
            }
            
            // Load failed quests
            if (data.FailedQuestIds != null)
            {
                foreach (var questId in data.FailedQuestIds)
                {
                    var quest = GetQuestById(questId);
                    if (quest != null)
                    {
                        _failedQuests.Add(quest);
                    }
                }
            }
            
            // Update UI
            GameManager.Instance.Ui.UpdateQuestLog(_activeQuests);
            OnQuestListUpdated?.Invoke(_activeQuests);
        }
        
        private string[] GetCompletedQuestIds()
        {
            var ids = new string[_completedQuests.Count];
            for (int i = 0; i < _completedQuests.Count; i++)
            {
                ids[i] = _completedQuests[i].QuestId;
            }
            return ids;
        }
        
        private string[] GetFailedQuestIds()
        {
            var ids = new string[_failedQuests.Count];
            for (int i = 0; i < _failedQuests.Count; i++)
            {
                ids[i] = _failedQuests[i].QuestId;
            }
            return ids;
        }
        
        // ===== CLEANUP =====
        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
            
            // Unsubscribe from all quest events
            foreach (var quest in _activeQuests)
            {
                quest.OnProgressUpdated -= OnQuestProgressUpdatedHandler;
                quest.OnCompleted -= OnQuestCompletedHandler;
                quest.OnFailed -= OnQuestFailedHandler;
            }
        }
        
        // ===== EDITOR HELPERS =====
        #if UNITY_EDITOR
        [ContextMenu("Print Quest Status")]
        private void PrintQuestStatus()
        {
            Debug.Log($"=== QUEST STATUS ===");
            Debug.Log($"Active Quests: {_activeQuests.Count}");
            Debug.Log($"Completed Quests: {_completedQuests.Count}");
            Debug.Log($"Failed Quests: {_failedQuests.Count}");
            
            foreach (var quest in _activeQuests)
            {
                Debug.Log($"- {quest.QuestData.Name}: {quest.ProgressPercentage:P0} complete");
            }
            
            Debug.Log($"====================");
        }
        #endif
    }
}