// PURPOSE: Creates, opens, localizes, and manages the About panel and its back button.
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public partial class MenuManager : MonoBehaviour
{
    public void OpenAbout()
    {
        PlayClickSound();
        CreateAboutPanelIfNeeded();

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(true);

            if (EventSystem.current != null && aboutBackButton != null)
                EventSystem.current.SetSelectedGameObject(aboutBackButton.gameObject);
        }
    }

    public void CloseAbout()
    {
        PlayClickSound();

        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == aboutBackButton.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (aboutPanel != null && aboutPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseAbout();
    }

    private void CaptureEditableAboutText()
    {
        if (aboutPanel == null || aboutEditableTextCaptured)
            return;

        foreach (TextMeshProUGUI label in aboutPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.gameObject.name == "Title") editableAboutTitle = label.text;
            else if (label.gameObject.name == "Content") editableAboutContent = label.text;
            else if (label.gameObject.name == "Label") editableAboutBackLabel = label.text;
        }

        aboutEditableTextCaptured = true;
    }
    public void RefreshAboutLocalization()
    {
        ApplyAboutPresentation();
    }

    private GameLanguage GetAboutLanguage()
    {
        return settingsPanel != null
            ? settingsPanel.GetSelectedLanguage()
            : (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt("GameLanguage", 0), 0, 2);
    }

    private string GetLocalizedAboutText(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Korean:
                return "Unity로 제작된 1인칭 필리핀 숲 공포 게임입니다.\n\n말라왁 숲을 탐험하고, 버려진 마을의 비밀을 밝히며, 필리핀 민속에서 영감을 받은 영혼들로부터 살아남으세요.\n\n전설을 배우고, 어둠에서 살아남으세요.\n\n제작: Bansag";
            case GameLanguage.Tagalog:
                return "Isang first-person na horror game sa kagubatan ng Pilipinas, na ginawa sa Unity.\n\nGalugarin ang Malawak Forest, tuklasin ang sinapit ng inabandonang nayon, at mabuhay laban sa mga espiritung hango sa kuwentong-bayan ng Pilipinas.\n\nAlamin ang mga alamat. Mabuhay sa dilim.\n\nGinawa ni Bansag";
            default:
                return aboutText;
        }
    }

    private void ApplyAboutPresentation()
    {
        if (aboutPanel == null)
            return;

        CaptureEditableAboutText();
        GameLanguage language = GetAboutLanguage();
        TMP_FontAsset koreanFont = settingsPanel != null ? settingsPanel.koreanFont : null;
        TextMeshProUGUI[] labels = aboutPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label.gameObject.name == "Title")
            {
                label.text = string.IsNullOrEmpty(editableAboutTitle) ? aboutTitle : editableAboutTitle;
                if (aboutHeadingFont != null) label.font = aboutHeadingFont;
                label.fontSize = 54f;
            }
            else if (label.gameObject.name == "Content")
            {
                label.text = language == GameLanguage.English
                    ? (string.IsNullOrEmpty(editableAboutContent) ? aboutText : editableAboutContent)
                    : GetLocalizedAboutText(language);
                if (language == GameLanguage.Korean && koreanFont != null)
                    label.font = koreanFont;
                else if (aboutBodyFont != null)
                    label.font = aboutBodyFont;
                label.fontSize = 22f;
                label.color = new Color(0.94f, 0.9f, 0.9f, 1f);
            }
            else if (label.gameObject.name == "Label")
            {
                label.text = language == GameLanguage.Korean ? "뒤로"
                    : language == GameLanguage.Tagalog ? "BUMALIK"
                    : (string.IsNullOrEmpty(editableAboutBackLabel) ? "BACK" : editableAboutBackLabel);
                if (language == GameLanguage.Korean && koreanFont != null)
                    label.font = koreanFont;
                else if (aboutHeadingFont != null)
                    label.font = aboutHeadingFont;
                label.fontSize = 25f;
            }
        }

        Transform panel = aboutPanel.transform.Find("Panel");
        if (panel != null)
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 500f);
    }
    private void CreateAboutPanelIfNeeded()
    {
        if (aboutPanel != null)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        aboutPanel = CreateUiObject("AboutPanel", canvas.transform);
        RectTransform overlayRect = aboutPanel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = aboutPanel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject panel = CreateUiObject("Panel", aboutPanel.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 390f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.02f, 0.12f, 0.97f);

        CreateAboutText("Title", panel.transform, aboutTitle, 30f, FontStyles.Bold,
            new Vector2(0f, 132f), new Vector2(550f, 52f), new Color(0.95f, 0.35f, 0.38f));

        GameObject divider = CreateUiObject("Divider", panel.transform);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0f, 83f);
        dividerRect.sizeDelta = new Vector2(535f, 2f);
        Image dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(0.85f, 0.15f, 0.2f, 0.85f);

        CreateAboutText("Content", panel.transform, aboutText, 20f, FontStyles.Normal,
            new Vector2(0f, -2f), new Vector2(540f, 180f), new Color(0.94f, 0.9f, 0.9f));

        GameObject backButton = CreateUiObject("BackButton", panel.transform);
        RectTransform buttonRect = backButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -138f);
        buttonRect.sizeDelta = new Vector2(180f, 48f);
        Image buttonImage = backButton.AddComponent<Image>();
        buttonImage.color = new Color(0.35f, 0.06f, 0.1f, 1f);
        Button button = backButton.AddComponent<Button>();
        aboutBackButton = button;
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(CloseAbout);
        CreateAboutText("Label", backButton.transform, "BACK", 20f, FontStyles.Bold,
            Vector2.zero, new Vector2(180f, 48f), Color.white);

        // Visible while editing, hidden by Start when the game begins.
        aboutPanel.SetActive(!Application.isPlaying);
    }

    private void BindAboutBackButton()
    {
        if (aboutPanel == null)
            return;

        if (aboutBackButton == null)
            aboutBackButton = aboutPanel.GetComponentInChildren<Button>(true);

        if (aboutBackButton != null)
        {
            aboutBackButton.onClick.RemoveListener(CloseAbout);
            aboutBackButton.onClick.AddListener(CloseAbout);
        }
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static TextMeshProUGUI CreateAboutText(string objectName, Transform parent, string text,
        float fontSize, FontStyles fontStyle, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }
}
