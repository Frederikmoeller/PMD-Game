using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueSystem.Data
{
    public static class DialogueVariables
    {
        private static Dictionary<string, object> _variables = new();
        private static Dictionary<string, Action<string, object>> _onVariableChanged = new();

        public static void Set(string key, object value)
        {
            object oldValue = _variables.ContainsKey(key) ? _variables[key] : null;
            _variables[key] = value;
            
            Debug.Log($"Dialogue Variable Set: {key} = {value} (was: {oldValue})");
            
            // Trigger change events
            if (_onVariableChanged.ContainsKey(key))
            {
                _onVariableChanged[key]?.Invoke(key, value);
            }
        }

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            if (_variables.ContainsKey(key) && _variables[key] is T)
            {
                return (T)_variables[key];
            }

            return defaultValue;
        }

        public static bool Exists(string key)
        {
            return _variables.ContainsKey(key);
        }

        public static void Delete(string key)
        {
            if (_variables.ContainsKey(key))
            {
                _variables.Remove(key);
                Debug.Log($"Dialogue Variable Deleted: {key}");
            }
        }

        // Type-specific convenience methods
        public static void SetInt(string key, int value) => Set(key, value);
        public static int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);
        
        public static void SetBool(string key, bool value) => Set(key, value);
        public static bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);
        
        public static void SetString(string key, string value) => Set(key, value);
        public static string GetString(string key, string defaultValue = "") => Get(key, defaultValue);
        
        public static void SetFloat(string key, float value) => Set(key, value);
        public static float GetFloat(string key, float defaultValue = 0f) => Get(key, defaultValue);

        public static void Increment(string key, int amount = 1)
        {
            int current = GetInt(key, 0);
            SetInt(key, current + amount);
        }
        
        public static void Decrement(string key, int amount = 1)
        {
            int current = GetInt(key, 0);
            SetInt(key, current - amount);
        }

        public static void Subscribe(string key, Action<string, object> callback)
        {
            if (!_onVariableChanged.ContainsKey(key))
            {
                _onVariableChanged[key] = null;
            }

            _onVariableChanged[key] += callback;
        }

        public static void Unsubscribe(string key, Action<string, object> callback)
        {
            if (_onVariableChanged.ContainsKey(key))
            {
                _onVariableChanged[key] -= callback;
            }
        }

        public static void Clear()
        {
            _variables.Clear();
            _onVariableChanged.Clear();
            Debug.Log("All dialogue variables cleared");
        }
        
        public static void PrintAll()
        {
            Debug.Log("=== DIALOGUE VARIABLES ===");
            foreach (var kvp in _variables)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value} ({kvp.Value.GetType().Name})");
            }
            Debug.Log("==========================");
        }
        
        public static Dictionary<string, object> GetAll()
        {
            return new Dictionary<string, object>(_variables);
        }
        
        public static int Count()
        {
            return _variables.Count;
        }
        
        public static void SaveToDisk()
        {
            DialogueSystem.Save.DialogueSaveManager.SaveAllVariables(_variables);
        }

        public static void LoadFromDisk()
        {
            var loadedVars = DialogueSystem.Save.DialogueSaveManager.LoadAllVariables();
            foreach (var kvp in loadedVars)
            {
                _variables[kvp.Key] = kvp.Value;
            }
        }

        public static void SetWithSave(string key, object value)
        {
            Set(key, value);
            DialogueSystem.Save.DialogueSaveManager.SaveVariable(key, value);
        }
    }
}

