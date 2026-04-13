using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CharacterSettings
{
    public string characterName = "AI Character";
    public string personalityPrompt = "優しく丁寧で、視聴者に寄り添う性格です。";
    public string llmApiUrl = "";
    public string llmModel = "";
    public string ttsApiUrl = "";
    public string ttsModel = "";
    public bool youtubeMode = false;
    public string youtubeApiUrl = "";
    public string youtubeApiKey = "";
    public int selectedModelIndex = 0;
}

public class CharacterSettingsController : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;
    public Button applySettingsButton;

    [Header("Character Model")]
    public TMP_Dropdown modelDropdown;
    public Transform modelPreviewRoot;
    public List<GameObject> characterModelPrefabs;
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalEulerAngles = Vector3.zero;
    public Vector3 previewLocalScale = Vector3.one;

    [Header("Character Data")]
    public TMP_InputField characterNameField;
    public TMP_InputField personalityPromptField;

    [Header("LLM Settings")]
    public TMP_InputField llmApiUrlField;
    public TMP_InputField llmModelField;

    [Header("TTS Settings")]
    public TMP_InputField ttsApiUrlField;
    public TMP_InputField ttsModelField;

    [Header("YouTube Settings")]
    public Toggle youtubeModeToggle;
    public TMP_InputField youtubeApiUrlField;
    public TMP_InputField youtubeApiKeyField;

    [Header("Status")]
    public TMP_Text statusText;

    public CharacterSettings CurrentSettings { get; private set; } = new CharacterSettings();

    private GameObject currentModelInstance;

    private void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.AddListener(OpenSettingsPanel);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
        }

        if (applySettingsButton != null)
        {
            applySettingsButton.onClick.AddListener(ApplySettings);
        }

        InitializeModelDropdown();
        LoadSettingsToUI();
    }

    private void InitializeModelDropdown()
    {
        if (modelDropdown == null)
        {
            return;
        }

        modelDropdown.ClearOptions();
        var options = new List<string>();
        for (int i = 0; i < characterModelPrefabs.Count; i++)
        {
            string name = characterModelPrefabs[i] != null
                ? characterModelPrefabs[i].name
                : $"Model {i + 1}";
            options.Add(name);
        }

        if (options.Count == 0)
        {
            options.Add("No model prefabs assigned");
        }

        modelDropdown.AddOptions(options);
        modelDropdown.onValueChanged.RemoveAllListeners();
        modelDropdown.onValueChanged.AddListener(OnModelDropdownChanged);

        if (CurrentSettings.selectedModelIndex >= 0 && CurrentSettings.selectedModelIndex < options.Count)
        {
            modelDropdown.value = CurrentSettings.selectedModelIndex;
        }

        modelDropdown.RefreshShownValue();
        LoadSelectedModel();
    }

    private void OnModelDropdownChanged(int index)
    {
        CurrentSettings.selectedModelIndex = index;
        LoadSelectedModel();
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        settingsPanel.SetActive(true);
        UpdateStatus("キャラクター設定画面を開きました。");
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        settingsPanel.SetActive(false);
        UpdateStatus("キャラクター設定画面を閉じました。");
    }

    public void ApplySettings()
    {
        if (characterNameField != null)
        {
            CurrentSettings.characterName = characterNameField.text?.Trim() ?? CurrentSettings.characterName;
        }

        if (personalityPromptField != null)
        {
            CurrentSettings.personalityPrompt = personalityPromptField.text?.Trim() ?? CurrentSettings.personalityPrompt;
        }

        if (llmApiUrlField != null)
        {
            CurrentSettings.llmApiUrl = llmApiUrlField.text?.Trim() ?? CurrentSettings.llmApiUrl;
        }

        if (llmModelField != null)
        {
            CurrentSettings.llmModel = llmModelField.text?.Trim() ?? CurrentSettings.llmModel;
        }

        if (ttsApiUrlField != null)
        {
            CurrentSettings.ttsApiUrl = ttsApiUrlField.text?.Trim() ?? CurrentSettings.ttsApiUrl;
        }

        if (ttsModelField != null)
        {
            CurrentSettings.ttsModel = ttsModelField.text?.Trim() ?? CurrentSettings.ttsModel;
        }

        if (youtubeModeToggle != null)
        {
            CurrentSettings.youtubeMode = youtubeModeToggle.isOn;
        }

        if (youtubeApiUrlField != null)
        {
            CurrentSettings.youtubeApiUrl = youtubeApiUrlField.text?.Trim() ?? CurrentSettings.youtubeApiUrl;
        }

        if (youtubeApiKeyField != null)
        {
            CurrentSettings.youtubeApiKey = youtubeApiKeyField.text?.Trim() ?? CurrentSettings.youtubeApiKey;
        }

        if (modelDropdown != null)
        {
            CurrentSettings.selectedModelIndex = modelDropdown.value;
        }

        LoadSelectedModel();
        UpdateStatus("設定を保存しました。");
    }

    private void LoadSettingsToUI()
    {
        if (characterNameField != null)
        {
            characterNameField.text = CurrentSettings.characterName;
        }

        if (personalityPromptField != null)
        {
            personalityPromptField.text = CurrentSettings.personalityPrompt;
        }

        if (llmApiUrlField != null)
        {
            llmApiUrlField.text = CurrentSettings.llmApiUrl;
        }

        if (llmModelField != null)
        {
            llmModelField.text = CurrentSettings.llmModel;
        }

        if (ttsApiUrlField != null)
        {
            ttsApiUrlField.text = CurrentSettings.ttsApiUrl;
        }

        if (ttsModelField != null)
        {
            ttsModelField.text = CurrentSettings.ttsModel;
        }

        if (youtubeModeToggle != null)
        {
            youtubeModeToggle.isOn = CurrentSettings.youtubeMode;
        }

        if (youtubeApiUrlField != null)
        {
            youtubeApiUrlField.text = CurrentSettings.youtubeApiUrl;
        }

        if (youtubeApiKeyField != null)
        {
            youtubeApiKeyField.text = CurrentSettings.youtubeApiKey;
        }
    }

    private void LoadSelectedModel()
    {
        if (modelPreviewRoot == null)
        {
            return;
        }

        for (int i = modelPreviewRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(modelPreviewRoot.GetChild(i).gameObject);
        }

        if (CurrentSettings.selectedModelIndex < 0 || CurrentSettings.selectedModelIndex >= characterModelPrefabs.Count)
        {
            return;
        }

        GameObject prefab = characterModelPrefabs[CurrentSettings.selectedModelIndex];
        if (prefab == null)
        {
            return;
        }

        currentModelInstance = Instantiate(prefab, modelPreviewRoot);
        currentModelInstance.transform.localPosition = previewLocalPosition;
        currentModelInstance.transform.localEulerAngles = previewLocalEulerAngles;
        currentModelInstance.transform.localScale = previewLocalScale;
    }

    public string BuildCharacterContext()
    {
        string name = string.IsNullOrWhiteSpace(CurrentSettings.characterName)
            ? "このキャラクター"
            : CurrentSettings.characterName;

        string prompt = string.IsNullOrWhiteSpace(CurrentSettings.personalityPrompt)
            ? "丁寧で優しい性格です。"
            : CurrentSettings.personalityPrompt;

        return $"あなたは {name} です。性格: {prompt}\n";
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
