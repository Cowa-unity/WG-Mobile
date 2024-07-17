using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private GUIStyle style;
    private Rect rect;

    public float currentFPS;

    private int screenWidth;
    private int screenHeight;

    void Start()
    {
        screenWidth = Screen.currentResolution.width;
        screenHeight = Screen.currentResolution.height;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 120;

        int w = Screen.width, h = Screen.height;
        rect = new Rect(0, 0, w, h * 2 / 100);
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.white;
    }

    void Update()
    {
        currentFPS = 1.0f / Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        string text = string.Format("Avg: {0} fps", (int)currentFPS);
        GUI.Label(rect, text, style);
    }
}