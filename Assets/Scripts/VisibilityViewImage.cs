using UnityEngine;
using UnityEngine.UI;

public class VisibilityViewImage : MonoBehaviour
{
    public RectTransform viewport; // Le viewport du ScrollView
    public Image previewImage;     // L’image à afficher/masquer

    void LateUpdate()
    {
        if (!viewport || !previewImage) return;

        // Vérifie si le centre de l'image est dans la zone visible
        Vector3 worldCenter = previewImage.rectTransform.TransformPoint(previewImage.rectTransform.rect.center);
        bool isVisible = RectTransformUtility.RectangleContainsScreenPoint(viewport, worldCenter);

        // Pour du masquage : active/désactive l’image ou manipulate alpha
        previewImage.enabled = isVisible; // Rend totalement invisible si hors viewport

        // Variante : pour un effet progressif (alpha graduel, comme pour TMP)
        // Tu peux aussi manipuler le color.a au lieu d’'enabled'.
        /*
        Color col = previewImage.color;
        col.a = isVisible ? 1f : 0f;
        previewImage.color = col;
        */
    }
}