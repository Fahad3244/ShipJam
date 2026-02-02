using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Loading Duration")]
    [Tooltip("Total time (seconds) to reach 100%")]
    public float loadingTime = 3f;

    [Header("UI References")]
    public Image fillImage;              // Horizontal fill image
    public TMP_Text percentageText;      // 0% - 100%
    public TMP_Text loadingText;         // Loading...

    [Header("Text Animation")]
    public float loadingTextSpeed = 0.5f;

    [Header("Disable After Complete")]
    public GameObject loadingPanel;      // Parent panel to disable

    private bool loadingComplete = false;

    void Start()
    {
        fillImage.fillAmount = 0f;
        percentageText.text = "0%";

        StartCoroutine(FillLoadingBar());
        StartCoroutine(AnimateLoadingText());
    }

    IEnumerator FillLoadingBar()
    {
        float elapsedTime = 0f;

        while (elapsedTime < loadingTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / loadingTime);

            fillImage.fillAmount = progress;
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        // Ensure final state
        fillImage.fillAmount = 1f;
        percentageText.text = "100%";

        loadingComplete = true;

        yield return new WaitForSeconds(0.3f);

        // Disable loading screen
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    IEnumerator AnimateLoadingText()
    {
        string baseText = "Loading";
        int dotCount = 0;

        while (!loadingComplete)
        {
            dotCount = (dotCount + 1) % 4; // 0 to 3 dots
            loadingText.text = baseText + new string('.', dotCount);

            yield return new WaitForSeconds(loadingTextSpeed);
        }

        loadingText.text = "Loading...";
    }
}
