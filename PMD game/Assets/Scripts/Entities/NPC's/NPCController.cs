using UnityEngine;
using DialogueSystem.Data;

public class NPCController : MonoBehaviour
{
    public DialogueAsset dialogueData; // Assuming DialogueAsset is your dialogue data type
    public string CharacterName = "NPC";
    public Sprite CharacterPortrait;
    
    void Start()
    {
        var gridEntity = GetComponent<GridEntity>();
        if (gridEntity != null)
        {
            gridEntity.Type = EntityType.NPC;
        }
    }
}
