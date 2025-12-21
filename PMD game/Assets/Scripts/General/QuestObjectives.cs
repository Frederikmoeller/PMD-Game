using System;
using UnityEngine;

namespace GameSystem
{
    [Serializable]
    public class QuestObjective
    {
        public string ObjectiveId;
        public QuestObjectiveType Type;
        public string Description;
        public string TargetId;
        public int RequiredAmount = 1;
        public int CurrentAmount = 0;
        public bool IsComplete => CurrentAmount >= RequiredAmount;
        
        // Location tracking
        public Vector2Int Location = Vector2Int.zero;
        public string SceneName = "";
        
        // State
        public bool IsActive = true;
        public bool IsOptional = false;
        
        // Events
        public event Action<QuestObjective> OnProgressUpdated;
        public event Action<QuestObjective> OnCompleted;
        
        public QuestObjective(QuestObjectiveData data)
        {
            ObjectiveId = data.ObjectiveId;
            Type = data.Type;
            Description = data.Description;
            TargetId = data.TargetId;
            RequiredAmount = data.RequiredAmount;
            Location = data.Location;
            SceneName = data.SceneName;
        }
        
        public bool UpdateProgress(int amount = 1)
        {
            if (!IsActive || IsComplete) return false;
            
            CurrentAmount = Mathf.Min(CurrentAmount + amount, RequiredAmount);
            
            OnProgressUpdated?.Invoke(this);
            
            if (IsComplete)
            {
                OnCompleted?.Invoke(this);
            }
            
            return IsComplete;
        }
        
        public void SetProgress(int amount)
        {
            if (!IsActive) return;
            
            CurrentAmount = Mathf.Clamp(amount, 0, RequiredAmount);
            OnProgressUpdated?.Invoke(this);
            
            if (IsComplete)
            {
                OnCompleted?.Invoke(this);
            }
        }
        
        public void Reset()
        {
            CurrentAmount = 0;
            IsActive = true;
        }
        
        public float GetProgressPercentage()
        {
            return RequiredAmount > 0 ? (float)CurrentAmount / RequiredAmount : 0f;
        }
        
        public string GetProgressText()
        {
            return $"{CurrentAmount}/{RequiredAmount}";
        }
        
        public QuestObjective Clone()
        {
            return new QuestObjective(new QuestObjectiveData
            {
                ObjectiveId = ObjectiveId,
                Type = Type,
                Description = Description,
                TargetId = TargetId,
                RequiredAmount = RequiredAmount,
                Location = Location,
                SceneName = SceneName
            })
            {
                CurrentAmount = CurrentAmount,
                IsActive = IsActive,
                IsOptional = IsOptional
            };
        }
    }
}