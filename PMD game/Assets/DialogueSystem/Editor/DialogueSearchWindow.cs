using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView _graphView;

        public void Init(DialogueGraphView view)
        {
            _graphView = view;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var list = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"))
            };
            list.Add(new SearchTreeEntry(new GUIContent("Dialogue Node")) { userData = "DialogueNode" });
            return list;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if ((string)entry.userData == "DialogueNode")
            {
                _graphView.CreateNodeAtPosition(context.screenMousePosition);
                return true;
            }
            return false;
        }
    }
}