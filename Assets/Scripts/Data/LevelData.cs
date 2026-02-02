using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class HoleProperties
{
    public HoleType holeType;
    public SpecialHoleType specialType;
    public bool isMystery = false;
}

[System.Serializable]
public class ColumnData
{
    public int rowCount;
    public HoleProperties[] holes;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (holes == null || holes.Length != rowCount)
        {
            var newHoles = new HoleProperties[rowCount];
            if (holes != null)
            {
                for (int i = 0; i < Mathf.Min(holes.Length, newHoles.Length); i++)
                    newHoles[i] = holes[i];
            }
            for (int i = 0; i < newHoles.Length; i++)
                if (newHoles[i] == null) newHoles[i] = new HoleProperties();
            holes = newHoles;
        }
    }
#endif

}

[System.Serializable]
public class CarData
{
    public CarType carType;
    public Vector3 startPos;
    public Vector3 startRotation;
    public List<Vector3> pathToJunction = new List<Vector3>();
}



[CreateAssetMenu(fileName = "LevelData", menuName = "IntoTheHole/LevelData")]
public class LevelData : ScriptableObject
{
    public ColumnData[] columns;

    [Header("Container Settings")]
    public int containerCount = 3;
    [Header("Timer Settings")]
    public bool isTimedLevel = true;
    public int LevelCompletedTime = 30;

    [Header("Cars")]
    public List<CarData> cars = new List<CarData>();

    public int columnCount => columns != null ? columns.Length : 0;
}
