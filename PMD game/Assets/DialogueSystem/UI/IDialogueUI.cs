using System;
using System.Collections.Generic;
using DialogueSystem.Data;

namespace DialogueSystem.UI
{
    public interface IDialogueUi
    {
        void ShowLine(DialogueLine line);
        void ShowChoices(List<DialogueChoice> choices);
        void HideAll();
    }
}
