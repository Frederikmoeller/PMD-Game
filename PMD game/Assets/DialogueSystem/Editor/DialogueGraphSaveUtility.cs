// Assets/DialogueSystem/Editor/DialogueGraphSaveUtility.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    public static class DialogueGraphSaveUtility
    {
        public static DialogueDefinitions Defs { get; private set; }

        public static void SetDefinitions(DialogueDefinitions d)
        {
            Defs = d;
        }

        public static DialogueDefinitions FindDefinitionsAsset()
        {
            if (Defs != null) return Defs;
            string[] guids = AssetDatabase.FindAssets("t:DialogueDefinitions");
            if (guids == null || guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Defs = AssetDatabase.LoadAssetAtPath<DialogueDefinitions>(path);
            return Defs;
        }

        public static DialogueLine CreateDialogueLine()
        {
            var dl = new DialogueLine()
            {
                SpeakerId = "",
                TextKey = "",
                Choices = new DialogueChoice[0],
                NextNodeId = "",
            };
            dl.Guid = Guid.NewGuid().ToString();
            return dl;
        }

        public static void SaveGraphToAsset(DialogueGraphView graphView, DialogueAsset asset)
        {
            var nodeViews = graphView.nodes.ToList().OfType<DialogueNodeView>().ToList();
            var lines = new List<DialogueLine>();

            // Find start node and save its connection
            if (graphView.nodes.ToList().FirstOrDefault(n => n is StartNodeView) is StartNodeView startNode)
            {
                asset.startNodeId = startNode.StartNodeId;
            }
            else
            {
                asset.startNodeId = null; // No start node or no connection
            }

            foreach (var nv in nodeViews)
            {
                var data = nv.NodeData;
                var rect = nv.GetPosition();
                data.Position = rect.position;
                if (string.IsNullOrEmpty(data.Guid)) data.Guid = Guid.NewGuid().ToString();
                lines.Add(data);
            }

            asset.nodes = lines;
            EditorUtility.SetDirty(asset);
        }
    }
}
