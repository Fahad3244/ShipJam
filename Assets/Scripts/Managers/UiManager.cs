using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class UiManager : MonoBehaviour
{
    public LevelManager levelManager; // Reference to LevelManager to notify about level completion
    public TextMeshProUGUI levelText; // Assign this in the inspector
    public WinPanel winPanel;
    public FailPanel losePanel;
    public CountdownTimer countdownTimer;
    public GameObject tutorialPanel;

    void Start()
    {
        DOVirtual.DelayedCall(0.5f, () =>
        {
            CheckTutorial();
        });
        if (levelManager != null)
        {
            levelManager.uiManager = this; // Set the reference in LevelManager
        }
    }

    public void StartCountdown(int seconds)
    {
        if (countdownTimer != null)
        {
            countdownTimer.gameObject.SetActive(true);
            countdownTimer.SetLevelManager(levelManager);
            countdownTimer.SetTimer(seconds);
        }
    }


    public void OnLevelWin()
    {
        AudioManager.Instance.PlaySFX("GameWin");
        countdownTimer.StopTimer();
        winPanel.gameObject.SetActive(true);
        
    }

    public void OnLevelLose(bool isTimeUp = false, bool isSpaceOut = false)
    {
        AudioManager.Instance.PlaySFX("GameFail");
        losePanel.gameObject.SetActive(true);
        losePanel.isSpaceOut = isSpaceOut;
        losePanel.isTimeUp = isTimeUp;
        countdownTimer.StopTimer();
    }
    public void UpdateLevelText(int currentLevelNum, int totalLevels)
    {
        if (levelText != null)
        {
            levelText.text = $"{currentLevelNum}";
        }
    }

    public void CheckTutorial()
    {
        if (tutorialPanel != null)
        {
            if (levelManager.levelProgressManager.CurrentLevelIndex == 0)
            {
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    tutorialPanel.SetActive(true);
                });
            }
            else
            {
                tutorialPanel.SetActive(false);
            }
        }
    }
}
