using System;
using System.Collections.Generic;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    [SerializeField] private Transform _parentPoints;
    [SerializeField] private GameObject _pointPref;
    [SerializeField] private GameObject _popupWindow;
    [SerializeField] private GameObject _helpWindow;

    [SerializeField] private RectTransform _areaRect;

    private List<GameObject> dots = new List<GameObject>();
    private List<Vector2> positions = new List<Vector2>();
    public int firstQuadrant, secondQuadrant;

    // Открываем доступ для конвертации screen -> local координат снаружи
    // (например, из MousePosControl), чтобы не дублировать ссылку на rect
    // ещё в одном инспекторе и не рассинхронизировать её вручную.
    public RectTransform AreaRect => _areaRect;
    public Transform ParentPoints => _parentPoints;

    void Start()
    {
        HidePopup();
        HideHelp();
        LoadSavedDots();
    }

    public void Spawn(Vector2Int pos)
    {
        if (dots.Count > 1)
        {
            Debug.Log("Already two dots");
            return;
        }

        GameObject point = Instantiate(_pointPref, _parentPoints);
        RectTransform rect = point.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;

        // Устанавливаем четверть для точки
        DragPoint dragPoint = point.GetComponent<DragPoint>();
        if (dragPoint != null)
        {
            int quadrant = GetQuadrant(pos);
            if (dots.Count == 0) firstQuadrant = quadrant;
            else secondQuadrant = quadrant;
        }

        dots.Add(point);
        positions.Add(pos);

        if (dots.Count == 1)
        {
            ShowHelp();
        }
        if (dots.Count == 2)
        {
            HideHelp();
            if (!IsOppositeQuadrant(firstQuadrant, secondQuadrant))
            {
                Debug.Log("Вторая точка должна быть в противоположной четверти");
                Clear();
                return;
            }
            else
            {
                Debug.Log($"Удовлетворяет {positions.Count}");
            }
            ShowPopup();
        }
    }

    // ИСПРАВЛЕНО: раньше верхняя граница сравнивалась с захардкоженными
    // 1920 / 1080, из-за чего логика ломалась на любом разрешении/размере
    // канваса, отличном от Full HD. Теперь квадрант считается только
    // относительно реальных текущих размеров _areaRect.
    //
    // ВАЖНО: эта логика подразумевает, что pivot у _areaRect равен (0,0)
    // (origin в левом нижнем углу), чтобы диапазон 0..width / 0..height
    // совпадал с диапазоном anchoredPosition точек. Если pivot другой —
    // приведите _areaRect к pivot (0,0) в инспекторе, это разовая настройка,
    // а не повторяющаяся калибровка под каждое разрешение.
    public int GetQuadrant(Vector2 pos)
    {
        float width = _areaRect.rect.width;
        float height = _areaRect.rect.height;

        bool right = pos.x >= width / 2f;
        bool top = pos.y >= height / 2f;

        if (right && top) return 1;
        if (!right && top) return 2;
        if (!right && !top) return 3;
        return 4;
    }

    public bool IsOppositeQuadrant(int q1, int q2)
    {
        return (q1 == 1 && q2 == 3) || (q1 == 3 && q2 == 1)
            || (q1 == 2 && q2 == 4) || (q1 == 4 && q2 == 2);
    }

    public void Clear()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            Destroy(dots[i]);
        }
        dots.Clear();
        positions.Clear();
        HidePopup();
        HideHelp();
    }

    public void UpdatePointPosition(GameObject point, Vector2 newPos)
    {
        int index = dots.IndexOf(point);
        if (index >= 0)
        {
            positions[index] = newPos;
            Debug.Log($"Точка {index} перемещена в позицию {newPos}");
        }
    }

    public void ShowPopup() => _popupWindow.SetActive(true);
    public void HidePopup() => _popupWindow.SetActive(false);
    public void ShowHelp() => _helpWindow.SetActive(true);
    public void HideHelp() => _helpWindow.SetActive(false);

    public void SaveDots()
    {
        HidePopup();
        if (SettingsManager.Instance != null && _areaRect != null)
        {
            SettingsManager.Instance.UpdatePositions(positions, _areaRect.rect.size);
            SettingsManager.Instance.Save();
        }
    }

    private void LoadSavedDots()
    {
        if (SettingsManager.Instance == null
            || SettingsManager.Instance.Data.savedPositions == null
            || _areaRect == null)
        {
            return;
        }

        Vector2 areaSize = _areaRect.rect.size;
        List<Vector2> savedPositions = SettingsManager.Instance.Data.savedPositions;

        foreach (Vector2 normalizedPos in savedPositions)
        {
            Vector2 pixelPos = CoordinateUtils.ToPixels(normalizedPos, areaSize);
            Vector2Int posInt = new Vector2Int(
                Mathf.RoundToInt(pixelPos.x),
                Mathf.RoundToInt(pixelPos.y));
            Spawn(posInt);
        }
    }
}