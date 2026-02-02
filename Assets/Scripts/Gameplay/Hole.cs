using UnityEngine;
using DG.Tweening;
using System;
using GogoGaga.OptimizedRopesAndCables;

public enum HoleType
{
    Red,
    Yellow,
    Purple,
    Pink,
    Lightpink,
    Orange,
    Blue,
    Lightblue,
    Darkblue,
    Green,
    Lightgreen,
    Darkgreen,
    Lightbrown,
    Darkbrown,
    none,
    
}
public enum SpecialHoleType 
{
    None,
    Grouped
}

public class Hole : MonoBehaviour
{
    [Header("Hole Info")]
    public GameObject mainModel;
    public GameObject mysteryModel;
    public GameObject outlineController;
    public HoleType holeType;
    public SpecialHoleType specialHoleType;
    public Color holeColor;
    public Rope rope;
    public Transform ropeEnd;
    public bool isMystery = false;
    public bool isFirstRow = false;
    public bool isAvailable = true;
    public bool onContainer = false;
    public int columnIndex = -1;
    public int rowIndex = -1;

    [Header("Grouped Hole Partner")]
    public Hole partnerHole;

    [Header("Effects")]
    public ParticleSystem mysteryEffect;
    [Header("Material Settings")]
    [Tooltip("The child GameObject that has the mesh renderer")]
    public Transform childWithMesh;
    public Transform childWithMesh2;
    [Tooltip("Which material slots to change color (0-based indices)")]
    public int[] materialIndices = { 0, 1 };

    // Reference to LevelManager for hole removal
    private LevelManager levelManager;

    private void Awake()
    {
        // Ensure trigger collider exists
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;

        gameObject.tag = "Hole"; // make sure all hole prefabs share this tag
    }

    public void Init(HoleType type, SpecialHoleType specialType, Color color, bool firstRow = false, int column = -1, int row = -1, LevelManager manager = null, bool isMysteryHole = false, Hole partner = null)
    {
        holeType = type;
        this.specialHoleType = specialType;
        holeColor = color;
        isFirstRow = firstRow;
        columnIndex = column;
        rowIndex = row;
        levelManager = manager;

        SetOutline(isFirstRow);

        // Apply material color
        Renderer rend = null;
        Renderer rend2 = null;
        if (childWithMesh != null) rend = childWithMesh.GetComponent<Renderer>();
        if (childWithMesh2 != null) rend2 = childWithMesh2.GetComponent<Renderer>();
        else if (transform.childCount > 1)
        {
            childWithMesh = transform.GetChild(0);
            childWithMesh2 = transform.GetChild(1);
            rend = childWithMesh.GetComponent<Renderer>();
            rend2 = childWithMesh2.GetComponent<Renderer>();
        }
        else
        {
            rend = GetComponent<Renderer>();
            rend2 = GetComponent<Renderer>();
        }

        if (rend != null)
        {
            if (rend.materials.Length > 1)
                ChangeMaterialColors(rend, color);
            else
                rend.material.color = color;
        }

        if (rend2 != null)
        {
            if (rend2.materials.Length > 1)
                ChangeMaterialColors(rend2, color);
            else
                rend2.material.SetColor("_BaseColor", color);
        }

        // 👇 Assign partner if passed
        if (specialHoleType == SpecialHoleType.Grouped && partner != null)
        {
            partnerHole = partner;
            partner.partnerHole = this; // mutual assignment
        }

        if (isMysteryHole)
        {
            MysteryHoleSetup();
        }
        if (specialHoleType == SpecialHoleType.Grouped)
        {
            GroupedHoleSetup();
        }
    }

    private void GroupedHoleSetup()
    {
        rope.gameObject.SetActive(true);
    }

    private void MysteryHoleSetup()
    {
        isMystery = true;
        mainModel.SetActive(false);
        mysteryModel.SetActive(true);
    }
    public void RevealMysteryHole()
    {
        if (isMystery)
        {
            mainModel.SetActive(true);
            mysteryModel.SetActive(false);
            if (mysteryEffect != null)
            { 
                mysteryEffect.Play();
            }
        }
    }

    void ChangeMaterialColors(Renderer renderer, Color color)
    {
        Material[] materials = renderer.materials;
        foreach (int index in materialIndices)
        {
            if (index >= 0 && index < materials.Length)
            {
                materials[index] = new Material(materials[index]);
                materials[index].color = color;
            }
        }
        renderer.materials = materials;
    }

    void OnMouseDown()
    {
        if (levelManager.isLevelFailed) return; // Prevent interaction if level is already failed
        HandleTap();
    }

    void HandleTap()
    {
        if (levelManager == null) return;

        UiManager uiManager = levelManager.uiManager;
        uiManager.tutorialPanel.SetActive(false);

        // Only grouped holes have special handling
        if (specialHoleType == SpecialHoleType.Grouped)
        {
            // Only allow grouped move if this special hole is on first row
            if (!isFirstRow) return;

            levelManager.RequestGroupedMove(this);
        }
        else if (isFirstRow)
        {
            AudioManager.Instance.PlaySFX("HoleMoving");
            // existing behavior for non-grouped first-row holes
            levelManager.RemoveHoleAndMoveToContainer(columnIndex);
        }
    }

    public void OnTapToGroupedHole()
    {
        rope.gameObject.SetActive(false);
    }


    public void SetManager(LevelManager manager)
    {
        levelManager = manager;
        Debug.Log($"Hole at column {columnIndex}, row {rowIndex} - Manager set: {manager != null}");
    }

    public void SetOutline(bool enable)
    {
        if (outlineController != null)
        {
            outlineController.SetActive(enable);
        }
    }
}
