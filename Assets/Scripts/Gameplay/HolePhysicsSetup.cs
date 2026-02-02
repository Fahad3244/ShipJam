using UnityEngine;

public class HolePhysicsSetup : MonoBehaviour
{
    [Header("Hole Configuration")]
    [SerializeField] private float holeDepth = 10f;
    [SerializeField] private float holeRadius = 3f;
    [SerializeField] private LayerMask holeLayer = 1;
    
    [Header("Collision Setup")]
    [SerializeField] private bool createHoleCollider = true;
    [SerializeField] private bool createBottomCollider = true;
    [SerializeField] private PhysicMaterial holePhysicsMaterial;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem dustEffect;
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private AudioClip[] impactSounds;
    
    private BoxCollider holeDetector;
    private BoxCollider bottomCollider;
    
    void Start()
    {
        SetupHolePhysics();
    }
    
    private void SetupHolePhysics()
    {
        // Set the hole to the correct layer
        gameObject.layer = Mathf.RoundToInt(Mathf.Log(holeLayer.value, 2));
        
        if (createHoleCollider)
        {
            CreateHoleDetector();
        }
        
        if (createBottomCollider)
        {
            CreateBottomCollider();
        }
        
        // Setup audio source
        if (impactAudio == null)
            impactAudio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }
    
    private void CreateHoleDetector()
    {
        // Create hole detector (trigger)
        GameObject holeDetectorObj = new GameObject("HoleDetector");
        holeDetectorObj.transform.SetParent(transform);
        holeDetectorObj.transform.localPosition = Vector3.zero;
        holeDetectorObj.layer = gameObject.layer;
        holeDetectorObj.tag = "Hole";
        
        holeDetector = holeDetectorObj.AddComponent<BoxCollider>();
        holeDetector.isTrigger = true;
        holeDetector.size = new Vector3(holeRadius * 2, 0.5f, holeRadius * 2);
        holeDetector.center = new Vector3(0, 0.25f, 0);
    }
    
    private void CreateBottomCollider()
    {
        // Create bottom collider for the hole
        GameObject bottomObj = new GameObject("HoleBottom");
        bottomObj.transform.SetParent(transform);
        bottomObj.transform.localPosition = new Vector3(0, -holeDepth, 0);
        bottomObj.layer = 0; // Default layer for collision
        
        bottomCollider = bottomObj.AddComponent<BoxCollider>();
        bottomCollider.size = new Vector3(holeRadius * 2.2f, 1f, holeRadius * 2.2f);
        
        if (holePhysicsMaterial != null)
            bottomCollider.material = holePhysicsMaterial;
        
        // Add impact handler
        HoleImpactHandler impactHandler = bottomObj.AddComponent<HoleImpactHandler>();
        impactHandler.Initialize(this);
    }
    
    public void OnCarImpact(Collision collision, float impactForce)
    {
        // Play impact effects
        if (dustEffect != null)
        {
            dustEffect.transform.position = collision.contacts[0].point;
            dustEffect.Play();
        }
        
        if (impactAudio != null && impactSounds != null && impactSounds.Length > 0)
        {
            AudioClip clip = impactSounds[Random.Range(0, impactSounds.Length)];
            impactAudio.PlayOneShot(clip, Mathf.Clamp01(impactForce / 10f));
        }
        
        Debug.Log($"Car impacted hole bottom with force: {impactForce}");
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw hole bounds
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(holeRadius * 2, 0.1f, holeRadius * 2));
        
        // Draw hole depth
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.down * holeDepth, 
                           new Vector3(holeRadius * 2.2f, 1f, holeRadius * 2.2f));
        
        // Draw hole sides
        Gizmos.color = Color.cyan;
        Vector3[] corners = {
            transform.position + new Vector3(-holeRadius, 0, -holeRadius),
            transform.position + new Vector3(holeRadius, 0, -holeRadius),
            transform.position + new Vector3(holeRadius, 0, holeRadius),
            transform.position + new Vector3(-holeRadius, 0, holeRadius)
        };
        
        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i] + Vector3.down * holeDepth);
        }
    }
}

// Separate component to handle impact events at the bottom of the hole
public class HoleImpactHandler : MonoBehaviour
{
    private HolePhysicsSetup holeSetup;
    
    public void Initialize(HolePhysicsSetup setup)
    {
        holeSetup = setup;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<RealisticCarFallAnimator>() != null)
        {
            float impactForce = collision.relativeVelocity.magnitude;
            holeSetup?.OnCarImpact(collision, impactForce);
        }
    }
}