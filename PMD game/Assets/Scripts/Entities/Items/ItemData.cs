using System;
using UnityEngine;
using UnityEngine.Events;

public enum ItemType
{
    Money,
    Key,
    Quest,
    Consumable,
    Food,
}
[CreateAssetMenu(fileName = "ItemData", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{
    public Sprite Icon;
    public string ItemId;
    public string ItemName;
    public string ItemDescription;
    public ItemType Type;
    public EquipmentSlot EquipmentSlot = EquipmentSlot.None;
    public int ItemValue;
    public int Quantity;
    public int MaxStack;
    public bool Sellable;
    public EquipmentStats ItemStats;

    public UnityEvent ItemEffect;
}

[Serializable]
public class EquipmentStats
{
    public int HealthBonus;
    public int ManaBonus;
    public int AttackBonus;
    public int DefenseBonus;
}
