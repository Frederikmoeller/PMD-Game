using System;
using DialogueSystem;
using DialogueSystem.Data;

using UnityEngine;

namespace DialogueSystem
{
    public static class DialogueActionSystem
    {

        public static event Action<ActionEvent> OnUnknownAction; 
        public static void Run(ActionEvent action)
        {
            if (action == null || string.IsNullOrEmpty(action.Id))
            {
                Debug.LogWarning("Null or empty action provided");
                return;
            }

            if (action.Args == null)
            {
                Debug.LogWarning($"Action '{action.Id}' has null args");
                return;
            }

            switch (action.Id.ToLower())
            {
                // Variable actions
                case "set_variable":
                    if (action.Args.Length < 2) return;
                    DialogueVariables.Set(action.Args[0], action.Args[1]);
                    break;
                    
                case "set_int":
                    if (action.Args.Length < 2) return;
                    if (int.TryParse(action.Args[1], out int intValue))
                        DialogueVariables.SetInt(action.Args[0], intValue);
                    break;
                    
                case "set_bool":
                    if (action.Args.Length < 2) return;
                    if (bool.TryParse(action.Args[1], out bool boolValue))
                        DialogueVariables.SetBool(action.Args[0], boolValue);
                    break;
                    
                case "set_float":
                    if (action.Args.Length < 2) return;
                    if (float.TryParse(action.Args[1], out float floatValue))
                        DialogueVariables.SetFloat(action.Args[0], floatValue);
                    break;
                    
                case "increment":
                    if (action.Args.Length < 1) return;
                    int incrementAmount = action.Args.Length > 1 ? int.Parse(action.Args[1]) : 1;
                    DialogueVariables.Increment(action.Args[0], incrementAmount);
                    break;
                    
                case "decrement":
                    if (action.Args.Length < 1) return;
                    int decrementAmount = action.Args.Length > 1 ? int.Parse(action.Args[1]) : 1;
                    DialogueVariables.Decrement(action.Args[0], decrementAmount);
                    break;
                    
                case "delete_variable":
                    if (action.Args.Length < 1) return;
                    DialogueVariables.Delete(action.Args[0]);
                    break;

                default:
                    OnUnknownAction?.Invoke(action);
                    break;
            }
        }
        
        // Helper to run multiple actions
        public static void RunAll(ActionEvent[] actions)
        {
            if (actions == null) return;
            
            foreach (var action in actions)
            {
                Run(action);
            }
        }
    }
}