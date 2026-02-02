using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [SerializeField] private int targetFPS = 60;

    void Awake()
    {
        // Disable VSync so Application.targetFrameRate works
        QualitySettings.vSyncCount = 0;

        // Set target FPS
        Application.targetFrameRate = targetFPS;
    }
}
