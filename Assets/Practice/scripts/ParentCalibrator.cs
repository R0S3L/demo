using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ParentCalibrator : MonoBehaviour,
    IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform _areaRect;
    [SerializeField] private Canvas _canvas;

    [Header("Внешний вид ручки")]
    [SerializeField] private Color _handleColor = new Color(1f, 1f, 0f, 0.85f);
    [SerializeField] private float _handleSize = 24f;

    // Собственный RectTransform — это и есть визуальная ручка
    private RectTransform _handle;
    private Image _handleImage;

    // Позиция _areaRect до начала текущего перетаскивания
    private Vector2 _areaStartPos;

    // ─── Unity ───────────────────────────────────────────────────────────

    void Awake()
    {
        _handle = GetComponent<RectTransform>();
        SetupHandle();
    }

    void Start()
    {
        ApplySavedOffset();   // восстанавливаем сохранённое смещение
        SnapHandleToCenter(); // ставим крестик в центр _areaRect
    }

    // ─── Drag ────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        // Запоминаем стартовую позицию _areaRect в момент нажатия
        _areaStartPos = _areaRect.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _areaStartPos = _areaRect.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canvas == null || _areaRect == null) return;

        // Перемещаем _areaRect на дельту мыши (с учётом масштаба Canvas)
        Vector2 delta = eventData.delta / _canvas.scaleFactor;
        _areaRect.anchoredPosition += delta;

        // Крестик тоже двигается вместе с _areaRect
        _handle.anchoredPosition += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SaveOffset();
    }

    // ─── Публичный API ───────────────────────────────────────────────────

    /// <summary>Сбросить смещение к (0,0) и сохранить.</summary>
    public void ResetCalibration()
    {
        if (_areaRect == null) return;
        _areaRect.anchoredPosition = Vector2.zero;
        SnapHandleToCenter();
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetCalibrationOffset(Vector2.zero);
            SettingsManager.Instance.Save();
        }
    }

    // ─── Внутреннее ─────────────────────────────────────────────────────

    private void SetupHandle()
    {
        // Настраиваем RectTransform ручки
        _handle.sizeDelta = Vector2.one * _handleSize;
        _handle.pivot = new Vector2(0.5f, 0.5f);

        // Создаём Image если нет
        _handleImage = GetComponent<Image>();
        if (_handleImage == null)
            _handleImage = gameObject.AddComponent<Image>();

        _handleImage.color = _handleColor;
        _handleImage.raycastTarget = true; // нужен для drag-событий

        // Рисуем крестик через дочерние полоски
        CreateCrossLine("H", new Vector2(_handleSize, 2f));
        CreateCrossLine("V", new Vector2(2f, _handleSize));

        // Скрываем основную Image (она только для raycast)
        _handleImage.color = new Color(0, 0, 0, 0);
    }

    private void CreateCrossLine(string lineName, Vector2 size)
    {
        var go = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_handle, false);

        var img = go.GetComponent<Image>();
        img.color = _handleColor;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    /// <summary>Ставит крестик в центр _areaRect.</summary>
    private void SnapHandleToCenter()
    {
        if (_areaRect == null) return;

        // Центр _areaRect в его локальном пространстве (pivot = 0,0)
        Vector2 center = _areaRect.anchoredPosition + _areaRect.rect.size * 0.5f;

        // Если _handle является дочерним того же родителя — позиция напрямую
        _handle.anchoredPosition = center;
    }

    private void ApplySavedOffset()
    {
        if (_areaRect == null || SettingsManager.Instance == null) return;

        Vector2 saved = SettingsManager.Instance.GetCalibrationOffset();
        _areaRect.anchoredPosition = saved;
    }

    private void SaveOffset()
    {
        if (_areaRect == null || SettingsManager.Instance == null) return;

        SettingsManager.Instance.SetCalibrationOffset(_areaRect.anchoredPosition);
        SettingsManager.Instance.Save();
        Debug.Log($"[ParentCalibrator] Смещение сохранено: {_areaRect.anchoredPosition}");
    }
}