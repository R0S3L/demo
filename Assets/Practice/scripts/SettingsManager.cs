using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

        // Цвет DOT
        public float dotR = 1f, dotG = 1f, dotB = 1f, dotA = 1f;
        // Цвет BORDER
        public float borderR = 1f, borderG = 0f, borderB = 0f, borderA = 1f;
        // Цвет LINE
        public float lineR = 0f, lineG = 0f, lineB = 1f, lineA = 1f;

        // Толщина рамки
        public float borderThickness = 2f;

        // Смещение _areaRect (калибровка parent)
        public float calibrationOffsetX = 0f;
        public float calibrationOffsetY = 0f;
    }

    public SettingsData Data { get; private set; } = new SettingsData();
    private string filePath;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Load();
    }

    public void Load()
    {
        if (File.Exists(filePath))
            Data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(filePath));
        else
        { Data = new SettingsData(); Save(); }
    }

    public void Save()
    {
        File.WriteAllText(filePath, JsonUtility.ToJson(Data, true));
    }

    private void OnApplicationQuit() => Save();
    private void OnApplicationPause(bool pause) { if (pause) Save(); }

    // ─── Позиции точек ───────────────────────────────────────────────────

    public void UpdatePositions(List<Vector2> currentPositions, Vector2 areaSize)
    {
        Data.savedPositions = new List<Vector2>();
        float w = Mathf.Max(1f, areaSize.x);
        float h = Mathf.Max(1f, areaSize.y);
        foreach (Vector2 pos in currentPositions)
            Data.savedPositions.Add(new Vector2(
                Mathf.Clamp01(pos.x / w),
                Mathf.Clamp01(pos.y / h)));
    }

    // ─── Цвета ───────────────────────────────────────────────────────────

    public void SetColor(int index, Color c)
    {
        switch (index)
        {
            case 0: Data.dotR=c.r; Data.dotG=c.g; Data.dotB=c.b; Data.dotA=c.a; break;
            case 1: Data.borderR=c.r; Data.borderG=c.g; Data.borderB=c.b; Data.borderA=c.a; break;
            case 2: Data.lineR=c.r; Data.lineG=c.g; Data.lineB=c.b; Data.lineA=c.a; break;
        }
    }

    public Color GetColor(int index) => index switch
    {
        0 => new Color(Data.dotR, Data.dotG, Data.dotB, Data.dotA),
        1 => new Color(Data.borderR, Data.borderG, Data.borderB, Data.borderA),
        2 => new Color(Data.lineR, Data.lineG, Data.lineB, Data.lineA),
        _ => Color.white
    };

    public void SetAllColors(Color dot, Color border, Color line)
    {
        Data.dotR=dot.r; Data.dotG=dot.g; Data.dotB=dot.b; Data.dotA=dot.a;
        Data.borderR=border.r; Data.borderG=border.g; Data.borderB=border.b; Data.borderA=border.a;
        Data.lineR=line.r; Data.lineG=line.g; Data.lineB=line.b; Data.lineA=line.a;
    }

    public (Color dot, Color border, Color line) GetAllColors() => (
        new Color(Data.dotR, Data.dotG, Data.dotB, Data.dotA),
        new Color(Data.borderR, Data.borderG, Data.borderB, Data.borderA),
        new Color(Data.lineR, Data.lineG, Data.lineB, Data.lineA));

    // ─── Толщина рамки ───────────────────────────────────────────────────

    public float GetBorderThickness() => Data.borderThickness;
    public void SetBorderThickness(float t) => Data.borderThickness = Mathf.Max(1f, t);

    // ─── Калибровка parent ───────────────────────────────────────────────

    public Vector2 GetCalibrationOffset() =>
        new Vector2(Data.calibrationOffsetX, Data.calibrationOffsetY);

    public void SetCalibrationOffset(Vector2 offset)
    {
        Data.calibrationOffsetX = offset.x;
        Data.calibrationOffsetY = offset.y;
    }

    // ─── Камера ──────────────────────────────────────────────────────────

    public void SetCameraSelection(int index, string name)
    { Data.selectedCameraIndex = index; Data.selectedCameraName = name; }

    public void SetCameraResolution(int width, int height)
    { Data.cameraWidth = Mathf.Max(0, width); Data.cameraHeight = Mathf.Max(0, height); }

    public Vector2Int GetCameraResolution() =>
        new Vector2Int(Data.cameraWidth, Data.cameraHeight);

    public int GetSelectedCameraIndex() => Data.selectedCameraIndex;
    public string GetSelectedCameraName() => Data.selectedCameraName;
}