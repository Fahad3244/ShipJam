using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinPanel : MonoBehaviour
{
    public string[] winTexts;
    public TMPro.TextMeshProUGUI winText;

    void OnEnable()
    {
        int index = Random.Range(0, winTexts.Length);
        if (index < winTexts.Length)
        {
            winText.text = winTexts[index];
        }
        else
        {
            winText.text = "Well done!";
        }
    }
    public void OnNextButtonPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
