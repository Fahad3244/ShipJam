using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FailPanel : MonoBehaviour
{
    public bool isTimeUp = false;
    public bool isSpaceOut = false;
    public TextMeshProUGUI failText;


    void Start()
    {
        if (isTimeUp)
        {
            OnTimeUp();
        }
        else if (isSpaceOut)
        {
            OnSpaceOut();
        }
    }

    public void OnSpaceOut()
    {
        failText.text = "NO SPOT LEFT..";
    }

    public void OnTimeUp()
    {
        failText.text = "TIME'S UP..";
    }

    public void OnRetryButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
