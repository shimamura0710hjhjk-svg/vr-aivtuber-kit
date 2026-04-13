using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class WorldCommentUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("コメントアイテムを並べる親。World Space Canvas 内のコンテント領域を指定してください。")]
    public RectTransform contentRoot;

    [Tooltip("スクロールが必要な場合は ScrollRect をアサインしてください。")]
    public ScrollRect scrollRect;

    [Tooltip("コメントの見た目をカスタムしたい場合に使います。未割り当てでも、ランタイムで簡易表示を生成します。")]
    public GameObject commentPrefab;

    [Header("Comment Behavior")]
    public int maxCommentCount = 8;
    public float commentDisplaySeconds = 16f;
    public bool autoScrollToBottom = true;

    [Header("Animation")]
    public float fadeInDuration = 0.32f;
    public float floatAmplitude = 6f;
    public float floatFrequency = 1.2f;
    public float arrivalScale = 1.08f;

    [Header("Fallback Style")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.25f);
    public Color textColor = Color.white;
    public int fontSize = 28;
    public Vector2 itemPadding = new Vector2(14f, 12f);
    public Vector2 itemMinSize = new Vector2(320f, 70f);

    private readonly List<GameObject> activeComments = new List<GameObject>();
    private readonly Dictionary<GameObject, Coroutine> floatCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        if (contentRoot == null)
        {
            Debug.LogError("WorldCommentUI: contentRoot が設定されていません。コメントを表示する RectTransform を割り当ててください。");
        }
    }

    public void AddComment(string message)
    {
        AddComment(null, message);
    }

    public void AddComment(string userName, string message)
    {
        if (contentRoot == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (activeComments.Count >= maxCommentCount)
        {
            RemoveOldestComment();
        }

        GameObject commentInstance = CreateCommentInstance();
        commentInstance.transform.SetParent(contentRoot, false);

        string formattedText = string.IsNullOrWhiteSpace(userName)
            ? message.Trim()
            : $"{userName}: {message.Trim()}";

        TextMeshProUGUI text = commentInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = formattedText;
        }

        CanvasGroup canvasGroup = commentInstance.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        commentInstance.transform.localScale = Vector3.one * arrivalScale;
        activeComments.Add(commentInstance);

        if (autoScrollToBottom && scrollRect != null)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }

        StartCoroutine(AnimateEntry(commentInstance));
        StartCoroutine(RemoveAfterSeconds(commentInstance, commentDisplaySeconds));
    }

    private GameObject CreateCommentInstance()
    {
        if (commentPrefab != null)
        {
            return Instantiate(commentPrefab);
        }

        GameObject root = new GameObject("CommentItem");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = itemMinSize;

        Image background = root.AddComponent<Image>();
        background.color = panelColor;

        CanvasGroup group = root.AddComponent<CanvasGroup>();

        GameObject textObject = new GameObject("CommentText");
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.SetParent(root.transform, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(itemPadding.x, itemPadding.y);
        textRect.offsetMax = new Vector2(-itemPadding.x, -itemPadding.y);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.color = textColor;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.text = "";

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.minHeight = itemMinSize.y;
        layout.minWidth = itemMinSize.x;
        layout.flexibleWidth = 1f;

        return root;
    }

    private IEnumerator AnimateEntry(GameObject commentInstance)
    {
        CanvasGroup group = commentInstance.GetComponent<CanvasGroup>();
        RectTransform rect = commentInstance.GetComponent<RectTransform>();
        float elapsed = 0f;

        Vector3 startScale = Vector3.one * arrivalScale;
        Vector3 endScale = Vector3.one;

        if (group != null)
        {
            group.alpha = 0f;
        }

        while (elapsed < fadeInDuration)
        {
            float t = elapsed / fadeInDuration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            if (group != null)
            {
                group.alpha = ease;
            }
            if (rect != null)
            {
                rect.localScale = Vector3.Lerp(startScale, endScale, ease);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (group != null)
        {
            group.alpha = 1f;
        }
        if (rect != null)
        {
            rect.localScale = endScale;
        }

        if (commentInstance != null)
        {
            floatCoroutines[commentInstance] = StartCoroutine(FloatLoop(commentInstance));
        }
    }

    private IEnumerator FloatLoop(GameObject commentInstance)
    {
        RectTransform rect = commentInstance.GetComponent<RectTransform>();
        if (rect == null)
        {
            yield break;
        }

        Vector3 startPosition = rect.localPosition;
        float timer = 0f;

        while (commentInstance != null)
        {
            timer += Time.deltaTime * floatFrequency;
            float offset = Mathf.Sin(timer) * floatAmplitude;
            rect.localPosition = startPosition + new Vector3(0f, offset, 0f);
            yield return null;
        }
    }

    private IEnumerator RemoveAfterSeconds(GameObject commentInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (commentInstance != null)
        {
            RemoveComment(commentInstance);
        }
    }

    private void RemoveComment(GameObject commentInstance)
    {
        if (activeComments.Contains(commentInstance))
        {
            activeComments.Remove(commentInstance);
        }

        if (floatCoroutines.TryGetValue(commentInstance, out Coroutine coroutine))
        {
            StopCoroutine(coroutine);
            floatCoroutines.Remove(commentInstance);
        }

        Destroy(commentInstance);
    }

    private void RemoveOldestComment()
    {
        if (activeComments.Count == 0)
        {
            return;
        }

        GameObject oldest = activeComments[0];
        RemoveComment(oldest);
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ClearAllComments()
    {
        for (int i = activeComments.Count - 1; i >= 0; i--)
        {
            if (activeComments[i] != null)
            {
                Destroy(activeComments[i]);
            }
        }
        activeComments.Clear();

        foreach (Coroutine coroutine in floatCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        floatCoroutines.Clear();
    }
}
