using UnityEngine;
using UnityEngine.UI;

public class ColorPickerScript : MonoBehaviour
{    
    public bool getStartingColorFromMaterial;
    [SerializeField] private FlexibleColorPicker fcp;
    public Material material;
    [SerializeField] private Button _btnColor;
    [SerializeField] private Button _btnLineColor;
    [SerializeField] private Button _btnBorderColor;
    [SerializeField] private RawImage _chosenColor;
    [SerializeField] private Image _dotColSet;
    [SerializeField] private Image _lineColSet;
    [SerializeField] private Image _borderColSet;
    
    private int currentImageIndex = -1;

    private void Start()
    {
        LoadSavedColors();
        HideColorPicker();
        if (fcp != null)
        {
            fcp.onColorChange.AddListener(UpdateTargetColor);
        }
        
    }
    
    private void LoadSavedColors()
    {
        if (SettingsManager.Instance != null)
        {
            // Загружаем цвета из float полей
            _dotColSet.color = SettingsManager.Instance.GetColor(0);
            _borderColSet.color = SettingsManager.Instance.GetColor(1);
            _lineColSet.color = SettingsManager.Instance.GetColor(2);
        }
    }
    
    private void UpdateTargetColor(Color newColor)
    {
        Image target = GetImageByIndex(currentImageIndex);
        if (target != null)
        {
            target.color = newColor;
            
            // Обновляем цвет в SettingsManager
            if (SettingsManager.Instance != null && currentImageIndex >= 0)
            {
                SettingsManager.Instance.SetColor(currentImageIndex, newColor);
            }
        }
    }
    
    public void OpenPickerForImage(int index)
    {
        currentImageIndex = index;
        Image target = GetImageByIndex(index);
        if (target != null && fcp != null)
        {
            fcp.color = target.color;
        }
        if (fcp != null) fcp.gameObject.SetActive(true);
    }
    
    private Image GetImageByIndex(int index)
    {
        return index switch
        {
            0 => _dotColSet,
            1 => _borderColSet,
            2 => _lineColSet,
            _ => null
        };
    }
    
    public void ShowColorPicker()
    {
        if (fcp != null)
        {
            fcp.gameObject.SetActive(true);
        }
    }

    public void HideColorPicker()
    {
        if (fcp != null)
        {
            fcp.gameObject.SetActive(false);
        }
        
        // Сохраняем все цвета при закрытии
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetAllColors(
                _dotColSet.color,
                _borderColSet.color,
                _lineColSet.color
            );
            SettingsManager.Instance.Save();
        }
    }
    
    public void SaveAllColors()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetAllColors(
                _dotColSet.color,
                _borderColSet.color,
                _lineColSet.color
            );
            SettingsManager.Instance.Save();
        }
    }
    
    
    private void OnDestroy()
    {
        if (fcp != null) fcp.onColorChange.RemoveListener(UpdateTargetColor);
        SaveAllColors();
    }
}