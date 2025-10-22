using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère la visibilité des éléments dans un Scroll View (version XR-friendly, non destructive).
/// Les boutons hors du viewport sont masqués visuellement et désactivés en interaction XR.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ScrollViewVisibilityController : MonoBehaviour
{
    [Header("Références")]
    public ScrollRect scrollRect;           // Ton ScrollRect
    public RectTransform viewport;          // Le viewport (souvent ScrollView/Viewport)
    public RectTransform content;           // Le Content (éléments enfants)
    public Camera eventCamera;              // La caméra XR (souvent MainCamera ou XR Origin Camera)

    [Header("Paramètres")]
    [Tooltip("Rafraîchir chaque frame (coûte un peu plus, mais utile pour scroll rapide)")]
    public bool continuousUpdate = true;

    private void Awake()
    {
        // Auto-détection si non assigné dans l’inspecteur
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        if (!viewport) viewport = scrollRect.viewport;
        if (!content) content = scrollRect.content;
        if (!eventCamera && scrollRect.GetComponentInParent<Canvas>())
            eventCamera = scrollRect.GetComponentInParent<Canvas>().worldCamera;
    }

    private void Update()
    {
        if (continuousUpdate)
            UpdateVisibility();
    }

    private void LateUpdate()
    {
        if (!continuousUpdate)
            UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (!scrollRect || !viewport || !content) return;

        foreach (RectTransform child in content)
        {
            // Centre du bouton dans le monde
            Vector3 worldCenter = child.TransformPoint(child.rect.center);

            // Vérifie si le centre du bouton est à l'intérieur du viewport visible
            bool isVisible = RectTransformUtility.RectangleContainsScreenPoint(
                viewport,
                worldCenter,
                eventCamera
            );

            // Récupère (ou crée) un CanvasGroup pour gérer visibilité + interactions
            var cg = child.GetComponent<CanvasGroup>();
            if (!cg) cg = child.gameObject.AddComponent<CanvasGroup>();

            if (isVisible)
            {
                if (cg.alpha != 1)
                {
                    cg.alpha = 1;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
            else
            {
                if (cg.alpha != 0)
                {
                    cg.alpha = 0;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }
        }
    }
}
