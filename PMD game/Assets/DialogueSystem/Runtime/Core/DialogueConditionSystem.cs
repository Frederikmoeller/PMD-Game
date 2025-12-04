using System;
using DialogueSystem;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem
{
    public static class DialogueConditionSystem
    {
        public static event Func<Condition, bool> OnUnknownCondition;
        public static bool Check(Condition condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.Id))
            {
                Debug.LogWarning("Null or empty condition provided");
                return true;
            }
            
            if (condition.Args == null)
            {
                Debug.LogWarning($"Condition '{condition.Id}' has null args");
                return false;
            }
            
            switch (condition.Id.ToLower())
            {
                // Variable conditions
                case "variable_exists":
                    return condition.Args.Length > 0 && DialogueVariables.Exists(condition.Args[0]);
                
                case "variable_between":
                    if (condition.Args.Length < 3) return false;
                    float betweenValue = DialogueVariables.Get<float>(condition.Args[0], 0f);
                    return betweenValue >= float.Parse(condition.Args[1]) && 
                           betweenValue <= float.Parse(condition.Args[2]);
                    
                case "variable_equals":
                    if (condition.Args.Length < 2) return false;
                    string stringValue = DialogueVariables.Get<string>(condition.Args[0], "");
                    return stringValue == condition.Args[1];
                    
                case "variable_greater":
                    if (condition.Args.Length < 2) return false;
                    float current = DialogueVariables.Get<float>(condition.Args[0], 0f);
                    return current > float.Parse(condition.Args[1]);
                    
                case "variable_less":
                    if (condition.Args.Length < 2) return false;
                    float currentVal = DialogueVariables.Get<float>(condition.Args[0], 0f);
                    return currentVal < float.Parse(condition.Args[1]);
                
                case "variable_greater_equal":
                    if (condition.Args.Length < 2) return false;
                    float geValue = DialogueVariables.Get<float>(condition.Args[0], 0f);
                    return geValue >= float.Parse(condition.Args[1]);
    
                case "variable_less_equal":
                    if (condition.Args.Length < 2) return false;
                    float leValue = DialogueVariables.Get<float>(condition.Args[0], 0f);
                    return leValue <= float.Parse(condition.Args[1]);

                case "variable_not_equals":
                    if (condition.Args.Length < 2) return false;
                    string neValue = DialogueVariables.Get<string>(condition.Args[0], "");
                    return neValue != condition.Args[1];
                    
                case "variable_bool":
                    if (condition.Args.Length < 1) return false;
                    return DialogueVariables.Get<bool>(condition.Args[0], false);

                default:
                    if (OnUnknownCondition != null)
                    {
                        return OnUnknownCondition(condition);
                    }
                    Debug.LogError($"Unknown condition type: {condition.Id}");
                    return false;
            }
        }

        public static bool CheckAll(Condition[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return true;

            foreach (var condition in conditions)
            {
                if (!Check(condition)) return false;
            }

            return true;
        }
    }
}
