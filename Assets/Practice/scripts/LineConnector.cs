using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Соединяет дочерние объекты _pointParent линиями по порядку индексов:
/// [0]→[1]→[2]→...→[n-1] и опционально [n-1]→[0] (замкнуть контур).
///
/// Каждая линия — повёрнутый Image-квад.
/// Цвет берётся из SettingsManager (index 2 = lineColor).
/// Толщина берётся из SettingsManager (borderThickness) или задаётся отдельно.
///
/// Вешается на любой GameObject внутри того же Canvas.
/// </summary>
public class LineConnector : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Родитель, чьи дочерние объекты будем соединять линиями по индексу")]
    [SerializeField] private RectTransform _pointParent;

    [Tooltip("Родитель для создаваемых линий (рекомендуется тот же Canvas-слой что и точки)")]
    [SerializeField] private RectTransform _lineParent;

    [Header("Настройки")]
    [Tooltip("Замкнуть контур: соединить последнюю точку с первой")]
    [SerializeField] private bool _closeLoop = false;

    [SerializeField] private float _lineThickness = 2f;

    // Пул линий чтобы не создавать/удалять каждый кадр
    private Image[] _linePool = new Image[0];

    void Update()
    {
        if (_pointParent == null || _lineParent == null) return;

        int pointCount = _pointParent.childCount;
        int lineCount  = pointCount < 2 ? 0
                       : _closeLoop     ? pointCount
                       :                  pointCount - 1;

        EnsurePool(lineCount);
        ApplySettings();

        for (int i = 0; i < _linePool.Length; i++)
        {
            if (i < lineCount)
            {
                RectTransform a = _pointParent.GetChild(i)           as RectTransform;
                RectTransform b = _pointParent.GetChild((i + 1) % pointCount) as RectTransform;

                _linePool[i].gameObject.SetActive(true);
                DrawLine(_linePool[i].rectTransform, a.anchoredPosition, b.anchoredPosition);
            }
            else
            {
                _linePool[i].gameObject.SetActive(false);
            }
        }
    }

    // ─── Внутреннее ─────────────────────────────────────────────────────

    /// <summary>
    /// Позиционирует линию между двумя точками.
    /// Линия = квад с pivot (0, 0.5), расположенный в точке A, 
    /// повёрнутый к B, с шириной = расстояние A→B.
    /// </summary>
    private void DrawLine(RectTransform line, Vector2 a, Vector2 b)
    {
        Vector2 dir      = b - a;
        float   length   = dir.magnitude;
        float   angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        line.anchoredPosition = a;                             // начало линии
        line.sizeDelta        = new Vector2(length, _lineThickness);
        line.localRotation    = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>Расширяет или сжимает пул линий до нужного размера.</summary>
    private void EnsurePool(int needed)
    {
        if (_linePool.Length == needed) return;

        // Уничтожаем лишние
        for (int i = needed; i < _linePool.Length; i++)
            if (_linePool[i] != null)
                Destroy(_linePool[i].gameObject);

        // Создаём недостающие
        Image[] newPool = new Image[needed];
        for (int i = 0; i < needed; i++)
        {
            if (i < _linePool.Length && _linePool[i] != null)
            {
                newPool[i] = _linePool[i];
            }
            else
            {
                newPool[i] = CreateLine(i);
            }
        }
        _linePool = newPool;
    }

    private Image CreateLine(int index)
    {
        var go = new GameObject($"Line_{index}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_lineParent, false);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot     = new Vector2(0f, 0.5f); // начало линии — левый центр
        return img;
    }

    /// <summary>Применяет цвет и толщину из SettingsManager каждый кадр.</summary>
    private void ApplySettings()
    {
        if (SettingsManager.Instance == null) return;

        Color color = SettingsManager.Instance.GetColor(2); // index 2 = lineColor
        _lineThickness = SettingsManager.Instance.GetBorderThickness();

        foreach (var line in _linePool)
            if (line != null)
                line.color = color;
    }
}
