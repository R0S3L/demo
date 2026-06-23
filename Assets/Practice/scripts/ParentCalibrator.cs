using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Вешается на тот же GameObject, что и UIRectangle.
/// В режиме калибровки пользователь тащит бордер — бордер двигается сам
/// и тянет _areaRect (со всеми дочерними точками) на ту же дельту.
///
/// Иерархия:
///   Canvas
///     ├─ _areaRect  (pivot 0,0)
///     │     └─ [точки, крестик и т.д.]
///     └─ UIRectangle + ParentCalibrator  ← этот GameObject (НЕ дочерний _areaRect)
///
/// Кнопка "Калибровка" → ToggleCalibration()
/// Кнопка "Скрыть"     → назначается автоматически через _hideBtn
/// Кнопка "Сбросить"   → назначается автоматически через _resetBtn
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(UIRectangle))]
public class ParentCalibrator : MonoBehaviour,
    IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Ссылки")]
    [SerializeField] private RectTransform _areaRect;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Button _hideBtn;
    [SerializeField] private Button _resetBtn;

    [Header("Цвет бордера в режиме калибровки")]
    [SerializeField] private Color _calibrationColor = new Color(1f, 1f, 0f, 0.85f);

    private RectTransform _rect;
    private UIRectangle _uiRectangle;

    // Сохраняем оригинальный цвет бордера чтобы вернуть его после калибровки
    private Color _normalColor;
    private bool _calibrationMode = false;

    // ─── Unity ───────────────────────────────────────────────────────────

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _uiRectangle = GetComponent<UIRectangle>();
    }

    void Start()
    {
        ApplySavedOffset();

        // Перерисовываем бордер после восстановления смещения
        if (_uiRectangle != null)
            _uiRectangle.DrawFromSettings();

        if (_hideBtn != null)  _hideBtn.onClick.AddListener(StopCalibration);
        if (_resetBtn != null) _resetBtn.onClick.AddListener(ResetCalibration);

        SetButtonsVisible(false);
        SetRaycast(false); // бордер не перехватывает клики вне режима калибровки
    }

    void OnDestroy()
    {
        if (_hideBtn != null)  _hideBtn.onClick.RemoveListener(StopCalibration);
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

        // Запоминаем нормальный цвет и подсвечиваем бордер
        if (SettingsManager.Instance != null)
            _normalColor = SettingsManager.Instance.GetColor(1);

        _uiRectangle?.SetColor(_calibrationColor);
        SetRaycast(true);
        SetButtonsVisible(true);
    }

    public void StopCalibration()
    {
        _calibrationMode = false;

        // Возвращаем оригинальный цвет бордера
        _uiRectangle?.SetColor(_normalColor);
        SetRaycast(false);
        SetButtonsVisible(false);

        SaveOffset();
    }

    // ─── Drag ────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_calibrationMode || _canvas == null || _areaRect == null) return;

        Vector2 delta = eventData.delta / _canvas.scaleFactor;

        // Двигаем бордер (этот объект)
        _rect.anchoredPosition += delta;

        // Двигаем _areaRect на ту же дельту — точки идут вместе
        _areaRect.anchoredPosition += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SaveOffset();
    }

    // ─── Публичный API ───────────────────────────────────────────────────

    public void ResetCalibration()
    {
        if (_areaRect == null) return;

        // Считаем на сколько сдвинут _areaRect и откатываем бордер на обратную дельту
        Vector2 currentOffset = _areaRect.anchoredPosition;
        _rect.anchoredPosition -= currentOffset;
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
        if (_hideBtn != null)  _hideBtn.gameObject.SetActive(visible);
        if (_resetBtn != null) _resetBtn.gameObject.SetActive(visible);
    }

    // Включаем/выключаем raycastTarget на всех Image внутри бордера,
    // чтобы вне режима калибровки клики проходили сквозь него.
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