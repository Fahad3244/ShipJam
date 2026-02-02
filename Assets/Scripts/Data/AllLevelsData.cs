using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AllLevels", menuName = "ScriptableObjects/All Levels Data", order = 1)]
public class AllLevelsData : ScriptableObject
{
    public List<LevelData> levels;
}