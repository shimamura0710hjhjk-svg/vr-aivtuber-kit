using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class ChatRequest
{
    public string userId;
    public string prompt;
    public string context;
    public bool return_base64;
}

[Serializable]
public class ChatResponse
{
    public string status;
    public string text;
    public string audio_path;
    public string audio_base64;
    public string model;
    public int duration_ms;
}

public class AITuberController : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:8000";

    [Header("UI")]
    public TMP_Text responseText;

    [Header("Audio")]
    public AudioSource audioSource;
    public bool requestBase64Audio = true;

    public void SendChat(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("Prompt is empty. Skipping chat request.");
            return;
        }

        StartCoroutine(PostChatCoroutine(prompt));
    }

    private IEnumerator PostChatCoroutine(string prompt)
    {
        var requestPayload = new ChatRequest
        {
            userId = "unity",
            prompt = prompt,
            context = null,
            return_base64 = requestBase64Audio,
        };

        string json = JsonUtility.ToJson(requestPayload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest($"{serverUrl}/chat", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Chat request failed: {request.error}");
            yield break;
        }

        ChatResponse response;
        try
        {
            response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse chat response: {e.Message}\n{request.downloadHandler.text}");
            yield break;
        }

        if (responseText != null)
        {
            responseText.text = response.text;
        }

        if (!string.IsNullOrEmpty(response.audio_base64))
        {
            LoadAudioFromBase64(response.audio_base64, response.text);
            yield break;
        }

        if (!string.IsNullOrEmpty(response.audio_path))
        {
            yield return StartCoroutine(LoadAudioFromFile(response.audio_path));
            yield break;
        }

        Debug.LogWarning("No audio source was returned from backend.");
    }

    private void LoadAudioFromBase64(string base64Audio, string responseText)
    {
        try
        {
            byte[] audioBytes = Convert.FromBase64String(base64Audio);
            AudioClip clip = WavUtility.ToAudioClip(audioBytes, "AITuberResponse");

            PlayClip(clip);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load audio from base64: {e.Message}");
        }
    }

    private IEnumerator LoadAudioFromFile(string filePath)
    {
        string uri = filePath;
        if (!uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            uri = "file://" + uri;
        }

        using var request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Failed to load audio from file: {request.error}");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null. Cannot play audio.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is not assigned.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavFileBytes, string clipName)
    {
        if (wavFileBytes == null || wavFileBytes.Length < 44)
        {
            throw new ArgumentException("Invalid WAV data");
        }

        int channels = wavFileBytes[22];
        int sampleRate = BitConverter.ToInt32(wavFileBytes, 24);
        int bitsPerSample = BitConverter.ToInt16(wavFileBytes, 34);
        int dataLength = BitConverter.ToInt32(wavFileBytes, 40);
        int bytesPerSample = bitsPerSample / 8;
        int sampleCount = dataLength / bytesPerSample;

        float[] audioData = new float[sampleCount];
        int dataOffset = 44;

        if (bitsPerSample == 16)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                int sampleIndex = dataOffset + i * 2;
                short sample = BitConverter.ToInt16(wavFileBytes, sampleIndex);
                audioData[i] = sample / 32768f;
            }
        }
        else
        {
            throw new NotSupportedException($"Only 16-bit WAV files are supported. Found {bitsPerSample}-bit.");
        }

        AudioClip audioClip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
        audioClip.SetData(audioData, 0);
        return audioClip;
    }
}
