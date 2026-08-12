using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ScanRingGraphic : MaskableGraphic
{
    [SerializeField, Min(3)] private int dashCount = 48;
    [SerializeField, Min(1f)] private float thickness = 4f;
    [SerializeField, Range(0.1f, 1f)] private float dashFill = 0.55f;
    [SerializeField] private bool solid;

    public void Configure(bool isSolid, int count, float width, float fill)
    {
        solid = isSolid;
        dashCount = Mathf.Max(3, count);
        thickness = width;
        dashFill = fill;
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int count = solid ? 96 : dashCount;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f - thickness;
        float step = Mathf.PI * 2f / count;
        float arc = solid ? step : step * dashFill;
        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            float b = a + arc;
            AddArc(vh, a, b, radius - thickness * 0.5f, radius + thickness * 0.5f);
        }
    }

    private void AddArc(VertexHelper vh, float a, float b, float inner, float outer)
    {
        int index = vh.currentVertCount;
        Color32 c = color;
        vh.AddVert(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inner, c, Vector2.zero);
        vh.AddVert(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * outer, c, Vector2.up);
        vh.AddVert(new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * outer, c, Vector2.one);
        vh.AddVert(new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * inner, c, Vector2.right);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }
}
