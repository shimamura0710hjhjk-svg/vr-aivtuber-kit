using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FurniturePlacementDefinition
{
    public SceneMode mode;
    public Transform[] placementPoints;
    public bool showMarkers = true;
}

public class FurniturePlacementController : MonoBehaviour
{
    [Header("Dependencies")]
    public SceneModeController sceneModeController;
    public Transform furnitureRoot;

    [Header("Furniture Options")]
    public GameObject[] furniturePrefabs;
    public TMP_Dropdown furnitureDropdown;

    [Header("Placement UI")]
    public GameObject placementPanel;
    public Button openPlacementButton;
    public Button closePlacementButton;
    public Button placeFurnitureButton;
    public Button clearFurnitureButton;
    public TMP_Text statusText;

    [Header("Scale Settings")]
    public TMP_InputField furnitureScaleField;
    public float defaultFurnitureScale = 1.0f;

    [Header("Placement Points")]
    public FurniturePlacementDefinition[] placementDefinitions;
    public GameObject placementMarkerPrefab;

    private SceneMode currentMode = SceneMode.FaceToFace;
    private readonly List<GameObject> placedFurniture = new List<GameObject>();
    private readonly List<GameObject> activeMarkers = new List<GameObject>();
    private readonly Dictionary<SceneMode, int> nextPlacementIndex = new Dictionary<SceneMode, int>();

    private float GetFurnitureScale()
    {
        float scale = defaultFurnitureScale;
        if (furnitureScaleField != null && float.TryParse(furnitureScaleField.text, out float parsedScale) && parsedScale > 0f)
        {
            scale = parsedScale;
        }
        return Mathf.Max(0.1f, scale);
    }

    private void Start()
    {
        if (placementPanel != null)
        {
            placementPanel.SetActive(false);
        }

        if (openPlacementButton != null)
        {
            openPlacementButton.onClick.AddListener(OpenPlacementPanel);
        }

        if (closePlacementButton != null)
        {
            closePlacementButton.onClick.AddListener(ClosePlacementPanel);
        }

        if (placeFurnitureButton != null)
        {
            placeFurnitureButton.onClick.AddListener(PlaceSelectedFurniture);
        }

        if (clearFurnitureButton != null)
        {
            clearFurnitureButton.onClick.AddListener(ClearPlacedFurniture);
        }

        InitializeFurnitureDropdown();
        RefreshCurrentMode();
    }

    private void Update()
    {
        if (sceneModeController == null)
        {
            return;
        }

        if (sceneModeController.CurrentMode != currentMode)
        {
            RefreshCurrentMode();
        }
    }

    private void InitializeFurnitureDropdown()
    {
        if (furnitureDropdown == null)
        {
            return;
        }

        furnitureDropdown.ClearOptions();
        var options = new List<string>();
        for (int i = 0; i < furniturePrefabs.Length; i++)
        {
            options.Add(furniturePrefabs[i] != null ? furniturePrefabs[i].name : $"家具 {i + 1}");
        }

        if (options.Count == 0)
        {
            options.Add("No furniture prefabs assigned");
        }

        furnitureDropdown.AddOptions(options);
        furnitureDropdown.RefreshShownValue();
    }

    public void OpenPlacementPanel()
    {
        if (placementPanel != null)
        {
            placementPanel.SetActive(true);
        }

        UpdateStatus("家具配置パネルを開きました。配置したい家具を選んでください。");
    }

    public void ClosePlacementPanel()
    {
        if (placementPanel != null)
        {
            placementPanel.SetActive(false);
        }

        UpdateStatus("家具配置パネルを閉じました。");
    }

    private void RefreshCurrentMode()
    {
        currentMode = sceneModeController != null ? sceneModeController.CurrentMode : SceneMode.FaceToFace;
        UpdatePlacementMarkers();
        UpdateStatus($"{currentMode} 用の配置ポイントを表示しています。");
    }

    private FurniturePlacementDefinition GetPlacementDefinition(SceneMode mode)
    {
        if (placementDefinitions == null)
        {
            return null;
        }

        foreach (var definition in placementDefinitions)
        {
            if (definition != null && definition.mode == mode)
            {
                return definition;
            }
        }

        return null;
    }

    private void UpdatePlacementMarkers()
    {
        foreach (var marker in activeMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        activeMarkers.Clear();

        var definition = GetPlacementDefinition(currentMode);
        if (definition == null || definition.placementPoints == null || definition.placementPoints.Length == 0)
        {
            return;
        }

        if (placementMarkerPrefab == null)
        {
            return;
        }

        if (!definition.showMarkers)
        {
            return;
        }

        foreach (var point in definition.placementPoints)
        {
            if (point == null)
            {
                continue;
            }

            GameObject marker = Instantiate(placementMarkerPrefab, point.position, point.rotation, point);
            marker.name = $"PlacementMarker_{currentMode}_{activeMarkers.Count + 1}";
            activeMarkers.Add(marker);
        }
    }

    public void PlaceSelectedFurniture()
    {
        if (furniturePrefabs == null || furniturePrefabs.Length == 0)
        {
            UpdateStatus("配置可能な家具が登録されていません。");
            return;
        }

        int selectedIndex = 0;
        if (furnitureDropdown != null)
        {
            selectedIndex = furnitureDropdown.value;
        }

        if (selectedIndex < 0 || selectedIndex >= furniturePrefabs.Length)
        {
            UpdateStatus("有効な家具が選択されていません。");
            return;
        }

        var furniturePrefab = furniturePrefabs[selectedIndex];
        if (furniturePrefab == null)
        {
            UpdateStatus("選択した家具が無効です。");
            return;
        }

        var definition = GetPlacementDefinition(currentMode);
        if (definition == null || definition.placementPoints == null || definition.placementPoints.Length == 0)
        {
            UpdateStatus("このモードには配置ポイントが設定されていません。");
            return;
        }

        int nextIndex = 0;
        if (!nextPlacementIndex.TryGetValue(currentMode, out nextIndex))
        {
            nextIndex = 0;
        }

        if (nextIndex >= definition.placementPoints.Length)
        {
            UpdateStatus("このモードの配置ポイントはすべて使用されています。先に配置をクリアしてください。");
            return;
        }

        var targetPoint = definition.placementPoints[nextIndex];
        if (targetPoint == null)
        {
            UpdateStatus("配置ポイントが無効です。設定を確認してください。");
            return;
        }

        float furnitureScale = GetFurnitureScale();
        GameObject instance = Instantiate(furniturePrefab, furnitureRoot != null ? furnitureRoot : targetPoint, false);
        instance.transform.position = targetPoint.position;
        instance.transform.rotation = targetPoint.rotation;
        instance.transform.localScale = furniturePrefab.transform.localScale * furnitureScale;
        placedFurniture.Add(instance);

        nextPlacementIndex[currentMode] = nextIndex + 1;
        UpdateStatus($"{furniturePrefab.name} を {currentMode} の配置ポイント {nextIndex + 1} に置きました。スケール: {furnitureScale:0.##}");
    }

    public void ClearPlacedFurniture()
    {
        foreach (var furniture in placedFurniture)
        {
            if (furniture != null)
            {
                Destroy(furniture);
            }
        }

        placedFurniture.Clear();
        nextPlacementIndex.Clear();
        UpdateStatus("配置した家具をすべて削除しました。");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
