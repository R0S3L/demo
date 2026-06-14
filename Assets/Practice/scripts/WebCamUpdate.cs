using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.iOS;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

public class WebCamUpdate : MonoBehaviour, IPointerClickHandler
{
    private WebCamTexture _texture;
    [SerializeField]private RawImage _img;
    [SerializeField]private int _index;
    [SerializeField]private TMP_Dropdown _drop;
    [SerializeField]private Button _btnStart;
    [SerializeField]private Button _btnStop;
    [SerializeField]private Button _btnPause;
    [SerializeField]private TextMeshProUGUI _resolution; 
    [SerializeField]private TextMeshProUGUI _fps;
    [SerializeField]private TextMeshProUGUI _cameraName;
    [SerializeField]private RectTransform _cursorDot;
    [SerializeField]private Texture2D _cursorTexture;
    [SerializeField]private Mouse _mouse;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DropShow(_drop);
        Vector2 vect = Mouse.current.position.ReadValue();
        _drop.onValueChanged.AddListener(DropChanged);
        _texture = new WebCamTexture();
        WebCamDevice[] deivce = WebCamTexture.devices; 
        if (_index < 0 || _index >= deivce.Length || deivce == null){            
            return;
        }
        _texture.deviceName = deivce[_index].name;
        _texture.Play();
        _img.texture = _texture;
        GetResolution();
        GetFPS();
        GetName();
    }

    void OnDestroy()
    {
       _texture.Stop();
       _drop.onValueChanged.RemoveListener(DropChanged);
    }
    void DropShow(TMP_Dropdown _drop)
    {
        _drop.ClearOptions();
        WebCamDevice[] device = WebCamTexture.devices; 
        List<TMP_Dropdown.OptionData> List = new List<TMP_Dropdown.OptionData>();
        for( int i = 0; i < device.Length; i++)
        {
            TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
            data.text = device[i].name;
            List.Add(data);
        }
        _drop.AddOptions(List);
    }
    public void DropChanged(int id)
    {
       WebCamDevice[] deivce = WebCamTexture.devices; 
        if (id < 0 || id > deivce.Length || deivce == null){            
            return;
        }
        _texture.Stop();
        _texture = new WebCamTexture();
        _texture.deviceName = deivce[id].name;
        _img.texture = _texture;
        _texture.Play();  
    }
    public void GetResolution()
    {
        if (_img.texture != null)
        {
            int width = _img.texture.width;
            int height = _img.texture.height;
            _resolution.text = ($"{width} x {height}");
        }
    }
    public void GetFPS()
    {
        if(_img.texture != null)
        {
            float fps = 1f / Time.deltaTime;
            _fps.text = Convert.ToString(Mathf.Round(fps));
        }
    }
    public void GetName()
    {
       if (_texture != null)
        {
            _cameraName.text = _texture.deviceName;
        }
    }
    public void Btn_Start()
    {
        _texture.Play();
        _img.texture = _texture;
    }
    public void Btn_Stop()
    {
       _texture.Stop(); 
       _img.texture = null;
    }
    public void Btn_Pause()
    {
       _texture.Pause(); 
    }
    void Update()
    {
        GetResolution();
        GetFPS();
        GetName();
       // Debug.Log(Mouse.current.position.ReadValue());
    }
    void Awake(){

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked");
        if (RectTransformUtility.RectangleContainsScreenPoint(_img.rectTransform, eventData.position, eventData.pressEventCamera))
        {
            if (_cursorDot != null)
            {
                _cursorDot.gameObject.SetActive(true);
                _cursorDot.position = eventData.position;
            }
            if (_cursorTexture != null)
            {
                Cursor.SetCursor(_cursorTexture, Vector2.zero, CursorMode.Auto);
            }
        }
    }

}
