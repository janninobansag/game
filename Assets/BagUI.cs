using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class BagUI : MonoBehaviour
{
    [Header("Editor UI")]
    [Min(1)] public int editorSlotCount = 3;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private List<TextMeshProUGUI> slotTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<Image> slotBackgrounds = new List<Image>();
    [SerializeField] private List<RawImage> slotIcons = new List<RawImage>();
    [SerializeField] private List<GameObject> slotHighlights = new List<GameObject>();
    [SerializeField] private Texture2D[] itemIcons = new Texture2D[6];
    private static readonly string[] ItemIconResourceNames = { "Battery", "Candle", "Key", "Flashlight", "Cross", "Bible" };
    [SerializeField] private int displayedCapacity = -1;
    private const int CurrentLayoutVersion = 8;
    [SerializeField] private int builtLayoutVersion = -1;

    void OnEnable()
    {
        if (!Application.isPlaying && (panel == null || builtLayoutVersion != CurrentLayoutVersion))
            CreatePanel(editorSlotCount);
    }

    void Update()
    {
        if (Inventory.Instance == null) return;
        Inventory.Instance.CleanupNullItems();

        int max = Inventory.Instance.GetMax();
        if (panel == null || displayedCapacity != max || builtLayoutVersion != CurrentLayoutVersion) CreatePanel(max);
        Refresh();
    }

    private void CreatePanel(int max)
    {
        if (panel != null)
        {
            if (Application.isPlaying) Destroy(panel);
            else DestroyImmediate(panel);
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        displayedCapacity = max;
        builtLayoutVersion = CurrentLayoutVersion;
        panel = CreateUiObject("Bag UI", canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 10f);
        panelRect.sizeDelta = new Vector2(390f, 110f);

        // Transparent layout container: only the three slot frames are visible.
        titleText = null;

        slotTexts.Clear();
        slotBackgrounds.Clear();
        Texture2D slotTexture = Resources.Load<Texture2D>("UI/BagSlotBackground");
        LoadItemIcons();
        Sprite slotFrame = slotTexture != null ? Sprite.Create(slotTexture, new Rect(0f, 0f, slotTexture.width, slotTexture.height), new Vector2(0.5f, 0.5f)) : null;
        slotIcons.Clear();
        slotHighlights.Clear();
        for (int i = 0; i < max; i++)
        {
            GameObject slotObject = CreateUiObject($"Bag Slot {i + 1}", panel.transform);
            Image slotBackground = slotObject.AddComponent<Image>();
            slotBackground.raycastTarget = false;
            SetRect(slotObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - (max - 1) * 0.5f) * 118f, 52f), new Vector2(94f, 94f));
            if (slotFrame != null)
                slotBackground.sprite = slotFrame;

            slotBackground.color = Color.white;

            GameObject highlight = CreateUiObject("Selected Slot Outline", slotObject.transform);
            SetRect(highlight.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(106f, 106f));
            highlight.transform.SetAsFirstSibling();
            CreateOutlineEdge("Top", highlight.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(106f, 4f));
            CreateOutlineEdge("Bottom", highlight.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(106f, 4f));
            CreateOutlineEdge("Left", highlight.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(4f, 106f));
            CreateOutlineEdge("Right", highlight.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-2f, 0f), new Vector2(4f, 106f));
            highlight.SetActive(false);
            slotHighlights.Add(highlight);

            TextMeshProUGUI slot = CreateText($"Slot {i + 1} Label", slotObject.transform, 12, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(slot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));
            slotTexts.Add(slot);
            slotBackgrounds.Add(slotBackground);
            GameObject iconObject = CreateUiObject($"Slot {i + 1} Icon", slotObject.transform);
            RawImage icon = iconObject.AddComponent<RawImage>();
            icon.raycastTarget = false;
            icon.color = Color.white;
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
            slotIcons.Add(icon);
        }

        hintText = null;
    }

    private void Refresh()
    {
        int count = Inventory.Instance.GetCount();
        int max = Inventory.Instance.GetMax();
        int selected = Inventory.Instance.GetSelectedIndex();
        List<GameObject> items = Inventory.Instance.GetItems();

        if (titleText != null) titleText.text = $"BAG  {count}/{max}";
        for (int i = 0; i < slotTexts.Count; i++)
        {
            string slotText = "EMPTY";
            if (i < items.Count && items[i] != null)
            {
                PickupItem pickup = items[i].GetComponent<PickupItem>();
                Key key = items[i].GetComponent<Key>();
                if (pickup != null && !string.IsNullOrEmpty(pickup.itemName)) slotText = pickup.itemName;
                else if (key != null && !string.IsNullOrEmpty(key.itemName)) slotText = key.itemName;
                else slotText = items[i].name.Replace("(Clone)", "");
            }

            GameObject item = i < items.Count ? items[i] : null;
            int iconIndex = GetIconIndex(item);
            Texture2D fixedIcon = GetItemIcon(iconIndex);
            Texture itemPreview;
            if (fixedIcon != null)
            {
                itemPreview = fixedIcon;
                slotIcons[i].uvRect = new Rect(0f, 0f, 1f, 1f);
            }
            else
            {
                itemPreview = item != null ? InventoryItemPreview.GetPreview(item, i) : null;
                slotIcons[i].uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            slotIcons[i].texture = itemPreview;
            slotIcons[i].enabled = itemPreview != null;
            SetIconSize(slotIcons[i], itemPreview);
            slotIcons[i].color = Color.white;
            if (i < slotBackgrounds.Count)
                slotBackgrounds[i].color = Color.white;
            if (i < slotHighlights.Count)
                slotHighlights[i].SetActive(i == selected);
            slotTexts[i].gameObject.SetActive(false);
            slotTexts[i].text = slotText;
            slotTexts[i].color = Color.white;
        }
    }

    private void LoadItemIcons()
    {
        if (itemIcons == null || itemIcons.Length != ItemIconResourceNames.Length)
            itemIcons = new Texture2D[ItemIconResourceNames.Length];

        for (int i = 0; i < itemIcons.Length; i++)
            if (itemIcons[i] == null)
                itemIcons[i] = Resources.Load<Texture2D>($"UI/ItemIcons/{ItemIconResourceNames[i]}");
    }

    private Texture2D GetItemIcon(int iconIndex)
    {
        if (iconIndex < 0 || iconIndex >= ItemIconResourceNames.Length) return null;
        LoadItemIcons();
        return itemIcons[iconIndex];
    }

    private static void SetIconSize(RawImage icon, Texture texture)
    {
        const float maximumSize = 62f;
        if (texture == null || texture.height == 0)
        {
            icon.rectTransform.sizeDelta = new Vector2(maximumSize, maximumSize);
            return;
        }

        float aspectRatio = (float)texture.width / texture.height;
        icon.rectTransform.sizeDelta = aspectRatio >= 1f
            ? new Vector2(maximumSize, maximumSize / aspectRatio)
            : new Vector2(maximumSize * aspectRatio, maximumSize);
    }

    private static int GetIconIndex(GameObject item)
    {
        if (item == null) return -1;

        string itemName = item.name.ToLowerInvariant();
        if (item.GetComponent<BatteryPickup>() != null || itemName.Contains("battery")) return 0;
        if (item.GetComponent<CandleItem>() != null || itemName.Contains("candle")) return 1;
        if (item.GetComponent<Key>() != null || item.GetComponent<KeyUse>() != null || itemName.Contains("key")) return 2;
        if (item.GetComponent<FlashlightPickup>() != null || itemName.Contains("flashlight")) return 3;
        if (itemName.Contains("cross")) return 4;
        if (itemName.Contains("bible") || itemName.Contains("book")) return 5;
        return -1;
    }

    [ContextMenu("Rebuild Bag UI in Canvas")]
    public void RebuildBagUiInCanvas()
    {
        if (!Application.isPlaying)
            CreatePanel(editorSlotCount);
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static void CreateOutlineEdge(string edgeName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject edge = CreateUiObject(edgeName, parent);
        Image image = edge.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(1f, 0.72f, 0.12f, 1f);
        SetRect(edge.GetComponent<RectTransform>(), anchorMin, anchorMax, position, size);
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
