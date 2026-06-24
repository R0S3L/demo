using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Вешается на тот же GameObject, что и UIRectangle.
/// UIRectangle + ParentCalibrator — дочерний объект _areaRect.
/// При перетаскивании двигается только _areaRect — бордер и точки
/// следуют за ним автоматически как дочерние объекты.
///
/// Иерархия:
///   Canvas
///     └─ _areaRect  (pivot 0,0)
///           ├─ UIRectangle + ParentCalibrator  ← дочерний _areaRect
///           └─ [точки DragPoint]
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(UIRectangle))]
public class ParentCalibrator : MonoBehaviour,
    IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform _areaRect;   // родитель этого объекта
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Button _hideBtn;
    [SerializeField] private Button _resetBtn;

    [Header("Цвет бордера в режиме калибровки")]
    [SerializeField] private Color _calibrationColor = new Color(1f, 1f, 0f, 0.85f);

    private UIRectangle _uiRectangle;
    private Color _normalColor;
    private bool _calibrationMode = false;

    // ─── Unity ───────────────────────────────────────────────────────────

    void Awake()
    {
        _uiRectangle = GetComponent<UIRectangle>();
    }

    void Start()
    {
        // Восстанавливаем сохранённое смещение _areaRect
        ApplySavedOffset();

        if (_hideBtn != null) _hideBtn.onClick.AddListener(StopCalibration);
        if (_resetBtn != null) _resetBtn.onClick.AddListener(ResetCalibration);

        SetButtonsVisible(false);
        SetRaycast(false);
    }

    void OnDestroy()
    {
        if (_hideBtn != null) _hideBtn.onClick.RemoveListener(StopCalibration);
        if (_resetBtn != null) _resetBtn.onClick.RemoveListener(ResetCalibration);
    }

    // ─── Режим калибровки ─────────────────────────────────────────────────

    public void ToggleCalibration()
    {
        if (_calibrationMode) StopCalibration();
        else StartCalibration();
    }

    public void StartCalibration()
    {
        _calibrationMode = true;

        if (SettingsManager.Instance != null)
            _normalColor = SettingsManager.Instance.GetColor(1);

        _uiRectangle?.ShowBorder();
        _uiRectangle?.SetColor(_calibrationColor);
        SetRaycast(true);
        SetButtonsVisible(true);
    }

    public void StopCalibration()
    {
        _calibrationMode = false;

        _uiRectangle?.SetColor(_normalColor);
        _uiRectangle?.HideBorder();
        SetRaycast(false);
        SetButtonsVisible(false);

        SaveOffset();
    }

    // ─── Drag ────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_calibrationMode || _canvas == null || _areaRect == null) return;

        // Двигаем только _areaRect.
        // Бордер и точки — дочерние объекты, двигаются автоматически.
        _areaRect.anchoredPosition += eventData.delta / _canvas.scaleFactor;
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

    private void SetRaycast(bool value)
    {
        foreach (var img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = value;
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