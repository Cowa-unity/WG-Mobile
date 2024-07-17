using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Checkbox : MonoBehaviour
{
    public CameraController cameraController;
    public Image Image;

    void Update()
    {
        if (cameraController.isPause)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(Image.GetComponent<RectTransform>(), touch.position))
                    {
                        Image.enabled = !Image.enabled;
                        if(Image.enabled){ cameraController.alternativeControls = -1;}
                        else if(!Image.enabled){ cameraController.alternativeControls = 1;}
                    }
                }
            }
        }
        
    }
}
