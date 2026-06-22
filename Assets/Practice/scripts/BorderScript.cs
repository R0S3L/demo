using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIRectangle : MonoBehaviour
{
    [SerializeField] private RectTransform _areaRect;

    private RectTransform _rect;
    private RectTransform _top;
    private RectTransform _bottom;
    private RectTransform _left;
    private RectTransform _right;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _rect.pivot = new Vector2(0f, 0f); // origin — левый верхний угол рамки
        CreateBorders();
    }

    void Start()
    {
        DrawFromSettings();
    }

    public void DrawFromSettings()
    {
        if (SettingsManager.Instance == null || _areaRect == null)
            return;

        List<Vector2> saved = SettingsManager.Instance.Data.savedPositions;
        if (saved == null || saved.Count < 2)
            return;

        Vector2 areaSize = _areaRect.rect.size;

        // Денормализуем обе точки в пиксельное пространство _areaRect
        Vector2 p1 = CoordinateUtils.ToPixels(saved[0], areaSize);
        Vector2 p2 = CoordinateUtils.ToPixels(saved[1], areaSize);

        // Две точки — диагональные углы прямоугольника.
        // Находим реальные left/right/bottom/top независимо от порядка точек.
        float minx   = Mathf.Min(p1.x, p2.x);
        float maxx  = Mathf.Max(p1.x, p2.x);
        float miny = Mathf.Min(p1.y, p2.y);
        float maxy    = Mathf.Max(p1.y, p2.y);


        Vector2 position = new Vector2(minx, maxy);
        Vector2 size     = new Vector2(maxx - minx, maxy - miny);

        DrawRectangle(position, size);
    }

    public void DrawRectangle(Vector2 position, Vector2 size)
    {
        _rect.anchoredPosition = position;
        _rect.sizeDelta = size;
        RefreshBorders();
    }

    private void CreateBorders()
    {
        _top    = CreateStrip("Border_Top");
        _bottom = CreateStrip("Border_Bottom");
        _left   = CreateStrip("Border_Left");
        _right  = CreateStrip("Border_Right");
    }

    private RectTransform CreateStrip(string stripName)
    {
        var go = new GameObject(stripName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_rect, false);

        go.GetComponent<Image>().raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 1f);
        return rt;
    }

    private void RefreshBorders()
    {
        if (_top == null) return;

        Color color = Color.white;
        float t = 2f;

        if (SettingsManager.Instance != null)
        {
            color = SettingsManager.Instance.GetColor(1);        // index 1 = border
            t     = SettingsManager.Instance.GetBorderThickness();
        }

        float w = _rect.sizeDelta.x;
        float h = _rect.sizeDelta.y;

        //  pivot (0,1) → y=0 вверху, y отрицательный вниз внутри рамки
        SetStrip(_top,    new Vector2(0f,     0f),      new Vector2(w, t));
        SetStrip(_bottom, new Vector2(0f,    -h + t),   new Vector2(w, t));
        SetStrip(_left,   new Vector2(0f,     0f),      new Vector2(t, h));
        SetStrip(_right,  new Vector2(w - t,  0f),      new Vector2(t, h));

        SetStripColor(_top,    color);
        SetStripColor(_bottom, color);
        SetStripColor(_left,   color);
        SetStripColor(_right,  color);
    }

    private static void SetStrip(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void SetStripColor(RectTransform rt, Color color)
    {
        if (rt == null) return;
        var img = rt.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}