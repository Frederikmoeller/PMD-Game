using UnityEngine;
using DialogueSystem.Data;

public class NpcController : MonoBehaviour
{
    public DialogueAsset DialogueData; // Assuming DialogueAsset is your dialogue data type
    public string CharacterName = "NPC";
    public Sprite CharacterPortrait;
    
    void Start()
    {
        var gridEntity = GetComponent<GridEntity>();
        if (gridEntity != null)
        {
            gridEntity.Type = EntityType.Npc;
        }
    }
}
