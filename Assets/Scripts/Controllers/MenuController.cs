using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject findMatchBtn;
    public NetworkController networkController;

    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if(RectTransformUtility.RectangleContainsScreenPoint(findMatchBtn.GetComponent<RectTransform>(), touch.position))
                {
                    networkController.Connect();
                }
            }
        }
    }
}
