using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class RealisticCarFallAnimator : MonoBehaviour
{
    [Header("Car Physics Settings")]
    [SerializeField] private float carMass = 1500f; // Realistic car mass in kg
    [SerializeField] private float dragCoefficient = 0.3f; // Air resistance
    [SerializeField] private float angularDrag = 5f;
    
    [Header("Driving Settings")]
    [SerializeField] private float drivingSpeed = 15f; // m/s (about 54 km/h)
    [SerializeField] private Vector3 drivingDirection = Vector3.forward;
    
    [Header("Hole Detection")]
    [SerializeField] private LayerMask holeLayerMask = 1; // Layer for hole detection
    [SerializeField] private float detectionDistance = 2f;
    [SerializeField] private Vector3 detectionOffset = new Vector3(0, -0.5f, 2f); // Offset from car center for detection
    
    [Header("Fall Physics")]
    [SerializeField] private float fallGravityMultiplier = 2f; // Make gravity stronger during fall
    [SerializeField] private bool addRandomTorqueOnFall = true;
    [SerializeField] private Vector2 torqueRange = new Vector2(50f, 150f);
    
    [Header("Audio & Effects")]
    [SerializeField] private AudioSource carAudioSource;
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip fallScreamSound;
    [SerializeField] private ParticleSystem dustParticles;
    
    private Rigidbody carRigidbody;
    private BoxCollider carCollider;
    private bool isDriving = false;
    private bool isFalling = false;
    private bool hasReachedHole = false;
    private Vector3 originalGravity;
    private System.Action onFallComplete;
    
    // Wheel positions for more realistic physics
    [Header("Wheel Physics (Optional)")]
    [SerializeField] private Transform[] wheelTransforms;
    [SerializeField] private float wheelRadius = 0.3f;
    
    void Awake()
    {
        SetupPhysics();
        originalGravity = Physics.gravity;
    }
    
    private void SetupPhysics()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carCollider = GetComponent<BoxCollider>();
        
        if (carCollider == null)
            carCollider = gameObject.AddComponent<BoxCollider>();
        
        // Configure rigidbody for realistic car physics
        carRigidbody.mass = carMass;
        carRigidbody.drag = dragCoefficient;
        carRigidbody.angularDrag = angularDrag;
        carRigidbody.centerOfMass = new Vector3(0, -0.3f, 0.2f); // Lower center of mass, slightly forward
        
        // Set up audio
        if (carAudioSource == null)
            carAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }
    
    /// <summary>
    /// Starts the car driving towards the hole with physics-based movement
    /// </summary>
    /// <param name="targetHolePosition">Position of the hole</param>
    /// <param name="onComplete">Callback when fall animation completes</param>
    public void StartDrivingToHole(Vector3 targetHolePosition, System.Action onComplete = null)
    {
        onFallComplete = onComplete;
        
        // Calculate direction to hole
        Vector3 directionToHole = (targetHolePosition - transform.position).normalized;
        drivingDirection = new Vector3(directionToHole.x, 0, directionToHole.z);
        
        // Start driving
        isDriving = true;
        PlayEngineSound();
        StartCoroutine(DriveTowardsHole());
    }
    
    private IEnumerator DriveTowardsHole()
    {
        while (isDriving && !isFalling)
        {
            // Apply driving force
            Vector3 driveForce = drivingDirection * drivingSpeed * carMass;
            carRigidbody.AddForce(driveForce, ForceMode.Force);
            
            // Check for hole ahead
            CheckForHole();
            
            // Rotate wheels if available
            RotateWheels();
            
            yield return new WaitForFixedUpdate();
        }
    }
    
    private void CheckForHole()
    {
        Vector3 rayOrigin = transform.position + transform.TransformDirection(detectionOffset);
        
        // Cast multiple rays for better detection
        RaycastHit hit;
        bool holeDetected = false;
        
        // Forward detection
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, detectionDistance, holeLayerMask))
        {
            holeDetected = true;
        }
        
        // Also check slightly ahead
        Vector3 aheadPosition = rayOrigin + drivingDirection * 1f;
        if (Physics.Raycast(aheadPosition, Vector3.down, out hit, detectionDistance, holeLayerMask))
        {
            holeDetected = true;
        }
        
        // Debug rays (remove in production)
        Debug.DrawRay(rayOrigin, Vector3.down * detectionDistance, holeDetected ? Color.red : Color.green);
        Debug.DrawRay(aheadPosition, Vector3.down * detectionDistance, holeDetected ? Color.red : Color.green);
        
        if (holeDetected && !hasReachedHole)
        {
            hasReachedHole = true;
            StartFall();
        }
    }
    
    private void StartFall()
    {
        isDriving = false;
        isFalling = true;
        
        // Stop engine sound and play fall sound
        StopEngineSound();
        PlayFallSound();
        
        // Increase gravity for more dramatic fall
        Physics.gravity = originalGravity * fallGravityMultiplier;
        
        // Add random torque for realistic tumbling
        if (addRandomTorqueOnFall)
        {
            Vector3 randomTorque = new Vector3(
                Random.Range(-torqueRange.y, torqueRange.y),
                Random.Range(-torqueRange.x, torqueRange.x),
                Random.Range(-torqueRange.y, torqueRange.y)
            );
            carRigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }
        
        // Maintain some forward momentum but reduce it
        Vector3 currentVelocity = carRigidbody.velocity;
        carRigidbody.velocity = new Vector3(
            currentVelocity.x * 0.7f, 
            currentVelocity.y, 
            currentVelocity.z * 0.7f
        );
        
        // Start dust particles
        if (dustParticles != null)
            dustParticles.Play();
        
        // Start checking for fall completion
        StartCoroutine(MonitorFall());
    }
    
    private IEnumerator MonitorFall()
    {
        float fallStartTime = Time.time;
        float lastYPosition = transform.position.y;
        float stagnationTime = 0f;
        
        while (isFalling)
        {
            // Check if car has stopped falling (hit bottom or stuck)
            float currentY = transform.position.y;
            if (Mathf.Abs(currentY - lastYPosition) < 0.1f)
            {
                stagnationTime += Time.deltaTime;
                if (stagnationTime > 1f || carRigidbody.velocity.magnitude < 0.5f)
                {
                    CompleteFall();
                    break;
                }
            }
            else
            {
                stagnationTime = 0f;
                lastYPosition = currentY;
            }
            
            // Safety timeout
            if (Time.time - fallStartTime > 10f)
            {
                CompleteFall();
                break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void CompleteFall()
    {
        isFalling = false;
        
        // Restore original gravity
        Physics.gravity = originalGravity;
        
        // Stop particles
        if (dustParticles != null)
            dustParticles.Stop();
        
        // Invoke completion callback
        onFallComplete?.Invoke();
    }
    
    private void RotateWheels()
    {
        if (wheelTransforms == null || wheelTransforms.Length == 0)
            return;
        
        float rotationSpeed = (carRigidbody.velocity.magnitude / wheelRadius) * Mathf.Rad2Deg;
        
        foreach (Transform wheel in wheelTransforms)
        {
            if (wheel != null)
                wheel.Rotate(rotationSpeed * Time.fixedDeltaTime, 0, 0);
        }
    }
    
    #region Audio Methods
    private void PlayEngineSound()
    {
        if (carAudioSource != null && engineSound != null)
        {
            carAudioSource.clip = engineSound;
            carAudioSource.loop = true;
            carAudioSource.Play();
        }
    }
    
    private void StopEngineSound()
    {
        if (carAudioSource != null && carAudioSource.isPlaying)
        {
            carAudioSource.Stop();
        }
    }
    
    private void PlayFallSound()
    {
        if (carAudioSource != null && fallScreamSound != null)
        {
            carAudioSource.clip = fallScreamSound;
            carAudioSource.loop = false;
            carAudioSource.Play();
        }
    }
    #endregion
    
    #region Collision Events
    private void OnCollisionEnter(Collision collision)
    {
        if (isFalling)
        {
            // Add impact effects or sounds here
            if (collision.relativeVelocity.magnitude > 5f)
            {
                // Play crash sound or particle effect
                Debug.Log($"Car hit {collision.gameObject.name} with impact force: {collision.relativeVelocity.magnitude}");
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // If the hole is a trigger, we can detect entry here
        if (other.CompareTag("Hole") && !isFalling)
        {
            StartFall();
        }
    }
    #endregion
    
    #region Public Control Methods
    public void SetDrivingSpeed(float speed)
    {
        drivingSpeed = speed;
    }
    
    public void StopCar()
    {
        isDriving = false;
        StopEngineSound();
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
    }
    
    public bool IsCurrentlyFalling()
    {
        return isFalling;
    }
    
    public bool IsCurrentlyDriving()
    {
        return isDriving;
    }
    #endregion
    
    private void OnDestroy()
    {
        // Restore original gravity
        Physics.gravity = originalGravity;
    }
    
    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw detection rays
        Gizmos.color = Color.yellow;
        Vector3 rayOrigin = transform.position + transform.TransformDirection(detectionOffset);
        Gizmos.DrawWireSphere(rayOrigin, 0.2f);
        Gizmos.DrawRay(rayOrigin, Vector3.down * detectionDistance);
        
        // Draw ahead detection
        Vector3 aheadPosition = rayOrigin + drivingDirection * 1f;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(aheadPosition, 0.15f);
        Gizmos.DrawRay(aheadPosition, Vector3.down * detectionDistance);
        
        // Draw center of mass
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(carRigidbody.centerOfMass), 0.1f);
    }
}