using UnityEngine;
using TMPro;

public class UiScaler : MonoBehaviour
{
    private float screenWidth, screenHeight;
    private float refX = 3120, refY = 1440;

    void Start()
    {
        // Initialiser screenWidth et screenHeight
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        // Adapter la position et taille de la UI au démarrage
        ScaleAndRepositionRectTransformRecursively(transform);

        refX = screenWidth;
        refY = screenHeight;
    }

    void Update()
    {
        // Vérifier les changements de résolution
        if (Screen.width != screenWidth || Screen.height != screenHeight)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            // Réadapter la UI en cas de changement de résolution
            ScaleAndRepositionRectTransformRecursively(transform);

            refX = screenWidth;
            refY = screenHeight;
        }
    }

    void ScaleAndRepositionRectTransformRecursively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            /*if(!child.CompareTag("Slider"));
            {*/
                TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    // Adapter la taille de la police en fonction des ratios X et Y
                    float originalFontSize = text.fontSize;
                    float scaleFactorX = screenWidth / refX;
                    float scaleFactorY = screenHeight / refY;
                    float newFontSize = originalFontSize * Mathf.Sqrt(scaleFactorX * scaleFactorY);
                    text.fontSize = newFontSize;
                }

                RectTransform rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Calculer la nouvelle taille
                    float originalWidth = rectTransform.rect.width;
                    float originalHeight = rectTransform.rect.height;
                    float newWidth = (screenWidth / refX) * originalWidth;
                    float newHeight = (screenHeight / refY) * originalHeight;

                    // Appliquer la nouvelle taille
                    rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

                    // Calculer la nouvelle position
                    float originalPosX = rectTransform.anchoredPosition.x;
                    float originalPosY = rectTransform.anchoredPosition.y;
                    float newPosX = (screenWidth / refX) * originalPosX;
                    float newPosY = (screenHeight / refY) * originalPosY;

                    // Appliquer la nouvelle position
                    rectTransform.anchoredPosition = new Vector2(newPosX, newPosY);
                }
            //}
            
            // Appeler récursivement pour les enfants de cet enfant
            ScaleAndRepositionRectTransformRecursively(child);
        }
    }
}