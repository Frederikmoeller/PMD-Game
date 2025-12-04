// DialogueSystem/UI/DialogueVariablesDebugUI.cs
using UnityEngine;
using TMPro;
using DialogueSystem.Data;

namespace DialogueSystem.UI
{
    public class DialogueVariablesDebugUI : MonoBehaviour
    {
        [SerializeField] private bool showInBuild = false;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private float refreshRate = 1f;
        
        private void Start()
        {
            if (!showInBuild && !Application.isEditor)
            {
                gameObject.SetActive(false);
                return;
            }
            
            InvokeRepeating(nameof(UpdateDebugDisplay), 0f, refreshRate);
        }
        
        private void UpdateDebugDisplay()
        {
            if (debugText != null)
            {
                debugText.text = "Dialogue Variables:\n";
                var variables = DialogueVariables.GetAll();
                foreach (var kvp in variables)
                {
                    debugText.text += $"{kvp.Key}: {kvp.Value}\n";
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