using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Соединяет обнаруженные точки линиями: от каждой точки ровно 2 линии.
///
/// Алгоритм:
/// 1. Копируем все точки в рабочий список remaining
/// 2. Берём первую точку из remaining, удаляем её
/// 3. Ищем двух ближайших к ней из remaining
/// 4. Рисуем две линии (текущая → ближайший1, текущая → ближайший2)
/// 5. Повторяем для следующей точки
///
/// Дублирующиеся линии пропускаются через HashSet пар индексов.
/// </summary>
public class LineConnector : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PointCenterTracker _tracker;
    [SerializeField] private RawImage _cameraDisplay;
    [SerializeField] private RectTransform _lineParent;

    [SerializeField] private float _lineThickness = 2f;

    private List<Image> _linePool = new List<Image>();
    private List<(Vector2 a, Vector2 b)> _segments = new List<(Vector2, Vector2)>();

    // Рабочие списки — переиспользуем без аллокаций каждый кадр
    private List<float2> _remaining = new List<float2>();
    private List<int> _remIndices = new List<int>(); // оригинальные индексы для дедупликации
    private HashSet<(int, int)> _drawnPairs = new HashSet<(int, int)>();

    void Update()
    {
        if (_tracker == null || _cameraDisplay == null || _lineParent == null) return;

        List<float2> centers = _tracker.DetectedCenters;
        if (centers == null || centers.Count < 2) { HideAll(); return; }

        BuildSegments(centers);
        DrawSegments();
    }

    // ─── Алгоритм ────────────────────────────────────────────────────────

    private void BuildSegments(List<float2> centers)
    {
        _segments.Clear();
        _drawnPairs.Clear();

        // remaining хранит точки с их оригинальными индексами для дедупликации
        _remaining.Clear();
        _remIndices.Clear();
        for (int i = 0; i < centers.Count; i++)
        {
            _remaining.Add(centers[i]);
            _remIndices.Add(i);
        }

        while (_remaining.Count > 0)
        {
            // Берём первую точку и удаляем её из рабочего списка
            float2 current = _remaining[0];
            int currentIdx = _remIndices[0];
            _remaining.RemoveAt(0);
            _remIndices.RemoveAt(0);

            if (_remaining.Count == 0) break; // больше не с кем соединять

            // Ищем двух ближайших из remaining
            FindTwoNearest(current, out int n1, out int n2);

            // Линия 1: current → nearest1
            TryAddSegment(current, currentIdx, _remaining[n1], _remIndices[n1]);

            // Линия 2: current → nearest2 (если нашлась вторая)
            if (n2 >= 0)
                TryAddSegment(current, currentIdx, _remaining[n2], _remIndices[n2]);
        }
    }

    /// <summary>Ищет индексы двух ближайших точек в _remaining к точке current.</summary>
    private void FindTwoNearest(float2 current, out int idx1, out int idx2)
    {
        idx1 = -1; idx2 = -1;
        float d1 = float.MaxValue, d2 = float.MaxValue;

        for (int i = 0; i < _remaining.Count; i++)
        {
            float d = math.distancesq(current, _remaining[i]);
            if (d < d1)
            {
                d2 = d1; idx2 = idx1;
                d1 = d; idx1 = i;
            }
            else if (d < d2)
            {
                d2 = d; idx2 = i;
            }
        }
    }

    /// <summary>
    /// Добавляет сегмент если пара ещё не была нарисована.
    /// Пара (i, j) и (j, i) считаются одинаковыми.
    /// </summary>
    private void TryAddSegment(float2 a, int idxA, float2 b, int idxB)
    {
        var key = idxA < idxB ? (idxA, idxB) : (idxB, idxA);
        if (_drawnPairs.Contains(key)) return;

        _drawnPairs.Add(key);
        _segments.Add((ToUI(a), ToUI(b)));
    }

    // ─── Отрисовка ───────────────────────────────────────────────────────

    private void DrawSegments()
    {
        EnsurePool(_segments.Count);

        Color color = Color.white;
        if (SettingsManager.Instance != null)
        {
            color = SettingsManager.Instance.GetColor(2);
            _lineThickness = SettingsManager.Instance.GetBorderThickness();
        }

        for (int i = 0; i < _linePool.Count; i++)
        {
            if (i < _segments.Count)
            {
                _linePool[i].gameObject.SetActive(true);
                _linePool[i].color = color;
                DrawLine(_linePool[i].rectTransform, _segments[i].a, _segments[i].b);
            }
            else
            {
                _linePool[i].gameObject.SetActive(false);
            }
        }
    }

    private void DrawLine(RectTransform line, Vector2 a, Vector2 b)
    {
        Vector2 dir = b - a;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        line.anchoredPosition = a;
        line.sizeDelta = new Vector2(dir.magnitude, _lineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ─── Конвертация координат ───────────────────────────────────────────

    private Vector2 ToUI(float2 cameraPixel)
    {
        Vector2 displaySize = _cameraDisplay.rectTransform.rect.size;
        float2 norm = cameraPixel / new float2(_tracker.CameraWidth, _tracker.CameraHeight);
        return new Vector2(
            (norm.x - 0.5f) * displaySize.x,
            (norm.y - 0.5f) * displaySize.y);
    }

    // ─── Пул объектов ────────────────────────────────────────────────────

    private void EnsurePool(int needed)
    {
        while (_linePool.Count < needed)
        {
            var go = new GameObject($"Line_{_linePool.Count}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_lineParent, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            _linePool.Add(img);
        }
    }

    private void HideAll()
    {
        foreach (var line in _linePool)
            if (line != null) line.gameObject.SetActive(false);
    }
}