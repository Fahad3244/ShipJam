using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private SerializedProperty columnsProp;
    private SerializedProperty containerCountProp;
    private string[] holeTypeNames;
    private int fillAllSelectedIndex = 0;

    private void OnEnable()
    {
        columnsProp = serializedObject.FindProperty("columns");
        containerCountProp = serializedObject.FindProperty("containerCount");

        try
        {
            holeTypeNames = Enum.GetNames(typeof(HoleType));
        }
        catch
        {
            holeTypeNames = new string[] { "None" };
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(containerCountProp);
        DrawDefaultInspectorFieldsExcept("columns", "containerCount");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Columns", EditorStyles.boldLabel);

        if (columnsProp == null)
        {
            EditorGUILayout.HelpBox("No columns property found.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        for (int i = 0; i < columnsProp.arraySize; i++)
        {
            SerializedProperty columnProp = columnsProp.GetArrayElementAtIndex(i);
            DrawColumnInspector(columnProp, i);
            EditorGUILayout.Space();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Column"))
        {
            columnsProp.InsertArrayElementAtIndex(columnsProp.arraySize);
            SerializedProperty newCol = columnsProp.GetArrayElementAtIndex(columnsProp.arraySize - 1);

            SerializedProperty rowCountProp = newCol.FindPropertyRelative("rowCount");
            rowCountProp.intValue = 1;
            SerializedProperty holesProp = newCol.FindPropertyRelative("holes");
            holesProp.arraySize = 1;
        }
        if (columnsProp.arraySize > 0 && GUILayout.Button("Remove Last Column"))
        {
            columnsProp.DeleteArrayElementAtIndex(columnsProp.arraySize - 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Cars Sync", EditorStyles.boldLabel);
        if (GUILayout.Button("Sync Cars from Scene"))
        {
            SyncCarsFromScene();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawColumnInspector(SerializedProperty columnProp, int index)
    {
        SerializedProperty rowCountProp = columnProp.FindPropertyRelative("rowCount");
        SerializedProperty holesProp = columnProp.FindPropertyRelative("holes");

        string title = $"Column {index + 1} (rows: {rowCountProp.intValue})";
        EditorGUILayout.LabelField(title, EditorStyles.foldoutHeader);

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(rowCountProp);

        if (holesProp.arraySize != rowCountProp.intValue)
        {
            holesProp.arraySize = Mathf.Max(0, rowCountProp.intValue);
        }

        EditorGUILayout.BeginHorizontal();
        fillAllSelectedIndex = EditorGUILayout.Popup(fillAllSelectedIndex, holeTypeNames);
        if (GUILayout.Button("Fill All"))
        {
            FillAllHoles(holesProp, fillAllSelectedIndex);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        for (int h = 0; h < holesProp.arraySize; h++)
        {
            SerializedProperty holeProp = holesProp.GetArrayElementAtIndex(h);
            SerializedProperty holeTypeProp = holeProp.FindPropertyRelative("holeType");
            SerializedProperty specialTypeProp = holeProp.FindPropertyRelative("specialType");
            SerializedProperty isMysteryProp = holeProp.FindPropertyRelative("isMystery");


            // Draw holeType and specialType on the same line
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            // Background color
            Color bg = GetColorForHoleType((HoleType)holeTypeProp.enumValueIndex);
            if (bg.a > 0f)
            {
                Color prev = GUI.color;
                GUI.color = new Color(bg.r, bg.g, bg.b, 0.2f);
                EditorGUI.DrawRect(rect, GUI.color);
                GUI.color = prev;
            }

            // Label
            Rect labelRect = new Rect(rect.x, rect.y, 80, rect.height);
            EditorGUI.LabelField(labelRect, $"Hole {h}");

            // HoleType dropdown
            Rect typeRect = new Rect(rect.x + 85, rect.y, 100, rect.height);
            holeTypeProp.enumValueIndex = EditorGUI.Popup(typeRect, holeTypeProp.enumValueIndex, holeTypeNames);

            // SpecialType dropdown
            Rect specialRect = new Rect(rect.x + 190, rect.y, 100, rect.height);
            EditorGUI.PropertyField(specialRect, specialTypeProp, GUIContent.none);


            // Mystery Toggle (manual layout: checkbox + label)
            Rect checkboxRect = new Rect(rect.x + 300, rect.y, 20, rect.height);
            Rect labelRect2 = new Rect(rect.x + 325, rect.y, 40, rect.height);

            isMysteryProp.boolValue = EditorGUI.Toggle(checkboxRect, isMysteryProp.boolValue); 
            EditorGUI.LabelField(labelRect2, "Myst");



        }

        EditorGUI.indentLevel--;
    }

    private void FillAllHoles(SerializedProperty holesProp, int holeTypeIndex)
    {
        for (int i = 0; i < holesProp.arraySize; i++)
        {
            SerializedProperty holeProp = holesProp.GetArrayElementAtIndex(i);
            SerializedProperty holeTypeProp = holeProp.FindPropertyRelative("holeType");
            holeTypeProp.enumValueIndex = holeTypeIndex;
        }
    }

    private Color GetColorForHoleType(HoleType type)
    {
        try
        {
            var cm = LevelManager.instance.colorManager;
            if (cm != null)
            {
                return cm.GetColorByType(type);
            }
        }
        catch { }
        return Color.magenta;
    }

    private void DrawDefaultInspectorFieldsExcept(params string[] exclude)
    {
        var excluded = new HashSet<string>(exclude);
        var so = serializedObject;
        SerializedProperty prop = so.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (excluded.Contains(prop.name)) continue;
            if (prop.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
                continue;
            }
            EditorGUILayout.PropertyField(prop, true);
        }
    }

    // ------------------- Scene Sync -------------------
    private void SyncCarsFromScene()
{
    LevelData levelData = (LevelData)target;

    if (levelData.cars == null)
        levelData.cars = new List<CarData>();

    levelData.cars.Clear();

    CarAuthoring[] sceneCars = GameObject.FindObjectsOfType<CarAuthoring>();

    if (sceneCars.Length == 0)
    {
        Debug.LogWarning("No CarAuthoring objects found in scene!");
        return;
    }

    foreach (var car in sceneCars)
    {
        if (car.carType == CarType.none) 
        {
            Debug.LogWarning($"Car '{car.name}' has no CarType assigned! Skipping.");
            continue;
        }

        CarData data = new CarData();
        data.carType = car.carType;
        data.startPos = car.transform.position;
        data.startRotation = car.transform.eulerAngles;

        var path = car.GetPathPoints();
        if (path.Count == 0)
            Debug.LogWarning($"Car '{car.name}' has no pathRoot or waypoints!");

        data.pathToJunction = path;
        levelData.cars.Add(data);

        Debug.Log($"Synced Car '{car.name}' with {path.Count} waypoints.");
    }

    EditorUtility.SetDirty(levelData);
    Debug.Log($"✅ Synced {levelData.cars.Count} cars into LevelData '{levelData.name}'");
}

}
