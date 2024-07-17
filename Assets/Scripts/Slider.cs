using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Slider : MonoBehaviour
{
    public CameraController cameraController;
    public GameObject bg;
    public GameObject bgSlider;
    public GameObject fill;
    public GameObject handle;
    public TMP_Text text;
    private float minX;
    private float maxX;
    private RectTransform bgSliderRect;
    private GameObject soundSystem;

    void Start()
    {
        //SetParams();
        soundSystem = GameObject.FindGameObjectWithTag("SoundSystem");
        bgSliderRect = bgSlider.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        bgSliderRect.GetWorldCorners(corners);

        minX = corners[0].x;
        maxX = corners[2].x;
    }

    void Update()
    {
        if (cameraController.isPause)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(bg.GetComponent<RectTransform>(), touch.position) || RectTransformUtility.RectangleContainsScreenPoint(fill.GetComponent<RectTransform>(), touch.position) || RectTransformUtility.RectangleContainsScreenPoint(handle.GetComponent<RectTransform>(), touch.position))
                    {
                        float clampedX = Mathf.Clamp(touch.position.x, minX, maxX);
                        handle.transform.position = new Vector3(clampedX, handle.transform.position.y, handle.transform.position.z);
                        float percentage = (clampedX - minX) / (maxX - minX);
                        int value = Mathf.RoundToInt(percentage * 100);
                        text.text = value.ToString();
                        
                        RectTransform fillRect = fill.GetComponent<RectTransform>();
                        float fillWidth = percentage * bgSliderRect.rect.width;
                        fillRect.sizeDelta = new Vector2(fillWidth, fillRect.sizeDelta.y);
                        fillRect.position = new Vector3((minX + ((handle.transform.position.x - minX) / 2)), fillRect.position.y, fillRect.position.z);

                        switch(bg.name)
                        {
                            case "Music Volume":
                                soundSystem.GetComponent<AudioSource>().volume = percentage;
                                PlayerPrefs.SetFloat("volume", percentage);
                                break;
                            case "Movement Sensitivity":
                                float clampedMovementSpeed = Mathf.Clamp(percentage * cameraController.maxMovementSpeed, cameraController.maxMovementSpeed / 4, cameraController.maxMovementSpeed);
                                cameraController.movementSpeed = clampedMovementSpeed;
                                break;
                            case "Rotation Sensitivity":
                                float clampedRotationSpeed = Mathf.Clamp(percentage * cameraController.maxRotationSpeed, cameraController.maxRotationSpeed / 4, cameraController.maxRotationSpeed);
                                cameraController.rotationSpeed = clampedRotationSpeed;
                                break;
                            case "Zoom Speed":
                                float clampedZoomSpeed = Mathf.Clamp(percentage * cameraController.maxZoomSpeed, cameraController.maxZoomSpeed / 4, cameraController.maxZoomSpeed);
                                cameraController.zoomSpeed = clampedZoomSpeed;
                                break;    
                        }
                        PlayerPrefs.Save();
                    }
                }
            }
        }
    }

    public void SetParams()
    {
        switch(bg.name)
        {
            case "Music Volume":
                float value = PlayerPrefs.GetFloat("volume", 0.5f);
                text.text = (value * 100).ToString();
                
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                float fillWidth = value * bgSliderRect.rect.width;
                fillRect.sizeDelta = new Vector2(fillWidth, fillRect.sizeDelta.y);
                fillRect.position = new Vector3((minX + ((handle.transform.position.x - minX) / 2)), fillRect.position.y, fillRect.position.z);
                break;
            case "Movement Sensitivity":
                
                break;
            case "Rotation Sensitivity":
                
                break;
            case "Zoom Speed":
                
                break;    
        }
    }
}
