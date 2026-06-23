using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ParentCalibrator : MonoBehaviour,
    IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform _areaRect;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private UIRectangle _uiRectangle; // бордер двигается вместе
    [SerializeField] private Button _hideBtn;
    [SerializeField] private Button _resetBtn;

    [Header("Внешний вид крестика")]
    [SerializeField] private Color _handleColor = new Color(1f, 1f, 0f, 0.85f);
    [SerializeField] private float _handleSize = 32f;

    private RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = Vector2.one * _handleSize;

        SetupRaycastImage();
        CreateCrossLine("H", new Vector2(_handleSize, 2f));
        CreateCrossLine("V", new Vector2(2f, _handleSize));
    }

    void Start()
    {
        ApplySavedOffset();
        CenterOnAreaRect();

        if (_hideBtn != null) _hideBtn.onClick.AddListener(HideHandle);
        if (_resetBtn != null) _resetBtn.onClick.AddListener(ResetCalibration);

        HideHandle();
    }

    void OnDestroy()
    {
        if (_hideBtn != null) _hideBtn.onClick.RemoveListener(HideHandle);
        if (_resetBtn != null) _resetBtn.onClick.RemoveListener(ResetCalibration);
    }

    // ─── Показ / скрытие ─────────────────────────────────────────────────

    public void ShowHandle()
    {
        CenterOnAreaRect();
        gameObject.SetActive(true);
        SetButtonsVisible(true);
    }

    public void HideHandle()
    {
        // ИСПРАВЛЕНО: сохраняем позицию при закрытии, а не только в OnEndDrag.
        // Иначе если пользователь нажал кнопку после перетаскивания —
        // изменения теряются.
        SaveOffset();

        gameObject.SetActive(false);
        SetButtonsVisible(false);
    }

    public void ToggleHandle()
    {
        if (gameObject.activeSelf) HideHandle();
        else ShowHandle();
    }

    // ─── Drag ────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canvas == null || _areaRect == null) return;

        _areaRect.anchoredPosition += eventData.delta / _canvas.scaleFactor;

        // Двигаем бордер вместе с _areaRect.
        // Если UIRectangle является дочерним _areaRect — он двигается
        // автоматически, и эту строку можно убрать.
        // Если UIRectangle находится отдельно — перерисовываем его позицию.
        if (_uiRectangle != null)
            _uiRectangle.DrawFromSettings();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SaveOffset();
    }

    // ─── Публичный API ───────────────────────────────────────────────────

    public void ResetCalibration()
    {
        if (_areaRect == null) return;
        _areaRect.anchoredPosition = Vector2.zero;

        if (_uiRectangle != null)
            _uiRectangle.DrawFromSettings();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetCalibrationOffset(Vector2.zero);
            SettingsManager.Instance.Save();
        }
    }

    // ─── Внутреннее ─────────────────────────────────────────────────────

    private void SetButtonsVisible(bool visible)
    {
        if (_hideBtn != null) _hideBtn.gameObject.SetActive(visible);
        if (_resetBtn != null) _resetBtn.gameObject.SetActive(visible);
    }

    private void SetupRaycastImage()
    {
        var img = GetComponent<Image>();
        if (img == null) img = gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;
    }

    private void CreateCrossLine(string lineName, Vector2 size)
    {
        var go = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

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

    private void CenterOnAreaRect()
    {
        if (_areaRect == null) return;
        _rect.anchoredPosition = _areaRect.rect.size * 0.5f;
    }

    private void ApplySavedOffset()
    {
        if (_areaRect == null || SettingsManager.Instance == null) return;
        _areaRect.anchoredPosition = SettingsManager.Instance.GetCalibrationOffset();
    }

    private void SaveOffset()
    {
        if (_areaRect == null || SettingsManager.Instance == null) return;
        SettingsManager.Instance.SetCalibrationOffset(_areaRect.anchoredPosition);
        SettingsManager.Instance.Save();
        Debug.Log($"[ParentCalibrator] Сохранено смещение: {_areaRect.anchoredPosition}");
    }
}