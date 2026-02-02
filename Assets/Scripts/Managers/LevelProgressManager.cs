using UnityEngine;
using System.Collections.Generic; // Needed for List<LevelData> indirectly via AllLevelsData

[CreateAssetMenu(fileName = "LevelProgressManager", menuName = "ScriptableObjects/Level Progress Manager")]
public class LevelProgressManager : ScriptableObject
{
    [Header("Level Data Storage")]
    public AllLevelsData allLevelsData; // Assign your AllLevelsData ScriptableObject here

    private const string CurrentLevelIndexKey = "CurrentLevelIndex";
    private int _currentLevelIndex = 0; // Backing field for property

    // Event to notify when a new level needs to be loaded by its index
    public event System.Action<int> OnLoadLevelRequested;
    // Event to notify UI or other systems about level number changes
    public event System.Action<int, int> OnLevelNumberChanged;

    public int CurrentLevelIndex
    {
        get { return _currentLevelIndex; }
        private set
        {
            _currentLevelIndex = value;
            // Notify subscribers about the updated level number
            if (allLevelsData != null && allLevelsData.levels.Count > 0)
            {
                OnLevelNumberChanged?.Invoke(_currentLevelIndex + 1, allLevelsData.levels.Count);
            }
        }
    }

    /// <summary>
    /// Initializes the manager by loading the saved level index.
    /// Should be called once at game start.
    /// </summary>
    public void Initialize()
    {
        LoadCurrentLevelIndex();
        Debug.Log($"LevelProgressManager Initialized. Loaded current level index: {CurrentLevelIndex}");
    }

    /// <summary>
    /// Loads the saved level index from PlayerPrefs or defaults to 0.
    /// </summary>
    private void LoadCurrentLevelIndex()
    {
        CurrentLevelIndex = PlayerPrefs.GetInt(CurrentLevelIndexKey, 0); // Default to 0 if not found
    }

    /// <summary>
    /// Saves the current level index to PlayerPrefs.
    /// </summary>
    private void SaveCurrentLevelIndex()
    {
        PlayerPrefs.SetInt(CurrentLevelIndexKey, CurrentLevelIndex);
        PlayerPrefs.Save(); // Ensure data is written to disk
        Debug.Log($"Saved current level index: {CurrentLevelIndex}");
    }

    /// <summary>
    /// Requests the loading of a specific level by its index.
    /// Notifies listeners via OnLoadLevelRequested event.
    /// </summary>
    public void RequestLevelLoad(int index)
    {
        if (allLevelsData == null || allLevelsData.levels.Count == 0)
        {
            Debug.LogError("No AllLevelsData assigned or no levels found in LevelProgressManager!");
            return;
        }

        if (index < 0 || index >= allLevelsData.levels.Count)
        {
            Debug.LogWarning($"Level index {index} is out of bounds. Looping to first level.");
            index = 0; // Loop back to first level or handle as "game completed"
        }

        CurrentLevelIndex = index; // Update internal index
        SaveCurrentLevelIndex();   // Save the new index

        Debug.Log($"LevelProgressManager requesting load of Level: {CurrentLevelIndex + 1}");
        OnLoadLevelRequested?.Invoke(CurrentLevelIndex); // Notify listeners
    }

    /// <summary>
    /// Called when the current level is successfully completed.
    /// Increments level index and requests the next level to be loaded.
    /// </summary>
    public void LevelCompleted()
    {
        Debug.Log($"Level {CurrentLevelIndex + 1} Completed! Requesting next level...");
        CurrentLevelIndex++; // Increment for the next level
        SaveCurrentLevelIndex(); // Save the updated index
        //RequestLevelLoad(CurrentLevelIndex); // Request loading the next level
    }

    /// <summary>
    /// Allows external systems to get the LevelData for the current level.
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        if (allLevelsData == null || allLevelsData.levels.Count == 0 || CurrentLevelIndex < 0 || CurrentLevelIndex >= allLevelsData.levels.Count)
        {
            Debug.LogError("Cannot get current level data: AllLevelsData not set or index out of bounds.");
            return null;
        }
        return allLevelsData.levels[CurrentLevelIndex];
    }
}