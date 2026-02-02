using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingPage : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance.PlayMusic("BG");
    }
    public void OnStartButtonPressed()
    {
        AudioManager.Instance.PlaySFX("StartButton");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }
}
