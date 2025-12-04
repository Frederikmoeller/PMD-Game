using DialogueSystem.Localization;
using UnityEngine;

namespace DialogueSystem.Data
{
    public interface IDialogueDataLoader
    {
        bool CanLoad(string filePath);
        LocalizationDatabase Load(string filePath);
    }
}
