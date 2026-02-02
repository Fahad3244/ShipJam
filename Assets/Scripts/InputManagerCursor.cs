using UnityEngine;

public class InputManagerCursor : MonoBehaviour
{
    public Vector2 PointerInputPosition { get; private set; }
    public bool PointerInputDown { get; private set; }
    public bool PointerInputHeld { get; private set; }
    public bool PointerInputUp { get; private set; }


    public static InputManagerCursor instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("Singleton Error Here" + transform.name);
        }
    }


#if UNITY_EDITOR
    private void Start()
    {
        Input.simulateMouseWithTouches = false;
    }
#endif

    // Update is called once per frame
    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            PointerInputPosition = touch.position;
            PointerInputDown = touch.phase == TouchPhase.Began;
            PointerInputHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            PointerInputUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else
        {
            PointerInputPosition = Input.mousePosition;
            PointerInputDown = Input.GetMouseButtonDown(0);
            PointerInputHeld = Input.GetMouseButton(0);
            PointerInputUp = Input.GetMouseButtonUp(0);
        }
    }
}
