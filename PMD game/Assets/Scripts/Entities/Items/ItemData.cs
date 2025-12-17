using UnityEngine;
using UnityEngine.Events;

public enum ItemType
{
    Money,
    Key,
    Quest,
    Consumable,
    Food,
    Equipment,
    
}
[CreateAssetMenu(fileName = "ItemData", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{
    public Sprite Icon;
    public string ItemName;
    public string ItemDescription;
    public ItemType Type;
    public int ItemValue;
    public bool Sellable;

    public UnityEvent ItemEffect;
}
