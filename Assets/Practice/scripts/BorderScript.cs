using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIRectangle : MonoBehaviour
{
    [SerializeField] private RectTransform _areaRect;

    private RectTransform _rect;
    private RectTransform _top, _bottom, _left, _right;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _rect.pivot = new Vector2(0f, 1f);
        CreateBorders();
    }

    void Start()
    {
        DrawFromSettings();
    }


   public void DrawFromSettings()
    {
        if (SettingsManager.Instance == null || _areaRect == null) return;
 
        List<Vector2> saved = SettingsManager.Instance.Data.savedPositions;
        if (saved == null || saved.Count < 2) return;
 
        Vector2 areaSize = _areaRect.rect.size;
        Vector2 p1 = CoordinateUtils.ToPixels(saved[0], areaSize);
        Vector2 p2 = CoordinateUtils.ToPixels(saved[1], areaSize);
 
        float left   = Mathf.Min(p1.x, p2.x);
        float right  = Mathf.Max(p1.x, p2.x);
        float bottom = Mathf.Min(p1.y, p2.y);
        float top    = Mathf.Max(p1.y, p2.y);
 
        // Позиция бордера в пространстве родителя _areaRect.
        // Бордер НЕ является дочерним _areaRect, поэтому переводим
        // локальные координаты _areaRect в координаты общего родителя.
        Vector2 areaOffset = _areaRect.anchoredPosition;
        Vector2 position = new Vector2(areaOffset.x + left, areaOffset.y + top);
        Vector2 size = new Vector2(right - left, top - bottom);
 
        DrawRectangle(position, size);
    }

    public void DrawRectangle(Vector2 position, Vector2 size)
    {
        _rect.anchoredPosition = position;
        _rect.sizeDelta = size;
        RefreshBorders();
    }

    // Вызывается из ParentCalibrator при смене цвета в режиме калибровки
    public void SetColor(Color color)
    {
        SetStripColor(_top,    color);
        SetStripColor(_bottom, color);
        SetStripColor(_left,   color);
        SetStripColor(_right,  color);
    }

    // ─── Внутреннее ─────────────────────────────────────────────────────

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
            color = SettingsManager.Instance.GetColor(1);
            t     = SettingsManager.Instance.GetBorderThickness();
        }

        float w = _rect.sizeDelta.x;
        float h = _rect.sizeDelta.y;

        SetStrip(_top,    new Vector2(0f,     0f),    new Vector2(w, t));
        SetStrip(_bottom, new Vector2(0f,    -h + t), new Vector2(w, t));
        SetStrip(_left,   new Vector2(0f,     0f),    new Vector2(t, h));
        SetStrip(_right,  new Vector2(w - t,  0f),    new Vector2(t, h));

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