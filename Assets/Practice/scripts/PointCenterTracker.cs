using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PointCenterTracker : MonoBehaviour
{
    [Header("Настройки вывода")]
    public RawImage cameraDisplay;

    [Header("Параметры трекинга и сегментации")]
    [Range(10, 255)]
    public byte maxPointBrightness = 100;

    [Tooltip("Минимальная площадь фигуры в пикселях")]
    public int minSegmentArea = 10;

    [Tooltip("Максимальная площадь фигуры в пикселях")]
    public int maxSegmentArea = 5000;

    [Header("Ограничение кадра (отсечение краев)")]
    [Tooltip("Отступ от верхнего края кадра в пикселях")]
    [SerializeField] int cropTop = 0;
    [Tooltip("Отступ от нижнего края кадра в пикселях")]
    [SerializeField] int cropBottom = 0;
    [Tooltip("Отступ от левого края кадра в пикселях")]
    [SerializeField] int cropLeft = 0;
    [Tooltip("Отступ от правого края кадра в пикселях")]
    [SerializeField] int cropRight = 0;

    public int CameraWidth { get; private set; }
    public int CameraHeight { get; private set; }

    private WebCamTexture webCamTexture;
    private Color32[] managedColorBuffer;
    private NativeArray<Color32> inputPixelsNative;
    private NativeArray<byte> binaryMaskBuffer;
    private NativeArray<byte> visitedMaskBuffer;
    private NativeList<int2> fillStackBuffer;
    private NativeList<float2> segmentCentersResult;

    private JobHandle jobHandle;
    private bool isInitialized = false;

    public List<float2> DetectedCenters { get; private set; } = new List<float2>();

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
            Debug.Log("Webcam available: " + devices[i].name);

        if (devices.Length == 0)
        {
            Debug.LogError("[PointCenterTracker] Камеры не найдены.");
            return;
        }

        webCamTexture = CreateWebCamTexture(devices);
        webCamTexture.Play();

        if (cameraDisplay != null)
            cameraDisplay.texture = webCamTexture;

        StartCoroutine(InitializeRoutine());
    }

    private WebCamTexture CreateWebCamTexture(WebCamDevice[] devices)
    {
        string deviceName = null;

        if (SettingsManager.Instance != null)
        {
            // Ищем по имени — оно стабильнее индекса при переподключении
            string savedName = SettingsManager.Instance.GetSelectedCameraName();
            if (!string.IsNullOrEmpty(savedName))
            {
                foreach (var d in devices)
                {
                    if (d.name == savedName)
                    {
                        deviceName = d.name;
                        break;
                    }
                }
            }

            // Если по имени не нашли — пробуем по индексу
            if (deviceName == null)
            {
                int savedIndex = SettingsManager.Instance.GetSelectedCameraIndex();
                if (savedIndex >= 0 && savedIndex < devices.Length)
                    deviceName = devices[savedIndex].name;
            }
        }

        // Фолбэк: первая доступная камера
        if (deviceName == null)
            deviceName = devices[0].name;

        // Разрешение из настроек или дефолтное 1920x1080
        Vector2Int res = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetCameraResolution()
            : Vector2Int.zero;

        Debug.Log($"[PointCenterTracker] Камера: {deviceName}, разрешение: {(res.x > 0 ? res.ToString() : "1920x1080 (default)")}");

        return (res.x > 0 && res.y > 0)
            ? new WebCamTexture(deviceName, res.x, res.y, 60)
            : new WebCamTexture(deviceName, 1920, 1080, 60);
    }

    System.Collections.IEnumerator InitializeRoutine()
    {
        while (webCamTexture.width <= 16) yield return null;

        CameraWidth = webCamTexture.width;
        CameraHeight = webCamTexture.height;
        int pixelCount = CameraWidth * CameraHeight;

        managedColorBuffer   = new Color32[pixelCount];
        inputPixelsNative    = new NativeArray<Color32>(pixelCount, Allocator.Persistent);
        binaryMaskBuffer     = new NativeArray<byte>(pixelCount, Allocator.Persistent);
        visitedMaskBuffer    = new NativeArray<byte>(pixelCount, Allocator.Persistent);
        fillStackBuffer      = new NativeList<int2>(1024, Allocator.Persistent);
        segmentCentersResult = new NativeList<float2>(128, Allocator.Persistent);

        // Применяем сохранённые границы ПОСЛЕ того, как узнали разрешение камеры
        ApplySavedBounds();

        isInitialized = true;
        Debug.Log($"Конвейер сегментации блобов запущен. Разрешение: {CameraWidth}x{CameraHeight}");
    }
    private void ApplySavedBounds()
    {
        if (SettingsManager.Instance == null) return;

        List<Vector2> saved = SettingsManager.Instance.Data.savedPositions;
        if (saved == null || saved.Count < 2) return;

        // Денормализуем обе точки в пиксели камеры
        Vector2 p1 = new Vector2(saved[0].x * CameraWidth, saved[0].y * CameraHeight);
        Vector2 p2 = new Vector2(saved[1].x * CameraWidth, saved[1].y * CameraHeight);

        // Находим реальный прямоугольник независимо от порядка точек
        int left   = Mathf.RoundToInt(Mathf.Min(p1.x, p2.x));
        int right  = Mathf.RoundToInt(Mathf.Max(p1.x, p2.x));
        int bottom = Mathf.RoundToInt(Mathf.Min(p1.y, p2.y));
        int top    = Mathf.RoundToInt(Mathf.Max(p1.y, p2.y));

        // Переводим в отступы от краёв кадра
        cropLeft   = Mathf.Clamp(left,                  0, CameraWidth);
        cropRight  = Mathf.Clamp(CameraWidth  - right,  0, CameraWidth);
        cropBottom = Mathf.Clamp(bottom,                 0, CameraHeight);
        cropTop    = Mathf.Clamp(CameraHeight - top,     0, CameraHeight);

        Debug.Log($"[PointCenterTracker] Границы применены: " +
                  $"L={cropLeft} R={cropRight} B={cropBottom} T={cropTop} " +
                  $"(область {right - left}x{top - bottom}px)");
    }

    void Update()
    {
        if (!isInitialized || !webCamTexture.didUpdateThisFrame) return;
        if (!jobHandle.IsCompleted) return;
        jobHandle.Complete();

        DetectedCenters.Clear();
        for (int i = 0; i < segmentCentersResult.Length; i++)
            DetectedCenters.Add(segmentCentersResult[i]);

        segmentCentersResult.Clear();

        webCamTexture.GetPixels32(managedColorBuffer);
        inputPixelsNative.CopyFrom(managedColorBuffer);

        ProcessFrame(inputPixelsNative, CameraWidth, CameraHeight);
    }

    private void ProcessFrame(NativeArray<Color32> rawPixels, int width, int height)
    {
        var preprocessJob = new GrayscaleAndThresholdJob
        {
            InputPixels = rawPixels,
            MaxPointBrightnessThreshold = maxPointBrightness,
            BinaryMask = binaryMaskBuffer
        };
        JobHandle preprocessHandle = preprocessJob.Schedule(rawPixels.Length, 1024);

        var segmentationJob = new ImageSegmentationJob
        {
            BinaryMask = binaryMaskBuffer,
            VisitedMask = visitedMaskBuffer,
            FillStack = fillStackBuffer,
            Width = width,
            Height = height,
            MinArea = minSegmentArea,
            MaxArea = maxSegmentArea,
            CropTop    = cropTop,
            CropBottom = cropBottom,
            CropLeft   = cropLeft,
            CropRight  = cropRight,
            OutputCenters = segmentCentersResult
        };

        jobHandle = segmentationJob.Schedule(preprocessHandle);
    }

    void OnDestroy()
    {
        jobHandle.Complete();

        if (inputPixelsNative.IsCreated)    inputPixelsNative.Dispose();
        if (binaryMaskBuffer.IsCreated)     binaryMaskBuffer.Dispose();
        if (visitedMaskBuffer.IsCreated)    visitedMaskBuffer.Dispose();
        if (fillStackBuffer.IsCreated)      fillStackBuffer.Dispose();
        if (segmentCentersResult.IsCreated) segmentCentersResult.Dispose();

        webCamTexture.Stop();
    }
}

// ============================================================================
// BURST-ЗАДАЧИ СЕГМЕНТАЦИИ (без изменений)
// ============================================================================

[BurstCompile(CompileSynchronously = true)]
public struct GrayscaleAndThresholdJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Color32> InputPixels;
    public byte MaxPointBrightnessThreshold;
    [WriteOnly] public NativeArray<byte> BinaryMask;

    public void Execute(int index)
    {
        Color32 c = InputPixels[index];
        float3 rgb = new float3(c.r, c.g, c.b);
        byte gray = (byte)math.clamp(math.dot(rgb, new float3(0.299f, 0.587f, 0.114f)), 0f, 255f);
        BinaryMask[index] = (gray <= MaxPointBrightnessThreshold) ? (byte)1 : (byte)0;
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct ImageSegmentationJob : IJob
{
    [ReadOnly] public NativeArray<byte> BinaryMask;
    public NativeArray<byte> VisitedMask;
    public NativeList<int2> FillStack;
    public int Width;
    public int Height;
    public int MinArea;
    public int MaxArea;
    public int CropTop;
    public int CropBottom;
    public int CropLeft;
    public int CropRight;

    public NativeList<float2> OutputCenters;

    public void Execute()
    {
        int totalPixels = Width * Height;
        for (int i = 0; i < totalPixels; i++)
            VisitedMask[i] = 0;

        int minX = math.max(0, CropLeft);
        int maxX = math.min(Width, Width - CropRight);
        int minY = math.max(0, CropBottom);
        int maxY = math.min(Height, Height - CropTop);

        if (minX >= maxX || minY >= maxY) return;

        for (int y = minY; y < maxY; y++)
        {
            int rowOffset = y * Width;
            for (int x = minX; x < maxX; x++)
            {
                int idx = rowOffset + x;
                if (BinaryMask[idx] == 0 || VisitedMask[idx] != 0) continue;

                bool hasBackgroundAbove = (y == minY) || (BinaryMask[idx - Width] == 0);
                if (!hasBackgroundAbove) continue;

                long sumX = 0, sumY = 0;
                int area = 0;
                FloodFillAccumulate(minX, maxX, minY, maxY, x, y, ref sumX, ref sumY, ref area);

                if (area >= MinArea && area <= MaxArea)
                    OutputCenters.Add(new float2((float)sumX / area, (float)sumY / area));
            }
        }
    }

    private void FloodFillAccumulate(int minX, int maxX, int minY, int maxY,
        int startX, int startY, ref long sumX, ref long sumY, ref int area)
    {
        NativeList<int2> stack = FillStack;
        stack.Clear();
        stack.Add(new int2(startX, startY));

        while (stack.Length > 0)
        {
            int index = stack.Length - 1;
            int2 p = stack[index];
            stack.Length = index;

            int idx = p.y * Width + p.x;
            if (BinaryMask[idx] == 0 || VisitedMask[idx] != 0) continue;

            int xLeft = p.x;
            while (xLeft > minX && BinaryMask[p.y * Width + (xLeft - 1)] != 0 && VisitedMask[p.y * Width + (xLeft - 1)] == 0)
                xLeft--;

            int xRight = p.x;
            while (xRight < maxX - 1 && BinaryMask[p.y * Width + (xRight + 1)] != 0 && VisitedMask[p.y * Width + (xRight + 1)] == 0)
                xRight++;

            int rowOffset = p.y * Width;
            for (int x = xLeft; x <= xRight; x++)
            {
                VisitedMask[rowOffset + x] = 1;
                sumX += x;
                sumY += p.y;
                area++;
            }

            for (int dy = -1; dy <= 1; dy += 2)
            {
                int ny = p.y + dy;
                if (ny < minY || ny >= maxY) continue;

                int neighborRowOffset = ny * Width;
                bool inSegment = false;
                for (int x = xLeft; x <= xRight; x++)
                {
                    int nidx = neighborRowOffset + x;
                    bool isObject = BinaryMask[nidx] != 0 && VisitedMask[nidx] == 0;
                    if (isObject && !inSegment) { stack.Add(new int2(x, ny)); inSegment = true; }
                    else if (!isObject) inSegment = false;
                }
            }
        }
    }
}