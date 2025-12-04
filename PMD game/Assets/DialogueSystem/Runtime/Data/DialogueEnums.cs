using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Data
{
    public enum StandardCondition
    {
        VariableExists,
        VariableEquals,
        VariableNotEquals,
        VariableGreater,
        VariableGreaterEqual,
        VariableLess, 
        VariableLessEqual,
        VariableBetween,
        VariableBool,
    }

    public enum StandardAction
    {
        // Variable actions
        SetVariable,
        SetInt,
        SetBool,
        SetFloat,
        Increment,
        Decrement,
        DeleteVariable
    }
    
    public static class ActionEnumConverter
    {
        private static readonly Dictionary<StandardCondition, string> _conditionMap = new()
        {
            { StandardCondition.VariableExists, "variable_exists" },
            { StandardCondition.VariableEquals, "variable_equals" },
            { StandardCondition.VariableNotEquals, "variable_not_equals" },
            { StandardCondition.VariableGreater, "variable_greater" },
            { StandardCondition.VariableGreaterEqual, "variable_greater_equal" },
            { StandardCondition.VariableLess, "variable_less" },
            { StandardCondition.VariableLessEqual, "variable_less_equal" },
            { StandardCondition.VariableBetween, "variable_between" },
            { StandardCondition.VariableBool, "variable_bool" },
        };
        
        private static readonly Dictionary<StandardAction, string> _actionMap = new()
        {
            { StandardAction.SetVariable, "set_variable" },
            { StandardAction.SetInt, "set_int" },
            { StandardAction.SetBool, "set_bool" },
            { StandardAction.SetFloat, "set_float" },
            { StandardAction.Increment, "increment" },
            { StandardAction.Decrement, "decrement" },
            { StandardAction.DeleteVariable, "delete_variable" },
        };
        
        public static string ToConditionId(this StandardCondition condition)
        {
            return _conditionMap.TryGetValue(condition, out string id) ? id : condition.ToString().ToLower();
        }
        
        public static string ToActionId(this StandardAction action)
        {
            return _actionMap.TryGetValue(action, out string id) ? id : action.ToString().ToLower();
        }
    }
}
