using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MousePosControl : MonoBehaviour
{
    [SerializeField] private Vector2 _mousePos;
    [SerializeField] private PointManager _pointManager;

    // Камера, в пространстве которой работает Canvas.
    // Оставьте поле пустым (null), если Canvas в режиме
    // Screen Space - Overlay. Если режим Screen Space - Camera или
    // World Space — назначьте сюда соответствующую камеру (обычно
    // canvas.worldCamera).
    [SerializeField] private Camera _uiCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       // _clickRef.action.performed += MouseClick;
    }
    void Destroy()
    {
     // _clickRef.action.performed -= MouseClick;  
    }
    private void MouseClick(InputAction.CallbackContext context)
    {
        Debug.Log($"[{gameObject.name}]MouseClick");
        TrySpawnAtMousePosition();
    }
    public void SpawnOnMousePos()
    {
        TrySpawnAtMousePosition();
    }

    // ИСПРАВЛЕНО: раньше сюда напрямую шла экранная позиция мыши
    // (Mouse.current.position в screen space) и присваивалась как
    // anchoredPosition точки. Это совпадает один в один только если у
    // parent-объекта точек pivot (0,0), anchor в углу и Canvas Scale
    // Factor == 1 — на других разрешениях экрана или других значениях
    // Canvas Scaler точка спавнилась не там, где реально был клик, и
    // приходилось подгонять parent вручную под конкретный экран.
    //
    // Теперь экранная точка явно переводится в локальные координаты
    // RectTransform через RectTransformUtility — это работает корректно
    // при любом разрешении экрана и любом UI Scale без ручной подгонки.
    private void TrySpawnAtMousePosition()
    {
        if (_pointManager == null || _pointManager.AreaRect == null)
            return;

        _mousePos = Mouse.current.position.ReadValue();

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _pointManager.AreaRect,
            _mousePos,
            _uiCamera,
            out Vector2 localPos);

        if (!converted)
            return;

        _pointManager.Spawn(new Vector2Int(
            Mathf.RoundToInt(localPos.x),
            Mathf.RoundToInt(localPos.y)));
    }

    // Update is called once per frame
    /*   
     void Update()
    {
        _mousePos = Mouse.current.position.ReadValue(); 
        //Debug.Log($"Current Position = <color=red>{_mousePos}</color>");
    }
    */
}
