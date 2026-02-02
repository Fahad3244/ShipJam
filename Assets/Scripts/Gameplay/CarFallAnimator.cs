using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using Sequence = DG.Tweening.Sequence;

public class CarFallAnimator : MonoBehaviour
{
    [Header("Fall Settings")]
    private float totalFallDuration = 1.2f; // Reduced for faster fall
    private float fallDepth = 6f;
    private int fallStyleCount;
    private float initialFallSpeed = 0.3f; // How fast it starts falling
    private float gravityAcceleration = 2f; // How much it accelerates
    private Ease movementEase = Ease.InOutCubic;

    private Transform carTransform;

    void Awake()
    {
        carTransform = transform;
    }

    void Start()
    {
        // Assuming CarManager is a singleton
        fallStyleCount = CarManager.Instance.carFallAnimationStyle; // Get from CarManager
        totalFallDuration = CarManager.Instance.totalFallDuration;
        fallDepth = CarManager.Instance.fallDepth;
        initialFallSpeed = CarManager.Instance.initialFallSpeed;
        gravityAcceleration = CarManager.Instance.gravityAcceleration;
        movementEase = CarManager.Instance.movementEase;
    }

    /// <summary>
    /// Starts a heavy object fall animation into the hole.
    /// </summary>
    /// <param name="holeCenter">Position of the hole.</param>
    /// <param name="onFallComplete">Callback when animation completes.</param>
    public void PlayFall(Vector3 holeCenter, System.Action onFallComplete)
    {
        Vector3 startPos = carTransform.position;
        Vector3 startRotation = carTransform.eulerAngles;
        Vector3 finalPos = new Vector3(holeCenter.x, holeCenter.y - fallDepth, holeCenter.z);

        //int fallStyle = Random.Range(0, fallStyleCount);

        Sequence fallSeq = null;

        // switch (fallStyle)
        // {
        //     case 0: fallSeq = HeavyNoseDiveFall(startRotation, finalPos); break;
        //     case 1: fallSeq = HeavyTiltAndFall(startRotation, finalPos); break;
        //     case 2: fallSeq = HeavySpiralFall(startPos, startRotation, finalPos); break;
        //     case 3: fallSeq = HeavyStraightDrop(startRotation, finalPos); break;
        //     default: fallSeq = HeavyNoseDiveFall(startRotation, finalPos); break;
        // }

        if (fallStyleCount == 0)
            fallSeq = HeavyNoseDiveFall(startRotation, finalPos);
        else if (fallStyleCount == 1)
            fallSeq = HeavyTiltAndFall(startRotation, finalPos);
        else if (fallStyleCount == 2)
            fallSeq = HeavySpiralFall(startPos, startRotation, finalPos);
        else // fallStyle == 3
            fallSeq = HeavyStraightDrop(startRotation, finalPos);

        if (fallSeq != null)
        {
            fallSeq.OnComplete(() => onFallComplete?.Invoke());
        }
        else
        {
            // Safety fallback
            DOVirtual.DelayedCall(0.5f, () => onFallComplete?.Invoke());
        }
    }

    // --- Heavy Object Fall Styles ---
    private Sequence HeavyNoseDiveFall(Vector3 startRotation, Vector3 finalPos)
    {
        Sequence fallSeq = DOTween.Sequence();

        Vector3 finalRotation = new Vector3(
            startRotation.x + Random.Range(60f, 90f),
            startRotation.y + Random.Range(-20f, 20f),
            startRotation.z + Random.Range(-15f, 15f)
        );

        // Heavy rotation with momentum
        fallSeq.Append(carTransform.DORotate(finalRotation, totalFallDuration, RotateMode.Fast)
            .SetEase(movementEase));
        
        // Fast accelerating fall like a heavy object
        fallSeq.Join(carTransform.DOMove(finalPos, totalFallDuration)
            .SetEase(movementEase));

        return fallSeq;
    }

    private Sequence HeavyTiltAndFall(Vector3 startRotation, Vector3 finalPos)
    {
        Sequence fallSeq = DOTween.Sequence();

        Vector3 finalRotation = new Vector3(
            startRotation.x + Random.Range(70f, 95f),
            startRotation.y + Random.Range(-40f, 40f),
            startRotation.z + Random.Range(30f, 60f)
        );

        // Quick initial tilt, then heavy fall
        fallSeq.Append(carTransform.DORotate(finalRotation, totalFallDuration * 0.8f, RotateMode.Fast)
            .SetEase(movementEase)); // OutBack gives a sudden heavy drop feeling
        
        fallSeq.Join(carTransform.DOMove(finalPos, totalFallDuration)
            .SetEase(movementEase)); // InExpo for very fast acceleration

        return fallSeq;
    }

    private Sequence HeavySpiralFall(Vector3 startPos, Vector3 startRotation, Vector3 finalPos)
    {
        Sequence fallSeq = DOTween.Sequence();

        // Smaller spiral - heavy objects don't drift much
        Vector3 spiralOffset = new Vector3(
            Random.Range(-0.2f, 0.2f), 0,
            Random.Range(-0.2f, 0.2f)
        );
        Vector3 midPoint = Vector3.Lerp(startPos, finalPos, 0.3f) + spiralOffset;
        Vector3[] spiralPath = { startPos, midPoint, finalPos };

        Vector3 finalRotation = new Vector3(
            startRotation.x + Random.Range(80f, 100f),
            startRotation.y + Random.Range(270f, 450f), // More rotation for spiral
            startRotation.z + Random.Range(-45f, 45f)
        );

        // Heavy spiral with fast acceleration
        fallSeq.Append(carTransform.DOPath(spiralPath, totalFallDuration, PathType.CatmullRom)
            .SetEase(movementEase));
        fallSeq.Join(carTransform.DORotate(finalRotation, totalFallDuration, RotateMode.Fast)
            .SetEase(movementEase));

        return fallSeq;
    }

    private Sequence HeavyStraightDrop(Vector3 startRotation, Vector3 finalPos)
    {
        Sequence fallSeq = DOTween.Sequence();

        Vector3 finalRotation = new Vector3(
            startRotation.x + Random.Range(40f, 60f), // Minimal front tilt
            startRotation.y + Random.Range(-5f, 5f),   // Minimal side rotation
            startRotation.z + Random.Range(-10f, 10f)  // Slight roll
        );

        // Very heavy, straight drop with minimal rotation
        fallSeq.Append(carTransform.DORotate(finalRotation, totalFallDuration, RotateMode.Fast)
            .SetEase(movementEase));
        
        // Extremely fast acceleration like dropping a brick
        fallSeq.Join(carTransform.DOMove(finalPos, totalFallDuration)
            .SetEase(movementEase).SetEase(AnimationCurve.EaseInOut(0, 0, 0.2f, 1))); // Custom curve for heavy drop

        return fallSeq;
    }
}