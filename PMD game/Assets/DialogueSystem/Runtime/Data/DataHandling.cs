using UnityEngine;
using System;
using System.Collections.Generic;

namespace DialogueSystem.Data
{
    [Serializable]
    public class DialogueLine
    {
        public string Guid; // unique ID for editor/serialization use
        public Vector2 Position; // editor position (not used at runtime except for saving)
        public string SpeakerId;
        public string TextKey;
        public DialogueChoice[] Choices;
        public string NextNodeId;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string TextKey;
        public string NextNodeId;
        public Condition[] Conditions;
        public ActionEvent[] Actions;
    }

    [Serializable]
    public class Condition
    {
        public StandardCondition StandardCondition;
        public string CustomConditionId;
        public string[] Args;

        public string Id 
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomConditionId))
                    return CustomConditionId;
                    
                return StandardCondition.ToConditionId(); // This uses your converter!
            }
            set
            {
                // This is the magic setter that makes it work!
                if (Enum.TryParse<StandardCondition>(value, true, out var stdCond))
                {
                    StandardCondition = stdCond;
                    CustomConditionId = null;
                }
                else
                {
                    CustomConditionId = value;
                    StandardCondition = default;
                }
            }
        }
    }

    [Serializable]
    public class ActionEvent
    {
        public StandardAction StandardAction;
        public string CustomActionId;
        public string[] Args;

        public string Id 
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomActionId))
                    return CustomActionId;
                    
                return StandardAction.ToActionId(); // This uses your converter!
            }
            set
            {
                if (Enum.TryParse<StandardAction>(value, true, out var stdAct))
                {
                    StandardAction = stdAct;
                    CustomActionId = null;
                }
                else
                {
                    CustomActionId = value;
                    StandardAction = default;
                }
            }
        }
    }
}
