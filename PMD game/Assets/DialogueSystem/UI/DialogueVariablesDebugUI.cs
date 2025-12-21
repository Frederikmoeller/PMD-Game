// DialogueSystem/UI/DialogueVariablesDebugUI.cs
using UnityEngine;
using TMPro;
using DialogueSystem.Data;

namespace DialogueSystem.UI
{
    public class DialogueVariablesDebugUi : MonoBehaviour
    {
        [SerializeField] private bool _showInBuild = false;
        [SerializeField] private TMP_Text _debugText;
        [SerializeField] private float _refreshRate = 1f;
        
        private void Start()
        {
            if (!_showInBuild && !Application.isEditor)
            {
                gameObject.SetActive(false);
                return;
            }
            
            InvokeRepeating(nameof(UpdateDebugDisplay), 0f, _refreshRate);
        }
        
        private void UpdateDebugDisplay()
        {
            if (_debugText != null)
            {
                _debugText.text = "Dialogue Variables:\n";
                var variables = DialogueVariables.GetAll();
                foreach (var kvp in variables)
                {
                    _debugText.text += $"{kvp.Key}: {kvp.Value}\n";
                }
            }
        }
        
        [ContextMenu("Print Variables to Console")]
        public void PrintToConsole()
        {
            DialogueVariables.PrintAll();
        }
    }
}