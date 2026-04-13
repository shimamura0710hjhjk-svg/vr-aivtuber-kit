using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RemoteStateUpdate
{
    public string emotion;
    public string last_interaction;
    public int tap_count;
    public int pet_head_count;
    public int pet_belly_count;
    public int punch_count;
}

[Serializable]
public class FramePayload
{
    public string frame_base64;
}

public class RemoteMonitorController : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:8000";
    public AITuberController aituberController;
    public float stateUpdateInterval = 1.0f;
    public float frameUploadInterval = 1.2f;
    public int jpgQuality = 40;

    private void Start()
    {
        if (aituberController == null)
        {
            Debug.LogWarning("AITuberController is not assigned on RemoteMonitorController.");
        }

        StartCoroutine(StateUpdateLoop());
        StartCoroutine(FrameUploadLoop());
    }

    private IEnumerator StateUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(stateUpdateInterval);
            yield return UploadState();
        }
    }

    private IEnumerator FrameUploadLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(frameUploadInterval);
            yield return UploadFrame();
        }
    }

    private IEnumerator UploadState()
    {
        if (aituberController == null)
        {
            yield break;
        }

        var state = new RemoteStateUpdate
        {
            emotion = aituberController.currentEmotion,
            last_interaction = aituberController.lastInteraction,
            tap_count = aituberController.tapCount,
            pet_head_count = aituberController.petHeadCount,
            pet_belly_count = aituberController.petBellyCount,
            punch_count = aituberController.punchCount,
        };

        string json = JsonUtility.ToJson(state);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest($"{serverUrl}/remote/state", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogWarning($"Remote state upload failed: {request.error}");
        }
    }

    private IEnumerator UploadFrame()
    {
        if (aituberController == null)
        {
            yield break;
        }

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
        {
            yield break;
        }

        byte[] jpgBytes = screenshot.EncodeToJPG(jpgQuality);
        UnityEngine.Object.Destroy(screenshot);

        string base64Image = Convert.ToBase64String(jpgBytes);
        var payload = new FramePayload { frame_base64 = base64Image };
        string json = JsonUtility.ToJson(payload);

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest($"{serverUrl}/remote/frame", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogWarning($"Remote frame upload failed: {request.error}");
        }
    }
}
