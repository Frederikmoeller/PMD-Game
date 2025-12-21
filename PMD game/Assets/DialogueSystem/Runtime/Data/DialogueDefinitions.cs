using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Data
{
    [Serializable]
    public class ArgumentDefinition
    {
        public string Name;
        public string Placeholder; // helpful hint
    }

    [Serializable]
    public class ConditionDefinition
    {
        public string Id; // e.g. "HAS_ITEM"
        public string DisplayName;
        public List<ArgumentDefinition> Args = new List<ArgumentDefinition>();
    }

    [Serializable]
    public class ActionDefinition
    {
        public string Id; // e.g. "GIVE_ITEM"
        public string DisplayName;
        public List<ArgumentDefinition> Args = new List<ArgumentDefinition>();
    }

    [CreateAssetMenu(menuName = "Dialogue/Definitions", fileName = "DialogueDefinitions")]
    public class DialogueDefinitions : ScriptableObject
    {
        public List<ConditionDefinition> Conditions = new List<ConditionDefinition>();
        public List<ActionDefinition> Actions = new List<ActionDefinition>();

        // Helpers
        public ConditionDefinition GetConditionDef(string id)
        {
            return Conditions.Find(c => c.Id == id);
        }

        public ActionDefinition GetActionDef(string id)
        {
            return Actions.Find(a => a.Id == id);
        }
    }
}
