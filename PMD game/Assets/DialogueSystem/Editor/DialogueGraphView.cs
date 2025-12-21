using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using DialogueSystem.Data;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueGraphView : GraphView
    {
        private readonly DialogueGraphEditor _editorWindow;
        private MiniMap _miniMap;
        private EdgeConnector<Edge> _edgeConnector;

        public DialogueGraphView(DialogueGraphEditor editorWindow)
        {
            _editorWindow = editorWindow;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Manually add the edge connector to the graph view
            this.AddManipulator(_edgeConnector);

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            graphViewChanged = OnGraphViewChanged;
        }
        
        public void RegisterPortToEdgeConnector(Port port)
        {
            if (port == null) return;

            // Create a new edge connector for this specific port
            var edgeConnectorListener = new DialogueEdgeConnectorListener(this);
            var edgeConnector = new EdgeConnector<Edge>(edgeConnectorListener);
            port.AddManipulator(edgeConnector);
        }
        
        // <<< Critical for making ports connectable >>>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            foreach (var port in ports)
            {
                if (port == startPort) continue;                     // cannot connect to itself
                if (port.direction == startPort.direction) continue; // must be opposite direction
                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Create Node", a => CreateNodeAtPosition(evt.localMousePosition));
        }

        public void CreateNodeAtPosition(Vector2 position)
        {
            var nodeData = DialogueGraphSaveUtility.CreateDialogueLine();
            var nodeView = new DialogueNodeView(nodeData, this);
            nodeView.SetPosition(new Rect(position, new Vector2(200, 150)));
            AddElement(nodeView);
            MarkAssetDirty();
        }

        public void LoadFromAsset(DialogueAsset asset)
        {
            graphElements.ForEach(RemoveElement);
            var startNode = GenerateEntryPointNode();
            AddElement(startNode);
    
            // Create node views for each DialogueLine
            if (asset.Nodes != null)
            {
                foreach (var node in asset.Nodes)
                {
                    var nv = new DialogueNodeView(node, this);
                    nv.SetPosition(new Rect(node.Position, new Vector2(200, 150)));
                    AddElement(nv);
                }
            }
    
            // Force a refresh of all nodes to ensure ports are created
            RefreshAllNodes();
    
            // Now reconnect edges
            ReconnectEdgesSimple(asset, startNode);
        }

        private void ReconnectEdgesSimple(DialogueAsset asset, StartNodeView startNode)
        {
            if (asset == null) return;
            
            var nodeViews = nodes.ToList().Where(n => n is DialogueNodeView).Cast<DialogueNodeView>().ToList();
            
            // Reconnect start node first
            if (!string.IsNullOrEmpty(asset.StartNodeId))
            {
                var startTarget = FindNodeViewByGuid(asset.StartNodeId);
                if (startTarget != null && startNode != null)
                {
                    CreateEdge(startNode.GetOutputPort(), startTarget.GetInputPort());
                    startNode.StartNodeId = asset.StartNodeId; // Update the StartNodeView's internal state
                }
            }
    
            foreach (var nv in nodeViews)
            {
                // Next node edge
                if (!string.IsNullOrEmpty(nv.NodeData.NextNodeId))
                {
                    var targetNode = FindNodeViewByGuid(nv.NodeData.NextNodeId);
                    if (targetNode != null && nv.GetNextPort() != null)
                    {
                        CreateEdge(nv.GetNextPort(), targetNode.GetInputPort());
                    }
                }
        
                // Choices edges
                if (nv.NodeData.Choices == null) continue;
                for (int i = 0; i < nv.NodeData.Choices.Length; i++)
                {
                    var choice = nv.NodeData.Choices[i];
                    if (string.IsNullOrEmpty(choice.NextNodeId)) continue;
                    var target = FindNodeViewByGuid(choice.NextNodeId);
                    if (target == null) continue;
                    var choicePort = nv.GetChoicePort(i);
                    if (choicePort != null)
                    {
                        CreateEdge(choicePort, target.GetInputPort());
                    }
                }
            }
        }

        private void RefreshAllNodes()
        {
            foreach (var node in nodes.ToList())
            {
                if (node is DialogueNodeView nodeView)
                {
                    // This will force the node to rebuild its ports
                    nodeView.MarkDirtyRepaint();
                    nodeView.RefreshPorts();
                }
            }
        }

        private DialogueNodeView FindNodeViewByGuid(string guid)
        {
            foreach (var n in nodes)
            {
                var dv = n as DialogueNodeView;
                if (dv == null) continue;
                if (dv.NodeData.Guid == guid) return dv;
            }

            return null;
        }

        public void ClearGraph()
        {
            graphElements.ForEach(RemoveElement);
            AddElement(GenerateEntryPointNode());
        }

        private void CreateEdge(Port outPort, Port inPort)
        {
            var edge = outPort.ConnectTo(inPort);
            AddElement(edge);
            edge.MarkDirtyRepaint();
        }

        private StartNodeView GenerateEntryPointNode()
        {
            var entry = new StartNodeView(this);
            entry.SetPosition(new Rect(10, 10, 150, 50));
            return entry;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change) 
        {
            // Handle newly created edges
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var outNode = edge.output.node as DialogueNodeView;
                    var inNode = edge.input.node as DialogueNodeView;

                    if (outNode == null || inNode == null)
                        continue;

                    // Determine if it's the main Next port
                    if (edge.output == outNode.GetNextPort())
                    {
                        outNode.NodeData.NextNodeId = inNode.NodeData.Guid;
                        Debug.Log($"Set nextNodeId for {outNode.NodeData.Guid} -> {inNode.NodeData.Guid}");
                    }
                    else
                    {
                        // Otherwise, it's a choice port
                        int choiceIndex = outNode.GetChoicePortIndex(edge.output);
                        if (choiceIndex >= 0)
                        {
                            outNode.NodeData.Choices[choiceIndex].NextNodeId = inNode.NodeData.Guid;
                            Debug.Log($"Set choice {choiceIndex} nextNodeId for {outNode.NodeData.Guid} -> {inNode.NodeData.Guid}");
                        }
                    }

                    // Optional: store edge userData for debugging
                    edge.userData = new { from = outNode.NodeData.Guid, to = inNode.NodeData.Guid };
                }
            }

            // Handle edge removal similarly
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        var outNode = edge.output.node as DialogueNodeView;
                        if (outNode == null) continue;

                        if (edge.output == outNode.GetNextPort())
                            outNode.NodeData.NextNodeId = null;
                        else
                        {
                            int idx = outNode.GetChoicePortIndex(edge.output);
                            if (idx >= 0)
                                outNode.NodeData.Choices[idx].NextNodeId = null;
                        }
                    }
                }
            }
            MarkAssetDirty();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return change;
        }
        
        public void ToggleMinimap()
        {
            if (_miniMap == null)
            {
                _miniMap = new MiniMap { anchored = true };
                _miniMap.SetPosition(new Rect(10, 30, 200, 140));
                Add(_miniMap);
            }
            else
            {
                Remove(_miniMap);
                _miniMap = null;
            }
        }
        
        public void CreateNodeFromEdgeDrop(Edge edge, Vector2 screenPosition)
        {
            Debug.Log($"CreateNodeFromEdgeDrop called at screen position: {screenPosition}");
    
            // Convert screen position to graph coordinates
            var graphPosition = this.contentViewContainer.WorldToLocal(screenPosition);
            Debug.Log($"Converted to graph position: {graphPosition}");
    
            // Create new node
            var nodeData = DialogueGraphSaveUtility.CreateDialogueLine();
            var nodeView = new DialogueNodeView(nodeData, this);
            nodeView.SetPosition(new Rect(graphPosition, new Vector2(200, 150)));
            AddElement(nodeView);
    
            Debug.Log($"Created new node with GUID: {nodeData.Guid}");
    
            // Connect the edge to the new node
            var newInputPort = nodeView.GetInputPort();
            if (newInputPort != null && edge.output != null)
            {
                Debug.Log("Connecting edge to new node...");
        
                // Create the edge properly
                var newEdge = edge.output.ConnectTo(newInputPort);
                AddElement(newEdge);
        
                // Update node data connections
                UpdateNodeConnections(newEdge);
        
                // Remove the temporary edge
                RemoveElement(edge);
                Debug.Log("Edge connected successfully!");
            }
            else
            {
                Debug.LogError("Failed to connect edge - null ports detected");
                Debug.Log($"newInputPort: {newInputPort}, edge.output: {edge.output}");
            }
            MarkAssetDirty();
        }

        public void UpdateNodeConnections(Edge edge)
        {
            var inputNode = edge.input.node as DialogueNodeView;
    
            if (edge.output.node is DialogueNodeView outputNode && inputNode != null)
            {
                if (edge.output == outputNode.GetNextPort())
                {
                    outputNode.NodeData.NextNodeId = inputNode.NodeData.Guid;
                }
                else
                {
                    int choiceIndex = outputNode.GetChoicePortIndex(edge.output);
                    if (choiceIndex >= 0)
                    {
                        outputNode.NodeData.Choices[choiceIndex].NextNodeId = inputNode.NodeData.Guid;
                    }
                }
            }
            else if (edge.output.node is StartNodeView startNode && inputNode != null)
            {
                startNode.StartNodeId = inputNode.NodeData.Guid;
                if (_editorWindow.CurrentAsset != null)
                {
                    _editorWindow.CurrentAsset.StartNodeId = inputNode.NodeData.Guid;
                }
            }
    
            // Mark asset as dirty
            MarkAssetDirty();
        }
        
        public void MarkAssetDirty()
        {
            if (_editorWindow?.CurrentAsset != null)
            {
                EditorUtility.SetDirty(_editorWindow.CurrentAsset);
                
                DialogueGraphSaveUtility.SaveGraphToAsset(this, _editorWindow?.CurrentAsset);
                // Debug.Log("Marked asset as dirty from GraphView");
        
                // Optional: Auto-save if you want immediate persistence
                // AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning("Cannot mark asset dirty: CurrentAsset is null");
            }
        }
    }
    public sealed class StartNodeView : Node
    {
        public string StartNodeId { get; set; }
        private readonly Port _outputPort;

        public StartNodeView(DialogueGraphView graphView = null)
        {
            var dialogueGraphView = graphView;
        
            title = "START";
            viewDataKey = "START_NODE";
            StartNodeId = "";
    
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;
    
            _outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            _outputPort.portName = "Next";
            outputContainer.Add(_outputPort);
        
            // Register with edge connector
            if (dialogueGraphView != null)
                dialogueGraphView.RegisterPortToEdgeConnector(_outputPort);
    
            RefreshExpandedState();
            RefreshPorts();
        }

        public Port GetOutputPort() => _outputPort;
    }
}
