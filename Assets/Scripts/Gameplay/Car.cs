using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class Car : MonoBehaviour
{
    [Header("Car Settings")]
    public CarType carType;
    public HoleType holeType;
    public bool isAvailable = true; // Indicates if the car is currently available
    public GameObject arrowIndicator; // Optional: visual indicator

    public CarManager CarManager { get; set; } // Set by CarManager during SpawnCars

    [Header("Effect Settings")]
    public ParticleSystem effect;
    public ParticleSystem hitEffect; // New: Hit effect particle system

    [Header("Debug Settings")]
    public bool drawDebugPath = true;
    public bool debugLastPathBlocked = false;
    private List<(Vector3 start, Vector3 end, bool blocked)> debugPathSegments = new List<(Vector3, Vector3, bool)>();


    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 30f;
    [SerializeField] private float maxMovementDuration = 1f;
    [SerializeField] private Ease movementEase = Ease.InOutCubic;

    [Header("Path Trigger Settings")]
    [SerializeField] private float distanceTriggerThreshold = 2f; // distance to trigger fall prep
    private bool hasTriggeredPathEvent = false;
    private bool isPreparingToFall = false;
    private Vector3 pendingHoleCenter;
    public float preFallTiltDuration = 0.25f;


    [Header("Fall Settings")]
    [SerializeField] private float fallStartDelay = 0.3f;
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("Collision Settings")]
    [SerializeField] private Vector3 collisionBoxSize = new Vector3(1f, 1f, 2f);
    [SerializeField] private Vector3 collisionBoxOffset = Vector3.zero;
    [SerializeField] private float returnToOriginDuration = 1f;

    [Header("Hit Animation Settings")]
    [SerializeField] private float hitAnimationDuration = 0.3f;
    [SerializeField] private float hitSwingAngle = 15f;
    [SerializeField] private float hitBounceDistance = 0.5f;
    [SerializeField] private AnimationCurve hitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<Vector3> pathPoints;
    private Rigidbody rb;
    private bool isFalling = false;
    private bool isMovingToHole = false;
    private bool isPlayingHitAnimation = false; // Prevents multiple hit animations
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private Action onFallCompleteCallback;
    private Tween currentMoveTween;

    private LevelManager levelManager;
    private CarFallAnimator fallAnimator;
    public Hole currentTargetHole;
    public HoleContainer currentTargetContainer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        fallAnimator = GetComponent<CarFallAnimator>();
    }

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.eulerAngles;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        else
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
        }
    }

    public void Init(CarData data)
    {
        carType = data.carType;
        transform.position = data.startPos;
        transform.rotation = Quaternion.Euler(data.startRotation);
        pathPoints = data.pathToJunction;

        originalPosition = transform.position;
        originalRotation = transform.eulerAngles;

        isAvailable = true;
        isFalling = false;
        isMovingToHole = false;
        isPlayingHitAnimation = false;
    }

    public void SetLevelManager(LevelManager manager)
    {
        levelManager = manager;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Hole"))
        {
            Debug.Log($"Car {name} entered hole trigger.");
        }
    }

    public void MoveToHole(Transform containerPos, System.Action onFallComplete = null)
    {
        if (!isAvailable)
        {
            Debug.LogWarning($"Car {name} is not available for movement, skipping.");
            return;
        }

        Debug.Log($"Car {name} starting movement to hole at {containerPos}");
        GetComponent<Collider>().enabled = false;
        currentTargetContainer = containerPos.GetComponent<HoleContainer>();
        currentTargetHole = currentTargetContainer?.storedHole;

        isAvailable = false;
        isMovingToHole = true;
        onFallCompleteCallback = onFallComplete;

        if (effect != null) effect.Play();

        List<Vector3> fullPath = new List<Vector3> { transform.position };

        if (pathPoints != null && pathPoints.Count > 0)
        {
            foreach (Vector3 point in pathPoints)
            {
                if (Vector3.Distance(fullPath[fullPath.Count - 1], point) > 0.1f)
                    fullPath.Add(point);
            }
        }
        fullPath.Add(containerPos.position);

        float totalDist = 0f;
        for (int i = 1; i < fullPath.Count; i++)
            totalDist += Vector3.Distance(fullPath[i - 1], fullPath[i]);

        float calculatedDuration = totalDist / movementSpeed;
        float duration = Mathf.Min(calculatedDuration, maxMovementDuration);

        currentMoveTween = transform.DOPath(fullPath.ToArray(), duration, PathType.CatmullRom)
            .SetEase(movementEase)
            .SetLookAt(0.05f, true)
            .OnUpdate(() =>
            {
                if (currentTargetHole == null || hasTriggeredPathEvent) return;

                float distanceToHole = Vector3.Distance(transform.position, currentTargetHole.transform.position);

                if (distanceToHole <= distanceTriggerThreshold)
                {
                    hasTriggeredPathEvent = true;
                    OnApproachingHoleDistance();
                }
            })
            .OnComplete(() =>
            {
                if (!isFalling && isMovingToHole)
                {
                    levelManager?.TriggerProcessAllMatches(0);
                    Debug.LogWarning($"Car {name} reached end of path without hitting hole, forcing fall.");
                    Collider[] colliders = Physics.OverlapSphere(containerPos.position, 0.5f);
                    Collider hitHole = System.Array.Find(colliders, c => c.CompareTag("Hole"));

                    if (hitHole != null)
                    {
                        Debug.Log("Car hit the hole!");
                        GetComponent<Collider>().enabled = false;
                        isFalling = true;
                        isMovingToHole = false;
                        transform.DOKill();

                        Vector3 holeCenter = hitHole.transform.position;
                        // Remove the delay - start fall animation immediately
                        PlayFallAnimation(holeCenter);
                    }
                    else
                    {
                        Debug.LogError($"Car {name} finished path but could not find a hole at {containerPos}. Destroying.");
                        FinalizeCarRemoval();
                    }
                }
            });
    }

    private void OnApproachingHoleDistance()
    {
        if (isPreparingToFall || currentTargetHole == null) return;

        isPreparingToFall = true;

        Debug.Log($"{name} reached within {distanceTriggerThreshold} units — preparing to fall!");

        pendingHoleCenter = currentTargetHole.transform.position;

        // Stop moving smoothly before fall
        if (currentMoveTween != null && currentMoveTween.IsActive())
            currentMoveTween.Kill();

        isMovingToHole = false;

        // Calculate tilt (you can tweak the tilt angle in Inspector too if you like)
        Vector3 tiltRotation = transform.eulerAngles + new Vector3(60f, 0f, 0f);

        transform.DORotate(tiltRotation, preFallTiltDuration)
            .SetEase(Ease.InQuart)
            .OnComplete(() =>
            {
                Debug.Log($"{name} tilt complete — starting fall!");
                PlayFallAnimation(pendingHoleCenter);
            });
    }



    public void StartMoving(Transform containerPos, System.Action onFallComplete = null)
    {
        if (!isAvailable)
        {
            Debug.LogWarning($"Car {name} is not available for movement, skipping.");
            return;
        }

        Debug.Log($"Car {name} starting movement to hole at {containerPos}");
        currentTargetContainer = containerPos.GetComponent<HoleContainer>();
        currentTargetHole = currentTargetContainer?.storedHole;

        isAvailable = false;
        isMovingToHole = true;
        onFallCompleteCallback = onFallComplete;

        if (effect != null) effect.Play();

        List<Vector3> fullPath = new List<Vector3> { transform.position };

        if (pathPoints != null && pathPoints.Count > 0)
        {
            foreach (Vector3 point in pathPoints)
            {
                if (Vector3.Distance(fullPath[fullPath.Count - 1], point) > 0.1f)
                    fullPath.Add(point);
            }
        }
        fullPath.Add(containerPos.position);

        float totalDist = 0f;
        for (int i = 1; i < fullPath.Count; i++)
            totalDist += Vector3.Distance(fullPath[i - 1], fullPath[i]);

        float calculatedDuration = totalDist / movementSpeed;
        float duration = Mathf.Min(calculatedDuration, maxMovementDuration);

        currentMoveTween = transform.DOPath(fullPath.ToArray(), duration, PathType.CatmullRom)
            .SetEase(movementEase)
            .SetLookAt(0.05f, true)
            .OnUpdate(() =>
            {
                CheckForCarCollisionDuringMovement();
            })
            .OnComplete(() =>
            {
                CarManager.Instance.isFallbackActive = false;
                if (!isFalling && isMovingToHole)
                {
                    Debug.LogWarning($"Car {name} reached end of path without hitting hole, forcing fall.");
                    Collider[] colliders = Physics.OverlapSphere(containerPos.position, 0.5f);
                    Collider hitHole = System.Array.Find(colliders, c => c.CompareTag("Hole"));

                    if (hitHole != null)
                    {
                        Debug.Log("Car hit the hole!");
                        GetComponent<Collider>().enabled = false;
                        isFalling = true;
                        isMovingToHole = false;
                        transform.DOKill();
                        Vector3 holeCenter = hitHole.transform.position;
                        Vector3 targetPos = new Vector3(holeCenter.x, transform.position.y, holeCenter.z);
                        transform.DOMove(targetPos, fallStartDelay)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() =>
                            {
                                PlayFallAnimation(holeCenter);
                            });
                    }
                    else
                    {
                        Debug.LogError($"Car {name} finished path but could not find a hole at {containerPos}. Destroying.");
                        FinalizeCarRemoval();
                    }
                }
            });
    }

    private void CheckForCarCollisionDuringMovement()
    {
        if (!isMovingToHole || isPlayingHitAnimation) return;

        // Use custom box size and offset
        Vector3 halfExtents = collisionBoxSize * 0.5f;
        Vector3 boxCenter = transform.position + transform.TransformDirection(collisionBoxOffset);

        Collider[] hitColliders = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation,
            LayerMask.GetMask("Car")
        );

        foreach (var collider in hitColliders)
        {
            if (collider.gameObject == gameObject) continue;

            Car otherCar = collider.GetComponent<Car>();
            if (otherCar != null && !otherCar.isPlayingHitAnimation)
            {
                Vector3 hitPoint = Vector3.Lerp(transform.position, otherCar.transform.position, 0.5f);

                Debug.Log($"🚗 Car {name} collided (box) with {otherCar.name}. Triggering hit animations.");

                PlayHitAnimation(otherCar.transform.position);
                otherCar.PlayHitAnimationInPlace();
                PlayHitEffectAtPoint(hitPoint);

                if (currentTargetHole != null)
                    currentTargetHole.isAvailable = true;

                currentTargetHole = null;

                DOVirtual.DelayedCall(hitAnimationDuration, ReturnToOriginalPosition);
                return;
            }
        }
    }



    private void PlayHitAnimation(Vector3 otherCarPosition)
    {
        if (isPlayingHitAnimation) return;

        isPlayingHitAnimation = true;

        if (currentMoveTween != null && currentMoveTween.IsActive())
            currentMoveTween.Kill();

        Vector3 hitDirection = (transform.position - otherCarPosition).normalized;
        Vector3 bouncePosition = transform.position + hitDirection * hitBounceDistance;

        Vector3 originalPos = transform.position;
        Vector3 originalRot = transform.eulerAngles;

        Sequence hitSequence = DOTween.Sequence();

        Vector3 swingRotation = originalRot + Vector3.up * hitSwingAngle;
        hitSequence.Append(transform.DORotate(swingRotation, hitAnimationDuration * 0.3f)
            .SetEase(Ease.OutBack));

        hitSequence.Join(transform.DOMove(bouncePosition, hitAnimationDuration * 0.4f)
            .SetEase(Ease.OutQuart));

        hitSequence.Append(transform.DORotate(originalRot + Vector3.up * (-hitSwingAngle * 0.5f), hitAnimationDuration * 0.3f)
            .SetEase(Ease.InOutSine));

        hitSequence.Append(transform.DORotate(originalRot, hitAnimationDuration * 0.4f)
            .SetEase(Ease.OutBounce));

        hitSequence.Join(transform.DOShakePosition(hitAnimationDuration * 0.6f, 0.1f, 10, 45f, false, true));

        hitSequence.OnComplete(() =>
        {
            isPlayingHitAnimation = false;
        });
    }

    private void PlayHitEffectAtPoint(Vector3 hitPoint)
    {
        if (hitEffect != null)
        {
            GameObject tempHitEffect = Instantiate(hitEffect.gameObject, hitPoint, Quaternion.identity);
            ParticleSystem tempPS = tempHitEffect.GetComponent<ParticleSystem>();

            if (tempPS != null)
            {
                tempPS.Play();
                Destroy(tempHitEffect, tempPS.main.duration + tempPS.main.startLifetime.constantMax);
            }
        }
        else
        {
            CreateSimpleHitEffect(hitPoint);
        }
    }

    private void PlayHitAnimationInPlace()
    {
        if (isPlayingHitAnimation) return;

        isPlayingHitAnimation = true;

        Vector3 originalRot = transform.eulerAngles;
        Vector3 originalScale = transform.localScale;

        Sequence hitSequence = DOTween.Sequence();

        Vector3 swingRotation = originalRot + Vector3.up * hitSwingAngle;
        hitSequence.Append(transform.DORotate(swingRotation, hitAnimationDuration * 0.3f)
            .SetEase(Ease.OutBack));

        hitSequence.Join(transform.DOScale(originalScale * 1.1f, hitAnimationDuration * 0.2f)
            .SetEase(Ease.OutQuart));

        hitSequence.Append(transform.DORotate(originalRot + Vector3.up * (-hitSwingAngle * 0.5f), hitAnimationDuration * 0.3f)
            .SetEase(Ease.InOutSine));

        hitSequence.Join(transform.DOScale(originalScale, hitAnimationDuration * 0.3f)
            .SetEase(Ease.OutBounce));

        hitSequence.Append(transform.DORotate(originalRot, hitAnimationDuration * 0.4f)
            .SetEase(Ease.OutBounce));

        hitSequence.Join(transform.DOShakeRotation(hitAnimationDuration * 0.4f, 5f, 10, 45f, false));

        hitSequence.OnComplete(() =>
        {
            isPlayingHitAnimation = false;
            transform.rotation = Quaternion.Euler(originalRot);
            transform.localScale = originalScale;
        });
    }

    private void CreateSimpleHitEffect(Vector3 hitPoint)
    {
        GameObject hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitSphere.transform.position = hitPoint;
        hitSphere.transform.localScale = Vector3.zero;

        Destroy(hitSphere.GetComponent<SphereCollider>());

        Renderer renderer = hitSphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }

        Sequence hitEffectSequence = DOTween.Sequence();
        hitEffectSequence.Append(hitSphere.transform.DOScale(0.5f, 0.2f).SetEase(Ease.OutBounce));
        hitEffectSequence.Join(renderer.material.DOFade(0f, 0.3f));
        hitEffectSequence.OnComplete(() => Destroy(hitSphere));
    }

    private void ReturnToOriginalPosition()
    {
        if (currentMoveTween != null && currentMoveTween.IsActive())
            currentMoveTween.Kill();

        isMovingToHole = false;
        if (effect != null) effect.Stop();

        Sequence returnSequence = DOTween.Sequence();
        returnSequence.Append(transform.DOMove(originalPosition, returnToOriginDuration).SetEase(Ease.OutQuart));
        returnSequence.Join(transform.DORotateQuaternion(Quaternion.Euler(originalRotation), returnToOriginDuration).SetEase(Ease.OutQuart));
        returnSequence.OnComplete(() =>
        {
            isAvailable = true;
            isPlayingHitAnimation = false;
            Debug.Log($"Car {name} returned to original position and is now available.");
            CarManager.Instance.isFallbackActive = false;
            levelManager?.TriggerProcessAllMatches(0);
        });
    }

    private void PlayFallAnimation(Vector3 holeCenter)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (fallAnimator != null)
        {
            effect?.Stop();
            fallAnimator.PlayFall(holeCenter, FinalizeCarRemoval);
            CleanUpContainer();
            Debug.Log("Cleaning up container after initiating fall animation.");
            DOVirtual.DelayedCall(destroyDelay + 0.5f, () =>
            {
                FinalizeCarRemoval();
            });
        }
        else
        {
            Debug.LogWarning($"Car {name} has no CarFallAnimator attached. Instantly finalizing removal.");
            FinalizeCarRemoval();
        }
    }

    public void CleanUpContainer()
    { 
        DOVirtual.DelayedCall(0.3f, () => 
        {
            currentTargetHole.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).OnComplete(() =>
            {
                if (currentTargetHole != null)
                {
                    if (currentTargetHole.specialHoleType == SpecialHoleType.Grouped)
                    { 
                        currentTargetHole.ropeEnd.gameObject.SetActive(false);
                        if (currentTargetHole.rope != null){ currentTargetHole.rope.gameObject.SetActive(false); }
                        if (currentTargetHole.partnerHole != null){ currentTargetHole.partnerHole.rope.gameObject.SetActive(false); currentTargetHole.partnerHole.ropeEnd.gameObject.SetActive(false);}
                    }
                    currentTargetContainer.isEmpty = true;
                    currentTargetContainer.storedHole = null;
                    currentTargetContainer.UpdateVisualFeedback();
                    Destroy(currentTargetHole.gameObject);
                    currentTargetHole = null;
                    levelManager.CheckPendingPartners();
                }
            });
        });
    }

    private void FinalizeCarRemoval()
    {
        if (effect != null) effect.Stop();

        DOVirtual.DelayedCall(destroyDelay, () =>
        {
            if (CarManager != null) CarManager.OnCarRemoved(this);
            else Debug.LogWarning($"Car {name} has no CarManager assigned.");

            onFallCompleteCallback?.Invoke();
            Destroy(gameObject);
        });
    }

    public void StopMovement()
    {
        if (currentMoveTween != null && currentMoveTween.IsActive())
            currentMoveTween.Kill();

        transform.DOKill();
        isAvailable = true;
        isFalling = false;
        isMovingToHole = false;
        isPlayingHitAnimation = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 halfExtents = collisionBoxSize * 0.5f;
        Vector3 boxCenter = transform.position + transform.TransformDirection(collisionBoxOffset);

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;

        Gizmos.DrawWireCube(Vector3.zero, collisionBoxSize);
    }


    public bool IsPathBlocked(Vector3 holePos, LayerMask carLayerMask)
    {
        debugPathSegments.Clear(); // Clear old data

        if (pathPoints == null || pathPoints.Count == 0)
            return false;

        List<Vector3> fullPath = new List<Vector3> { transform.position };
        fullPath.AddRange(pathPoints);
        fullPath.Add(holePos);

        bool blocked = false;

        for (int i = 0; i < fullPath.Count - 1; i++)
        {
            Vector3 start = fullPath[i];
            Vector3 end = fullPath[i + 1];
            Vector3 dir = (end - start).normalized;
            float dist = Vector3.Distance(start, end);

            bool segmentBlocked = false;

            RaycastHit[] hits = Physics.SphereCastAll(start, 0.2f, dir, dist, carLayerMask);
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject != gameObject) 
                {
                    segmentBlocked = true;
                    blocked = true;
                    break;
                }
            }

            debugPathSegments.Add((start, end, segmentBlocked));
        }

        debugLastPathBlocked = blocked;
        return blocked;
    }

    
    void OnDrawGizmos()
    {
        if (!drawDebugPath || debugPathSegments.Count == 0) return;

        foreach (var seg in debugPathSegments)
        {
            Gizmos.color = seg.blocked ? Color.red : Color.green;
            Gizmos.DrawLine(seg.start, seg.end);
            Gizmos.DrawSphere(seg.start, 0.1f);
            Gizmos.DrawSphere(seg.end, 0.1f);
        }
    }


}
