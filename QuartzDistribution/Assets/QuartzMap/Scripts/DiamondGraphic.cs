using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DiamondGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float half = Mathf.Min(rect.width, rect.height) * .44f;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center + Vector2.up * half; vh.AddVert(vertex);
            vertex.position = center + Vector2.right * half; vh.AddVert(vertex);
            vertex.position = center + Vector2.down * half; vh.AddVert(vertex);
            vertex.position = center + Vector2.left * half; vh.AddVert(vertex);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
