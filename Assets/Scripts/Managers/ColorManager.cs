using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class HoleColorMapping
{
    public HoleType holeType;
    public Color color;
    
    public HoleColorMapping(HoleType type, Color col)
    {
        holeType = type;
        color = col;
    }
}

[CreateAssetMenu(fileName = "ColorManager", menuName = "ScriptableObjects/Color Manager")]
public class ColorManager : ScriptableObject
{
    public List<HoleColorMapping> holeColorMappings = new List<HoleColorMapping>
    {
        new HoleColorMapping(HoleType.Red, Color.red),
    };

    public Color GetColorByType(HoleType type)
    {
        HoleColorMapping mapping = holeColorMappings.FirstOrDefault(m => m.holeType == type);
        return mapping != null ? mapping.color : Color.magenta; // Magenta as a fallback for unmapped types
    }
}