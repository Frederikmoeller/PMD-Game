using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonTemplate", menuName = "Dungeon/DungeonTemplate")]
public class DungeonTemplate : ScriptableObject
{
    public int Floors;
    public string Name;
    public string Description;
    public List<CharacterPresetSO> Enemies;
    public List<ItemData> Items;
    public int[] EnemyLevels;
    public AudioClip DungeonTheme;
    //Add TileSets so the dungeon can load the correct ones

}
