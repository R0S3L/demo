using System.Collections.Generic;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    [SerializeField] private Transform _parentPoints;
    [SerializeField] private GameObject _pointPref;
    [SerializeField] private GameObject _popupWindow;
    [SerializeField] private GameObject _helpWindow;

    // RectTransform области, относительно центра которой считаются четверти
    [SerializeField] private RectTransform _areaRect;

    private List<GameObject> dots = new List<GameObject>();
    private List<Vector2> positions = new List<Vector2>();

    void Start()
    {
        HidePopup();
        HideHelp();
    }

    public void Spawn(Vector2Int pos)
    {
        if (dots.Count > 1)
        {
            Debug.Log("Already two dots");
            return;
        }

        if (dots.Count == 1)
        {
            int firstQuadrant = GetQuadrant(positions[0]);
            int secondQuadrant = GetQuadrant(pos);

            if (!IsOppositeQuadrant(firstQuadrant, secondQuadrant))
            {
                Debug.Log("Вторая точка должна быть в противоположной четверти");
                return; // точку не ставим
            }
        }

        GameObject point = Instantiate(_pointPref, _parentPoints);
        RectTransform rect = point.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        dots.Add(point);
        positions.Add(pos);

        if (dots.Count == 1)
        {
            ShowHelp();
        }
        if (dots.Count == 2)
        {
            HideHelp();
            ShowPopup();
        }
    }

    // 1 - верх-право, 2 - верх-лево, 3 - низ-лево, 4 - низ-право
    private int GetQuadrant(Vector2 pos)
    {
        Vector2 center = _areaRect.rect.center;

        bool right = pos.x >= center.x;
        bool top = pos.y >= center.y;

        if (right && top) return 1;
        if (!right && top) return 2;
        if (!right && !top) return 3;
        return 4;
    }

    private bool IsOppositeQuadrant(int q1, int q2)
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

    public void ShowPopup() => _popupWindow.SetActive(true);
    public void HidePopup() => _popupWindow.SetActive(false);
    public void ShowHelp() => _helpWindow.SetActive(true);
    public void HideHelp() => _helpWindow.SetActive(false);
}