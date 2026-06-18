using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class DragPoint : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField]private RectTransform _rect;
    [SerializeField]private Canvas _canvas;
    [SerializeField]private PointManager _pointManager;
    [SerializeField]private int _myQuadrant;
    [SerializeField]private bool _isInitialized = false;
    [SerializeField]private Vector2 _mousePos;

    [System.Obsolete]
    private void Awake()
    {
        // Получаем все компоненты автоматически
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _pointManager = FindFirstObjectByType<PointManager>();
        
    }
    
    private void Start()
    {
        if (_rect != null && _pointManager != null)
        {
            _myQuadrant = _pointManager.GetQuadrant(_rect.anchoredPosition);
            _isInitialized = true;
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isInitialized || _rect == null || _pointManager == null || _canvas == null)
            return;
        
        Vector2 newPos = _rect.anchoredPosition + eventData.delta / _canvas.scaleFactor;
        _mousePos = Mouse.current.position.ReadValue(); 
        if (_pointManager.GetQuadrant(newPos) == _myQuadrant)
        {
            _rect.anchoredPosition = newPos;
        }
        if (_pointManager.GetQuadrant(newPos) != _pointManager.GetQuadrant(_mousePos))
        {
            Debug.Log("Точка на границе четверти");           
            ExecuteEvents.ExecuteHierarchy(gameObject, eventData, ExecuteEvents.endDragHandler);            
            eventData.pointerDrag = null;
            eventData.dragging = false;            
            return;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isInitialized && _pointManager != null && _rect != null)
        {         
            _pointManager.UpdatePointPosition(gameObject, _rect.anchoredPosition);
        }
    }
}