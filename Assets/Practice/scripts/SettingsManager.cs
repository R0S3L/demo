using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance => _instance;
    [SerializeField] string fileName = "settings.json";
    
    [System.Serializable]
    public class SettingsData
    {
        public List<Vector2> savedPositions = new List<Vector2>();
        public int selectedCameraIndex = -1;
        public string selectedCameraName = "";
        public int cameraWidth = 0;
        public int cameraHeight = 0;
        
        // Цвет DOT (точка)
        public float dotR = 1f;
        public float dotG = 1f;
        public float dotB = 1f;
        public float dotA = 1f;
        
        // Цвет BORDER (граница)
        public float borderR = 1f;
        public float borderG = 0f;
        public float borderB = 0f;
        public float borderA = 1f;
        
        // Цвет LINE (линия)
        public float lineR = 0f;
        public float lineG = 0f;
        public float lineB = 1f;
        public float lineA = 1f;

        // Толщина рамки (пиксели UI)
        public float borderThickness = 2f;
    }
    
    public SettingsData Data { get; private set; } = new SettingsData();
    private string filePath;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log(filePath);
        Load();
    }

    public void Load()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Data = JsonUtility.FromJson<SettingsData>(json);
        }
        else
        {
            Data = new SettingsData();
            Save();
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(filePath, json);
    }
    
    private void OnApplicationQuit() => Save();
    private void OnApplicationPause(bool pause) 
    { 
        if (pause) Save(); 
    }
    
    public void UpdatePositions(List<Vector2> currentPositions, Vector2 areaSize)
    {
        Data.savedPositions = new List<Vector2>();

        float width = Mathf.Max(1f, areaSize.x);
        float height = Mathf.Max(1f, areaSize.y);

        foreach (Vector2 pos in currentPositions)
        {
            float x = Mathf.Clamp01(pos.x / width);
            float y = Mathf.Clamp01(pos.y / height);
            Data.savedPositions.Add(new Vector2(x, y));
        }
    }
    
    public void SetColor(int index, Color color)
    {
        switch (index)
        {
            case 0:
                Data.dotR = color.r; Data.dotG = color.g;
                Data.dotB = color.b; Data.dotA = color.a;
                break;
            case 1:
                Data.borderR = color.r; Data.borderG = color.g;
                Data.borderB = color.b; Data.borderA = color.a;
                break;
            case 2:
                Data.lineR = color.r; Data.lineG = color.g;
                Data.lineB = color.b; Data.lineA = color.a;
                break;
        }
    }
    
    public Color GetColor(int index)
    {
        return index switch
        {
            0 => new Color(Data.dotR, Data.dotG, Data.dotB, Data.dotA),
            1 => new Color(Data.borderR, Data.borderG, Data.borderB, Data.borderA),
            2 => new Color(Data.lineR, Data.lineG, Data.lineB, Data.lineA),
            _ => Color.white
        };
    }
    
    public void SetAllColors(Color dot, Color border, Color line)
    {
        Data.dotR = dot.r; Data.dotG = dot.g;
        Data.dotB = dot.b; Data.dotA = dot.a;
        
        Data.borderR = border.r; Data.borderG = border.g;
        Data.borderB = border.b; Data.borderA = border.a;
        
        Data.lineR = line.r; Data.lineG = line.g;
        Data.lineB = line.b; Data.lineA = line.a;
    }
    
    public (Color dot, Color border, Color line) GetAllColors()
    {
        return (
            new Color(Data.dotR, Data.dotG, Data.dotB, Data.dotA),
            new Color(Data.borderR, Data.borderG, Data.borderB, Data.borderA),
            new Color(Data.lineR, Data.lineG, Data.lineB, Data.lineA)
        );
    }

    // ─── Толщина рамки ───────────────────────────────────────────────────

    public float GetBorderThickness() => Data.borderThickness;

    public void SetBorderThickness(float thickness)
    {
        Data.borderThickness = Mathf.Max(1f, thickness);
    }

    // ─── Камера ──────────────────────────────────────────────────────────

    public void SetCameraSelection(int index, string name)
    {
        Data.selectedCameraIndex = index;
        Data.selectedCameraName = name;
    }

    public void SetCameraResolution(int width, int height)
    {
        Data.cameraWidth = Mathf.Max(0, width);
        Data.cameraHeight = Mathf.Max(0, height);
    }

    public Vector2Int GetCameraResolution()
    {
        return new Vector2Int(Data.cameraWidth, Data.cameraHeight);
    }

    public int GetSelectedCameraIndex() => Data.selectedCameraIndex;
    public string GetSelectedCameraName() => Data.selectedCameraName;
}