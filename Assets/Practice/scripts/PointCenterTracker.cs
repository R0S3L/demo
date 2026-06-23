using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PointCenterTracker : MonoBehaviour
{
    [Header("Настройки вывода")]
    public RawImage cameraDisplay; 

    [Header("Параметры трекинга")]
    [Range(10, 255)] 
    public byte maxPointBrightness = 100; // Всё, что темнее -> точка

    [Header("Параметры объединения точек")]
    [Tooltip("Максимальное расстояние в пикселях между точками, при котором они сольются в одну")]
    public float mergeDistance = 15f; 

    public int CameraWidth { get; private set; }
    public int CameraHeight { get; private set; }

    private WebCamTexture webCamTexture;
    private Color32[] managedColorBuffer;       
    private NativeArray<Color32> inputPixelsNative; 
    private NativeArray<byte> grayscaleBuffer;     
    private NativeQueue<float2> foundCentersQueue; 
    
    // НАДСТРОЙКА: Нативный список для хранения финальных объединенных точек
    private NativeList<float2> mergedPointsResult; 
    
    private JobHandle jobHandle;                   
    private bool isInitialized = false;
    [SerializeField]private PointManager pm;
    
    public List<float2> DetectedCenters { get; private set; } = new List<float2>();
    private List<Vector2> savedPositions = new List<Vector2>();
    
    void Start()
    {
        ApplySavedCameraSettings();
        StartCoroutine(InitializeRoutine());
        LoadSavedDots();
    }

    private void ApplySavedCameraSettings()
    {
        if (SettingsManager.Instance != null)
        {
            int savedIndex = SettingsManager.Instance.GetSelectedCameraIndex();
            if (savedIndex >= 0 && savedIndex < WebCamTexture.devices.Length)
            {
                webCamTexture = new WebCamTexture();
                webCamTexture.deviceName = WebCamTexture.devices[savedIndex].name;
            }
            else
            {
                webCamTexture = new WebCamTexture(1920, 1080, 60);
            }
        }
        else
        {
            webCamTexture = new WebCamTexture(1920, 1080, 60);
        }
        if (cameraDisplay != null)
            cameraDisplay.texture = webCamTexture;

        webCamTexture.Play();

    }

    System.Collections.IEnumerator InitializeRoutine()
    {
        while (webCamTexture.width <= 16) yield return null;

        CameraWidth = webCamTexture.width;
        CameraHeight = webCamTexture.height;
        int pixelCount = CameraWidth * CameraHeight;

        managedColorBuffer = new Color32[pixelCount];
        inputPixelsNative = new NativeArray<Color32>(pixelCount, Allocator.Persistent);
        grayscaleBuffer = new NativeArray<byte>(pixelCount, Allocator.Persistent);
        foundCentersQueue = new NativeQueue<float2>(Allocator.Persistent);
        
        // Инициализируем список для мерж-задачи
        mergedPointsResult = new NativeList<float2>(Allocator.Persistent);

        isInitialized = true;
        Debug.Log($"Конвейер трекинга с функцией мержа запущен. Разрешение: {CameraWidth}x{CameraHeight}");
    }

    void Update()
    {
        if (!isInitialized || !webCamTexture.didUpdateThisFrame) return;

        // 1. ЗАБИРАЕМ ОЧИЩЕННЫЕ РЕЗУЛЬТАТЫ ИЗ ПОСЛЕДНЕГО JOB'А
        jobHandle.Complete();

        DetectedCenters.Clear();
        // Читаем данные напрямую из финального нативного списка
        for (int i = 0; i < mergedPointsResult.Length; i++)
        {
            DetectedCenters.Add(mergedPointsResult[i]);
        }

        if (DetectedCenters.Count > 0)
        {
            Debug.Log($"После объединения осталось точек: {DetectedCenters.Count}");
        }

        // Очищаем результирующий список и очередь для нового кадра.
        mergedPointsResult.Clear(); 

        // 2. ЗАГРУЖАЕМ НОВЫЙ КАДР
        webCamTexture.GetPixels32(managedColorBuffer);
        inputPixelsNative.CopyFrom(managedColorBuffer);

        ProcessFrame(inputPixelsNative, CameraWidth, CameraHeight);
    }

   private void ProcessFrame(NativeArray<Color32> rawPixels, int width, int height)
    {
        // Шаг А: Конвертация кадра в Grayscale
        var grayscaleJob = new ConvertToGrayscaleJob
        {
            InputPixels = rawPixels,
            OutputGrayscale = grayscaleBuffer
        };
        JobHandle grayscaleHandle = grayscaleJob.Schedule(rawPixels.Length, 1024);

        // Получаем область из сохраненных координат
        Vector2Int areaMin = Vector2Int.zero;
        Vector2Int areaMax = new Vector2Int(width, height);

        if (SettingsManager.Instance != null && SettingsManager.Instance.Data.savedPositions.Count >= 2)
        {
            Vector2 p1 = SettingsManager.Instance.Data.savedPositions[0];
            Vector2 p2 = SettingsManager.Instance.Data.savedPositions[1];

            bool isNormalized = p1.x <= 1f && p1.y <= 1f && p2.x <= 1f && p2.y <= 1f;

            if (isNormalized)
            {
                float minX = Mathf.Min(p1.x, p2.x) * width;
                float maxX = Mathf.Max(p1.x, p2.x) * width;
                float minY = Mathf.Min(p1.y, p2.y) * height;
                float maxY = Mathf.Max(p1.y, p2.y) * height;

                areaMin = new Vector2Int(Mathf.RoundToInt(minX), Mathf.RoundToInt(minY));
                areaMax = new Vector2Int(Mathf.RoundToInt(maxX), Mathf.RoundToInt(maxY));
            }
            else
            {
                areaMin = new Vector2Int(Mathf.RoundToInt(Mathf.Min(p1.x, p2.x)), Mathf.RoundToInt(Mathf.Min(p1.y, p2.y)));
                areaMax = new Vector2Int(Mathf.RoundToInt(Mathf.Max(p1.x, p2.x)), Mathf.RoundToInt(Mathf.Max(p1.y, p2.y)));
            }

            Debug.Log($"Область поиска: {areaMin} -> {areaMax}");
        }

        // Шаг Б: Поиск субпиксельных центров с ограничением области
        var darkPointsJob = new FindDarkPointCentersJob
        {
            GrayscalePixels = grayscaleBuffer,
            Width = width,
            Height = height,
            MaxPointBrightnessThreshold = maxPointBrightness,
            cornerLeft = areaMin,
            cornerRight = areaMax,
            FoundCenters = foundCentersQueue.AsParallelWriter()
        };
        JobHandle findPointsHandle = darkPointsJob.Schedule(rawPixels.Length, 1024, grayscaleHandle);

        // Шаг В: Объединение близких точек
        var mergeJob = new MergePointsFromQueueJob
        {
            InputQueue = foundCentersQueue,
            OutputPoints = mergedPointsResult,
            MergeDistance = mergeDistance,
            Width = width,
            Height = height
        };

        jobHandle = mergeJob.Schedule(findPointsHandle);
        Debug.Log($" {areaMin} {areaMax}");
    }

    void OnDestroy()
    {
        jobHandle.Complete();
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            webCamTexture = null;
        }
        if (inputPixelsNative.IsCreated) inputPixelsNative.Dispose();
        if (grayscaleBuffer.IsCreated) grayscaleBuffer.Dispose();
        if (foundCentersQueue.IsCreated) foundCentersQueue.Dispose();
        if (mergedPointsResult.IsCreated) mergedPointsResult.Dispose();
    }
    private void LoadSavedDots()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.Data.savedPositions != null)
        {
            savedPositions = SettingsManager.Instance.Data.savedPositions;
        }
    }
}

// ============================================================================
// BURST-ЗАДАЧИ
// ============================================================================

[BurstCompile(CompileSynchronously = true)]
public struct ConvertToGrayscaleJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Color32> InputPixels;
    public NativeArray<byte> OutputGrayscale;

    public void Execute(int index)
    {
        Color32 c = InputPixels[index];
        float3 rgb = new float3(c.r, c.g, c.b);
        OutputGrayscale[index] = (byte)math.dot(rgb, new float3(0.299f, 0.587f, 0.114f));
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct FindDarkPointCentersJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<byte> GrayscalePixels;
    public int Width;
    public int Height;
    public byte MaxPointBrightnessThreshold; 
    public NativeQueue<float2>.ParallelWriter FoundCenters;
    public Vector2Int cornerLeft;
    public Vector2Int cornerRight;

    public void Execute(int index)
    {
        int x = index % Width;
        int y = index / Width;
        int minX = cornerLeft.x, minY = cornerLeft.y, maxx = cornerRight.x, maxY = cornerRight.y;

        if(cornerLeft.x > cornerRight.x)
        {
            minX = cornerRight.x;
            maxx = cornerLeft.x;
        }
        if(cornerLeft.y > cornerRight.y)
        {
            minY = cornerRight.y;
            maxY = cornerLeft.y;
        }
        if (x < 2 || x >= Width - 2 || y < 2 || y >= Height - 2) return;
        if (x < minX || x > maxx || y < minY || y > maxY) return;

        byte centerValue = GrayscalePixels[index];
        if (centerValue > MaxPointBrightnessThreshold) return;
        if (!IsStrictLocalMinimum(x, y, centerValue)) return;

        float sumX = 0;
        float sumY = 0;
        float totalWeight = 0;

        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                byte pixelBrightness = GrayscalePixels[py * Width + px];

                float weight = math.max(0f, (float)MaxPointBrightnessThreshold - pixelBrightness);

                if (weight > 0)
                {
                    sumX += px * weight;
                    sumY += py * weight;
                    totalWeight += weight;
                }
            }
        }
        if (totalWeight > 0f)
        {
            FoundCenters.Enqueue(new float2(sumX / totalWeight, sumY / totalWeight));
        }
    }

    private bool IsStrictLocalMinimum(int cx, int cy, byte centerValue)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int neighborIndex = (cy + dy) * Width + (cx + dx);
                byte neighborValue = GrayscalePixels[neighborIndex];
                
                if (neighborValue < centerValue) return false;
                
                int currentIndex = cy * Width + cx;
                if (neighborValue == centerValue && neighborIndex < currentIndex) return false;
            }
        }
        return true;
    }
}

// МОДЕРНИЗИРОВАННАЯ BURST-ЗАДАЧА С ИСПОЛЬЗОВАНИЕМ SPATIAL GRID
[BurstCompile(CompileSynchronously = true)]
public struct MergePointsFromQueueJob : IJob
{
    public NativeQueue<float2> InputQueue;
    public NativeList<float2> OutputPoints;
    public float MergeDistance;
    public int Width;
    public int Height;

    public void Execute()
    {
        // Определяем размер ячейки сетки на основе дистанции мержа
        int cellSize = (int)math.ceil(MergeDistance);
        if (cellSize < 1) cellSize = 1;

        // Рассчитываем количество строк и столбцов сетки для текущего разрешения
        int cols = (Width + cellSize - 1) / cellSize;
        int rows = (Height + cellSize - 1) / cellSize;
        int totalCells = cols * rows;

        // Создаем временную сетку хэш-карты. Хранит индексы добавленных точек (-1 означает пусто)
        // Использование Allocator.Temp внутри Burst работает со скоростью стека памяти
        NativeArray<int> grid = new NativeArray<int>(totalCells, Allocator.Temp);
        for (int i = 0; i < totalCells; i++) grid[i] = -1;

        float mergeDistanceSq = MergeDistance * MergeDistance;

        while (InputQueue.TryDequeue(out float2 point))
        {
            // Находим координаты ячейки, в которую попадает текущая точка
            int cellX = (int)(point.x / cellSize);
            int cellY = (int)(point.y / cellSize);
            
            cellX = math.clamp(cellX, 0, cols - 1);
            cellY = math.clamp(cellY, 0, rows - 1);

            bool isCloseToExisting = false;

            // Вместо полного перебора списка проверяем только ТЕКУЩУЮ ячейку сетки и 8 её соседей
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cellX + dx;
                    int ny = cellY + dy;

                    if (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                    {
                        int cellIndex = ny * cols + nx;
                        int existingPointIdx = grid[cellIndex];

                        // Если в проверяемой ячейке уже зарегистрирована точка
                        if (existingPointIdx != -1)
                        {
                            float2 existingPoint = OutputPoints[existingPointIdx];
                            
                            // Считаем быстрое расстояние между ними (без math.sqrt)
                            if (math.distancesq(point, existingPoint) < mergeDistanceSq)
                            {
                                isCloseToExisting = true;
                                // Плавное слияние (усредняем координаты)
                                OutputPoints[existingPointIdx] = (existingPoint + point) * 0.5f;
                                break;
                            }
                        }
                    }
                }
                if (isCloseToExisting) break;
            }

            // Если в радиусе 9 соседних ячеек совпадений не обнаружено — это новая уникальная точка
            if (!isCloseToExisting)
            {
                int newIdx = OutputPoints.Length;
                OutputPoints.Add(point);

                // Регистрируем индекс точки в нашей пространственной сетке
                int cellIndex = cellY * cols + cellX;
                grid[cellIndex] = newIdx;
            }
        }

        grid.Dispose(); // Обязательно освобождаем временную сетку
    }
}