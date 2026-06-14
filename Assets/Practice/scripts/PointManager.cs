using System;
using System.Collections.Generic;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is creat
    [SerializeField]private Transform _parentPoints;
    [SerializeField]private GameObject _pointPref;
    [SerializeField]private Vector3Int _position;
    List<GameObject> dots = new List<GameObject>();


    void Start()
    {
        
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
        dots.Add(point);
    }
    public void Clear()
    {
        for( int i = 0; i < dots.Count; i++)
        {
            Destroy(dots[i]);
        }
        dots.Clear();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
