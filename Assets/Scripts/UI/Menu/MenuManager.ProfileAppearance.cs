using UnityEngine;
using UnityEngine.UI;

public partial class MenuManager
{
    [SerializeField, Tooltip("Default font for headings created at runtime. Scene labels use their own Text font.")]
    private Font profileDisplayFont;
    [SerializeField, Tooltip("Font for profile rows created at runtime. Scene labels use their own Text font.")]
    private Font profileBodyFont;
    [SerializeField, Tooltip("Font for Hangul text when Korean is selected.")]
    private Font profileKoreanFont;
    [SerializeField] private RenderTexture profileVideoTexture;

    private static readonly Color ProfileBlood = new Color(0.34f, 0.055f, 0.055f, 1f);
    private static readonly Color ProfileParchment = new Color(0.92f, 0.83f, 0.7f, 1f);

    public void ConfigureProfileAppearance(Font display, Font body, Font korean, RenderTexture video)
    {
        if (profileDisplayFont == null)
            profileDisplayFont = display;
        if (profileBodyFont == null)
            profileBodyFont = body;
        if (profileKoreanFont == null)
            profileKoreanFont = korean;
        profileVideoTexture = video;
        if (profilePanel == null)
            return;

        Image overlay = profilePanel.GetComponent<Image>();
        overlay.color = new Color(0.018f, 0.004f, 0.005f, 0.76f);

        Transform card = profilePanel.transform.Find("Profile Card");
        if (card != null)
        {
            card.GetComponent<Image>().color = new Color(0.13f, 0.025f, 0.028f, 0.96f);
            SetVideoBackground(card, new Color(0.25f, 0.18f, 0.18f, 0.3f));
            SetImage(card.Find("Profile List Viewport"), new Color(0.015f, 0.005f, 0.006f, 0.62f));
            SetImage(card.Find("New Profile Name"), new Color(0.085f, 0.025f, 0.025f, 0.95f));
            SetImage(card.Find("Create Profile"), ProfileBlood);
            SetTextColor(card.Find("Title"), ProfileParchment, 36);
            SetTextColor(card.Find("Hint"), ProfileParchment, 17);
            SetTextColor(card.Find("Message"), new Color(1f, 0.5f, 0.45f), 16);
        }

        if (switchProfileButton != null)
        {
            switchProfileButton.GetComponent<Image>().color = ProfileBlood;
            SetVideoBackground(switchProfileButton.transform, new Color(0.7f, 0.35f, 0.35f, 0.22f));
        }

        if (deleteConfirmationPanel != null)
        {
            RectTransform dialog = deleteConfirmationPanel.GetComponent<RectTransform>();
            dialog.sizeDelta = new Vector2(570f, 250f);
            dialog.GetComponent<Image>().color = new Color(0.16f, 0.025f, 0.03f, 1f);
            SetImage(dialog.Find("Confirm Delete"), ProfileBlood);
            SetImage(dialog.Find("Cancel Delete"), new Color(0.09f, 0.065f, 0.055f, 1f));
        }

        if (Application.isPlaying)
            RefreshProfileLocalization();
    }

    private void SetVideoBackground(Transform parent, Color tint)
    {
        if (profileVideoTexture == null)
            return;
        Transform existing = parent.Find("VAREN Menu Footage");
        RectTransform rect = existing != null ? existing.GetComponent<RectTransform>() :
            CreateProfileRect("VAREN Menu Footage", parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        RawImage image = rect.GetComponent<RawImage>();
        if (image == null)
            image = rect.gameObject.AddComponent<RawImage>();
        image.texture = profileVideoTexture;
        image.color = tint;
        image.raycastTarget = false;
    }

    private static void SetImage(Transform target, Color color)
    {
        if (target != null && target.TryGetComponent(out Image image))
            image.color = color;
    }

    private static void SetTextColor(Transform target, Color color, int size)
    {
        if (target != null && target.TryGetComponent(out Text label))
        {
            label.color = color;
            label.fontSize = size;
        }
    }
}
