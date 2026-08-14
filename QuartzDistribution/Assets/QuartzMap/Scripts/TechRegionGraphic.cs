using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TechRegionGraphic : MaskableGraphic
    {
        private static readonly Vector2[] Shape =
        {
            new Vector2(.08f,.38f), new Vector2(.18f,.68f), new Vector2(.38f,.78f), new Vector2(.57f,.68f),
            new Vector2(.82f,.82f), new Vector2(.93f,.58f), new Vector2(.82f,.33f), new Vector2(.62f,.18f),
            new Vector2(.37f,.24f), new Vector2(.18f,.18f)
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            Color fill = color;
            fill.a *= .14f;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = fill;
            vertex.position = rect.center;
            vh.AddVert(vertex);
            for (int i = 0; i < Shape.Length; i++)
            {
                vertex.position = Point(rect, Shape[i]);
                vh.AddVert(vertex);
            }
            for (int i = 0; i < Shape.Length; i++) vh.AddTriangle(0, i + 1, ((i + 1) % Shape.Length) + 1);
            for (int i = 0; i < Shape.Length; i++) AddLine(vh, Point(rect, Shape[i]), Point(rect, Shape[(i + 1) % Shape.Length]), color, 4f);
        }

        private static Vector2 Point(Rect rect, Vector2 normalized)
        {
            return new Vector2(rect.xMin + normalized.x * rect.width, rect.yMin + normalized.y * rect.height);
        }

        private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 normal = new Vector2(-(end - start).y, (end - start).x).normalized * width;
            int index = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = start + normal; vh.AddVert(vertex);
            vertex.position = end + normal; vh.AddVert(vertex);
            vertex.position = end - normal; vh.AddVert(vertex);
            vertex.position = start - normal; vh.AddVert(vertex);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
