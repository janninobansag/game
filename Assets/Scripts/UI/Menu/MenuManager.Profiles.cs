using UnityEngine;
using UnityEngine.UI;

public partial class MenuManager : MonoBehaviour
{
    private bool profileChosen;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private RectTransform profileList;
    [SerializeField] private RectTransform profileTemplatesRoot;
    [SerializeField] private RectTransform originalProfileRowTemplate;
    [SerializeField] private RectTransform playerProfileRowTemplate;
    [SerializeField] private InputField profileNameInput;
    [SerializeField] private Text profileErrorText;
    [SerializeField] private Text activeProfileText;
    [SerializeField] private Button switchProfileButton;
    [SerializeField] private Button createProfileButton;
    [SerializeField] private Button closeProfileButton;
    [SerializeField] private GameObject deleteConfirmationPanel;
    [SerializeField] private Text deleteConfirmationText;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;
    private Font profileFont;
    private string pendingDeleteId;
    private string pendingDeleteName;

    private void Awake()
    {
        if (Application.isPlaying && profileTemplatesRoot != null)
            profileTemplatesRoot.gameObject.SetActive(false);
    }

    private void InitializeProfileMenu()
    {
        if (!Application.isPlaying)
            return;

        profileFont = profileBodyFont != null ? profileBodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (profilePanel == null && !EnsureProfileUiInCanvas())
            return;
        EnsureProfileRowTemplates();
        EnsureProfileCloseButton();

        // On first use, require a profile choice. Once the player has created
        // a separate profile, keep using the saved active profile on startup.
        profileChosen = GameProfiles.GetProfiles().Count > 1;
        switchProfileButton.onClick.RemoveListener(OpenProfilePanel);
        createProfileButton.onClick.RemoveListener(CreateAndSelectProfile);
        confirmDeleteButton.onClick.RemoveListener(ConfirmDeleteProfile);
        cancelDeleteButton.onClick.RemoveListener(CancelDeleteProfile);
        closeProfileButton.onClick.RemoveListener(CloseProfilePanel);
        switchProfileButton.onClick.AddListener(OpenProfilePanel);
        createProfileButton.onClick.AddListener(CreateAndSelectProfile);
        confirmDeleteButton.onClick.AddListener(ConfirmDeleteProfile);
        cancelDeleteButton.onClick.AddListener(CancelDeleteProfile);
        closeProfileButton.onClick.AddListener(CloseProfilePanel);
        deleteConfirmationPanel.SetActive(false);
        RefreshProfileLocalization();
        profilePanel.SetActive(!profileChosen);
    }

    // Called once by the Editor scene builder; runtime also repairs a missing panel.
    public bool EnsureProfileUiInCanvas()
    {
        if (profilePanel != null)
            return false;

        Canvas menuCanvas = null;
        foreach (Canvas candidate in FindObjectsOfType<Canvas>(true))
        {
            if (candidate.gameObject.scene == gameObject.scene && candidate.gameObject.name == "Canvas")
            {
                menuCanvas = candidate;
                break;
            }
        }

        if (menuCanvas == null)
        {
            Debug.LogError("[MenuManager] The menu scene needs a Canvas for the profile UI.");
            return false;
        }

        profileFont = profileBodyFont != null ? profileBodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        switchProfileButton = CreateProfileButton("Switch Profile", menuCanvas.transform,
            "PROFILE", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(265f, 48f));
        RectTransform switchRect = switchProfileButton.GetComponent<RectTransform>();
        switchRect.pivot = new Vector2(1f, 1f);
        switchRect.anchoredPosition = new Vector2(-30f, -30f);
        activeProfileText = switchProfileButton.GetComponentInChildren<Text>();

        profilePanel = CreateProfileRect("ProfilePanel", menuCanvas.transform).gameObject;
        RectTransform overlay = profilePanel.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        Image overlayImage = profilePanel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.94f);

        RectTransform card = CreateProfileRect("Profile Card", overlay);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(620f, 610f);
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.09f, 0.105f, 0.11f, 1f);

        CreateProfileText("Title", card, "WHO IS PLAYING?", 30, new Vector2(0f, 253f),
            new Vector2(560f, 48f), new Color(0.92f, 0.82f, 0.57f));
        CreateProfileText("Hint", card, "Choose a profile before starting or loading a game.", 18,
            new Vector2(0f, 213f), new Vector2(560f, 38f), Color.white);

        RectTransform viewport = CreateProfileRect("Profile List Viewport", card);
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 0.5f);
        viewport.anchoredPosition = new Vector2(0f, 58f);
        viewport.sizeDelta = new Vector2(550f, 245f);
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        profileList = CreateProfileRect("Profiles", viewport);
        profileList.anchorMin = new Vector2(0f, 1f);
        profileList.anchorMax = new Vector2(1f, 1f);
        profileList.pivot = new Vector2(0.5f, 1f);
        profileList.anchoredPosition = Vector2.zero;
        profileList.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = profileList.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = profileList.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = profileList;
        scroll.horizontal = false;
        scroll.vertical = true;

        RectTransform inputRect = CreateProfileRect("New Profile Name", card);
        inputRect.anchorMin = inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0f, -113f);
        inputRect.sizeDelta = new Vector2(550f, 48f);
        inputRect.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.19f, 0.19f, 1f);
        profileNameInput = inputRect.gameObject.AddComponent<InputField>();
        profileNameInput.characterLimit = 24;
        Text inputText = CreateProfileText("Name", inputRect, "", 20, Vector2.zero,
            Vector2.zero, Color.white);
        StretchProfileText(inputText.rectTransform, 14f);
        Text placeholder = CreateProfileText("Placeholder", inputRect, "Enter a new profile name", 20,
            Vector2.zero, Vector2.zero, new Color(0.65f, 0.65f, 0.65f));
        StretchProfileText(placeholder.rectTransform, 14f);
        profileNameInput.textComponent = inputText;
        profileNameInput.placeholder = placeholder;

        createProfileButton = CreateProfileButton("Create Profile", card, "CREATE PROFILE",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -174f), new Vector2(550f, 48f));

        profileErrorText = CreateProfileText("Message", card, "", 16,
            new Vector2(0f, -228f), new Vector2(550f, 38f), new Color(1f, 0.55f, 0.5f));

        RectTransform confirmation = CreateProfileRect("Delete Confirmation", profilePanel.transform);
        confirmation.anchorMin = confirmation.anchorMax = new Vector2(0.5f, 0.5f);
        confirmation.sizeDelta = new Vector2(530f, 240f);
        confirmation.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.09f, 0.09f, 1f);
        deleteConfirmationPanel = confirmation.gameObject;
        deleteConfirmationText = CreateProfileText("Warning", confirmation,
            "Permanently delete this profile and its saves?", 21,
            new Vector2(0f, 47f), new Vector2(490f, 100f), Color.white);
        confirmDeleteButton = CreateProfileButton("Confirm Delete", confirmation, "DELETE FOREVER",
            new Vector2(0.5f, 0.5f), new Vector2(-125f, -65f), new Vector2(220f, 48f));
        confirmDeleteButton.GetComponent<Image>().color = new Color(0.45f, 0.13f, 0.12f, 1f);
        cancelDeleteButton = CreateProfileButton("Cancel Delete", confirmation, "CANCEL",
            new Vector2(0.5f, 0.5f), new Vector2(125f, -65f), new Vector2(220f, 48f));
        deleteConfirmationPanel.SetActive(false);
        profilePanel.SetActive(false);
        return true;
    }

    public void OpenProfilePanel()
    {
        if (profilePanel == null)
            return;

        RefreshProfileLocalization();
        profileErrorText.text = "";
        pendingDeleteId = null;
        deleteConfirmationPanel.SetActive(false);
        // The profile picker replaces the other menu views, so none remain visible.
        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(false);
        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);
        if (loadPanel != null)
            loadPanel.SetActive(false);
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        profilePanel.SetActive(true);
    }

    public void EnsureProfileCloseButton()
    {
        if (profilePanel == null)
            return;

        Transform card = profilePanel.transform.Find("Profile Card");
        if (card == null)
            return;

        if (closeProfileButton == null)
        {
            Transform existing = card.Find("Close Profile");
            if (existing != null)
                closeProfileButton = existing.GetComponent<Button>();
        }

        bool createdButton = false;
        if (closeProfileButton == null)
        {
            closeProfileButton = CreateProfileButton("Close Profile", card, "CLOSE",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -256f), new Vector2(550f, 44f));
            closeProfileButton.GetComponent<Image>().color = new Color(0.12f, 0.035f, 0.035f, 1f);
            createdButton = true;
        }

        RectTransform closeRect = closeProfileButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(0f, -256f);
        closeRect.sizeDelta = new Vector2(550f, 44f);

        // Set the initial Inspector font while creating/updating the scene. At
        // runtime, retain any font assigned to this Text component in the Inspector.
        if (createdButton || !Application.isPlaying)
        {
            Text closeLabel = closeProfileButton.GetComponentInChildren<Text>();
            if (closeLabel != null)
            {
                closeLabel.font = profileBodyFont != null ? profileBodyFont : profileFont;
                closeLabel.fontSize = 18;
                closeLabel.color = ProfileParchment;
                closeLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                closeLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        if (profileErrorText == null)
        {
            Transform message = card.Find("Message");
            if (message != null)
                profileErrorText = message.GetComponent<Text>();
        }

        if (profileErrorText != null)
        {
            RectTransform messageRect = profileErrorText.rectTransform;
            messageRect.anchoredPosition = new Vector2(0f, -220f);
            messageRect.sizeDelta = new Vector2(550f, 24f);
        }
    }

    private void CloseProfilePanel()
    {
        pendingDeleteId = null;
        pendingDeleteName = null;
        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
    }

    private bool RequireProfileSelection()
    {
        if (profileChosen)
            return true;

#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning($"[Profiles] Menu action needs a profile selection. Active profile: {GameProfiles.ActiveId}. Caller:\n{new System.Diagnostics.StackTrace(1, true)}", this);
#endif
        OpenProfilePanel();
        return false;
    }

    private void RefreshProfileMenu()
    {
        EnsureProfileRowTemplates();
        string activeName = GameProfiles.ActiveId == GameProfiles.OriginalProfileId
            ? ProfileText("Original Save", "Dating Save", "기존 저장")
            : GameProfiles.ActiveName;
        activeProfileText.text = ProfileLabel(activeName);
        ApplyProfileLocalizedFont(activeProfileText, activeProfileText.text);
        activeProfileText.color = ProfileParchment;

        foreach (Transform child in profileList)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (GameProfiles.Profile profile in GameProfiles.GetProfiles())
        {
            string id = profile.id;
            string name = ProfileDisplayName(profile);
            RectTransform template = id == GameProfiles.OriginalProfileId
                ? originalProfileRowTemplate : playerProfileRowTemplate;
            RectTransform row = Instantiate(template, profileList);
            row.name = "Profile " + name;
            row.gameObject.SetActive(true);
            Button selectButton = row.Find("Select").GetComponent<Button>();
            Text selectLabel = selectButton.GetComponentInChildren<Text>();
            selectLabel.text = name;
            if (GetAboutLanguage() == GameLanguage.Korean && profileKoreanFont != null && ContainsKorean(name))
                selectLabel.font = profileKoreanFont;
            selectButton.onClick.AddListener(() => SelectProfile(id));

            if (id != GameProfiles.OriginalProfileId)
            {
                Button deleteButton = row.Find("Delete Profile").GetComponent<Button>();
                deleteButton.onClick.AddListener(() => RequestDeleteProfile(id, profile.name));
            }

            // Profile rows contain local player names and exist only at runtime.
            foreach (Transform part in row.GetComponentsInChildren<Transform>(true))
                part.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(profileList);
        Canvas.ForceUpdateCanvases();
        ScrollRect listScroll = profileList.parent.GetComponent<ScrollRect>();
        if (listScroll != null)
            listScroll.verticalNormalizedPosition = 1f;
    }

    // Stored as inactive Canvas children, so artists can edit both row layouts
    // in the Inspector without adding a specific player's profile to the scene.
    public void EnsureProfileRowTemplates()
    {
        if (profileList == null)
            return;

        profileFont = profileBodyFont != null ? profileBodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (profileTemplatesRoot == null)
        {
            Transform card = profileList.parent != null ? profileList.parent.parent : null;
            if (card == null)
                return;

            Transform existingRoot = card.Find("Profile Row Templates");
            if (existingRoot != null)
                profileTemplatesRoot = existingRoot.GetComponent<RectTransform>();
            else
            {
                profileTemplatesRoot = CreateProfileRect("Profile Row Templates", card);
            }
        }

        profileTemplatesRoot.anchorMin = Vector2.zero;
        profileTemplatesRoot.anchorMax = Vector2.one;
        profileTemplatesRoot.offsetMin = Vector2.zero;
        profileTemplatesRoot.offsetMax = Vector2.zero;

        if (originalProfileRowTemplate != null)
            originalProfileRowTemplate.SetParent(profileTemplatesRoot, false);
        if (playerProfileRowTemplate != null)
            playerProfileRowTemplate.SetParent(profileTemplatesRoot, false);
        if (originalProfileRowTemplate == null)
            originalProfileRowTemplate = CreateProfileRowTemplate("Original Profile Row Template", "Original Save", false, profileTemplatesRoot);
        if (playerProfileRowTemplate == null)
            playerProfileRowTemplate = CreateProfileRowTemplate("Player Profile Row Template", "Profile Name", true, profileTemplatesRoot);

        ConfigureProfileRowPreview(originalProfileRowTemplate, 149f);
        ConfigureProfileRowPreview(playerProfileRowTemplate, 95f);
        Text playerTemplateLabel = playerProfileRowTemplate.Find("Select/Label")?.GetComponent<Text>();
        if (!Application.isPlaying && playerTemplateLabel != null)
            playerTemplateLabel.text = "Profile Name";
        originalProfileRowTemplate.gameObject.SetActive(true);
        playerProfileRowTemplate.gameObject.SetActive(true);
        profileTemplatesRoot.gameObject.SetActive(!Application.isPlaying);
        ConfigureProfileScroll();
    }

    private static void ConfigureProfileRowPreview(RectTransform row, float yPosition)
    {
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(550f, 46f);
        row.anchoredPosition = new Vector2(0f, yPosition);
    }

    public void ConfigureProfileScroll()
    {
        if (profileList == null || profileList.parent == null)
            return;

        RectTransform viewport = profileList.parent as RectTransform;
        if (viewport == null)
            return;

        ScrollRect scroll = viewport.GetComponent<ScrollRect>();
        if (scroll == null)
            scroll = viewport.gameObject.AddComponent<ScrollRect>();

        scroll.viewport = viewport;
        scroll.content = profileList;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.inertia = true;
    }

    private RectTransform CreateProfileRowTemplate(string objectName, string labelText, bool canDelete, Transform parent)
    {
        RectTransform row = CreateProfileRect(objectName, parent);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(550f, 46f);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        Button select = CreateProfileButton("Select", row, labelText,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(canDelete ? 410f : 550f, 46f));
        select.GetComponent<Image>().color = canDelete
            ? new Color(0.075f, 0.025f, 0.025f, 0.96f)
            : new Color(0.29f, 0.08f, 0.07f, 1f);
        Text label = select.GetComponentInChildren<Text>();
        label.font = profileFont;
        label.color = ProfileParchment;
        LayoutElement selectSize = select.gameObject.AddComponent<LayoutElement>();
        selectSize.preferredWidth = canDelete ? 410f : 550f;
        selectSize.flexibleWidth = 1f;

        if (canDelete)
        {
            Button delete = CreateProfileButton("Delete Profile", row, "",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 46f));
            delete.GetComponent<Image>().color = ProfileBlood;
            delete.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
            CreateTrashIcon(delete.transform);
        }

        row.gameObject.SetActive(true);
        return row;
    }

    private void RequestDeleteProfile(string id, string name)
    {
        pendingDeleteId = id;
        pendingDeleteName = name;
        UpdateDeleteConfirmationText();
        deleteConfirmationPanel.SetActive(true);
    }

    private void CancelDeleteProfile()
    {
        pendingDeleteId = null;
        pendingDeleteName = null;
        deleteConfirmationPanel.SetActive(false);
    }

    private void ConfirmDeleteProfile()
    {
        if (string.IsNullOrEmpty(pendingDeleteId))
            return;

        string id = pendingDeleteId;
        if (id == GameProfiles.ActiveId)
        {
            GameProfiles.Select(GameProfiles.OriginalProfileId);
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.ReinitializeDatabase();
            if (ProgressionSystem.Instance != null)
                ProgressionSystem.Instance.LoadProgress();
            CheckpointTrigger.HasCheckpointSaved = false;
            profileChosen = false;
        }

        if (!GameProfiles.TryDelete(id, out string error))
        {
            deleteConfirmationText.text = LocalizeProfileError(error);
            return;
        }

        pendingDeleteId = null;
        pendingDeleteName = null;
        deleteConfirmationPanel.SetActive(false);
        profileErrorText.text = ProfileDeletedMessage();
        RefreshProfileLocalization();
        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
    }

    private void CreateAndSelectProfile()
    {
        if (GameProfiles.TryCreate(profileNameInput.text, out GameProfiles.Profile created, out string error))
        {
            profileNameInput.text = "";
            SelectProfile(created.id);
            return;
        }

        profileErrorText.text = LocalizeProfileError(error);
    }

    private void SelectProfile(string id)
    {
        if (!GameProfiles.Select(id))
        {
            profileErrorText.text = LocalizeProfileError("Could not open that profile.");
            return;
        }

        profileChosen = true;
        CheckpointTrigger.HasCheckpointSaved = false;
        if (ProgressionSystem.Instance != null)
            ProgressionSystem.Instance.LoadProgress();
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.ReinitializeDatabase();

        profilePanel.SetActive(false);
        RefreshProfileLocalization();
        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
        LoadAndDisplayProgression();
    }

    private static RectTransform CreateProfileRect(string name, Transform parent)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Text CreateProfileText(string name, Transform parent, string content, int fontSize,
        Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateProfileRect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = (name == "Title" || name == "Label") && profileDisplayFont != null
            ? profileDisplayFont
            : profileFont;
        label.text = content;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateProfileButton(string name, Transform parent, string title,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateProfileRect(name, parent);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.24f, 0.25f, 0.24f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.42f, 0.37f, 0.25f, 1f);
        colors.pressedColor = new Color(0.5f, 0.39f, 0.2f, 1f);
        button.colors = colors;
        Text label = CreateProfileText("Label", rect, title, 19, Vector2.zero, Vector2.zero, Color.white);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static void StretchProfileText(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, 0f);
        rect.offsetMax = new Vector2(-padding, 0f);
    }

    private static void CreateTrashIcon(Transform parent)
    {
        AddIconPart("Handle", parent, new Vector2(0f, 10f), new Vector2(9f, 3f));
        AddIconPart("Lid", parent, new Vector2(0f, 6f), new Vector2(23f, 3f));
        AddIconPart("Bin", parent, new Vector2(0f, -3f), new Vector2(18f, 15f));
        AddIconPart("Slot Left", parent, new Vector2(-4f, -3f), new Vector2(2f, 10f), new Color(0.35f, 0.07f, 0.07f));
        AddIconPart("Slot Right", parent, new Vector2(4f, -3f), new Vector2(2f, 10f), new Color(0.35f, 0.07f, 0.07f));
    }

    private static void AddIconPart(string name, Transform parent, Vector2 position, Vector2 size, Color? color = null)
    {
        RectTransform rect = CreateProfileRect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color ?? ProfileParchment;
        image.raycastTarget = false;
    }
}
