using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CameraController : MonoBehaviour
{
    public float movementSpeed;
    public float rotationSpeed;
    public float zoomSpeed;
    public GameObject zoomInButton;
    public GameObject zoomOutButton;
    public GameObject joystickMovementBg;
    public GameObject joystickMovementCenter;
    public GameObject rotateLeft;
    public GameObject rotateRight;
    public GameObject optionsButton;
    public GameObject optionsMenu;
    public GameObject regenerationButton;
    public int reversedMovement = 1;
    public int reversedRotation = 1;
    public bool isPause = false;
    
    public float maxMovementSpeed = 200;
    public float maxRotationSpeed = 200;
    public float maxZoomSpeed = 200;

    void Start()
    {
        float volume = PlayerPrefs.GetFloat("volume", 0.5f);
        GameObject soundSystem = GameObject.FindGameObjectWithTag("SoundSystem");
        soundSystem.GetComponent<AudioSource>().volume = volume;
    }

    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if(RectTransformUtility.RectangleContainsScreenPoint(optionsButton.GetComponent<RectTransform>(), touch.position))
                {
                    isPause = !isPause;
                    if(isPause)
                    {
                        optionsButton.GetComponent<Image>().color = Color.red;
                    }
                    else
                    {
                        optionsButton.GetComponent<Image>().color = Color.white;
                    }
                    zoomInButton.GetComponent<Image>().enabled = !isPause;
                    zoomInButton.transform.GetChild(0).GetComponent<Image>().enabled = !isPause;
                    zoomOutButton.transform.GetChild(0).GetComponent<Image>().enabled = !isPause;
                    zoomOutButton.GetComponent<Image>().enabled = !isPause;
                    rotateLeft.GetComponent<Image>().enabled = !isPause;
                    rotateRight.GetComponent<Image>().enabled = !isPause;
                    joystickMovementBg.GetComponent<Image>().enabled = !isPause;
                    regenerationButton.transform.parent.GetComponent<Image>().enabled = !isPause;
                    regenerationButton.GetComponent<TMP_Text>().enabled = !isPause;
                    EnableImageAndTextRecursively(optionsMenu.transform, isPause);
                }
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && !isPause)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(zoomInButton.GetComponent<RectTransform>(), touch.position))
                {
                    Zoom(true);
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(zoomOutButton.GetComponent<RectTransform>(), touch.position))
                {
                    Zoom(false);
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(rotateLeft.GetComponent<RectTransform>(), touch.position))
                {
                    float rotateAmountY = -rotationSpeed * reversedRotation * Time.deltaTime;
                    transform.Rotate(Vector3.up, rotateAmountY, Space.World);
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(rotateRight.GetComponent<RectTransform>(), touch.position))
                {
                    float rotateAmountY = rotationSpeed * reversedRotation * Time.deltaTime;
                    transform.Rotate(Vector3.up, rotateAmountY, Space.World);
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(joystickMovementBg.GetComponent<RectTransform>(), touch.position))
                {
                    Vector2 touchDelta = touch.position - (Vector2)joystickMovementCenter.transform.position;
                    if (touchDelta.sqrMagnitude > 1f)
                    {
                        Vector3 move = new Vector3(touchDelta.x, 0, touchDelta.y);
                        Vector3 cameraForward = transform.forward;
                        cameraForward.y = 0; 
                        cameraForward.Normalize();

                        Vector3 cameraRight = transform.right;
                        cameraRight.y = 0; 
                        cameraRight.Normalize();

                        Vector3 direction = (cameraRight * move.x * reversedMovement + cameraForward * move.z * reversedMovement).normalized;
                        
                        Vector3 newPosition = transform.position + direction * movementSpeed * Time.deltaTime;
                        newPosition.x = Mathf.Clamp(newPosition.x, -25f, 225f);
                        newPosition.z = Mathf.Clamp(newPosition.z, -30f, 255f);

                        transform.position = newPosition;
                    }
                }
            }
        }
    }

    public void EnableImageAndTextRecursively(Transform parentTransform, bool enable)
    {
        if(parentTransform.CompareTag("CheckBoxMovement"))
        {
            if(!enable)
            {
                Image imageComponent = parentTransform.GetComponent<Image>();
                if (imageComponent != null)
                {
                    imageComponent.enabled = enable;
                }
            }
            else if(reversedMovement == -1)
            {
                Image imageComponent = parentTransform.GetComponent<Image>();
                if (imageComponent != null)
                {
                    imageComponent.enabled = true;
                }
            }
        }
        else if(parentTransform.CompareTag("CheckBoxRotation"))
        {
            if(!enable)
            {
                Image imageComponent = parentTransform.GetComponent<Image>();
                if (imageComponent != null)
                {
                    imageComponent.enabled = enable;
                }
            }
            else if(reversedRotation == -1)
            {
                Image imageComponent = parentTransform.GetComponent<Image>();
                if (imageComponent != null)
                {
                    imageComponent.enabled = true;
                }
            }
        }
        else
        {
            Image imageComponent = parentTransform.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.enabled = enable;
            }

            TMP_Text textComponent = parentTransform.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.enabled = enable;
            }
        }
        

        foreach (Transform child in parentTransform)
        {
            EnableImageAndTextRecursively(child, enable);
        }
    }
    

    public void Zoom(bool isZooming)
    {
        float directionFactor = isZooming ? 1f : -1f;
        Vector3 moveDirection = transform.forward * directionFactor;
        Vector3 newPosition = transform.position + moveDirection * zoomSpeed * Time.deltaTime;
        newPosition.y = Mathf.Clamp(newPosition.y, 50f, 130f);
        transform.position = new Vector3(transform.position.x, newPosition.y, transform.position.z);

        float minY = 50f;
        float maxY = 130f;
        float minRotationX = 35f;
        float maxRotationX = 55f;

        float t = (newPosition.y - minY) / (maxY - minY); 
        float newRotationX = Mathf.Lerp(minRotationX, maxRotationX, t);

        Vector3 newRotation = transform.eulerAngles;
        newRotation.x = newRotationX;
        transform.eulerAngles = newRotation;
    }


}
