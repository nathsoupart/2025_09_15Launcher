using UnityEngine;
using TMPro;

/// <summary>
/// Masque dynamiquement les parties d’un TMP_Text dépassant le viewport
/// en ajustant l’alpha des vertices (compatible XR, sans caméra ; soft edge en option).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class InvisibilityTMP : MonoBehaviour
{
    public RectTransform viewport;
    [Tooltip("Largeur de la bande de transition en pixels TMP (bord flou)")]
    public float softEdge = 8f;

    private TMP_Text tmp;
    private Vector3[] worldCorners = new Vector3[4];

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    void LateUpdate()
    {
        if (!viewport || !tmp || !tmp.enabled) return;
        tmp.ForceMeshUpdate();

        // Coins du viewport en local TMP
        viewport.GetWorldCorners(worldCorners);
        for (int i = 0; i < 4; i++)
            worldCorners[i] = tmp.rectTransform.InverseTransformPoint(worldCorners[i]);

        float left = worldCorners[0].x, right = worldCorners[2].x;
        float bottom = worldCorners[0].y, top = worldCorners[2].y;

        var textInfo = tmp.textInfo;
        for (int c = 0; c < textInfo.characterCount; c++)
        {
            var charInfo = textInfo.characterInfo[c];
            if (!charInfo.isVisible) continue;

            int matIdx = charInfo.materialReferenceIndex;
            int vertIdx = charInfo.vertexIndex;
            var verts = textInfo.meshInfo[matIdx].vertices;
            var colors = textInfo.meshInfo[matIdx].colors32;

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = verts[vertIdx + j];
                byte alpha = 255;

                // Flou sur chaque bord avec softEdge en pixels TMP
                if (v.x < left)      alpha = (byte)(Mathf.Clamp01((v.x - (left - softEdge)) / softEdge) * 255);
                else if (v.x > right)  alpha = (byte)(Mathf.Clamp01(((right + softEdge) - v.x) / softEdge) * 255);

                if (v.y < bottom)      alpha = (byte)Mathf.Min(alpha, Mathf.Clamp01((v.y - (bottom - softEdge)) / softEdge) * 255);
                else if (v.y > top)    alpha = (byte)Mathf.Min(alpha, Mathf.Clamp01(((top + softEdge) - v.y) / softEdge) * 255);

                colors[vertIdx + j].a = alpha;
            }
        }

        // Appliquer le changement couleur à l’affichage
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
