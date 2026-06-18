using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
public class PointVisualizer : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    public PointCenterTracker tracker; // Ссылка на наш основной скрипт-трекер
    public RawImage cameraDisplay;     // Ссылка на тот же UI RawImage, где крутится вебкамера
    
    [Header("Настройки маркера")]
    public GameObject markerPrefab;    // Префаб маркера (например, UI Image в виде точки или крестика)
    public Transform markerParent;     // UI панель-родитель, куда будут спавниться маркеры

    // Пул объектов, чтобы избежать постоянного вызова Instantiate/Destroy во время игры
    private List<GameObject> markerPool = new List<GameObject>();

    void Update()
    {
        if (tracker == null || cameraDisplay == null || markerPrefab == null) return;

        List<float2> centers = tracker.DetectedCenters;
        
        // Если точек на экране больше, чем объектов в пуле — расширяем пул
        while (markerPool.Count < centers.Count)
        {
            GameObject newMarker = Instantiate(markerPrefab, markerParent != null ? markerParent : transform);
            markerPool.Add(newMarker);
        }

        // Получаем физический размер картинки вебкамеры и физический размер UI элемента на экране
        Vector2 textureSize = new Vector2(tracker.CameraWidth, tracker.CameraHeight);
        Vector2 displaySize = cameraDisplay.rectTransform.rect.size;

        Color dotColor = Color.white;
        Color borderColor = Color.white;
        Color lineColor = Color.white;

        if (SettingsManager.Instance != null)
        {
            (dotColor, borderColor, lineColor) = SettingsManager.Instance.GetAllColors();
        }

        for (int i = 0; i < markerPool.Count; i++)
        {
            if (i < centers.Count)
            {
                // Включаем маркер для обнаруженной точки
                markerPool[i].SetActive(true);
                
                // 1. Нормализуем координаты точки из пространства пикселей камеры в диапазон (0.0 - 1.0)
                float2 normPos = centers[i] / (float2)textureSize;

                // 2. Конвертируем нормализованные координаты в локальные координаты UI (с учетом центра UI элемента)
                Vector2 anchoredPos = new Vector2(
                    (normPos.x - 0.5f) * displaySize.x,
                    (normPos.y - 0.5f) * displaySize.y
                );

                // Перемещаем маркер
                RectTransform rectTransform = markerPool[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = anchoredPos;
                }

                Image[] images = markerPool[i].GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    string name = image.name.ToLower();
                    if (name.Contains("border"))
                        image.color = borderColor;
                    else if (name.Contains("line"))
                        image.color = lineColor;
                    else
                        image.color = dotColor;
                }
            }
            else
            {
                // Если точек сейчас меньше, чем объектов в пуле, лишние маркеры просто скрываем
                markerPool[i].SetActive(false);
            }
        }
    }
}
