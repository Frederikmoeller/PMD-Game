using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystem
{
    [Serializable]
    public class ActiveQuest
    {
        public QuestData QuestData;
        public List<QuestObjective> Objectives = new List<QuestObjective>();
        public QuestStatus Status = QuestStatus.NotStarted;
        public DateTime StartTime;
        public DateTime? CompletionTime;
        
        // Progress tracking
        public float TimeRemainingMinutes => CalculateTimeRemaining();
        public bool HasTimeLimit => QuestData.TimeLimitMinutes > 0;
        public bool IsTimedOut => HasTimeLimit && TimeRemainingMinutes <= 0;
        
        // Events
        public event Action<ActiveQuest> OnStatusChanged;
        public event Action<ActiveQuest> OnProgressUpdated;
        public event Action<ActiveQuest> OnCompleted;
        public event Action<ActiveQuest> OnFailed;
        
        // Properties
        public bool IsComplete
        {
            get
            {
                if (Objectives.Count == 0) return true;
                
                foreach (var objective in Objectives)
                {
                    if (!objective.IsOptional && !objective.IsComplete)
                        return false;
                }
                
                return true;
            }
        }
        
        public int CompletedObjectives
        {
            get
            {
                int count = 0;
                foreach (var objective in Objectives)
                {
                    if (objective.IsComplete) count++;
                }
                return count;
            }
        }
        
        public int TotalObjectives => Objectives.Count;
        
        public float ProgressPercentage
        {
            get
            {
                if (Objectives.Count == 0) return 1f;
                
                float total = 0f;
                foreach (var objective in Objectives)
                {
                    if (!objective.IsOptional)
                    {
                        total += objective.GetProgressPercentage();
                    }
                }
                
                // Count optional objectives as complete for percentage calculation
                int optionalCount = 0;
                foreach (var objective in Objectives)
                {
                    if (objective.IsOptional) optionalCount++;
                }
                
                int totalNonOptional = Objectives.Count - optionalCount;
                return totalNonOptional > 0 ? total / totalNonOptional : 1f;
            }
        }
        
        public ActiveQuest(QuestData questData)
        {
            QuestData = questData;
            InitializeObjectives();
            StartTime = DateTime.Now;
            Status = QuestStatus.InProgress;
        }
        
        private void InitializeObjectives()
        {
            Objectives.Clear();
            
            if (QuestData.Objectives == null) return;
            
            foreach (var objectiveData in QuestData.Objectives)
            {
                var objective = new QuestObjective(objectiveData);
                objective.OnProgressUpdated += OnObjectiveProgressUpdated;
                objective.OnCompleted += OnObjectiveCompleted;
                Objectives.Add(objective);
            }
        }
        
        private void OnObjectiveProgressUpdated(QuestObjective objective)
        {
            OnProgressUpdated?.Invoke(this);
            
            // Check if all objectives are complete
            if (IsComplete && Status == QuestStatus.InProgress)
            {
                Status = QuestStatus.ReadyToComplete;
                OnStatusChanged?.Invoke(this);
            }
        }
        
        private void OnObjectiveCompleted(QuestObjective objective)
        {
            // Log to game log
            GameManager.Instance?.Ui.AddLogEntry($"Objective complete: {objective.Description}", Color.yellow);
        }
        
        public bool UpdateObjective(string objectiveId, int amount = 1)
        {
            var objective = GetObjective(objectiveId);
            if (objective == null || !objective.IsActive) return false;
            
            return objective.UpdateProgress(amount);
        }
        
        public QuestObjective GetObjective(string objectiveId)
        {
            foreach (var objective in Objectives)
            {
                if (objective.ObjectiveId == objectiveId)
                    return objective;
            }
            return null;
        }
        
        public void CheckEnemyDefeated(string enemyType)
        {
            foreach (var objective in Objectives)
            {
                if (objective.Type == QuestObjectiveType.DefeatEnemies && 
                    objective.TargetId == enemyType && 
                    objective.IsActive)
                {
                    objective.UpdateProgress(1);
                }
            }
        }
        
        public void CheckItemCollected(ItemData itemData)
        {
            foreach (var objective in Objectives)
            {
                if (objective.Type == QuestObjectiveType.CollectItems && 
                    objective.TargetId == itemData.ItemId && 
                    objective.IsActive)
                {
                    objective.UpdateProgress(1);
                }
            }
        }
        
        public void CheckFloorReached(int floorNumber)
        {
            foreach (var objective in Objectives)
            {
                if (objective.Type == QuestObjectiveType.ReachFloor && 
                    floorNumber >= objective.RequiredAmount && 
                    objective.IsActive)
                {
                    objective.SetProgress(floorNumber);
                }
            }
        }
        
        public void CheckNPCInteraction(string npcId)
        {
            foreach (var objective in Objectives)
            {
                if (objective.Type == QuestObjectiveType.TalkToNPC && 
                    objective.TargetId == npcId && 
                    objective.IsActive)
                {
                    objective.UpdateProgress(1);
                }
            }
        }
        
        public void CheckLocationDiscovered(Vector2Int location, string sceneName)
        {
            foreach (var objective in Objectives)
            {
                if (objective.Type == QuestObjectiveType.DiscoverLocation && 
                    objective.Location == location && 
                    objective.SceneName == sceneName && 
                    objective.IsActive)
                {
                    objective.UpdateProgress(1);
                }
            }
        }
        
        public void CheckDungeonCompleted(string dungeonName)
        {
            foreach (var objective in Objectives)
            {
                if (objective.Type == QuestObjectiveType.CompleteDungeon && 
                    objective.TargetId == dungeonName && 
                    objective.IsActive)
                {
                    objective.UpdateProgress(1);
                }
            }
        }
        
        public void Complete()
        {
            if (Status != QuestStatus.ReadyToComplete && !IsComplete) return;
            
            Status = QuestStatus.Completed;
            CompletionTime = DateTime.Now;
            
            OnCompleted?.Invoke(this);
            OnStatusChanged?.Invoke(this);
        }
        
        public void Fail()
        {
            Status = QuestStatus.Failed;
            OnFailed?.Invoke(this);
            OnStatusChanged?.Invoke(this);
        }
        
        private float CalculateTimeRemaining()
        {
            if (!HasTimeLimit) return float.PositiveInfinity;
            
            float elapsedMinutes = (float)(DateTime.Now - StartTime).TotalMinutes;
            return Mathf.Max(0, QuestData.TimeLimitMinutes - elapsedMinutes);
        }
        
        public void Update()
        {
            // Check for timeouts
            if (HasTimeLimit && TimeRemainingMinutes <= 0 && Status == QuestStatus.InProgress)
            {
                Fail();
                GameManager.Instance?.Ui.AddLogEntry($"Quest failed: {QuestData.Name} (time limit expired)", Color.red);
            }
        }
        
        public ActiveQuest Clone()
        {
            var clone = new ActiveQuest(QuestData)
            {
                Status = Status,
                StartTime = StartTime,
                CompletionTime = CompletionTime
            };
            
            // Clone objectives
            clone.Objectives.Clear();
            foreach (var objective in Objectives)
            {
                var objectiveClone = objective.Clone();
                objectiveClone.OnProgressUpdated += clone.OnObjectiveProgressUpdated;
                objectiveClone.OnCompleted += clone.OnObjectiveCompleted;
                clone.Objectives.Add(objectiveClone);
            }
            
            return clone;
        }
    }
}