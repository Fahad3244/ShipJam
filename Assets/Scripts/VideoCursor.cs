using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// special cursor for making videos
/// </summary>
public class VideoCursor : MonoBehaviour
{
    public Transform cursorObject;
    public Image cursoeImage;

    public Color PressedColor = Color.white;
    public Vector3 PressedScale = Vector3.one;
    public float PressedEffectDuration = 0.15f;

    public Color ReleasedColor = Color.white;
    public Vector3 ReleasedScale = Vector3.one;
    public float ReleasedEffectDuration = 0.15f;

    public bool DoVideo = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!DoVideo)
            return;

        cursorObject.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!DoVideo) 
            return;

        var input = InputManagerCursor.instance;

        if(input.PointerInputDown)
        {
            cursorObject.DOScale(PressedScale, PressedEffectDuration);
            cursoeImage.DOColor(PressedColor, PressedEffectDuration);
        }

        if(input.PointerInputUp)
        {
            cursorObject.transform.DOScale(ReleasedScale, ReleasedEffectDuration);
            cursoeImage.DOColor(ReleasedColor, ReleasedEffectDuration);
        }

        cursorObject.transform.position = input.PointerInputPosition;
    }
}
