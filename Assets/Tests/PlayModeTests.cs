using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

public class PlayModeTests
{
    [UnityTest]
    public IEnumerator WorldCommentUI_AddComment_CreatesCommentItem()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas));
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(canvasGO.transform, false);

        var contentRoot = contentGO.GetComponent<RectTransform>();
        var worldCommentUI = canvasGO.AddComponent<WorldCommentUI>();
        worldCommentUI.contentRoot = contentRoot;
        worldCommentUI.maxCommentCount = 4;
        worldCommentUI.commentDisplaySeconds = 0.5f;
        worldCommentUI.autoScrollToBottom = false;
        worldCommentUI.fadeInDuration = 0.1f;
        worldCommentUI.floatAmplitude = 0f;

        worldCommentUI.AddComment("Tester", "Hello from PlayMode test.");
        yield return null;

        Assert.AreEqual(1, contentRoot.childCount, "コメントが1つ追加されているはずです。");

        GameObject.Destroy(canvasGO);
        yield return null;
    }

    [UnityTest]
    public IEnumerator WorldCommentUI_RemovesOldestComment_WhenMaxExceeded()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas));
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(canvasGO.transform, false);

        var contentRoot = contentGO.GetComponent<RectTransform>();
        var worldCommentUI = canvasGO.AddComponent<WorldCommentUI>();
        worldCommentUI.contentRoot = contentRoot;
        worldCommentUI.maxCommentCount = 2;
        worldCommentUI.commentDisplaySeconds = 0.5f;
        worldCommentUI.autoScrollToBottom = false;
        worldCommentUI.fadeInDuration = 0.1f;
        worldCommentUI.floatAmplitude = 0f;

        worldCommentUI.AddComment("User1", "First comment");
        yield return null;
        worldCommentUI.AddComment("User2", "Second comment");
        yield return null;
        worldCommentUI.AddComment("User3", "Third comment");
        yield return null;

        Assert.AreEqual(2, contentRoot.childCount, "最大コメント数を超えた場合、最も古いコメントが削除されるはずです。");
        Assert.IsTrue(contentRoot.GetChild(0).name.Contains("CommentItem") || contentRoot.GetChild(0).name.Contains("Comment"), "残っているコメントオブジェクトが正しい形式であるはずです。");

        GameObject.Destroy(canvasGO);
        yield return null;
    }

    [UnityTest]
    public IEnumerator WorldCommentUI_ClearAllComments_RemovesAllCreatedComments()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas));
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(canvasGO.transform, false);

        var contentRoot = contentGO.GetComponent<RectTransform>();
        var worldCommentUI = canvasGO.AddComponent<WorldCommentUI>();
        worldCommentUI.contentRoot = contentRoot;
        worldCommentUI.maxCommentCount = 5;
        worldCommentUI.commentDisplaySeconds = 0.5f;
        worldCommentUI.autoScrollToBottom = false;
        worldCommentUI.fadeInDuration = 0.1f;
        worldCommentUI.floatAmplitude = 0f;

        worldCommentUI.AddComment("A", "Comment A");
        yield return null;
        worldCommentUI.AddComment("B", "Comment B");
        yield return null;

        worldCommentUI.ClearAllComments();
        yield return null;

        Assert.AreEqual(0, contentRoot.childCount, "コメントがすべてクリアされているはずです。");

        GameObject.Destroy(canvasGO);
        yield return null;
    }

    [Test]
    public void WavUtility_GetWavBytes_ToAudioClip_RoundTrip_ReturnsSameClipProperties()
    {
        int sampleRate = 8000;
        int sampleLength = 256;
        var originalClip = AudioClip.Create("RoundTrip", sampleLength, 1, sampleRate, false);
        var samples = new float[sampleLength];
        for (int i = 0; i < sampleLength; i++)
        {
            samples[i] = Mathf.Sin(2 * Mathf.PI * i / sampleLength);
        }

        originalClip.SetData(samples, 0);
        byte[] wavBytes = WavUtility.GetWavBytes(originalClip);

        Assert.IsNotNull(wavBytes);
        Assert.Greater(wavBytes.Length, 44, "WAVデータはヘッダーより大きい必要があります。");

        var restoredClip = WavUtility.ToAudioClip(wavBytes, "Restored");
        Assert.IsNotNull(restoredClip);
        Assert.AreEqual(sampleRate, restoredClip.frequency, "サンプルレートが一致するはずです。");
        Assert.AreEqual(1, restoredClip.channels, "チャンネル数が一致するはずです。");
        Assert.AreEqual(sampleLength, restoredClip.samples, "サンプル数が一致するはずです。");
    }
}
