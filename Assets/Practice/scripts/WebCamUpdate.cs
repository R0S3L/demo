using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        if (_drop != null)
        {
            DropShow(_drop);
            _drop.onValueChanged.AddListener(DropChanged);
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
            return;

        int savedIndex = _index;
        if (SettingsManager.Instance != null && SettingsManager.Instance.GetSelectedCameraIndex() >= 0)
        {
            savedIndex = SettingsManager.Instance.GetSelectedCameraIndex();
        }

        if (savedIndex < 0 || savedIndex >= devices.Length)
            savedIndex = 0;

        _index = savedIndex;
        if (_drop != null && _drop.options.Count > 0)
            _drop.value = _index;

        ApplyCamera(_index);
        GetResolution();
    }

    public void SaveCam()
    {
        if (_texture != null && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetCameraSelection(_index, _texture.deviceName);
            SettingsManager.Instance.Save();
        }
    }

    public void LoadCam()
    {
        if (SettingsManager.Instance == null)
            return;

        int savedIndex = SettingsManager.Instance.GetSelectedCameraIndex();
        if (savedIndex >= 0)
        {
            _index = savedIndex;
            ApplyCamera(savedIndex);
        }
    }

    void OnDestroy()
    {
        if (_texture != null)
            _texture.Stop();
        if (_drop != null)
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
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || id < 0 || id >= devices.Length){
            return;
        }
        _index = id;
        ApplyCamera(id);
        SaveCam();
    }

    private void ApplyCamera(int id)
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
            return;

        if (id < 0 || id >= devices.Length)
            return;

        if (_texture != null)
            _texture.Stop();

        _texture = new WebCamTexture();
        if (_img != null)
            _img.texture = _texture;

        _index = id;
        _texture.deviceName = devices[id].name;
        _texture.Play();


        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetCameraSelection(id, devices[id].name);
    }
    public void GetResolution()
    {
        if (_img.texture == null)
        {
            return;
        }
        int width = _img.texture.width;
        int height = _img.texture.height;
        if(_resolution.text != null) {
        _resolution.text = ($"{width} x {height}");
        }
        
    }
    public void GetFPS()
    {
        if(_img.texture == null)
        {
            return;
        }
        float fps = 1f / Time.deltaTime;
        _fps.text = Convert.ToString(Mathf.Round(fps));
    }
    public void GetName()
    {
       if (_texture == null)
        {
            return;
        }
        _cameraName.text = _texture.deviceName;
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
       //Debug.Log(Mouse.current.position.ReadValue());
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
