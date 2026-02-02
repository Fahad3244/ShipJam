using UnityEngine;

public class BoatWakeController : MonoBehaviour
{
    public ParticleSystem wakeParticles;
    public float emissionMultiplier = 30f;
    public float minMoveThreshold = 0.02f;

    Vector3 lastPosition;
    ParticleSystem.EmissionModule emission;

    void Start()
    {
        lastPosition = transform.position;
        emission = wakeParticles.emission;
    }

    void Update()
    {
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        if (speed > minMoveThreshold)
            emission.rateOverTime = speed * emissionMultiplier;
        else
            emission.rateOverTime = 0;
    }
}
