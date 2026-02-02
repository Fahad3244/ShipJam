using UnityEngine;
using UnityEngine.UI;

public class AudioUIController : MonoBehaviour
{
    [Header("Configuration")]
    public bool isMusicControl; // Check this for Music button, uncheck for SFX
    
    [Header("UI Elements")]
    public Image iconImage;
    public Sprite spriteOn;
    public Sprite spriteOff;

    void Start()
    {
        UpdateUI();
    }

    public void OnTogglePressed()
    {
        if (isMusicControl)
            AudioManager.Instance.ToggleMusic();
        else
            AudioManager.Instance.ToggleSFX();

        UpdateUI();
    }

    private void UpdateUI()
    {
        bool isMuted = isMusicControl ? AudioManager.Instance.IsMusicMuted() : AudioManager.Instance.IsSFXMuted();
        
        // If muted, show 'Off' icon. If not muted, show 'On' icon.
        iconImage.sprite = isMuted ? spriteOff : spriteOn;
    }
}