using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    private LevelManager levelManager;
    
    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    public Image timerBG;

    [Header("Timer Settings")]
    public float startTimeInSeconds = 60f; // Example: 1 minute

    [Header("Animation Settings")]
    [Range(0.1f, 1f)]
    public float popDuration = 0.2f;
    [Range(1.1f, 2f)]
    public float popScale = 1.3f;
    [Range(0.1f, 1f)]
    public float warningFlashDuration = 0.15f;

    [Header("Color Settings")]
    public Color normalTextColor = Color.white;
    public Color warningTextColor = Color.red;
    public Color normalBGColor = Color.blue;
    public Color warningBGColor = Color.red;

    private float currentTime;
    private bool isRunning = false;
    private int lastSecond = -1;
    private Vector3 originalTextScale;
    private Color originalTextColor;
    private Color originalBGColor;
    private bool isInWarningMode = false;

    void Start()
    {
        // Store original values
        if (timerText != null)
        {
            originalTextScale = timerText.transform.localScale;
            originalTextColor = timerText.color;
        }
        
        if (timerBG != null)
        {
            originalBGColor = timerBG.color;
        }

        // Set initial colors
        SetNormalColors();
    }

    public void SetLevelManager(LevelManager manager)
    {
        levelManager = manager;
    }

    public void SetTimer(int seconds)
    {
        startTimeInSeconds = seconds;
        StartTimer(startTimeInSeconds);
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            TimerFinished();
        }

        UpdateTimerDisplay(currentTime);
        CheckForSecondChange();
        CheckWarningMode();
    }

    // Call this to start timer with any value
    public void StartTimer(float timeInSeconds)
    {
        currentTime = timeInSeconds;
        isRunning = true;
        lastSecond = Mathf.FloorToInt(currentTime);
        UpdateTimerDisplay(currentTime);
        SetNormalColors();
        isInWarningMode = false;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void UpdateTimerDisplay(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void CheckForSecondChange()
    {
        int currentSecond = Mathf.FloorToInt(currentTime);
        
        if (currentSecond != lastSecond && currentSecond >= 0)
        {
            lastSecond = currentSecond;
            TriggerPopEffect();
            
            // Trigger warning flash only on second change if in warning mode
            if (currentTime <= 10f && currentTime > 0f)
            {
                TriggerWarningFlash();
            }
        }
    }

    private void CheckWarningMode()
    {
        bool shouldBeInWarning = currentTime <= 10f && currentTime > 0f;
        
        if (shouldBeInWarning && !isInWarningMode)
        {
            if (!isInWarningMode) AudioManager.Instance.PlaySFX("Clock");
            isInWarningMode = true;
            // Don't start continuous flashing, just set warning state
        }
        else if (!shouldBeInWarning && isInWarningMode)
        {
            isInWarningMode = false;
            SetNormalColors();
        }
    }

    private void TriggerPopEffect()
    {
        if (timerText != null)
        {
            StartCoroutine(PopEffectCoroutine());
        }
    }

    private void TriggerWarningFlash()
    {
        if (timerBG != null)
        {
            StartCoroutine(WarningFlashCoroutine());
        }
    }

    private IEnumerator PopEffectCoroutine()
    {
        // Scale up with smooth easing
        float elapsed = 0f;
        Vector3 startScale = timerText.transform.localScale;
        Vector3 targetScale = originalTextScale * popScale;
        
        while (elapsed < popDuration / 2)
        {
            float t = elapsed / (popDuration / 2);
            t = SmoothStep(t); // Much smoother easing
            timerText.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale back down with smooth easing
        elapsed = 0f;
        startScale = timerText.transform.localScale;
        while (elapsed < popDuration / 2)
        {
            float t = elapsed / (popDuration / 2);
            t = SmoothStep(t); // Much smoother easing
            timerText.transform.localScale = Vector3.Lerp(startScale, originalTextScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we end at original scale
        timerText.transform.localScale = originalTextScale;
    }

    private IEnumerator WarningFlashCoroutine()
    {
        // Flash to warning colors
        SetWarningColors();
        yield return new WaitForSeconds(warningFlashDuration);
        
        // Flash back to normal colors
        SetNormalColors();
    }

    private void SetNormalColors()
    {
        if (timerText != null)
            timerText.color = normalTextColor;
        
        if (timerBG != null)
            timerBG.color = normalBGColor;
    }

    private void SetWarningColors()
    {
        if (timerText != null)
            timerText.color = warningTextColor;
        
        if (timerBG != null)
            timerBG.color = warningBGColor;
    }

    // Smooth easing function for natural animation
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void TimerFinished()
    {
        // Reset colors when timer finishes
        SetNormalColors();
        isInWarningMode = false;
        
        levelManager.OnLevelFailed(true, false); // Notify LevelManager that time is up
    }
}