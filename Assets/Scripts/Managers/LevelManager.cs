using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;
using System;
using GogoGaga.OptimizedRopesAndCables;

public class LevelManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Set a level index to force start. Use -1 for normal behavior.")]
    public int debugStartLevelIndex = -1;

    public static LevelManager instance;
    [Header("UI Manager")]
    public UiManager uiManager;

    [Header("Game Progression")]
    public LevelProgressManager levelProgressManager;

    // REFERENCE TO THE NEW CAR MANAGER MONOBEHAVIOUR
    [Header("Car Management")]
    public CarManager carManager; // Assign a GameObject with CarManager component here

    [Header("References")]
    public Hole holePrefab;
    public Transform boardParent;

    // CarPrefabs and activeCars are now in CarManager
    // public List<Car> carPrefabs;
    // private List<Car> activeCars = new List<Car>();

    [Header("Container System")]
    public HoleContainer containerPrefab;
    public List<Hole> pendingPartners = new List<Hole>();
    public Transform containerParent;
    public float containerSpacing = 3f;
    public float containerZOffset = 5f;
    public float containerYOffset = 1f;

    [Header("UI References")]
    public GameObject noSpaceText;

    [Header("Color Settings")]
    public ColorManager colorManager;

    [Header("DOTween Animation Settings")]
    [Range(0.1f, 3f)]
    public float moveAnimationDuration = 0.8f;
    public Ease moveEase = Ease.OutBack;

    [Range(0.1f, 2f)]
    public float scaleAnimationDuration = 0.5f;
    public Ease scaleEase = Ease.OutElastic;

    [Range(0.1f, 2f)]
    public float textAnimationDuration = 1.5f;
    public Ease textFadeEase = Ease.OutQuad;

    [Range(0f, 1f)]
    public float containerPunchIntensity = 0.2f;
    [Range(0.1f, 1f)]
    public float containerPunchDuration = 0.3f;

    [Header("Spacing")]
    public float spacingX = 2f;
    public float spacingZ = 2f;

    [Header("Front Row Scaling")]
    public float frontRowScaleMultiplier = 1.2f;

    [Header("Board Offset")]
    public float xOffset = 0f;  // NEW
    public float yOffset = 0f;
    public float zOffset;
    [Header("Existing holes")]
    public float existingHolesCallbackDelay = 0.3f;

    private List<List<Hole>> holeColumns = new List<List<Hole>>();
    private List<HoleContainer> containers = new List<HoleContainer>();
    private List<Tween> activeTweens = new List<Tween>();

    public bool isLevelFailed = false;
    public bool isLevelWin = false;

    void Awake()
    {
        instance = this;
        DOTween.Init();
    }

    void OnEnable()
    {
        if (levelProgressManager != null)
        {
            levelProgressManager.OnLoadLevelRequested += HandleLoadLevelRequested;
            levelProgressManager.OnLevelNumberChanged += HandleLevelNumberChanged;
        }
    }

    void OnDisable()
    {
        if (levelProgressManager != null)
        {
            levelProgressManager.OnLoadLevelRequested -= HandleLoadLevelRequested;
            levelProgressManager.OnLevelNumberChanged -= HandleLevelNumberChanged;
        }
    }

    void Start()
    {
        // --- Dependency Checks ---
        if (levelProgressManager == null) { Debug.LogError("LevelProgressManager not assigned!"); enabled = false; return; }
        if (colorManager == null) { Debug.LogError("ColorManager not assigned!"); enabled = false; return; }
        if (carManager == null) { Debug.LogError("CarManager not assigned!"); enabled = false; return; }
        if (uiManager == null) { Debug.LogWarning("UiManager is not assigned. UI updates will not function."); }
        // --- End Dependency Checks ---

        // Initialize managers
        levelProgressManager.Initialize();
        carManager.Init(this); // Pass reference to LevelManager for callbacks

        // ✅ Debug override
        if (debugStartLevelIndex >= 0)
        {
            Debug.Log($"[DEBUG] Forcing level to {debugStartLevelIndex} from LevelManager!");
            levelProgressManager.RequestLevelLoad(debugStartLevelIndex);
        }
        else
        {
            levelProgressManager.RequestLevelLoad(levelProgressManager.CurrentLevelIndex);
        }
    }

    public void OnLevelFailed(bool isTimeUp = false, bool isSpaceOut = false)
    {
        isLevelFailed = true;
        if (uiManager != null)
            uiManager.OnLevelLose(isTimeUp,isSpaceOut); // Make sure UiManager has a method called OnLevelFailed()
    }

    void OnDestroy()
    {
        foreach (var tween in activeTweens)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();
        }
        activeTweens.Clear();
    }

    // --- EVENT HANDLERS ---
    private void HandleLoadLevelRequested(int levelIndex)
    {
        LevelData levelToLoad = levelProgressManager.allLevelsData.levels[levelIndex];
        Debug.Log($"LevelManager received request to load level: {levelIndex + 1}");
        StartCoroutine(ClearOldLevelWithAnimation(levelToLoad));
        HandleLevelStartUi(levelToLoad);
    }

    private void HandleLevelStartUi(LevelData levelToLoad)
    {
        if (uiManager != null && levelToLoad.isTimedLevel)
        uiManager.StartCountdown(levelToLoad.LevelCompletedTime);
    }

    private void HandleLevelNumberChanged(int currentLevelNumber, int totalLevels)
    {
        if (uiManager != null)
        {
            uiManager.UpdateLevelText(currentLevelNumber, totalLevels);
        }
    }
    // --- END EVENT HANDLERS ---

    // This is called by CarManager when all cars are removed from the board
    public void OnCarRemovalComplete()
    {
        if (isLevelFailed) return; // Prevent multiple calls if level already failed
        if (isLevelWin) return; // Prevent multiple calls if level already won
        isLevelWin = true;
        if (uiManager != null)
            uiManager.OnLevelWin();
        
        levelProgressManager.LevelCompleted(); 
    }

    private IEnumerator ClearOldLevelWithAnimation(LevelData levelData)
    {
        foreach (var tween in activeTweens)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();
        }
        activeTweens.Clear();

        List<Transform> existingHoles = new List<Transform>();
        foreach (Transform child in boardParent)
            existingHoles.Add(child);

        foreach (Transform hole in existingHoles)
        {
            var tween = hole.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            activeTweens.Add(tween);
        }

        List<Transform> existingContainers = new List<Transform>();
        foreach (Transform child in containerParent)
            existingContainers.Add(child);

        foreach (Transform container in existingContainers)
        {
            var tween = container.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            activeTweens.Add(tween);
        }

        yield return new WaitForSeconds(0.4f);

        foreach (Transform child in boardParent)
            Destroy(child.gameObject);

        foreach (Transform child in containerParent)
            Destroy(child.gameObject);

        holeColumns.Clear();
        containers.Clear();

        CreateContainersWithAnimation(levelData.containerCount);
        yield return new WaitForSeconds(0.2f);
        CreateBoardWithAnimation(levelData);
        
        // --- DELEGATE CAR SPAWNING TO CAR MANAGER ---
        carManager.SpawnCars(levelData);
    }

    void CreateContainersWithAnimation(int containerCount)
    {
        float totalContainerWidth = (containerCount - 1) * containerSpacing;
        float containerXOffset = -totalContainerWidth / 2f;

        for (int i = 0; i < containerCount; i++)
        {
            Vector3 containerPos = new Vector3(
                i * containerSpacing + containerXOffset,
                containerYOffset,
                containerZOffset
            );

            HoleContainer container = Instantiate(containerPrefab, containerPos, Quaternion.identity, containerParent);
            container.containerIndex = i;
            containers.Add(container);

            container.transform.localScale = Vector3.zero;
            var tween = container.transform.DOScale(1f, scaleAnimationDuration)
                .SetEase(scaleEase)
                .SetDelay(i * 0.1f);
            activeTweens.Add(tween);
        }

        Debug.Log($"Created {containerCount} containers with animations");
    }

    void CreateBoardWithAnimation(LevelData levelData)
    {
        float totalWidth = (levelData.columnCount - 1) * spacingX;
        float centerXOffset = -totalWidth / 2f;

        for (int c = 0; c < levelData.columnCount; c++)
        {
            holeColumns.Add(new List<Hole>());
        }

        for (int c = 0; c < levelData.columnCount; c++)
        {
            ColumnData col = levelData.columns[c];
            for (int r = 0; r < col.rowCount; r++)
            {
                Vector3 pos = new Vector3(
                    c * spacingX + centerXOffset + xOffset,
                    yOffset,
                    -r * spacingZ + zOffset
                );

                Hole hole = Instantiate(holePrefab, pos, Quaternion.identity, boardParent);

                // ✅ Updated: Fetch both hole type and special type
                HoleType type = col.holes[r].holeType;
                SpecialHoleType specialType = col.holes[r].specialType;
                bool isMystery = col.holes[r].isMystery;

                // ✅ Handle color or visuals depending on the special type
                Color color = colorManager.GetColorByType(type);

                bool isFirstRow = (r == 0);

                // ✅ Pass specialType to hole.Init if you support it
                hole.Init(type, specialType, color, isFirstRow, c, r, this, isMystery);

                hole.transform.localScale = Vector3.zero;
                float delay = (c * 0.1f) + (r * 0.05f);

                Vector3 targetScale = (r == 0) ? Vector3.one * frontRowScaleMultiplier : Vector3.one;

                var tween = hole.transform.DOScale(targetScale, scaleAnimationDuration)
                    .SetEase(scaleEase)
                    .SetDelay(delay);
                activeTweens.Add(tween);

                holeColumns[c].Add(hole);
            }
        }

        for (int c = 0; c < levelData.columnCount; c++)
        {
            for (int r = 0; r < holeColumns[c].Count - 1; r++)
            {
                Hole hole = holeColumns[c][r];
                if (hole.specialHoleType == SpecialHoleType.Grouped)
                {
                    Hole nextHole = holeColumns[c][r + 1];

                    // 🔹 Set partner relationship (mutual assignment)
                    hole.partnerHole = nextHole;
                    nextHole.partnerHole = hole;

                    // 🔹 Enable rope visuals
                    nextHole.ropeEnd.gameObject.SetActive(true);
                    hole.rope.endPoint = nextHole.ropeEnd;
                    hole.rope.transform.GetComponent<RopeMesh>().enabled = true;
                }
            }
        }
    }


    /// <summary>
    /// Called by a Hole when the player taps a grouped hole (that is on first row).
    /// Handles 2-slot, 1-slot and 0-slot cases per your spec.
    /// </summary>
    public void RequestGroupedMove(Hole tappedHole)
    {
        if (tappedHole == null) return;

        // find partner: same column, next row (rowIndex + 1)
        int col = tappedHole.columnIndex;
        int partnerRow = tappedHole.rowIndex + 1;

        if (col < 0 || col >= holeColumns.Count) return;
        if (partnerRow < 0 || partnerRow >= holeColumns[col].Count) return;

        Hole partner = holeColumns[col][partnerRow];
        if (partner == null) return;

        // Make sure tappedHole is really first row (extra safety)
        if (!tappedHole.isFirstRow) return;

        int freeSlots = GetEmptyContainerCount();

        if (freeSlots >= 2)
        {
            // tappedHole.OnTapToGroupedHole();
            // partner.ropeEnd.gameObject.SetActive(false);
            RemoveHoleAndMoveToContainer(tappedHole.columnIndex, true);
            RemoveHoleAndMoveToContainer(partner.columnIndex, false,true); // Only the second one should check
        }
        else if (freeSlots == 1)
        {
            // tappedHole.OnTapToGroupedHole();
            // partner.ropeEnd.gameObject.SetActive(false);
            // Move tapped hole now, partner waits
            RemoveHoleAndMoveToContainer(tappedHole.columnIndex);

            // Add partner to pending list only if not already pending and still available
            if (!pendingPartners.Contains(partner) && partner.isAvailable)
                pendingPartners.Add(partner);
        }
        else // freeSlots == 0
        {
            // Per your instruction: do nothing (don't queue)
            return;
        }
    }


    // OnCarRemoved is now in CarManager
    // public void OnCarRemoved(Car car) { ... }

    public void RemoveHoleAndMoveToContainer(int columnIndex, bool skipFullCheck = false, bool isMovingPartner = false)
    {
        if (columnIndex < 0 || columnIndex >= holeColumns.Count || holeColumns[columnIndex].Count == 0)
            return;

        Hole firstHole = holeColumns[columnIndex][0];

        HoleContainer closestContainer = null;
        float minDist = float.MaxValue;

        foreach (var container in containers)
        {
            if (!container.isEmpty) continue;

            float dist = Vector3.Distance(firstHole.transform.position, container.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestContainer = container;
            }
        }

        if (closestContainer == null)
        {
            ShowNoSpaceAnimationDOTween();
            return;
        }

        holeColumns[columnIndex].RemoveAt(0);

        closestContainer.isEmpty = false;
        closestContainer.storedHole = firstHole;
        closestContainer.UpdateVisualFeedback();

        var containerPunch = closestContainer.transform.DOPunchScale(
            Vector3.one * containerPunchIntensity,
            containerPunchDuration,
            vibrato: 2,
            elasticity: 0.5f
        );
        activeTweens.Add(containerPunch);

        // --- DELEGATE CAR FINDING TO CAR MANAGER ---
        Car matchingCar = carManager.FindClosestUnblockedCar(firstHole.holeType, closestContainer.transform.position);

        MoveHoleToContainerDOTween(firstHole, closestContainer, columnIndex, matchingCar, skipFullCheck, isMovingPartner);
        firstHole.RevealMysteryHole();
    }

    /// <summary>
    /// Try to move pending partners whenever a slot frees up.
    /// Should be called AFTER a hole finishes moving to container (i.e. after a slot is freed).
    /// </summary>
    public void CheckPendingPartners()
    {
        if (pendingPartners.Count == 0) return;

        int freeSlots = GetEmptyContainerCount();
        int i = 0;

        while (freeSlots > 0 && i < pendingPartners.Count)
        {
            Hole partner = pendingPartners[i];

            if (partner == null || !partner.isAvailable)
            {
                pendingPartners.RemoveAt(i);
                continue;
            }

            RemoveHoleAndMoveToContainer(partner.columnIndex,false,true); // isMovingPartner = true
            pendingPartners.RemoveAt(i);

            freeSlots = GetEmptyContainerCount(); 
            // OR freeSlots--; if no external changes
        }
    }

    public void TriggerProcessAllMatches(float delay)
    {
        StartCoroutine(ProcessAllMatches(delay));
    }

    public IEnumerator ProcessAllMatches(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("=== PROCESSING ALL AVAILABLE MATCHES ===");

        var containersWithHoles = containers.Where(c => !c.isEmpty && c.storedHole != null).ToList();

        if (containersWithHoles.Count == 0)
        {
            Debug.Log("No containers with holes found");
            yield break;
        }

        bool movedAnyCar = false; // 👈 track whether any car actually moved this round

        foreach (var container in containersWithHoles)
        {
            if (container.storedHole == null || !container.storedHole.isAvailable)
                continue;

            Car matchingCar = carManager.FindClosestUnblockedCar(container.storedHole.holeType, container.transform.position);

            if (matchingCar != null)
            {
                movedAnyCar = true; // ✅ mark that a car was moved

                Debug.Log($"MATCH FOUND: Car {matchingCar.name} → Container {container.containerIndex}");
                container.storedHole.isAvailable = false;

                // Trigger another round since a match happened
                TriggerProcessAllMatches(0);

                matchingCar.MoveToHole(container.transform, () =>
                {
                    // CarManager handles car removal and checks
                });
            }
        }

        // ✅ AFTER the loop — if no car moved and everything is full, trigger fail
        if (!movedAnyCar)
        {
            if (AreAllContainersFull())
            {
                Debug.Log("All containers are full and no car movement possible - Level Failed!");
                OnLevelFailed(false, true); // isSpaceOut = true
            }
            else
            {
                Debug.Log("No matches found this round, but still space available.");
            }
        }
    }


    // Add this method to check if all containers are full
    // Add this method to check if all containers are full
    public bool AreAllContainersFull()
    {
        // Check if all containers have holes in them and all those holes are available
        return containers.All(c => !c.isEmpty && c.storedHole != null && c.storedHole.isAvailable && c.storedHole.onContainer);
    }

    // Modified MoveHoleToContainerDOTween method - only the OnComplete part changes
    void MoveHoleToContainerDOTween(Hole hole, HoleContainer container, int columnIndex, Car matchingCar = null, bool skipFullCheck = false, bool isMovingPartner = false)
    {
        var moveTween = hole.transform.DOMove(container.transform.position, moveAnimationDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                if (isMovingPartner)
                {
                    hole.ropeEnd.gameObject.SetActive(false);
                    if (hole.rope != null){ hole.rope.gameObject.SetActive(false); }
                    if (hole.partnerHole != null){ hole.partnerHole.rope.gameObject.SetActive(false); hole.partnerHole.ropeEnd.gameObject.SetActive(false);}
                }
                hole.transform.SetParent(container.transform);
                hole.transform.localPosition = Vector3.zero;
                hole.transform.localScale = Vector3.one * frontRowScaleMultiplier;
                hole.isFirstRow = false;
                hole.onContainer = true;
                hole.SetOutline(false);

                if (matchingCar != null && matchingCar.isAvailable && !carManager.isFallbackActive)
                {
                    hole.isAvailable = false;
                    Debug.Log($"Moving matched car: {matchingCar.name}");
                    matchingCar.MoveToHole(container.transform, () =>
                    {
                        // CarManager will handle car removal from its active list
                        //StartCoroutine(CleanupContainerAndHole(container, hole));
                    });

                    StartCoroutine(ProcessAllMatches(existingHolesCallbackDelay));
                }
                else if (matchingCar == null)
                {
                    // NEW: Check if all containers are full when no matching car is found
                    if (!skipFullCheck && AreAllContainersFull())
                    {
                        Debug.Log("All containers are full and no matching car found - Level Failed!");
                        OnLevelFailed(false, true); // isSpaceOut = true
                        // Don't continue with fallback logic if level failed
                    }

                    if (!carManager.isFallbackActive)
                    {
                        //NEW: Try to find closest car of same type (even if blocked)
                        Car fallbackCar = carManager.FindClosestCarOfType(hole.holeType, container.transform.position);

                        if (fallbackCar != null)
                        {
                            Debug.Log($"No unblocked car found, trying to move closest same-type car: {fallbackCar.name}");
                            hole.isAvailable = false;
                            carManager.isFallbackActive = true; // Indicate fallback is active
                            fallbackCar.StartMoving(container.transform,
                            () =>
                            {
                                //StartCoroutine(CleanupContainerAndHole(container, hole));
                            });
                        }
                        else
                        {
                            Debug.Log($"No car of type {hole.holeType} found at all.");
                        }

                        StartCoroutine(ProcessAllMatches(existingHolesCallbackDelay));
                    }
                }

                ShiftColumnForwardDOTween(columnIndex);
            });

        activeTweens.Add(moveTween);
    }

    void ShiftColumnForwardDOTween(int columnIndex)
    {
        List<Hole> column = holeColumns[columnIndex];
        if (column.Count == 0) return;

        LevelData currentLevelData = levelProgressManager.GetCurrentLevelData();
        if (currentLevelData == null) {
            Debug.LogError("Could not retrieve current level data for shifting column!");
            return;
        }

        float totalWidth = (currentLevelData.columnCount - 1) * spacingX;
        float centerXOffset = -totalWidth / 2f;
        float columnX = columnIndex * spacingX + centerXOffset + xOffset;

        Sequence shiftSequence = DOTween.Sequence();

        for (int i = 0; i < column.Count; i++)
        {
            Hole hole = column[i];
            Vector3 targetPos = new Vector3(columnX, yOffset, -i * spacingZ + zOffset);

            hole.rowIndex = i;
            hole.isFirstRow = (i == 0);

            var moveTween = hole.transform.DOMove(targetPos, moveAnimationDuration * 0.8f)
                .SetEase(moveEase)
                .SetDelay(i * 0.05f);

            shiftSequence.Join(moveTween);
            moveTween.OnComplete(() =>
            {
                hole.SetOutline(hole.isFirstRow);
            });

            if (i == 0)
            {
                Vector3 newScale = Vector3.one * frontRowScaleMultiplier;
                var scaleTween = hole.transform.DOScale(newScale, scaleAnimationDuration)
                    .SetEase(scaleEase)
                    .SetDelay(moveAnimationDuration * 0.5f);
                shiftSequence.Join(scaleTween);
            }
            else
            {
                var scaleTween = hole.transform.DOScale(Vector3.one, scaleAnimationDuration)
                    .SetEase(scaleEase)
                    .SetDelay(moveAnimationDuration * 0.5f);
                shiftSequence.Join(scaleTween);
            }
        }

        activeTweens.Add(shiftSequence);
    }

    void ShowNoSpaceAnimationDOTween()
    {
        if (noSpaceText != null)
        {
            noSpaceText.SetActive(true);

            CanvasGroup canvasGroup = noSpaceText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = noSpaceText.AddComponent<CanvasGroup>();

            RectTransform rectTransform = noSpaceText.GetComponent<RectTransform>();
            Sequence textSequence = DOTween.Sequence();

            canvasGroup.alpha = 0;

            textSequence.Append(canvasGroup.DOFade(1f, textAnimationDuration * 0.3f).SetEase(textFadeEase))
                        .Join(rectTransform.DOPunchScale(Vector3.one * 0.2f, textAnimationDuration * 0.4f, vibrato: 3))
                        .AppendInterval(0.5f)
                        .Append(canvasGroup.DOFade(0f, textAnimationDuration * 0.3f).SetEase(textFadeEase))
                        .OnComplete(() =>
                        {
                            noSpaceText.SetActive(false);
                        });

            activeTweens.Add(textSequence);
        }
    }

    public bool IsColumnEmpty(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= holeColumns.Count)
            return true;
        return holeColumns[columnIndex].Count == 0;
    }

    public int GetColumnHoleCount(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= holeColumns.Count)
            return 0;
        return holeColumns[columnIndex].Count;
    }

    public int GetEmptyContainerCount()
    {
        return containers.Count(c => c.isEmpty);
    }

    public int GetTotalContainerCount()
    {
        return containers.Count;
    }
}