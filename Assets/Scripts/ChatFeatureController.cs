using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class YouTubeCommentsRequest
{
    public string video_id;
    public int max_results = 10;
}

[Serializable]
public class YouTubeCommentsResponse
{
    public string status;
    public string video_id;
    public string[] comments;
    public string reply_text;
    public string reply_audio_base64;
    public int duration_ms;
}

[Serializable]
public class VoiceResponse
{
    public string status;
    public string message;
    public string audio_path;
    public string audio_base64;
    public int duration_ms;
}

[Serializable]
public class VoiceRequestPayload
{
    public string userId;
    public string audio_base64;
    public string format = "wav";
    public bool return_base64 = true;
}

public class ChatFeatureController : MonoBehaviour
{
    [Header("Server")]
    public string serverUrl = "http://localhost:8000";

    [Header("UI")]
    public TMP_InputField textInputField;
    public TMP_InputField youtubeVideoIdField;
    public TMP_Text statusText;
    public WorldCommentUI worldCommentUI;

    [Header("Voice")]
    public AudioSource audioSource;
    public string voiceUserName = "Voice";
    public int recordingSeconds = 6;
    public int recordingSampleRate = 16000;
    public string microphoneDevice;

    private AudioClip recordingClip;

    public void SendTextInput()
    {
        if (textInputField == null)
        {
            Debug.LogWarning("TextInputField is not assigned.");
            return;
        }

        string message = textInputField.text?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            UpdateStatus("テキストを入力してください。");
            return;
        }

        UpdateStatus("文字送信中...");
        FindObjectOfType<AITuberController>()?.SendChat(message);
    }

    public void FetchYouTubeComments()
    {
        if (youtubeVideoIdField == null)
        {
            Debug.LogWarning("YouTubeVideoIdField is not assigned.");
            return;
        }

        string videoId = youtubeVideoIdField.text?.Trim();
        if (string.IsNullOrEmpty(videoId))
        {
            UpdateStatus("YouTube動画IDを入力してください。");
            return;
        }

        StartCoroutine(FetchYouTubeCommentsCoroutine(videoId, 8));
    }

    public void StartVoiceRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            UpdateStatus("マイクが見つかりません。");
            return;
        }

        string device = string.IsNullOrEmpty(microphoneDevice) ? Microphone.devices[0] : microphoneDevice;
        if (Microphone.IsRecording(device))
        {
            UpdateStatus("すでに録音中です。");
            return;
        }

        recordingClip = Microphone.Start(device, false, recordingSeconds, recordingSampleRate);
        if (recordingClip == null)
        {
            UpdateStatus("録音を開始できませんでした。");
            return;
        }

        UpdateStatus($"録音中: {recordingSeconds}秒...");
    }

    public void StopVoiceRecordingAndSend()
    {
        if (recordingClip == null)
        {
            UpdateStatus("録音が開始されていません。");
            return;
        }

        string device = string.IsNullOrEmpty(microphoneDevice) ? Microphone.devices[0] : microphoneDevice;
        int position = Microphone.GetPosition(device);
        Microphone.End(device);

        if (position <= 0)
        {
            UpdateStatus("録音が正しく終了しませんでした。");
            recordingClip = null;
            return;
        }

        float[] samples = new float[position * recordingClip.channels];
        recordingClip.GetData(samples, 0);

        AudioClip fullClip = AudioClip.Create("VoiceInput", position, recordingClip.channels, recordingClip.frequency, false);
        fullClip.SetData(samples, 0);

        recordingClip = null;

        byte[] wavBytes = WavUtility.GetWavBytes(fullClip);
        string base64 = Convert.ToBase64String(wavBytes);
        StartCoroutine(SendVoiceCoroutine(base64));
    }

    private IEnumerator FetchYouTubeCommentsCoroutine(string videoId, int maxResults)
    {
        var requestPayload = new
        {
            video_id = videoId,
            max_results = maxResults,
            generate_reply = true,
            return_base64 = true,
        };

        string json = JsonUtility.ToJson(requestPayload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest($"{serverUrl}/youtube-comments", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"YouTubeコメント取得に失敗しました: {request.error}");
            UpdateStatus("YouTubeコメントの取得に失敗しました。");
            yield break;
        }

        YouTubeCommentsResponse response;
        try
        {
            response = JsonUtility.FromJson<YouTubeCommentsResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"YouTubeコメントレスポンス解析エラー: {e.Message}");
            UpdateStatus("YouTubeコメントの解析に失敗しました。");
            yield break;
        }

        UpdateStatus($"{response.comments?.Length ?? 0} 件のコメントを取得しました。");
        if (worldCommentUI != null && response.comments != null)
        {
            foreach (string comment in response.comments)
            {
                worldCommentUI.AddComment("YT", comment);
            }
        }

        if (!string.IsNullOrEmpty(response.reply_text) && worldCommentUI != null)
        {
            worldCommentUI.AddComment("YTAI", response.reply_text);
        }

        if (!string.IsNullOrEmpty(response.reply_audio_base64) && audioSource != null)
        {
            try
            {
                byte[] replyAudio = Convert.FromBase64String(response.reply_audio_base64);
                AudioClip clip = WavUtility.ToAudioClip(replyAudio, "YouTubeReply");
                audioSource.clip = clip;
                audioSource.Play();
            }
            catch (Exception e)
            {
                Debug.LogError($"YouTube reply audio parse failed: {e.Message}");
            }
        }
    }

    private IEnumerator SendVoiceCoroutine(string base64Audio)
    {
        var payload = new VoiceRequestPayload
        {
            userId = "unity_voice",
            audio_base64 = base64Audio,
            format = "wav",
            return_base64 = true,
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest($"{serverUrl}/voice", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Voice request failed: {request.error}");
            UpdateStatus("音声送信に失敗しました。");
            yield break;
        }

        VoiceResponse response;
        try
        {
            response = JsonUtility.FromJson<VoiceResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Voice response parse failed: {e.Message}");
            UpdateStatus("音声応答の解析に失敗しました。");
            yield break;
        }

        UpdateStatus(response.message);
        if (worldCommentUI != null)
        {
            worldCommentUI.AddComment(voiceUserName, response.message);
        }

        if (!string.IsNullOrEmpty(response.audio_base64))
        {
            try
            {
                byte[] audioBytes = Convert.FromBase64String(response.audio_base64);
                AudioClip clip = WavUtility.ToAudioClip(audioBytes, "VoiceReply");
                if (audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Voice reply audio parse failed: {e.Message}");
            }
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
