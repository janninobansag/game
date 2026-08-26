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
    private void Awake() { if (instance == null) instance = this; if (Application.isPlaying) FindReferences(); }
    private void Start() { if (!Application.isPlaying) return; FindReferences(); BindClose(); if (promptPanel != null) promptPanel.SetActive(false); if (readerPanel != null) readerPanel.SetActive(false); }
    private void OnEnable()
    {
        if (instance == null) instance = this;
        if (!Application.isPlaying && readerPanel == null) BuildUI();
        else if (Application.isPlaying) { FindReferences(); BindClose(); }
    }

    private void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        promptPanel = Panel("Note Prompt", transform, new Color(0f, 0f, 0f, .65f));
        RectTransform p = promptPanel.GetComponent<RectTransform>(); p.anchorMin = p.anchorMax = new Vector2(.5f, .5f); p.pivot = new Vector2(.5f, 0f); p.anchoredPosition = new Vector2(0f, 48f); p.sizeDelta = new Vector2(430f, 46f);
        promptText = Text("Text", promptPanel.transform, 21, TextAlignmentOptions.Center, Color.white); Stretch(promptText.rectTransform, 10f, 5f, 10f, 5f); promptPanel.SetActive(false);
        readerPanel = Panel("Note Reader UI", transform, new Color(0f, 0f, 0f, .78f)); Stretch(readerPanel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        GameObject paper = Panel("Old Paper", readerPanel.transform, new Color(.9f, .82f, .62f, 1f)); RectTransform paperRect = paper.GetComponent<RectTransform>(); paperRect.anchorMin = paperRect.anchorMax = new Vector2(.5f, .5f); paperRect.sizeDelta = new Vector2(680f, 510f);
        Texture2D parchment = Resources.Load<Texture2D>("UI/OldPaperBookBackground"); if (parchment != null) paper.GetComponent<Image>().sprite = Sprite.Create(parchment, new Rect(0f, 0f, parchment.width, parchment.height), new Vector2(.5f, .5f));
        titleText = Text("Title", paper.transform, 28, TextAlignmentOptions.Center, new Color(.2f, .09f, .03f)); titleText.fontStyle = FontStyles.Bold; RectTransform title = titleText.rectTransform; title.anchorMin = new Vector2(0f, 1f); title.anchorMax = new Vector2(1f, 1f); title.pivot = new Vector2(.5f, 1f); title.anchoredPosition = new Vector2(0f, -40f); title.sizeDelta = new Vector2(-120f, 38f);
        contentText = Text("Content", paper.transform, 20, TextAlignmentOptions.TopLeft, new Color(.14f, .06f, .02f)); contentText.enableWordWrapping = true; Stretch(contentText.rectTransform, 90f, 95f, 90f, 100f);
        closeButton = Button("Close Note", paper.transform, "Close", new Vector2(0f, -205f)); if (Application.isPlaying) BindClose();
        hintText = Text("Hint", paper.transform, 14, TextAlignmentOptions.Center, new Color(.27f, .14f, .06f)); hintText.text = "Press F to Close."; RectTransform h = hintText.rectTransform; h.anchorMin = new Vector2(0f, 0f); h.anchorMax = new Vector2(1f, 0f); h.pivot = new Vector2(.5f, 0f); h.anchoredPosition = new Vector2(0f, 18f); h.sizeDelta = new Vector2(-80f, 23f);
        readerPanel.SetActive(false);
    }

    public void ShowPrompt(Note owner, string message) { if (readerPanel == null) BuildUI(); if (readerPanel.activeSelf) return; promptOwner = owner; promptText.text = message; promptPanel.SetActive(true); }
    public void HidePrompt(Note owner) { if (promptOwner != owner) return; promptOwner = null; if (promptPanel != null) promptPanel.SetActive(false); }
    public void Open(Note note) { if (readerPanel == null) BuildUI(); openNote = note; HidePrompt(note); titleText.text = note.noteTitle; contentText.text = note.noteContent; readerPanel.SetActive(true); }
    public void Close(Note note) { if (openNote != note) return; openNote = null; if (readerPanel != null) readerPanel.SetActive(false); }
    private void FindReferences()
    {
        if (promptPanel == null) { Transform t = transform.Find("Note Prompt"); if (t != null) promptPanel = t.gameObject; }
        if (readerPanel == null) { Transform t = transform.Find("Note Reader UI"); if (t != null) readerPanel = t.gameObject; }
        if (readerPanel == null) return;
        Transform paper = readerPanel.transform.Find("Old Paper"); if (paper == null) return;
        if (closeButton == null) { Transform t = paper.Find("Close Note"); if (t != null) closeButton = t.GetComponent<Button>(); }
        if (titleText == null) { Transform t = paper.Find("Title"); if (t != null) titleText = t.GetComponent<TextMeshProUGUI>(); }
        if (contentText == null) { Transform t = paper.Find("Content"); if (t != null) contentText = t.GetComponent<TextMeshProUGUI>(); }
        if (hintText == null) { Transform t = paper.Find("Hint"); if (t != null) hintText = t.GetComponent<TextMeshProUGUI>(); }
        if (hintText != null) hintText.text = "Press F to Close.";
    }
    private void BindClose() { if (closeBound || closeButton == null) return; closeButton.onClick.AddListener(() => { if (openNote != null) openNote.CloseNote(); }); closeBound = true; }
    private static GameObject Panel(string name, Transform parent, Color color) { GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image)); item.transform.SetParent(parent, false); item.GetComponent<Image>().color = color; return item; }
    private static TextMeshProUGUI Text(string name, Transform parent, float size, TextAlignmentOptions align, Color color) { GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); item.transform.SetParent(parent, false); TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.fontSize = size; text.alignment = align; text.color = color; return text; }
    private static Button Button(string name, Transform parent, string label, Vector2 pos) { GameObject item = Panel(name, parent, new Color(.25f, .11f, .035f, .9f)); RectTransform r = item.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(120f, 44f); r.anchoredPosition = pos; Button b = item.AddComponent<Button>(); TextMeshProUGUI t = Text("Label", item.transform, 17, TextAlignmentOptions.Center, new Color(1f, .91f, .70f)); t.text = label; Stretch(t.rectTransform, 4f, 2f, 4f, 2f); return b; }
    private static void Stretch(RectTransform r, float l, float b, float right, float top) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-right, -top); }
}
