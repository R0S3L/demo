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

    
    public int GetQuadrant(Vector2 pos)
    {
        float rect_width = _areaRect.rect.width;
        float rect_height = _areaRect.rect.height;

        bool width = pos.x >= rect_width/2;
        bool height = pos.y >= rect_height/2;

        if (width && height) return 1;
        if (!width && height) return 2;
        if (!width && !height) return 3;
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
}