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
        Application.targetFrameRate = 60;

        int w = Screen.width, h = Screen.height;
        rect = new Rect(0, 0, w, h * 2 / 100);
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 3 / 100;
        style.normal.textColor = Color.white;
    }

    void Update()
    {
        currentFPS = 1.0f / Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        if(screenWidth != Screen.currentResolution.width)
        {
            screenWidth = Screen.currentResolution.width;
            screenHeight = Screen.currentResolution.height;

            int w = Screen.width, h = Screen.height;
            rect = new Rect(0, 0, w, h * 2 / 100);
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 3 / 100;
            style.normal.textColor = Color.white;
        }
        string text = string.Format("{0} fps", (int)currentFPS);
        GUI.Label(rect, text, style);
    }
}