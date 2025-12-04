using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueSystem.Editor
{
    public class DialogueEdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly DialogueGraphView _graphView; // base class

        public DialogueEdgeConnectorListener(DialogueGraphView graphView)
        {
            _graphView = graphView;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            // Create a new node where the edge was dropped
            _graphView.CreateNodeFromEdgeDrop(edge, position);
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            _graphView.AddElement(edge);
            edge.userData = $"Edge from {edge.output.node.viewDataKey} to {edge.input.node.viewDataKey}";

            // Update the node data connections
            _graphView.UpdateNodeConnections(edge);
        }
    }
}
