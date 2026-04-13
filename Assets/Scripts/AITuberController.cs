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
    public string emotion;
}

public class AITuberController : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:8000";

    [Header("UI")]
    public TMP_Text responseText;
    public WorldCommentUI worldCommentUI;
    public CharacterSettingsController characterSettingsController;
    public string commentUserName = "AI";
    public bool showChatComments = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public bool requestBase64Audio = true;

    [Header("Face")]
    public SkinnedMeshRenderer faceRenderer;
    public int happyBlendShapeIndex = 0;
    public int sadBlendShapeIndex = 1;
    public int angryBlendShapeIndex = 2;
    public int surprisedBlendShapeIndex = 3;

    [Header("Animator")]
    public Animator characterAnimator;
    public string happyTrigger = "Happy";
    public string sadTrigger = "Sad";
    public string angryTrigger = "Angry";
    public string surprisedTrigger = "Surprised";
    public string cryTrigger = "Cry";

    public bool IsThinking { get; private set; }

    public string currentEmotion = "neutral";
    public string lastInteraction = "";
    public int tapCount;
    public int petHeadCount;
    public int petBellyCount;
    public int punchCount;
    private Coroutine emotionResetRoutine;

    public void SendChat(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("Prompt is empty. Skipping chat request.");
            return;
        }

        if (IsThinking)
        {
            Debug.LogWarning("Already waiting for a response. Please wait before sending another request.");
            return;
        }

        StartCoroutine(PostChatCoroutine(prompt));
    }

    public void ReactToInteraction(string interactionType, string region = "body")
    {
        if (worldCommentUI == null)
        {
            Debug.LogWarning("WorldCommentUI is not assigned. Cannot display interaction response.");
            return;
        }

        string responseText = null;
        interactionType = (interactionType ?? "").Trim().ToLowerInvariant();
        region = (region ?? "body").Trim().ToLowerInvariant();

        switch (interactionType)
        {
            case "tap":
                tapCount++;
                lastInteraction = region == "head" ? "頭を軽くタップ" : "体をタップ";
                if (tapCount > 5)
                {
                    responseText = "ちょっと触りすぎだよ…やさしくして。";
                    ApplyEmotion("angry");
                }
                else if (region == "head")
                {
                    responseText = "その頭、くすぐったいよ…でも悪くないかも。";
                    ApplyEmotion("surprised");
                }
                else
                {
                    responseText = "触られたよ。もっと優しくしてね。";
                    ApplyEmotion("happy");
                }
                break;
            case "pet":
                if (region == "head")
                {
                    petHeadCount++;
                    lastInteraction = "頭をなでられた";
                    responseText = "頭いい子だね。そんなにやさしくしてくれるの、うれしい。";
                    ApplyEmotion("happy");
                }
                else if (region == "belly")
                {
                    petBellyCount++;
                    lastInteraction = "お腹をなでられた";
                    responseText = "そこはちょっと恥ずかしいけど…ふふっ。";
                    ApplyEmotion("surprised");
                }
                else
                {
                    lastInteraction = "なでなでされた";
                    responseText = "なでなで、気持ちいいかも。";
                    ApplyEmotion("happy");
                }
                break;
            case "punch":
                punchCount++;
                lastInteraction = "げんこつされた";
                if (punchCount >= 2)
                {
                    responseText = "痛いよ…もう泣いちゃいそう…";
                    ApplyEmotion("sad");
                }
                else
                {
                    responseText = "そんなに強くしないでよ！";
                    ApplyEmotion("angry");
                }
                break;
            default:
                lastInteraction = "不明な反応";
                responseText = "反応したよ。";
                ApplyEmotion("surprised");
                break;
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            worldCommentUI.AddComment(commentUserName, responseText);
        }
    }

    private IEnumerator PostChatCoroutine(string prompt)
    {
        IsThinking = true;
        try
        {
            var requestPayload = new ChatRequest
            {
                userId = characterSettingsController != null && !string.IsNullOrWhiteSpace(characterSettingsController.CurrentSettings.characterName)
                    ? characterSettingsController.CurrentSettings.characterName
                    : "unity",
                prompt = prompt,
                context = characterSettingsController != null ? characterSettingsController.BuildCharacterContext() : null,
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

            if (showChatComments && worldCommentUI != null && !string.IsNullOrWhiteSpace(response.text))
            {
                worldCommentUI.AddComment(commentUserName, response.text);
            }

            if (!string.IsNullOrWhiteSpace(response.emotion))
            {
                lastInteraction = "AIが返信";
                ApplyEmotion(response.emotion);
            }

            if (!string.IsNullOrEmpty(response.audio_base64))
            {
                LoadAudioFromBase64(response.audio_base64);
                yield break;
            }

            if (!string.IsNullOrEmpty(response.audio_path))
            {
                yield return StartCoroutine(LoadAudioFromFile(response.audio_path));
                yield break;
            }

            Debug.LogWarning("No audio source was returned from backend.");
        }
        finally
        {
            IsThinking = false;
        }
    }

    private void LoadAudioFromBase64(string base64Audio)
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

    private void ApplyEmotion(string emotion)
    {
        if (string.IsNullOrEmpty(emotion) || faceRenderer == null)
        {
            return;
        }

        emotion = emotion.Trim().ToLowerInvariant();
        currentEmotion = emotion;
        ResetBlendShapes();
        SetAnimatorTriggerForEmotion(emotion);

        switch (emotion)
        {
            case "happy":
            case "joy":
                SetBlendShape(happyBlendShapeIndex, 100);
                StartCoroutine(ResetEmotionAfterDelay(2.0f));
                break;
            case "sad":
            case "sorrow":
                SetBlendShape(sadBlendShapeIndex, 100);
                StartCoroutine(ResetEmotionAfterDelay(3.0f));
                break;
            case "angry":
            case "anger":
                SetBlendShape(angryBlendShapeIndex, 100);
                StartCoroutine(ResetEmotionAfterDelay(2.5f));
                break;
            case "surprised":
            case "surprise":
                SetBlendShape(surprisedBlendShapeIndex, 100);
                StartCoroutine(ResetEmotionAfterDelay(2.0f));
                break;
            default:
                StartCoroutine(ResetEmotionAfterDelay(1.5f));
                break;
        }
    }

    private void ResetBlendShapes()
    {
        if (faceRenderer == null)
        {
            return;
        }

        faceRenderer.SetBlendShapeWeight(happyBlendShapeIndex, 0f);
        faceRenderer.SetBlendShapeWeight(sadBlendShapeIndex, 0f);
        faceRenderer.SetBlendShapeWeight(angryBlendShapeIndex, 0f);
        faceRenderer.SetBlendShapeWeight(surprisedBlendShapeIndex, 0f);
    }

    private void SetAnimatorTriggerForEmotion(string emotion)
    {
        if (characterAnimator == null)
        {
            return;
        }

        switch (emotion)
        {
            case "happy":
            case "joy":
                characterAnimator.SetTrigger(happyTrigger);
                break;
            case "sad":
            case "sorrow":
                characterAnimator.SetTrigger(sadTrigger);
                break;
            case "angry":
            case "anger":
                characterAnimator.SetTrigger(angryTrigger);
                break;
            case "surprised":
            case "surprise":
                characterAnimator.SetTrigger(surprisedTrigger);
                break;
            default:
                break;
        }
    }

    private IEnumerator ResetEmotionAfterDelay(float delay)
    {
        if (emotionResetRoutine != null)
        {
            StopCoroutine(emotionResetRoutine);
        }

        emotionResetRoutine = ResetEmotionCoroutine(delay);
        yield return emotionResetRoutine;
    }

    private IEnumerator ResetEmotionCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetBlendShapes();
        currentEmotion = "neutral";
    }

    private void SetBlendShape(int index, float weight)
    {
        if (faceRenderer == null)
        {
            return;
        }

        if (index < 0 || index >= faceRenderer.sharedMesh.blendShapeCount)
        {
            Debug.LogWarning($"BlendShape index {index} is out of range.");
            return;
        }

        faceRenderer.SetBlendShapeWeight(index, weight);
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

    public static byte[] GetWavBytes(AudioClip clip)
    {
        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip));
        }

        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int sampleCount = clip.samples * channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        byte[] wav = new byte[44 + sampleCount * 2];
        System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("RIFF"), 0, wav, 0, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(wav.Length - 8), 0, wav, 4, 4);
        System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("WAVE"), 0, wav, 8, 4);
        System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("fmt "), 0, wav, 12, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(16), 0, wav, 16, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, wav, 20, 2);
        System.Buffer.BlockCopy(BitConverter.GetBytes((short)channels), 0, wav, 22, 2);
        System.Buffer.BlockCopy(BitConverter.GetBytes(sampleRate), 0, wav, 24, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(sampleRate * channels * 2), 0, wav, 28, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes((short)(channels * 2)), 0, wav, 32, 2);
        System.Buffer.BlockCopy(BitConverter.GetBytes((short)16), 0, wav, 34, 2);
        System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("data"), 0, wav, 36, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(sampleCount * 2), 0, wav, 40, 4);

        int offset = 44;
        for (int i = 0; i < sampleCount; i++)
        {
            short intData = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            byte[] byteData = BitConverter.GetBytes(intData);
            wav[offset++] = byteData[0];
            wav[offset++] = byteData[1];
        }

        return wav;
    }
}
