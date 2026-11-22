using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class SettingsPanel
{
    [Header("Language Confirmation")]
    [SerializeField] private GameObject languageConfirmationPanel;
    [SerializeField] private TextMeshProUGUI languageConfirmationMessage;
    [SerializeField] private Button confirmLanguageButton;
    [SerializeField] private Button cancelLanguageButton;

    private int pendingLanguage = -1;
    private readonly Dictionary<Selectable, bool> languageBlockedControls = new Dictionary<Selectable, bool>();

    public bool IsLanguageConfirmationOpen => languageConfirmationPanel != null && languageConfirmationPanel.activeInHierarchy;

    private void Update()
    {
        if (pendingLanguage < 0)
            return;
        if (!IsLanguageConfirmationOpen ||
            ((PauseMenu.Instance == null || !PauseMenu.Instance.isPaused) && Input.GetKeyDown(KeyCode.Escape)))
            CancelLanguageChange();
    }

    private void InitializeLanguageConfirmation()
    {
        if (languageConfirmationPanel != null)
            languageConfirmationPanel.SetActive(false);
        if (confirmLanguageButton != null)
            confirmLanguageButton.onClick.AddListener(ConfirmLanguageChange);
        if (cancelLanguageButton != null)
            cancelLanguageButton.onClick.AddListener(CancelLanguageChange);
    }

    private void RequestLanguageChange(int languageIndex)
    {
        if (languageIndex < 0 || languageIndex > 2 || languageIndex == currentLanguage || IsLanguageConfirmationOpen)
            return;

        // A missing scene reference must not cause an unconfirmed language switch.
        if (languageConfirmationPanel == null || languageConfirmationMessage == null ||
            confirmLanguageButton == null || cancelLanguageButton == null || settingsPanel == null)
        {
            Debug.LogWarning("[Settings] Assign the language confirmation panel and its controls in the Inspector.", this);
            return;
        }

        pendingLanguage = languageIndex;
        string[] names = { "English", "Korean", "Tagalog" };
        string requestedName = names[languageIndex];
        string message = $"Switch language to {requestedName}?\nCancel keeps your current language.";
        string confirm = "CONFIRM";
        string cancel = "CANCEL";
        if (currentLanguage == (int)GameLanguage.Tagalog)
        {
            message = $"Palitan ang wika sa {requestedName}?\nPiliin ang KANSELA upang panatilihin ang kasalukuyang wika.";
            confirm = "KUMPIRMAHIN";
            cancel = "KANSELA";
        }
        else if (currentLanguage == (int)GameLanguage.Korean)
        {
            message = $"언어를 {requestedName}(으)로 변경하시겠습니까?\n취소하면 현재 언어가 유지됩니다.";
            confirm = "확인";
            cancel = "취소";
        }

        languageConfirmationMessage.text = message;
        SetLanguageConfirmationLabel(confirmLanguageButton.GetComponentInChildren<TextMeshProUGUI>(true), confirm);
        SetLanguageConfirmationLabel(cancelLanguageButton.GetComponentInChildren<TextMeshProUGUI>(true), cancel);
        SetLanguageConfirmationLabel(languageConfirmationMessage, message);

        languageBlockedControls.Clear();
        foreach (Selectable control in settingsPanel.GetComponentsInChildren<Selectable>(true))
        {
            if (control.transform.IsChildOf(languageConfirmationPanel.transform))
                continue;
            languageBlockedControls[control] = control.interactable;
            control.interactable = false;
        }

        languageConfirmationPanel.transform.SetAsLastSibling();
        PrepareLanguageConfirmationCanvas();
        languageConfirmationPanel.SetActive(true);
    }

    private void PrepareLanguageConfirmationCanvas()
    {
        Canvas dialogCanvas = languageConfirmationPanel.GetComponent<Canvas>();
        if (dialogCanvas == null)
            dialogCanvas = languageConfirmationPanel.AddComponent<Canvas>();

        Canvas parentCanvas = settingsPanel.GetComponentInParent<Canvas>();
        int highestOrder = 1000;
        if (parentCanvas != null)
        {
            dialogCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            foreach (Canvas canvas in parentCanvas.rootCanvas.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas != dialogCanvas && !canvas.transform.IsChildOf(languageConfirmationPanel.transform))
                    highestOrder = Mathf.Max(highestOrder, canvas.sortingOrder);
            }
        }

        // Settings and some of its children have their own sorting overrides.
        // A later sibling alone cannot place the dialog above those canvases.
        dialogCanvas.overrideSorting = true;
        dialogCanvas.sortingOrder = highestOrder + 1;
        if (languageConfirmationPanel.GetComponent<GraphicRaycaster>() == null)
            languageConfirmationPanel.AddComponent<GraphicRaycaster>();

        Image backdrop = languageConfirmationPanel.GetComponent<Image>();
        if (backdrop != null)
            backdrop.raycastTarget = true;
    }

    private void SetLanguageConfirmationLabel(TextMeshProUGUI label, string text)
    {
        if (label == null)
            return;
        label.text = text;
        if (currentLanguage == (int)GameLanguage.Korean && koreanFont != null)
        {
            label.font = koreanFont;
            label.fontSharedMaterial = koreanFont.material;
        }
        else if (volumeLabel != null)
        {
            label.font = volumeLabel.font;
            label.fontSharedMaterial = volumeLabel.fontSharedMaterial;
        }
    }

    private void ConfirmLanguageChange()
    {
        int requestedLanguage = pendingLanguage;
        CancelLanguageChange();
        if (requestedLanguage >= 0)
            ApplyLanguageChange(requestedLanguage);
    }

    public void CancelLanguageChange()
    {
        pendingLanguage = -1;
        if (languageConfirmationPanel != null)
            languageConfirmationPanel.SetActive(false);
        foreach (KeyValuePair<Selectable, bool> entry in languageBlockedControls)
        {
            if (entry.Key != null)
                entry.Key.interactable = entry.Value;
        }
        languageBlockedControls.Clear();
    }
}
