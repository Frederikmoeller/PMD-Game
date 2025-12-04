using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem;
using DialogueSystem.Data;
using UnityEditor.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueGraphEditor : EditorWindow
    {
        private DialogueGraphView _graphView;
        private string _currentAssetPath;
        public DialogueAsset CurrentAsset => _loadedAsset;
        private DialogueAsset _loadedAsset;
        
        // Tab system variables
        private VisualElement _mainContainer;
        private Button _graphTabButton;
        private Button _definitionsTabButton;
        private VisualElement _graphViewContainer;
        private VisualElement _definitionsContainer;

        [MenuItem("Window/Dialogue System/Dialogue Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditor>();
            window.titleContent = new GUIContent("Dialogue Editor");
        }

        private void OnEnable()
        {
            CreateTabSystem();
            ShowGraphView(); // Start with graph view visible
        }

        private void CreateTabSystem()
        {
            // Remove existing elements
            rootVisualElement.Clear();

            // Create main container
            _mainContainer = new VisualElement();
            _mainContainer.style.flexGrow = 1;
            rootVisualElement.Add(_mainContainer);

            // Create toolbar (now part of main container)
            ContructToolbar();

            // Create tab buttons
            var tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            tabContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            tabContainer.style.paddingLeft = 10;
            _mainContainer.Add(tabContainer);

            _graphTabButton = new Button(ShowGraphView) { text = "Dialogue Graph" };
            _graphTabButton.style.width = 150;
            _graphTabButton.style.height = 30;
            _graphTabButton.style.marginRight = 2;
            tabContainer.Add(_graphTabButton);

            _definitionsTabButton = new Button(ShowDefinitions) { text = "Definitions" };
            _definitionsTabButton.style.width = 150;
            _definitionsTabButton.style.height = 30;
            tabContainer.Add(_definitionsTabButton);

            // Create content containers
            _graphViewContainer = new VisualElement();
            _graphViewContainer.style.flexGrow = 1;
            
            _definitionsContainer = new VisualElement();
            _definitionsContainer.style.flexGrow = 1;
            _definitionsContainer.style.display = DisplayStyle.None; // Start hidden

            _mainContainer.Add(_graphViewContainer);
            _mainContainer.Add(_definitionsContainer);

            // Initialize graph view
            ContructGraphView();
        }

        private void ShowGraphView()
        {
            _graphViewContainer.style.display = DisplayStyle.Flex;
            _definitionsContainer.style.display = DisplayStyle.None;
            
            // Update tab appearance
            _graphTabButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            _graphTabButton.style.color = Color.white;
            _definitionsTabButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _definitionsTabButton.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        }

        private void ShowDefinitions()
        {
            _graphViewContainer.style.display = DisplayStyle.None;
            _definitionsContainer.style.display = DisplayStyle.Flex;
            
            // Update tab appearance
            _definitionsTabButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            _definitionsTabButton.style.color = Color.white;
            _graphTabButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _graphTabButton.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            // Refresh definitions UI when switching to this tab
            RefreshDefinitionsUI();
        }

        private void ContructGraphView()
        {
            _graphView = new DialogueGraphView(this)
            {
                name = "Dialogue Graph"
            };
            _graphView.StretchToParentSize();
            _graphViewContainer.Add(_graphView);
        }

        private void ContructToolbar()
        {
            var toolbar = new Toolbar();
            _mainContainer.Add(toolbar);
            
            // Asset field to load DialogueAsset
            var assetField = new ObjectField("Dialogue Asset")
            {
                objectType = typeof(DialogueAsset),
                allowSceneObjects = false,
                style = { width = 300 }
            };
            assetField.RegisterValueChangedCallback(evt =>
            {
                _loadedAsset = evt.newValue as DialogueAsset;
                if (_loadedAsset != null)
                {
                    _currentAssetPath = AssetDatabase.GetAssetPath(_loadedAsset);
                    _graphView.LoadFromAsset(_loadedAsset);
                }
                else
                {
                    _currentAssetPath = null;
                    _graphView.ClearGraph();
                }
            });
            toolbar.Add(assetField);
            
            // Create new DialogueAsset
            var newButton = new Button(() =>
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Asset", "NewDialogue", "asset",
                    "Create Dialogue asset");
                if (string.IsNullOrEmpty(path)) return;
                var newAsset = ScriptableObject.CreateInstance<DialogueAsset>();
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newAsset;
                assetField.value = newAsset;
            }) { text = "Create New" };
            toolbar.Add(newButton);
            
            // Save
            var saveButton = new Button(() =>
            {
                if (_loadedAsset == null) return;
                DialogueGraphSaveUtility.SaveGraphToAsset(_graphView, _loadedAsset);
                EditorUtility.SetDirty(_loadedAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }) { text = "Save" };
            toolbar.Add(saveButton);
            
            // Refresh
            var refreshButton = new Button(() =>
            {
                if (_loadedAsset == null) return;
                _graphView.LoadFromAsset(_loadedAsset);
            }) { text = "Reload" };
            toolbar.Add(refreshButton);
            
            // Zoom to fit
            var fitButton = new Button(() => _graphView.FrameAll()) { text = "Frame All" };
            toolbar.Add(fitButton);

            // Minimap toggle
            var mmButton = new Button(() => _graphView.ToggleMinimap()) { text = "Toggle Minimap" };
            toolbar.Add(mmButton);
            
            // Definitions picker
            var defContainer = new VisualElement();
            defContainer.style.flexDirection = FlexDirection.Row;
            defContainer.style.alignItems = Align.Center;

            var defField = new ObjectField("Definitions")
            {
                objectType = typeof(DialogueDefinitions),
                allowSceneObjects = false,
                style = { width = 180 }
            };
            var defs = DialogueGraphSaveUtility.FindDefinitionsAsset();
            defField.value = defs;
            defField.RegisterValueChangedCallback(evt => 
            {
                DialogueGraphSaveUtility.SetDefinitions(evt.newValue as DialogueDefinitions);
                // Refresh definitions UI if we're on that tab
                if (_definitionsContainer.style.display == DisplayStyle.Flex)
                {
                    RefreshDefinitionsUI();
                }
            });
            defContainer.Add(defField);

            // Create new definitions button
            var createDefsBtn = new Button(() =>
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Dialogue Definitions",
                    "DialogueDefinitions",
                    "asset",
                    "Create new Dialogue Definitions");
                
                if (!string.IsNullOrEmpty(path))
                {
                    var newDefs = ScriptableObject.CreateInstance<DialogueDefinitions>();
                    AssetDatabase.CreateAsset(newDefs, path);
                    AssetDatabase.SaveAssets();
                    defField.value = newDefs;
                    DialogueGraphSaveUtility.SetDefinitions(newDefs);
                    RefreshDefinitionsUI();
                }
            }) { text = "New", style = { width = 50, marginLeft = 5 } };
            defContainer.Add(createDefsBtn);

            toolbar.Add(defContainer);
        }

        private void OnDisable()
        {
            // No need to manually remove _graphView since we're clearing the rootVisualElement
        }

        private void RefreshDefinitionsUI()
        {
            _definitionsContainer.Clear();
            
            var defs = DialogueGraphSaveUtility.Defs;
            if (defs == null)
            {
                _definitionsContainer.Add(new Label("No Definitions asset assigned."));
                return;
            }

            // Create scroll view for definitions
            var scrollView = new ScrollView();
            scrollView.style.paddingLeft = 15;
            scrollView.style.paddingRight = 15;
            scrollView.style.paddingTop = 10;
            scrollView.style.paddingBottom = 10;
            _definitionsContainer.Add(scrollView);

            // Conditions section
            var conditionsLabel = new Label("Conditions") { 
                style = { 
                    unityFontStyleAndWeight = FontStyle.Bold, 
                    fontSize = 16, 
                    marginTop = 10,
                    marginBottom = 10,
                    color = Color.white
                } 
            };
            scrollView.Add(conditionsLabel);
            
            for (int i = 0; i < defs.conditions.Count; i++)
            {
                AddConditionDefinitionUI(scrollView, i, defs.conditions[i]);
            }
            
            var addConditionBtn = new Button(() =>
            {
                defs.conditions.Add(new ConditionDefinition { 
                    id = "NEW_CONDITION", 
                    displayName = "New Condition" 
                });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(defs);
            }) { text = "Add Condition", style = { marginBottom = 20, marginTop = 10 } };
            scrollView.Add(addConditionBtn);

            // Actions section
            var actionsLabel = new Label("Actions") { 
                style = { 
                    unityFontStyleAndWeight = FontStyle.Bold, 
                    fontSize = 16, 
                    marginTop = 20,
                    marginBottom = 10,
                    color = Color.white
                } 
            };
            scrollView.Add(actionsLabel);
            
            for (int i = 0; i < defs.actions.Count; i++)
            {
                AddActionDefinitionUI(scrollView, i, defs.actions[i]);
            }
            
            var addActionBtn = new Button(() =>
            {
                defs.actions.Add(new ActionDefinition { 
                    id = "NEW_ACTION", 
                    displayName = "New Action" 
                });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(defs);
            }) { text = "Add Action", style = { marginTop = 10 } };
            scrollView.Add(addActionBtn);
        }

        private void AddConditionDefinitionUI(VisualElement parent, int index, ConditionDefinition condition)
        {       
            var container = new VisualElement 
            { 
                style = 
                { 
                    flexDirection = FlexDirection.Column,
                    marginBottom = 10, 
                    paddingBottom = 10,
                    paddingTop = 10,
                    paddingRight = 10,
                    paddingLeft = 10,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.2f),
                    borderLeftWidth = 3,
                    borderLeftColor = Color.cyan
                } 
            };
        
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        
            var idField = new TextField("ID") { value = condition.id, style = { flexGrow = 1 } };
            idField.RegisterValueChangedCallback(evt => 
            { 
                condition.id = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            header.Add(idField);
        
            var nameField = new TextField("Display Name") { value = condition.displayName, style = { flexGrow = 1, marginLeft = 5 } };
            nameField.RegisterValueChangedCallback(evt => 
            { 
                condition.displayName = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            header.Add(nameField);
        
            container.Add(header);
        
            // Arguments
            var argsLabel = new Label("Arguments:") { style = { marginTop = 5, unityFontStyleAndWeight = FontStyle.Bold } };
            container.Add(argsLabel);
        
            var argsContainer = new VisualElement();
            container.Add(argsContainer);
        
            for (int i = 0; i < condition.args.Count; i++)
            {
                int argIndex = i; // Capture the index
                AddArgumentDefinitionUi(argsContainer, i, condition.args[i], () =>
                {
                    condition.args.RemoveAt(argIndex);
                    RefreshDefinitionsUI();
                    EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
                });
            }
        
            var addArgBtn = new Button(() =>
            {
                condition.args.Add(new ArgumentDefinition { name = "new_arg", placeholder = "value" });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }) { text = "Add Argument" };
            container.Add(addArgBtn);
        
            var removeBtn = new Button(() =>
            {
                DialogueGraphSaveUtility.Defs.conditions.RemoveAt(index);
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }) { text = "Remove Condition", style = { marginTop = 5 } };
            container.Add(removeBtn);
        
            parent.Add(container);
        }
        
        private void AddActionDefinitionUI(VisualElement parent, int index, ActionDefinition action)
        {
            var container = new VisualElement 
            { 
                style = 
                { 
                    flexDirection = FlexDirection.Column,
                    marginBottom = 10, 
                    paddingTop = 10, 
                    paddingRight = 10, 
                    paddingLeft = 10, 
                    paddingBottom = 10, 
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.2f),
                    borderLeftWidth = 3,
                    borderLeftColor = Color.green
                } 
            };
        
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        
            var idField = new TextField("ID") { value = action.id, style = { flexGrow = 1 } };
            idField.RegisterValueChangedCallback(evt => 
            { 
                action.id = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            header.Add(idField);
        
            var nameField = new TextField("Display Name") { value = action.displayName, style = { flexGrow = 1, marginLeft = 5 } };
            nameField.RegisterValueChangedCallback(evt => 
            { 
                action.displayName = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            header.Add(nameField);
        
            container.Add(header);
        
            // Arguments
            var argsLabel = new Label("Arguments:") { style = { marginTop = 5, unityFontStyleAndWeight = FontStyle.Bold } };
            container.Add(argsLabel);
        
            var argsContainer = new VisualElement();
            container.Add(argsContainer);
        
            for (int i = 0; i < action.args.Count; i++)
            {
                int argIndex = i; // Capture the index
                AddArgumentDefinitionUi(argsContainer, i, action.args[i], () =>
                {
                    action.args.RemoveAt(argIndex);
                    RefreshDefinitionsUI();
                    EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
                });
            }
        
            var addArgBtn = new Button(() =>
            {
                action.args.Add(new ArgumentDefinition { name = "new_arg", placeholder = "value" });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }) { text = "Add Argument" };
            container.Add(addArgBtn);
        
            var removeBtn = new Button(() =>
            {
                DialogueGraphSaveUtility.Defs.actions.RemoveAt(index);
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }) { text = "Remove Action", style = { marginTop = 5 } };
            container.Add(removeBtn);
        
            parent.Add(container);
        }
        
        private void AddArgumentDefinitionUi(VisualElement parent, int index, ArgumentDefinition arg, Action onRemove)
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5, alignItems = Align.Center } };
        
            var nameField = new TextField() { value = arg.name, style = { flexGrow = 1 } };
            nameField.RegisterValueChangedCallback(evt => 
            { 
                arg.name = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            container.Add(nameField);
        
            var placeholderField = new TextField() { value = arg.placeholder, style = { flexGrow = 1, marginLeft = 5 } };
            placeholderField.RegisterValueChangedCallback(evt => 
            { 
                arg.placeholder = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            container.Add(placeholderField);
        
            var removeBtn = new Button(() =>
            {
                onRemove?.Invoke(); // Call the removal callback
            }) { text = "X", style = { width = 25, height = 20, marginLeft = 5, unityTextAlign = TextAnchor.MiddleCenter } };
            container.Add(removeBtn);
        
            parent.Add(container);
        }
    }
}