using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class NoteUIController : MonoBehaviour
{
    private static NoteUIController instance;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject readerPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button closeButton;
    private TextMeshProUGUI hintText;
    private Note promptOwner;
    private Note openNote;
    private bool closeBound;

    // Runtime-created UI uses the same responsive baseline in every scene.
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    public static NoteUIController GetOrCreate()
    {
        if (instance != null) return instance;
        instance = FindObjectOfType<NoteUIController>();
        if (instance != null) return instance;
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return null;
        instance = canvas.gameObject.AddComponent<NoteUIController>();
        instance.BuildUI();
        return instance;
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        ConfigureCanvasScaler();
        if (Application.isPlaying) FindReferences();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        ConfigureCanvasScaler();
        FindReferences();
        ApplyResponsiveLayout();
        BindClose();
        if (promptPanel != null) promptPanel.SetActive(false);
        if (readerPanel != null) readerPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (instance == null) instance = this;
        if (!Application.isPlaying && readerPanel == null) BuildUI();
        else if (Application.isPlaying)
        {
            ConfigureCanvasScaler();
            FindReferences();
            ApplyResponsiveLayout();
            BindClose();
        }
    }

    private void OnRectTransformDimensionsChange() => ApplyResponsiveLayout();

    private void BuildUI()
    {
        ConfigureCanvasScaler();
        if (FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        promptPanel = Panel("Note Prompt", transform, new Color(0f, 0f, 0f, .65f));
        RectTransform promptRect = promptPanel.GetComponent<RectTransform>();
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(.5f, .5f);
        promptRect.pivot = new Vector2(.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 48f);
        promptRect.sizeDelta = new Vector2(430f, 46f);
        promptText = Text("Text", promptPanel.transform, 21, TextAlignmentOptions.Center, Color.white);
        EnableAutoSizing(promptText, 14f, 24f);
        Stretch(promptText.rectTransform, 10f, 5f, 10f, 5f);
        promptPanel.SetActive(false);

        readerPanel = Panel("Note Reader UI", transform, new Color(0f, 0f, 0f, .78f));
        Stretch(readerPanel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        GameObject paper = Panel("Old Paper", readerPanel.transform, new Color(.9f, .82f, .62f, 1f));
        RectTransform paperRect = paper.GetComponent<RectTransform>();
        paperRect.anchorMin = paperRect.anchorMax = new Vector2(.5f, .5f);
        paperRect.sizeDelta = new Vector2(680f, 510f);
        Texture2D parchment = Resources.Load<Texture2D>("UI/OldPaperBookBackground");
        if (parchment != null) paper.GetComponent<Image>().sprite = Sprite.Create(parchment, new Rect(0f, 0f, parchment.width, parchment.height), new Vector2(.5f, .5f));

        titleText = Text("Title", paper.transform, 28, TextAlignmentOptions.Center, new Color(.2f, .09f, .03f));
        EnableAutoSizing(titleText, 18f, 32f);
        titleText.fontStyle = FontStyles.Bold;
        RectTransform title = titleText.rectTransform;
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(.5f, 1f);
        title.anchoredPosition = new Vector2(0f, -40f);
        title.sizeDelta = new Vector2(-120f, 38f);

        contentText = Text("Content", paper.transform, 20, TextAlignmentOptions.TopLeft, new Color(.14f, .06f, .02f));
        EnableAutoSizing(contentText, 14f, 23f);
        contentText.enableWordWrapping = true;
        Stretch(contentText.rectTransform, 90f, 95f, 90f, 100f);

        closeButton = Button("Close Note", paper.transform, "Close", new Vector2(0f, -205f));
        if (Application.isPlaying) BindClose();
        hintText = Text("Hint", paper.transform, 14, TextAlignmentOptions.Center, new Color(.27f, .14f, .06f));
        EnableAutoSizing(hintText, 11f, 16f);
        hintText.text = "Press F to Close.";
        RectTransform hint = hintText.rectTransform;
        hint.anchorMin = new Vector2(0f, 0f);
        hint.anchorMax = new Vector2(1f, 0f);
        hint.pivot = new Vector2(.5f, 0f);
        hint.anchoredPosition = new Vector2(0f, 18f);
        hint.sizeDelta = new Vector2(-80f, 23f);
        readerPanel.SetActive(false);
        ApplyResponsiveLayout();
    }

    public void ShowPrompt(Note owner, string message)
    {
        if (readerPanel == null) BuildUI();
        if (readerPanel.activeSelf) return;
        promptOwner = owner;
        promptText.text = message;
        promptPanel.SetActive(true);
    }

    public void HidePrompt(Note owner)
    {
        if (promptOwner != owner) return;
        promptOwner = null;
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    public void Open(Note note)
    {
        if (readerPanel == null) BuildUI();
        openNote = note;
        HidePrompt(note);
        titleText.text = note.noteTitle;
        contentText.text = note.noteContent;
        ApplyResponsiveLayout();
        readerPanel.SetActive(true);
    }

    public void Close(Note note)
    {
        if (openNote != note) return;
        openNote = null;
        if (readerPanel != null) readerPanel.SetActive(false);
    }

    private void FindReferences()
    {
        if (promptPanel == null) { Transform t = transform.Find("Note Prompt"); if (t != null) promptPanel = t.gameObject; }
        if (readerPanel == null) { Transform t = transform.Find("Note Reader UI"); if (t != null) readerPanel = t.gameObject; }
        if (readerPanel == null) return;
        Transform paper = readerPanel.transform.Find("Old Paper");
        if (paper == null) return;
        if (closeButton == null) { Transform t = paper.Find("Close Note"); if (t != null) closeButton = t.GetComponent<Button>(); }
        if (titleText == null) { Transform t = paper.Find("Title"); if (t != null) titleText = t.GetComponent<TextMeshProUGUI>(); }
        if (contentText == null) { Transform t = paper.Find("Content"); if (t != null) contentText = t.GetComponent<TextMeshProUGUI>(); }
        if (hintText == null) { Transform t = paper.Find("Hint"); if (t != null) hintText = t.GetComponent<TextMeshProUGUI>(); }
        if (hintText != null) hintText.text = "Press F to Close.";
        ApplyResponsiveLayout();
    }

    private void ConfigureCanvasScaler()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ApplyResponsiveLayout()
    {
        if (promptPanel != null)
        {
            RectTransform prompt = promptPanel.GetComponent<RectTransform>();
            if (prompt != null) prompt.sizeDelta = new Vector2(ClampToCanvasWidth(430f, .72f), 46f);
        }
        if (readerPanel == null) return;
        Transform paper = readerPanel.transform.Find("Old Paper");
        if (paper == null) return;
        RectTransform paperRect = paper.GetComponent<RectTransform>();
        if (paperRect == null) return;
        float width = ClampToCanvasWidth(680f, .76f);
        paperRect.sizeDelta = new Vector2(width, Mathf.Min(510f, width * .75f));
    }

    private float ClampToCanvasWidth(float preferredWidth, float screenFraction)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.pixelRect.width <= 0f) return preferredWidth;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        float scale = scaler != null && scaler.scaleFactor > 0f ? scaler.scaleFactor : 1f;
        float available = canvas.pixelRect.width / scale * screenFraction;
        return Mathf.Clamp(Mathf.Min(preferredWidth, available), 320f, preferredWidth);
    }

    private void BindClose()
    {
        if (closeBound || closeButton == null) return;
        closeButton.onClick.AddListener(() => { if (openNote != null) openNote.CloseNote(); });
        closeBound = true;
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        item.GetComponent<Image>().color = color;
        return item;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, float size, TextAlignmentOptions align, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        return text;
    }

    private static void EnableAutoSizing(TextMeshProUGUI text, float minSize, float maxSize)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
    }

    private static Button Button(string name, Transform parent, string label, Vector2 pos)
    {
        GameObject item = Panel(name, parent, new Color(.25f, .11f, .035f, .9f));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(120f, 44f);
        rect.anchoredPosition = pos;
        Button button = item.AddComponent<Button>();
        TextMeshProUGUI text = Text("Label", item.transform, 17, TextAlignmentOptions.Center, new Color(1f, .91f, .70f));
        EnableAutoSizing(text, 12f, 18f);
        text.text = label;
        Stretch(text.rectTransform, 4f, 2f, 4f, 2f);
        return button;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}