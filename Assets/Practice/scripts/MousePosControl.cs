using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MousePosControl : MonoBehaviour
{
    [SerializeField]private Vector2 _mousePos;
    [SerializeField]private PointManager _pointManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       // _clickRef.action.performed += MouseClick;
    }
    void Oestroy()
    {
     // _clickRef.action.performed -= MouseClick;  
    }
    private void MouseClick(InputAction.CallbackContext context)
    {
        Debug.Log($"[{gameObject.name}]MouseClick");
        _pointManager.Spawn(new Vector2Int(Mathf.RoundToInt(_mousePos.x), Mathf.RoundToInt(_mousePos.y)));
    }
    public void SpawnOnMousePos()
    {
        _pointManager.Spawn(new Vector2Int(Mathf.RoundToInt(_mousePos.x), Mathf.RoundToInt(_mousePos.y))); 
    }

    // Update is called once per frame
    void Update()
    {
        _mousePos = Mouse.current.position.ReadValue(); 
        //Debug.Log($"Current Position = <color=red>{_mousePos}</color>");
    }
}
