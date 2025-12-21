using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystem
{
    public class QuestEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI questNameText;
        [SerializeField] private TextMeshProUGUI questDescriptionText;
        [SerializeField] private TextMeshProUGUI questProgressText;
        [SerializeField] private Slider questProgressSlider;
        
        private ActiveQuest _quest;
        
        public void Initialize(ActiveQuest quest)
        {
            _quest = quest;
            
            if (questNameText != null)
                questNameText.text = _quest.QuestData.Name;
            
            if (questDescriptionText != null)
                questDescriptionText.text = _quest.QuestData.Description;
            
            UpdateProgress();
        }
        
        private void UpdateProgress()
        {
            if (_quest == null || _quest.Objectives == null) return;
            
            int completed = 0;
            int total = _quest.Objectives.Count;
            
            foreach (var objective in _quest.Objectives)
            {
                if (objective.IsComplete) completed++;
            }
            
            if (questProgressText != null)
                questProgressText.text = $"{completed}/{total}";
            
            if (questProgressSlider != null)
            {
                questProgressSlider.maxValue = total;
                questProgressSlider.value = completed;
            }
        }
    }
}
