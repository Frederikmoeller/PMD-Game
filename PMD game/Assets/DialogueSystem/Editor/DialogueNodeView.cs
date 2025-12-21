// Assets/DialogueSystem/Editor/DialogueNodeView.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    public class DialogueNodeView : Node
    {
        public DialogueLine NodeData { get; private set; }
        private DialogueGraphView _graphView;

        private Port _inputPort;
        private Port _nextPort;
        private List<Port> _choicePorts = new List<Port>();
        private VisualElement _choicesContainer;
        private VisualElement _conditionActionContainer;

        public DialogueNodeView(DialogueLine data, DialogueGraphView graphView = null)
        {
            _graphView = graphView;
            NodeData = data ?? DialogueGraphSaveUtility.CreateDialogueLine();
            title = string.IsNullOrEmpty(NodeData.TextKey) ? "New Node" : NodeData.TextKey;
            viewDataKey = NodeData.Guid;

            InitializePorts();
            BuildMainContent();
            RefreshExpandedState();
            RefreshPorts();
        }

        #region Ports
        private void InitializePorts()
        {
            _inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            _inputPort.portName = "In";
            inputContainer.Add(_inputPort);

            _nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            _nextPort.portName = "Next";
            outputContainer.Add(_nextPort);
    
            // Register with edge connector
            if (_graphView != null)
                _graphView.RegisterPortToEdgeConnector(_nextPort);
        }

        public Port GetInputPort() => _inputPort;
        public Port GetNextPort() => _nextPort;
        public Port GetChoicePort(int index) => (index >= 0 && index < _choicePorts.Count) ? _choicePorts[index] : null;
        #endregion

        private void BuildMainContent()
        {
            // Title is already set
            var speakerField = new TextField("Speaker ID")
            {
                value = NodeData.SpeakerId
            };
            speakerField.RegisterValueChangedCallback(evt =>
            {
                NodeData.SpeakerId = evt.newValue;
                UpdateNodeTitle();
            });
            mainContainer.Add(speakerField);

            var textKeyField = new TextField("Text Key")
            {
                value = NodeData.TextKey
            };
            textKeyField.RegisterValueChangedCallback(evt =>
            {
                NodeData.TextKey = evt.newValue;
                UpdateNodeTitle();
            });
            mainContainer.Add(textKeyField);

            // Next vs Choices toggle area
            var addChoiceBtn = new Button(AddChoice) { text = "Add Choice" };
            mainContainer.Add(addChoiceBtn);

            // Choices container
            _choicesContainer = new VisualElement();
            _choicesContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.Add(_choicesContainer);

            // Condition/action container (shared for node-level commands)
            _conditionActionContainer = new VisualElement();
            _conditionActionContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.Add(_conditionActionContainer);

            // Fill current mode UI
            RefreshModeUI();
        }

        void UpdateNodeTitle()
        {
            string key = string.IsNullOrEmpty(NodeData.TextKey) ? "New Node" : NodeData.TextKey;
            string speaker = string.IsNullOrEmpty(NodeData.SpeakerId) ? "" : $"{NodeData.SpeakerId}: ";
            title = speaker + key;
        }
        
        private void RefreshModeUI()
        {
            // Collect edges that will be disconnected before changing ports
            var edgesToRemove = new List<Edge>();
    
            if (NodeData.Choices.Length > 0 && _nextPort != null)
            {
                edgesToRemove.AddRange(_nextPort.connections);
                // Clear the nextNodeId since we're switching to choice mode
                if (!string.IsNullOrEmpty(NodeData.NextNodeId))
                {
                    Debug.Log($"Clearing nextNodeId '{NodeData.NextNodeId}' when switching to choice mode");
                    NodeData.NextNodeId = null;
                }
            }
    
            // 2. Remove edges from any choice ports that will be removed
            // This covers both reducing choice count AND switching to next port mode (where choices.Length == 0)
            for (int i = NodeData.Choices.Length; i < _choicePorts.Count; i++)
            {
                edgesToRemove.AddRange(_choicePorts[i].connections);
            }
            
            // Always clear and rebuild choices UI
            _choicesContainer.Clear();
            for (int i = 0; i < NodeData.Choices.Length; i++)
            {
                AddChoiceUI(i);
            }

            // Clear output container and rebuild based on choices count
            outputContainer.Clear();

            if (NodeData.Choices.Length > 0)
            {
                // CHOICE MODE: Create choice ports
                _nextPort = null; // Clear next port in choice mode
        
                // Ensure we have the right number of choice ports
                while (_choicePorts.Count < NodeData.Choices.Length)
                {
                    var cp = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    cp.portName = $"Choice {_choicePorts.Count}";
                    _choicePorts.Add(cp);
                    outputContainer.Add(cp);

                    // Register with edge connector
                    if (_graphView != null)
                        _graphView.RegisterPortToEdgeConnector(cp);
                }
        
                // Remove excess choice ports
                while (_choicePorts.Count > NodeData.Choices.Length)
                {
                    _choicePorts.RemoveAt(_choicePorts.Count - 1);
                }
        
                // Add current choice ports to output container with choice key as name
                for (int i = 0; i < _choicePorts.Count; i++)
                {
                    var choice = NodeData.Choices[i];
                    var portName = string.IsNullOrEmpty(choice.TextKey) ? $"Choice {i}" : choice.TextKey;
                    _choicePorts[i].portName = portName;
                    outputContainer.Add(_choicePorts[i]);
                }
            }
            else
            {
                // NEXT PORT MODE: Use single next port
                _choicePorts.Clear(); // Clear all choice ports
        
                if (_nextPort == null)
                {
                    _nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    _nextPort.portName = "Next";
                }
                outputContainer.Add(_nextPort);
            }

            RefreshExpandedState();
            RefreshPorts();
    
            // Remove the disconnected edges
            if (_graphView != null && edgesToRemove.Count > 0)
            {
                foreach (var edge in edgesToRemove)
                {
                    _graphView.RemoveElement(edge);
                }
                Debug.Log($"Removed {edgesToRemove.Count} disconnected edges due to port mode change");
            }
        }
        
        private Port GetMatchingPortForEdge(Edge edge)
        {
            if (NodeData.Choices.Length == 0)
            {
                return _nextPort;
            }
            else
            {
                // Try to find which choice port this edge should connect to
                // This is complex - you might need to store which choice index an edge belongs to
                return _choicePorts.Count > 0 ? _choicePorts[0] : null;
            }
        }

        private void RefreshChoicesUi()
        {
            _choicesContainer.Clear();
            for (int i = 0; i < NodeData.Choices.Length; i++)
            {
                AddChoiceUI(i);
            }
        }
        
        private Port CreateNextPort(Orientation orientation)
        {
            return InstantiatePort(
                orientation,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool)
            );
        }

        private void AddChoiceUI(int index)
        {
            var choice = NodeData.Choices[index];

            var choiceBox = new VisualElement { style = { flexDirection = FlexDirection.Column, borderLeftWidth = 1 } };

            var choiceKey = new TextField("Choice Key") { value = choice.TextKey };
            choiceKey.RegisterValueChangedCallback(evt => 
            { 
                choice.TextKey = evt.newValue;
                // Update the port name when choice key changes
                UpdateChoicePortName(index);
            });
            choiceBox.Add(choiceKey);

            // Conditions foldout
            var condFold = new Foldout { text = "Conditions" };
            BuildConditionsUi(condFold, choice);
            choiceBox.Add(condFold);

            // Actions foldout
            var actFold = new Foldout { text = "Actions" };
            BuildActionsUi(actFold, choice);
            choiceBox.Add(actFold);

            var targetField = new TextField("Target GUID") { value = choice.NextNodeId };
            targetField.RegisterValueChangedCallback(evt => choice.NextNodeId = evt.newValue);
            choiceBox.Add(targetField);

            var removeBtn = new Button(() =>
            {
                RemoveChoiceAt(index);
            }) { text = "Remove Choice" };
            choiceBox.Add(removeBtn);

            _choicesContainer.Add(choiceBox);
        }

        // Add this helper method to update individual port names
        private void UpdateChoicePortName(int index)
        {
            if (index >= 0 && index < _choicePorts.Count)
            {
                var choice = NodeData.Choices[index];
                var portName = string.IsNullOrEmpty(choice.TextKey) ? $"Choice {index}" : choice.TextKey;
                _choicePorts[index].portName = portName;
        
                // Refresh the port visual
                _choicePorts[index].MarkDirtyRepaint();
            }
        }

        private void BuildConditionsUi(VisualElement fieldParent, DialogueChoice choice)
        {
            fieldParent.Clear();

            // List each condition in the choice
            choice.Conditions ??= Array.Empty<Condition>();

            for (int i = 0; i < choice.Conditions.Length; i++)
            {
                int conditionIndex = i;
                var cond = choice.Conditions[i];
                // Main container for this condition
                var conditionContainer = new VisualElement { 
                    style = { 
                        flexDirection = FlexDirection.Column,
                        marginBottom = 10,
                        paddingTop = 5,
                        paddingBottom = 5,
                        paddingLeft = 5,
                        paddingRight = 5,
                        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f),
                        borderLeftWidth = 2,
                        borderLeftColor = Color.cyan
                    } 
                };
                
                var selectorRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };

                // Check if this is using a standard condition or custom
                bool isUsingStandard = string.IsNullOrEmpty(cond.CustomConditionId);
        
                if (isUsingStandard)
                {
                    // Use ENUM dropdown for standard conditions
                    var standardConditions = Enum.GetValues(typeof(StandardCondition)).Cast<StandardCondition>().ToList();
                    var currentValue = cond.StandardCondition;
                    var pop = new PopupField<StandardCondition>(standardConditions, currentValue)
                    {
                        style =
                        {
                            flexGrow = 1
                        },
                        label = "Condition Type"
                    };

                    pop.RegisterValueChangedCallback(evt =>
                    {
                        cond.StandardCondition = evt.newValue;
                        cond.CustomConditionId = null; // Clear custom ID
                
                        // Update args based on the standard condition
                        UpdateConditionArgs(cond);
                        MarkAssetDirty();
                        BuildConditionsUi(fieldParent, choice); // Rebuild to show updated args
                    });
                    selectorRow.Add(pop);
                }
                else
                {
                    // Use STRING dropdown for custom conditions (from DialogueDefinitions)
                    var defs = DialogueGraphSaveUtility.Defs;
                    if (defs != null)
                    {
                        var conditionChoices = defs.Conditions.ConvertAll(d => d.DisplayName);
                        var currentIndex = defs.Conditions.FindIndex(c => c.Id == cond.CustomConditionId);
                        var pop = new PopupField<string>(conditionChoices, currentIndex >= 0 ? currentIndex : 0)
                        {
                            style =
                            {
                                flexGrow = 1
                            },
                            label = "Condition Type"
                        };

                        pop.RegisterValueChangedCallback(evt =>
                        {
                            var selectedDef = defs.Conditions.Find(c => c.DisplayName == evt.newValue);
                            if (selectedDef != null)
                            {
                                cond.CustomConditionId = selectedDef.Id;
                                cond.StandardCondition = default; // Clear standard condition
                        
                                // Update args based on the custom condition
                                UpdateConditionArgs(cond);
                                MarkAssetDirty();
                                BuildConditionsUi(fieldParent, choice);
                            }
                        });
                        selectorRow.Add(pop);
                    }
                    else
                    {
                        // Fallback: text field for custom ID
                        var textField = new TextField("Condition ID") { value = cond.CustomConditionId, style = { flexGrow = 1 } };
                        textField.RegisterValueChangedCallback(evt =>
                        {
                            cond.CustomConditionId = evt.newValue;
                            cond.StandardCondition = default;
                            
                            UpdateConditionArgs(cond);
                            MarkAssetDirty();
                            BuildConditionsUi(fieldParent, choice);
                        });
                        selectorRow.Add(textField);
                    }
                }

                // Toggle button to switch between standard and custom
                var toggleBtn = new Button(() =>
                {
                    if (isUsingStandard)
                    {
                        // Switch to custom
                        var defs = DialogueGraphSaveUtility.Defs;
                        cond.CustomConditionId = defs?.Conditions.Count > 0 ? defs.Conditions[0].Id : "custom_condition";
                        cond.StandardCondition = default;
                    }
                    else
                    {
                        // Switch to standard
                        cond.StandardCondition = StandardCondition.VariableExists;
                        cond.CustomConditionId = null;
                    }
                    UpdateConditionArgs(cond);
                    MarkAssetDirty();
                    BuildConditionsUi(fieldParent, choice);
                }) { text = isUsingStandard ? "Custom" : "Standard", style = { width = 70, marginLeft = 5 } };
                selectorRow.Add(toggleBtn);

                var remove = new Button(() =>
                {
                    var list = choice.Conditions.ToList();
                    list.RemoveAt(conditionIndex);
                    choice.Conditions = list.ToArray();
                    UpdateConditionArgs(cond);
                    MarkAssetDirty();
                    BuildConditionsUi(fieldParent, choice);
                }) { text = "X", style = { width = 30, marginLeft = 5 } };
                selectorRow.Add(remove);

                conditionContainer.Add(selectorRow);

                // Show argument fields
                var argsContainer = new VisualElement { style = { marginTop = 5 } };
                ShowConditionArgsUi(argsContainer, cond);
                conditionContainer.Add(argsContainer);
                
                fieldParent.Add(conditionContainer);
            }

            var addBtn = new Button(() =>
            {
                var list = choice.Conditions.ToList();
                Array.Resize(ref choice.Conditions, list.Count + 1);
                // Start with a standard condition by default
                choice.Conditions[list.Count] = new Condition() 
                { 
                    StandardCondition = StandardCondition.VariableExists,
                    CustomConditionId = null,
                    Args = new string[0] 
                };
                BuildConditionsUi(fieldParent, choice);
            }) { text = "Add Condition" };
            fieldParent.Add(addBtn);
        }

        private void BuildActionsUi(VisualElement fieldParent, DialogueChoice choice)
        {
            fieldParent.Clear();

            choice.Actions ??= Array.Empty<ActionEvent>();

            for (int i = 0; i < choice.Actions.Length; i++)
            {
                int actionIndex = i;
                var act = choice.Actions[i];
        
                // Main container for this action
                var actionContainer = new VisualElement { 
                    style = { 
                        flexDirection = FlexDirection.Column,
                        marginBottom = 10,
                        paddingTop = 5,
                        paddingBottom = 5,
                        paddingLeft = 5,
                        paddingRight = 5,
                        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f),
                        borderLeftWidth = 2,
                        borderLeftColor = Color.cyan
                    } 
                };

                // First row: Action selector and buttons
                var selectorRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };

                // Check if this is using a standard action or custom
                bool isUsingStandard = string.IsNullOrEmpty(act.CustomActionId);
        
                if (isUsingStandard)
                {
                    // Use ENUM dropdown for standard actions
                    var standardActions = Enum.GetValues(typeof(StandardAction)).Cast<StandardAction>().ToList();
                    var currentValue = act.StandardAction;
                    var pop = new PopupField<StandardAction>(standardActions, currentValue);
                    pop.style.flexGrow = 1;
                    pop.label = "Action Type";
            
                    pop.RegisterValueChangedCallback(evt =>
                    {
                        act.StandardAction = evt.newValue;
                        act.CustomActionId = null;
                        UpdateActionArgs(act);
                        MarkAssetDirty();
                        BuildActionsUi(fieldParent, choice);
                    });
                    selectorRow.Add(pop);
                }
                else
                {
                    // Use STRING dropdown for custom actions
                    var defs = DialogueGraphSaveUtility.Defs;
                    if (defs != null)
                    {
                        var actionChoices = defs.Actions.ConvertAll(d => d.DisplayName);
                        var currentIndex = defs.Actions.FindIndex(a => a.Id == act.CustomActionId);
                        var pop = new PopupField<string>(actionChoices, currentIndex >= 0 ? currentIndex : 0)
                        {
                            style =
                            {
                                flexGrow = 1
                            },
                            label = "Action Type"
                        };

                        pop.RegisterValueChangedCallback(evt =>
                        {
                            var selectedDef = defs.Actions.Find(a => a.DisplayName == evt.newValue);
                            if (selectedDef != null)
                            {
                                act.CustomActionId = selectedDef.Id;
                                act.StandardAction = default;
                                UpdateActionArgs(act);
                                MarkAssetDirty();
                                BuildActionsUi(fieldParent, choice);
                            }
                        });
                        selectorRow.Add(pop);
                    }
                    else
                    {
                        // Fallback: text field for custom ID
                        var textField = new TextField("Action ID") { value = act.CustomActionId, style = { flexGrow = 1 } };
                        textField.RegisterValueChangedCallback(evt =>
                        {
                            act.CustomActionId = evt.newValue;
                            act.StandardAction = default;
                        });
                        selectorRow.Add(textField);
                    }
                }

                // Toggle button
                var toggleBtn = new Button(() =>
                {
                    if (isUsingStandard)
                    {
                        var defs = DialogueGraphSaveUtility.Defs;
                        act.CustomActionId = defs?.Actions.Count > 0 ? defs.Actions[0].Id : "custom_action";
                        act.StandardAction = default;
                    }
                    else
                    {
                        act.StandardAction = StandardAction.SetVariable;
                        act.CustomActionId = null;
                    }
                    UpdateActionArgs(act);
                    MarkAssetDirty();
                    BuildActionsUi(fieldParent, choice);
                }) { text = isUsingStandard ? "Use Custom" : "Use Standard", style = { width = 100, marginLeft = 5 } };
                selectorRow.Add(toggleBtn);

                // Remove button
                var remove = new Button(() =>
                {
                    var list = choice.Actions.ToList();
                    list.RemoveAt(actionIndex);
                    choice.Actions = list.ToArray();
                    UpdateActionArgs(act);
                    MarkAssetDirty();
                    BuildActionsUi(fieldParent, choice);
                }) { text = "Remove", style = { width = 70, marginLeft = 5 } };
                selectorRow.Add(remove);

                actionContainer.Add(selectorRow);

                // Second part: Argument fields
                var argsContainer = new VisualElement { style = { marginTop = 5 } };
                ShowActionArgsUi(argsContainer, act);
                actionContainer.Add(argsContainer);

                fieldParent.Add(actionContainer);
            }

            var addBtn = new Button(() =>
            {
                var list = choice.Actions.ToList();
                Array.Resize(ref choice.Actions, list.Count + 1);
                choice.Actions[list.Count] = new ActionEvent() 
                { 
                    StandardAction = StandardAction.SetVariable,
                    CustomActionId = null,
                    Args = Array.Empty<string>() 
                };
                UpdateActionArgs(choice.Actions[list.Count]);
                MarkAssetDirty();
                BuildActionsUi(fieldParent, choice);
            }) { text = "Add Action" };
            fieldParent.Add(addBtn);
}
        
        private void UpdateConditionArgs(Condition cond)
        {
            // Set up appropriate arguments based on the condition type
            if (string.IsNullOrEmpty(cond.CustomConditionId))
            {
                // Standard condition - set up default args
                switch (cond.StandardCondition)
                {
                    case StandardCondition.VariableExists:
                    case StandardCondition.VariableBool:
                        cond.Args = new[] { "" }; // variable name
                        break;
                    case StandardCondition.VariableEquals:
                    case StandardCondition.VariableNotEquals:
                    case StandardCondition.VariableGreater:
                    case StandardCondition.VariableLess:
                    case StandardCondition.VariableGreaterEqual:
                    case StandardCondition.VariableLessEqual:
                        cond.Args = new[] { "", "" }; // variable name, value
                        break;
                    case StandardCondition.VariableBetween:
                        cond.Args = new[] { "", "", "" }; // variable name, min, max
                        break;
                    default:
                        cond.Args = Array.Empty<string>();
                        break;
                }
            }
            else
            {
                // Custom condition - use DialogueDefinitions to determine args
                var defs = DialogueGraphSaveUtility.Defs;
                var def = defs?.GetConditionDef(cond.Id);
                cond.Args = def != null ? new string[def.Args.Count] : Array.Empty<string>();
            }
        }

        private void ShowConditionArgsUi(VisualElement fieldParent, Condition cond)
        {
            string conditionId = cond.Id;
    
            // Get argument definitions
            var defs = DialogueGraphSaveUtility.Defs;
            var def = defs?.GetConditionDef(conditionId);

            // For standard conditions, we'll create sensible argument fields
            if (string.IsNullOrEmpty(cond.CustomConditionId))
            {
                // Standard condition - create appropriate argument fields
                switch (cond.StandardCondition)
                {
                    case StandardCondition.VariableExists:
                    case StandardCondition.VariableBool:
                        AddArgumentField(fieldParent, "Variable Name", cond, 0, "Enter variable name");
                        break;
                
                    case StandardCondition.VariableEquals:
                    case StandardCondition.VariableNotEquals:
                        AddArgumentField(fieldParent, "Variable Name", cond, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Compare Value", cond, 1, "Value to compare against");
                        break;
                
                    case StandardCondition.VariableGreater:
                    case StandardCondition.VariableLess:
                    case StandardCondition.VariableGreaterEqual:
                    case StandardCondition.VariableLessEqual:
                        AddArgumentField(fieldParent, "Variable Name", cond, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Number Value", cond, 1, "Number to compare against");
                        break;
                
                    case StandardCondition.VariableBetween:
                        AddArgumentField(fieldParent, "Variable Name", cond, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Min Value", cond, 1, "Minimum value");
                        AddArgumentField(fieldParent, "Max Value", cond, 2, "Maximum value");
                        break;
                }
            }
            else
            {
                // Custom condition - use DialogueDefinitions
                int expectedArgCount = def?.Args.Count ?? 0;
        
                // Ensure Args array is properly sized
                if (cond.Args == null || cond.Args.Length != expectedArgCount)
                {
                    cond.Args = new string[expectedArgCount];
                }

                // Show argument fields based on definition
                for (int a = 0; a < expectedArgCount; a++)
                {
                    string argName = def?.Args[a].Name ?? $"Argument {a + 1}";
                    string placeholder = def?.Args[a].Placeholder ?? "Enter value";
            
                    AddArgumentField(fieldParent, argName, cond, a, placeholder);
                }
            }
        }

        // Helper method for creating argument fields
        private void AddArgumentField(VisualElement fieldParent, string label, Condition condition, int index, string placeholder)
        {
            // Ensure Args array exists and is properly sized
            if (condition.Args == null)
            {
                condition.Args = new string[index + 1];
            }
            else if (condition.Args.Length <= index)
            {
                Array.Resize(ref condition.Args, index + 1);
            }
    
            string argVal = condition.Args[index] ?? "";
    
            var field = new TextField(label) { 
                value = argVal,
                tooltip = placeholder
            };
    
            int captureIndex = index;
            field.RegisterValueChangedCallback(evt =>
            {
                condition.Args[captureIndex] = evt.newValue;
                MarkAssetDirty();
            });
    
            fieldParent.Add(field);
        }
        
        private void AddArgumentField(VisualElement fieldParent, string label, ActionEvent actionEvent, int index, string placeholder)
        {
            // Ensure Args array exists and is properly sized
            if (actionEvent.Args == null)
            {
                actionEvent.Args = new string[index + 1];
            }
            else if (actionEvent.Args.Length <= index)
            {
                Array.Resize(ref actionEvent.Args, index + 1);
            }
    
            string argVal = actionEvent.Args[index] ?? "";
    
            var field = new TextField(label) { 
                value = argVal,
                tooltip = placeholder
            };
    
            int captureIndex = index;
            field.RegisterValueChangedCallback(evt =>
            {
                actionEvent.Args[captureIndex] = evt.newValue;
                MarkAssetDirty();
            });
    
            fieldParent.Add(field);
        }

        private void MarkAssetDirty()
        {
            _graphView?.MarkAssetDirty();
        }

        private void UpdateActionArgs(ActionEvent act)
        {
            // Set up appropriate arguments based on the action type
            if (string.IsNullOrEmpty(act.CustomActionId))
            {
                // Standard action - set up default args
                switch (act.StandardAction)
                {
                    case StandardAction.SetVariable:
                    case StandardAction.SetInt:
                    case StandardAction.SetBool:
                    case StandardAction.SetFloat:
                        act.Args = new[] { "", "" }; // key, value
                        break;
                    case StandardAction.Increment:
                    case StandardAction.Decrement:
                        act.Args = new[] { "", "1" }; // key, amount (default 1)
                        break;
                    case StandardAction.DeleteVariable:
                        act.Args = new[] { "" }; // key
                        break;
                    default:
                        act.Args = Array.Empty<string>();
                        break;
                }
            }
            else
            {
                // Custom action - use DialogueDefinitions to determine args
                var defs = DialogueGraphSaveUtility.Defs;
                var def = defs?.GetActionDef(act.Id);
                act.Args = def != null ? new string[def.Args.Count] : Array.Empty<string>();
            }
        }

        private void ShowActionArgsUi(VisualElement fieldParent, ActionEvent act)
        {
            string actionId = act.Id;
    
            // Get argument definitions
            var defs = DialogueGraphSaveUtility.Defs;
            var def = defs?.GetActionDef(actionId);
    
            // For standard actions, we'll create sensible argument fields
            if (string.IsNullOrEmpty(act.CustomActionId))
            {
                // Standard action - create appropriate argument fields
                switch (act.StandardAction)
                {
                    case StandardAction.SetVariable:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Value", act, 1, "Value to set");
                        break;
                
                    case StandardAction.SetInt:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Integer Value", act, 1, "Number to set (e.g., 5)");
                        break;
                
                    case StandardAction.SetBool:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Boolean Value", act, 1, "true or false");
                        break;
                
                    case StandardAction.SetFloat:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Float Value", act, 1, "Decimal number (e.g., 3.14)");
                        break;
                
                    case StandardAction.Increment:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Amount (Optional)", act, 1, "Amount to add (default: 1)");
                        break;
                
                    case StandardAction.Decrement:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name");
                        AddArgumentField(fieldParent, "Amount (Optional)", act, 1, "Amount to subtract (default: 1)");
                        break;
                
                    case StandardAction.DeleteVariable:
                        AddArgumentField(fieldParent, "Variable Name", act, 0, "Enter variable name to delete");
                        break;
                }
            }
            else
            {
                // Custom action - use DialogueDefinitions
                int expectedArgCount = def?.Args.Count ?? 0;
        
                // Ensure Args array is properly sized
                if (act.Args == null || act.Args.Length != expectedArgCount)
                {
                    act.Args = new string[expectedArgCount];
                }

                // Show argument fields based on definition
                for (int a = 0; a < expectedArgCount; a++)
                {
                    string argName = def?.Args[a].Name ?? $"Argument {a + 1}";
                    string placeholder = def?.Args[a].Placeholder ?? "Enter value";
            
                    AddArgumentField(fieldParent, argName, act, a, placeholder);
                }
            }
        }

        #region Choice modifications
        private void RemoveChoiceAt(int index)
        {
            var list = NodeData.Choices.ToList();
            list.RemoveAt(index);
            NodeData.Choices = list.ToArray();
        
            // Simply refresh - the logic above will handle switching modes
            RefreshModeUI();
        }

        private void AddChoice()
        {
            var list = (NodeData.Choices ?? Array.Empty<DialogueChoice>()).ToList();
            list.Add(new DialogueChoice { 
                TextKey = "choice", 
                NextNodeId = "", 
                Conditions = Array.Empty<Condition>(), 
                Actions = Array.Empty<ActionEvent>()
            });
            NodeData.Choices = list.ToArray();
        
            // Simply refresh - the logic above will handle switching modes
            RefreshModeUI();
        }
        
        public int GetChoicePortIndex(Port port)
        {
            for (int i = 0; i < _choicePorts.Count; i++)
            {
                if (_choicePorts[i] == port)
                    return i;
            }
            return -1;
        }
        #endregion
    }
}
