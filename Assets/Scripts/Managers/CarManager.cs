using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // Needed for FirstOrDefault


public enum CarType
{
    none,
    car1_red,
    car1_yellow,
    car1_purple,
    car1_pink,
    car1_lightpink,
    car1_orange,
    car1_blue,
    car1_lightblue,
    car1_darkblue,
    car1_green,
    car1_lightgreen,
    car1_darkgreen,
    car1_lightbrown,
    car1_darkbrown,
    car2_red,
    car2_yellow,
    car2_purple,
    car2_pink,
    car2_lightpink,
    car2_orange,
    car2_blue,
    car2_lightblue,
    car2_darkblue,
    car2_green,
    car2_lightgreen,
    car2_darkgreen,
    car2_lightbrown,
    car2_darkbrown,
    car3_red,
    car3_yellow,
    car3_purple,
    car3_pink,
    car3_lightpink,
    car3_orange,
    car3_blue,
    car3_lightblue,
    car3_darkblue,
    car3_green,
    car3_lightgreen,
    car3_darkgreen,
    car3_lightbrown,
    car3_darkbrown,
    car4_red,
    car4_yellow,
    car4_purple,
    car4_pink,
    car4_lightpink,
    car4_orange,
    car4_blue,
    car4_lightblue,
    car4_darkblue,
    car4_green,
    car4_lightgreen,
    car4_darkgreen,
    car4_lightbrown,
    car4_darkbrown
}
public class CarManager : MonoBehaviour
{
    public static CarManager Instance;
    public bool isFallbackActive = false;
    [Header("Car Settings")]
    public int carFallAnimationStyle;
    public float totalFallDuration = 1.2f; // Reduced for faster fall
    public float fallDepth = 6f;
    public float initialFallSpeed = 0.3f; // How fast it starts falling
    public float gravityAcceleration = 2f; // How much it accelerates
    public Ease movementEase = Ease.InOutCubic;
    public List<Car> carPrefabs;   // Assign prefabs by HoleType in inspector

    private List<Car> _activeCars = new List<Car>(); // Runtime spawned cars
    public IReadOnlyList<Car> ActiveCars => _activeCars; // Public read-only access

    // Reference to the LevelManager to notify about car removal for level completion checks
    private LevelManager _levelManager;

    // Initialize with a reference to the LevelManager
    public void Init(LevelManager levelManager)
    {
        _levelManager = levelManager;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Spawns cars based on the provided LevelData.
    /// </summary>
    public void SpawnCars(LevelData levelData)
    {
        // Clear old cars first
        foreach (var car in _activeCars)
        {
            if (car != null) Destroy(car.gameObject);
        }
        _activeCars.Clear();

        foreach (var carData in levelData.cars)
        {
            // Find prefab for this car type
            Car prefab = carPrefabs.FirstOrDefault(c => c.carType == carData.carType);
            if (prefab == null)
            {
                Debug.LogWarning($"No prefab assigned for car type {carData.carType}");
                continue;
            }

            // Instantiate car
            Car car = Instantiate(prefab, carData.startPos, Quaternion.Euler(carData.startRotation), transform); // Parent to CarManager for organization
            car.Init(carData);
            car.CarManager = this; // Give car a reference back to this manager for callbacks

            // NEW: Set LevelManager reference on car for ProcessAllMatches callback
            car.SetLevelManager(_levelManager);

            _activeCars.Add(car);
        }

        Debug.Log($"Spawned {_activeCars.Count} cars from LevelData");
    }

    /// <summary>
    /// Called by a Car when it is successfully removed (e.g., reaches a hole).
    /// </summary>
    public void OnCarRemoved(Car car)
    {
        if (_activeCars.Contains(car))
            _activeCars.Remove(car);

        Debug.Log($"Car removed. {_activeCars.Count} cars remaining.");

        if (_activeCars.Count == 0)
        {
            Debug.Log("All cars removed! Level completion check needed.");
            // Notify LevelManager or LevelProgressManager that cars are all gone
            _levelManager?.OnCarRemovalComplete(); // LevelManager will then call LevelProgressManager.LevelCompleted()
        }
    }

    /// <summary>
    /// Finds the closest available car of a specific type that has an unblocked path to a given position.
    /// </summary>
    public Car FindClosestUnblockedCar(HoleType type, Vector3 holePos)
    {
        Car closest = null;
        float minDist = float.MaxValue;
        int carLayer = LayerMask.GetMask("Car");

        foreach (var car in _activeCars)
        {
            if (car.holeType != type || !car.isAvailable) continue;

            // Ask the car if its path is blocked
            if (car.IsPathBlocked(holePos, carLayer))
                continue; // Skip blocked cars

            float dist = Vector3.Distance(car.transform.position, holePos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = car;
            }
        }

        return closest;
    }


    /// <summary>
    /// NEW METHOD: Finds the closest car of a specific type regardless of blocking status.
    /// This is used when no unblocked car is found, so we try to move the closest one anyway.
    /// </summary>
    public Car FindClosestCarOfType(HoleType type, Vector3 holePos)
    {
        Car closest = null;
        float minDist = float.MaxValue;

        foreach (var car in _activeCars)
        {
            // Only check type and availability, ignore blocking
            if (car.holeType != type || !car.isAvailable) continue;

            float dist = Vector3.Distance(car.transform.position, holePos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = car;
            }
        }
        return closest;
    }

    // Optional: If you need to stop all cars immediately (e.g., on game over)
    public void StopAllCars()
    {
        foreach (var car in _activeCars)
        {
            if (car != null)
            {
                // Assuming your Car class has a method to stop movement or kill tweens
                car.StopMovement();
                // Or just kill its DOTween sequences/tweens
                // DOTween.Kill(car.transform); 
            }
        }
    }
}