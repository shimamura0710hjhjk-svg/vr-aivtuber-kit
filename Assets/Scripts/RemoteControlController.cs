using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RemoteCommand
{
    public int id;
    public string type;
    public string prompt;
    public string @event;
    public string region;
    public float timestamp;
}

[Serializable]
public class RemoteCommandList
{
    public RemoteCommand[] commands;
}

public class RemoteControlController : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:8000";
    public AITuberController aituberController;
    public float pollInterval = 1.0f;

    private int lastCommandId = 0;

    private void Start()
    {
        if (aituberController == null)
        {
            Debug.LogWarning("AITuberController is not assigned on RemoteControlController.");
        }

        StartCoroutine(PollRemoteCommandsCoroutine());
    }

    private IEnumerator PollRemoteCommandsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(pollInterval);
            string url = $"{serverUrl}/remote/commands?last_id={lastCommandId}";
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"Remote command poll failed: {request.error}");
                continue;
            }

            if (string.IsNullOrEmpty(request.downloadHandler.text))
            {
                continue;
            }

            RemoteCommand[] commands = JsonHelper.FromJson<RemoteCommand>(request.downloadHandler.text);
            if (commands == null || commands.Length == 0)
            {
                continue;
            }

            foreach (var command in commands)
            {
                ProcessRemoteCommand(command);
                lastCommandId = Math.Max(lastCommandId, command.id);
            }
        }
    }

    private void ProcessRemoteCommand(RemoteCommand command)
    {
        if (aituberController == null)
        {
            Debug.LogWarning("Cannot process remote command, AITuberController is not assigned.");
            return;
        }

        switch (command.type)
        {
            case "chat":
                if (!string.IsNullOrEmpty(command.prompt))
                {
                    aituberController.SendChat(command.prompt);
                }
                break;
            case "interaction":
                aituberController.ReactToInteraction(command.@event ?? "tap", command.region ?? "body");
                break;
            default:
                Debug.LogWarning($"Unknown remote command type: {command.type}");
                break;
        }
    }
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string wrapped = $"{{\"array\":{json}}}";
        var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return wrapper?.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
