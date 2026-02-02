#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LevelExporter : MonoBehaviour
{
    [MenuItem("IntoTheHole/Export Current Scene To LevelData")]
    public static void ExportLevelData()
    {
        // Create new LevelData asset
        LevelData level = ScriptableObject.CreateInstance<LevelData>();

        // Find all CarAuthoring objects in the scene
        CarAuthoring[] cars = GameObject.FindObjectsOfType<CarAuthoring>();

        foreach (var car in cars)
        {
            CarData carData = new CarData();
            carData.carType = car.carType;
            carData.startPos = car.transform.position;
            carData.startRotation = car.transform.eulerAngles;
            carData.pathToJunction = car.GetPathPoints();

            level.cars.Add(carData);
        }

        // Save asset
        string path = "Assets/Levels/NewLevel.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Exported LevelData with {level.cars.Count} cars to {path}");
    }
}
#endif
