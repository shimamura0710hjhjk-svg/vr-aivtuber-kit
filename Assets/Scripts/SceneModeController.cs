using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SceneMode
{
    FaceToFace,
    SleepOver,
    LapPillow,
    WalkAndTalk,
    LiveStream,
    SofaChat,
    CustomFree,
}

[Serializable]
public class SceneModeDefinition
{
    public SceneMode mode;
    public string title;
    public string description;
    public GameObject[] modeObjects;
    public Animator[] animators;
    public string[] animatorTriggers;
}

public class SceneModeController : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;
    public Button applySettingsButton;

    [Header("Mode Buttons")]
    public Button modeButtonPrefab;
    public Transform modeButtonContainer;

    [Header("Mode Display")]
    public TMP_Text modeTitleText;
    public TMP_Text modeDescriptionText;
    public TMP_Text statusText;

    [Header("Custom Free Mode")]
    public TMP_InputField customModeNameField;
    public TMP_InputField customModeDescriptionField;
    public TMP_InputField customSettingsField;

    [Header("Scene Mode Definitions")]
    public SceneModeDefinition[] modeDefinitions;

    public SceneMode CurrentMode { get; private set; } = SceneMode.FaceToFace;
    public string CustomModeName => customModeNameField != null ? customModeNameField.text : "Custom Free Mode";

    private readonly Dictionary<SceneMode, Button> modeButtons = new Dictionary<SceneMode, Button>();

    private void Start()
    {
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
            applySettingsButton.onClick.AddListener(ApplyCurrentMode);
        }

        BuildModeButtons();
        ApplySceneMode(CurrentMode);
        HideSettingsPanel();
    }

    private void BuildModeButtons()
    {
        if (modeButtonPrefab == null || modeButtonContainer == null)
        {
            return;
        }

        foreach (Transform child in modeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        modeButtons.Clear();

        foreach (SceneMode definition in Enum.GetValues(typeof(SceneMode)))
        {
            Button button = Instantiate(modeButtonPrefab, modeButtonContainer);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = definition == SceneMode.CustomFree ? "Custom Free" : definition.ToString();
            }

            SceneMode capturedMode = definition;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectMode(capturedMode));
            modeButtons[capturedMode] = button;
        }
    }

    public void SelectMode(SceneMode mode)
    {
        CurrentMode = mode;
        UpdateModeDescription();
        if (statusText != null)
        {
            statusText.text = $"選択中のシーンモード: {GetModeTitle(mode)}";
        }
    }

    public void ApplyCurrentMode()
    {
        ApplySceneMode(CurrentMode);
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        UpdateStatus("シーンモード設定を開きました。");
    }

    public void CloseSettingsPanel()
    {
        HideSettingsPanel();
        UpdateStatus("シーンモード設定を閉じました。");
    }

    private void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void UpdateModeDescription()
    {
        string title = GetModeTitle(CurrentMode);
        string description = GetModeDescription(CurrentMode);

        if (modeTitleText != null)
        {
            modeTitleText.text = title;
        }

        if (modeDescriptionText != null)
        {
            modeDescriptionText.text = description;
        }
    }

    private string GetModeTitle(SceneMode mode)
    {
        if (mode == SceneMode.CustomFree && !string.IsNullOrWhiteSpace(CustomModeName))
        {
            return CustomModeName;
        }

        foreach (var def in modeDefinitions)
        {
            if (def.mode == mode)
            {
                return string.IsNullOrWhiteSpace(def.title) ? def.mode.ToString() : def.title;
            }
        }

        return mode.ToString();
    }

    private string GetModeDescription(SceneMode mode)
    {
        if (mode == SceneMode.CustomFree && customModeDescriptionField != null && !string.IsNullOrWhiteSpace(customModeDescriptionField.text))
        {
            return customModeDescriptionField.text;
        }

        foreach (var def in modeDefinitions)
        {
            if (def.mode == mode)
            {
                return def.description;
            }
        }

        return "このモードに合わせてモデルとアセットを配置します。";
    }

    public void ApplySceneMode(SceneMode mode)
    {
        CurrentMode = mode;
        foreach (var def in modeDefinitions)
        {
            bool active = def.mode == mode;
            if (def.modeObjects != null)
            {
                foreach (var obj in def.modeObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(active);
                    }
                }
            }

            if (active && def.animators != null && def.animatorTriggers != null)
            {
                for (int i = 0; i < def.animators.Length && i < def.animatorTriggers.Length; i++)
                {
                    Animator animator = def.animators[i];
                    string trigger = def.animatorTriggers[i];
                    if (animator != null && !string.IsNullOrWhiteSpace(trigger))
                    {
                        animator.SetTrigger(trigger);
                    }
                }
            }
        }

        if (CurrentMode == SceneMode.CustomFree)
        {
            UpdateStatus("カスタムフリーモードを適用しました。");
        }
        else
        {
            UpdateStatus($"{GetModeTitle(CurrentMode)} を適用しました。");
        }

        UpdateModeDescription();
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
