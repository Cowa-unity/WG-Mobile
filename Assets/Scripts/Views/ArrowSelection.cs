using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ArrowSelection : MonoBehaviour
{
    public CameraController cameraController;
    public GameObject leftArrow;
    public GameObject rightArrow;
    public TMP_Text text;
    [SerializeField] public List<string> key;
    private string currentKey = "100%";
    private int nativeWidth;
    private int nativeHeight; 
    public Image leftArrowImage;
    public Image rightArrowImage;

    void Start()
    {
        nativeWidth = Screen.currentResolution.width;
        nativeHeight = Screen.currentResolution.height;
        leftArrowImage = leftArrow.GetComponent<Image>();
        rightArrowImage = rightArrow.GetComponent<Image>();
    }

    void Update()
    {
        if (cameraController.isPause)
        {
            if(key.IndexOf(currentKey) == 0)
            {
                leftArrowImage.enabled = false;
            }
            else
            {
                leftArrowImage.enabled = true;
            }
            if(key.IndexOf(currentKey) == key.Count -1)
            {
                rightArrowImage.enabled = false;
            }
            else
            {
                rightArrowImage.enabled = true;
            }
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(leftArrow.GetComponent<RectTransform>(), touch.position))
                    {
                        int currentIndex = key.IndexOf(currentKey);
                        if (currentIndex > 0)
                        {
                            currentKey = key[currentIndex - 1];
                            text.text = currentKey;
                            UpdateResolution((key.IndexOf(currentKey) + 1)  * 0.2f);
                        }
                    }
                    else if (RectTransformUtility.RectangleContainsScreenPoint(rightArrow.GetComponent<RectTransform>(), touch.position))
                    {
                        int currentIndex = key.IndexOf(currentKey);
                        if (currentIndex < key.Count - 1)
                        {
                            currentKey = key[currentIndex + 1];
                            text.text = currentKey;
                            UpdateResolution((key.IndexOf(currentKey) + 1) * 0.2f);
                        }
                        
                    }
                }
            }
        }
    }

    void UpdateResolution(float reductionFactor)
    {
        int newWidth = Mathf.RoundToInt(nativeWidth * reductionFactor);
        int newHeight = Mathf.RoundToInt(nativeHeight * reductionFactor);
        Screen.SetResolution(newWidth, newHeight, true);
    }
}
