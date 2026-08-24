using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Creates the reusable normal Unity Canvas UI used by every readable book.</summary>
[ExecuteAlways]
public class BookUIController : MonoBehaviour
{
    private static BookUIController instance;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject readerPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private TextMeshProUGUI pageNumberText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    private Book promptOwner;
    private Book openBook;
    private bool buttonsBound;

    public static BookUIController GetOrCreate()
    {
        if (instance != null) return instance;
        instance = FindObjectOfType<BookUIController>();
        if (instance != null) return instance;
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("Book UI needs a Canvas in the scene."); return null; }
        GameObject controller = new GameObject("Book UI Controller", typeof(BookUIController));
        controller.transform.SetParent(canvas.transform, false);
        instance = controller.GetComponent<BookUIController>();
        instance.BuildUI();
        return instance;
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        if (Application.isPlaying) FindExistingReferences();
    }
    private void Start()
    {
        if (!Application.isPlaying) return;
        FindExistingReferences();
        BindButtons();
        if (promptPanel != null) promptPanel.SetActive(false);
        if (readerPanel != null) readerPanel.SetActive(false);
    }
    private void OnEnable()
    {
        if (instance == null) instance = this;
        if (!Application.isPlaying && readerPanel == null)
            BuildUI();
        else if (Application.isPlaying)
        {
            FindExistingReferences();
            BindButtons();
        }
    }

    private void BuildUI()
    {
        EnsureEventSystem();
        promptPanel = CreatePanel("Book Prompt", transform, new Color(0f, 0f, 0f, 0.65f));
        RectTransform promptRect = promptPanel.GetComponent<RectTransform>();
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f); promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 48f); promptRect.sizeDelta = new Vector2(430f, 46f);
        promptText = CreateText("Text", promptPanel.transform, 21, TextAlignmentOptions.Center, Color.white);
        Stretch(promptText.rectTransform, 10f, 5f, 10f, 5f); promptPanel.SetActive(false);

        readerPanel = CreatePanel("Book Reader UI", transform, new Color(0f, 0f, 0f, 0.78f));
        Stretch(readerPanel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        GameObject paper = CreatePanel("Old Paper", readerPanel.transform, new Color(0.9f, 0.82f, 0.62f, 1f));
        RectTransform paperRect = paper.GetComponent<RectTransform>();
        paperRect.anchorMin = paperRect.anchorMax = new Vector2(0.5f, 0.5f); paperRect.sizeDelta = new Vector2(760f, 570f);
        Image paperImage = paper.GetComponent<Image>();
        Texture2D parchment = Resources.Load<Texture2D>("UI/OldPaperBookBackground");
        if (parchment != null) paperImage.sprite = Sprite.Create(parchment, new Rect(0f, 0f, parchment.width, parchment.height), new Vector2(0.5f, 0.5f));

        titleText = CreateText("Page Title", paper.transform, 29, TextAlignmentOptions.Center, new Color(0.20f, 0.09f, 0.03f)); titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleText.rectTransform; titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = new Vector2(1f, 1f); titleRect.pivot = new Vector2(0.5f, 1f); titleRect.anchoredPosition = new Vector2(0f, -42f); titleRect.sizeDelta = new Vector2(-130f, 42f);
        contentText = CreateText("Page Content", paper.transform, 21, TextAlignmentOptions.TopLeft, new Color(0.14f, 0.06f, 0.02f)); contentText.enableWordWrapping = true; contentText.overflowMode = TextOverflowModes.Overflow;
        Stretch(contentText.rectTransform, 105f, 108f, 105f, 105f);
        pageNumberText = CreateText("Page Number", paper.transform, 15, TextAlignmentOptions.Center, new Color(0.27f, 0.14f, 0.06f));
        RectTransform pageRect = pageNumberText.rectTransform; pageRect.anchorMin = new Vector2(0f, 0f); pageRect.anchorMax = new Vector2(1f, 0f); pageRect.pivot = new Vector2(0.5f, 0f); pageRect.anchoredPosition = new Vector2(0f, 50f); pageRect.sizeDelta = new Vector2(-210f, 25f);

        previousButton = CreateButton("Previous Page", paper.transform, "◀  Back", new Vector2(-260f, -225f));
        nextButton = CreateButton("Next Page", paper.transform, "Next  ▶", new Vector2(260f, -225f));
        closeButton = CreateButton("Close Book", paper.transform, "Close", new Vector2(0f, -245f)); closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(115f, 40f);
        if (Application.isPlaying) BindButtons();
        TextMeshProUGUI hint = CreateText("Controls Hint", paper.transform, 14, TextAlignmentOptions.Center, new Color(0.27f, 0.14f, 0.06f)); hint.text = "Click the buttons or press E / Q.  ESC or F closes the book.";
        RectTransform hintRect = hint.rectTransform; hintRect.anchorMin = new Vector2(0f, 0f); hintRect.anchorMax = new Vector2(1f, 0f); hintRect.pivot = new Vector2(0.5f, 0f); hintRect.anchoredPosition = new Vector2(0f, 18f); hintRect.sizeDelta = new Vector2(-80f, 23f);
        readerPanel.SetActive(false);
    }

    public void ShowPrompt(Book owner, string message)
    {
        if (readerPanel == null) BuildUI();
        if (readerPanel != null && readerPanel.activeSelf) return;
        promptOwner = owner; promptText.text = message; promptPanel.SetActive(true);
    }
    public void HidePrompt(Book owner) { if (promptOwner != owner) return; promptOwner = null; if (promptPanel != null) promptPanel.SetActive(false); }
    public void Open(Book book) { if (readerPanel == null) BuildUI(); openBook = book; HidePrompt(book); readerPanel.SetActive(true); Refresh(book); }
    public void Close(Book book) { if (openBook != book) return; openBook = null; if (readerPanel != null) readerPanel.SetActive(false); }
    public void Refresh(Book book)
    {
        if (book == null || book != openBook || book.PageCount == 0) return;
        titleText.text = book.CurrentPageTitle; contentText.text = book.CurrentPageText; pageNumberText.text = $"Page {book.CurrentPage + 1} of {book.PageCount}";
        previousButton.gameObject.SetActive(book.CurrentPage > 0); nextButton.gameObject.SetActive(book.CurrentPage < book.PageCount - 1);
    }

    private void FindExistingReferences()
    {
        if (readerPanel == null)
        {
            Transform reader = transform.Find("Book Reader UI");
            if (reader != null) readerPanel = reader.gameObject;
        }
        if (promptPanel == null)
        {
            Transform prompt = transform.Find("Book Prompt");
            if (prompt != null) promptPanel = prompt.gameObject;
        }
        if (readerPanel == null) return;

        Transform paper = readerPanel.transform.Find("Old Paper");
        if (paper == null) return;
        if (previousButton == null) previousButton = FindButton(paper, "Previous Page");
        if (nextButton == null) nextButton = FindButton(paper, "Next Page");
        if (closeButton == null) closeButton = FindButton(paper, "Close Book");
    }

    private static Button FindButton(Transform parent, string name)
    {
        Transform item = parent.Find(name);
        return item == null ? null : item.GetComponent<Button>();
    }

    private void BindButtons()
    {
        if (buttonsBound || previousButton == null || nextButton == null || closeButton == null) return;
        previousButton.onClick.AddListener(() => { if (openBook != null) openBook.PreviousPage(); });
        nextButton.onClick.AddListener(() => { if (openBook != null) openBook.NextPage(); });
        closeButton.onClick.AddListener(() => { if (openBook != null) openBook.CloseBook(); });
        buttonsBound = true;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false); panel.GetComponent<Image>().color = color; return panel;
    }
    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); item.transform.SetParent(parent, false); TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.fontSize = size; text.alignment = alignment; text.color = color; return text;
    }
    private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        GameObject item = CreatePanel(name, parent, new Color(0.25f, 0.11f, 0.035f, 0.90f)); RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(145f, 46f); rect.anchoredPosition = position;
        Button button = item.AddComponent<Button>(); ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 0.86f, 0.58f, 1f); colors.pressedColor = new Color(0.75f, 0.58f, 0.30f, 1f); button.colors = colors;
        TextMeshProUGUI text = CreateText("Label", item.transform, 17, TextAlignmentOptions.Center, new Color(1f, 0.91f, 0.70f)); text.text = label; Stretch(text.rectTransform, 4f, 2f, 4f, 2f); return button;
    }
    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); }
    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
