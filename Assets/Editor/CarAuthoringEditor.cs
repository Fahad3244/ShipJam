using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CarAuthoring))]
public class CarAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CarAuthoring carAuthoring = (CarAuthoring)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Update CarType from Car Component"))
        {
            Car car = carAuthoring.GetComponent<Car>();
            if (car != null)
            {
                carAuthoring.carType = car.carType; 
                EditorUtility.SetDirty(carAuthoring);
                Debug.Log("CarType updated from Car component!");
            }
            else
            {
                Debug.LogWarning("No 'Car' component found on this GameObject.");
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Generate Straight Path In Front"))
        {
            CreateStraightPath(carAuthoring);
        }
    }

    private void CreateStraightPath(CarAuthoring carAuthoring)
    {
        if (carAuthoring.pathRoot == null)
        {
            GameObject pathRootObj = new GameObject(carAuthoring.name + "_PathRoot");
            pathRootObj.transform.SetParent(carAuthoring.transform);
            pathRootObj.transform.localPosition = Vector3.zero;
            carAuthoring.pathRoot = pathRootObj.transform;
        }
        else
        {
            for (int i = carAuthoring.pathRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(carAuthoring.pathRoot.GetChild(i).gameObject);
            }
        }

        int pointCount = 5;
        float spacing = 2f;

        // ✅ First point 2 units forward instead of 1
        Vector3 firstPointPos = carAuthoring.transform.position + carAuthoring.transform.forward * 2f;

        for (int i = 0; i < pointCount; i++)
        {
            GameObject point = new GameObject("Waypoint_" + i);
            point.transform.SetParent(carAuthoring.pathRoot);
            point.transform.position = firstPointPos + carAuthoring.transform.forward * (i * spacing);
        }

        EditorUtility.SetDirty(carAuthoring);
        Debug.Log("Straight Path Generated In Front with 2-unit offset!");
    }
}
