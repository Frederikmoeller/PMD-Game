using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Data
{
    [Serializable]
    public class ArgumentDefinition
    {
        public string name;
        public string placeholder; // helpful hint
    }

    [Serializable]
    public class ConditionDefinition
    {
        public string id; // e.g. "HAS_ITEM"
        public string displayName;
        public List<ArgumentDefinition> args = new List<ArgumentDefinition>();
    }

    [Serializable]
    public class ActionDefinition
    {
        public string id; // e.g. "GIVE_ITEM"
        public string displayName;
        public List<ArgumentDefinition> args = new List<ArgumentDefinition>();
    }

    [CreateAssetMenu(menuName = "Dialogue/Definitions", fileName = "DialogueDefinitions")]
    public class DialogueDefinitions : ScriptableObject
    {
        public List<ConditionDefinition> conditions = new List<ConditionDefinition>();
        public List<ActionDefinition> actions = new List<ActionDefinition>();

        // Helpers
        public ConditionDefinition GetConditionDef(string id)
        {
            return conditions.Find(c => c.id == id);
        }

        public ActionDefinition GetActionDef(string id)
        {
            return actions.Find(a => a.id == id);
        }
    }
}
