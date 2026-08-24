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
    [SerializeField] private int displayedCapacity = -1;

    void OnEnable()
    {
        if (!Application.isPlaying && panel == null)
            CreatePanel(editorSlotCount);
    }

    void Update()
    {
        if (Inventory.Instance == null) return;
        Inventory.Instance.CleanupNullItems();

        int max = Inventory.Instance.GetMax();
        if (panel == null || displayedCapacity != max) CreatePanel(max);
        Refresh();
    }

    private void CreatePanel(int max)
    {
        if (panel != null) Destroy(panel);

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        displayedCapacity = max;
        panel = CreateUiObject("Bag UI", canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta = new Vector2(280f, 70f + max * 30f);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.68f);
        background.raycastTarget = false;

        titleText = CreateText("Bag Title", panel.transform, 18, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(250f, 28f));

        slotTexts.Clear();
        for (int i = 0; i < max; i++)
        {
            TextMeshProUGUI slot = CreateText($"Slot {i + 1}", panel.transform, 15, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(slot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f - i * 30f), new Vector2(245f, 26f));
            slotTexts.Add(slot);
        }

        hintText = CreateText("Bag Hint", panel.transform, 11, FontStyles.Normal, TextAlignmentOptions.Center);
        hintText.color = Color.gray;
        SetRect(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(250f, 22f));
        hintText.text = "Scroll to switch  |  G to drop";
    }

    private void Refresh()
    {
        int count = Inventory.Instance.GetCount();
        int max = Inventory.Instance.GetMax();
        int selected = Inventory.Instance.GetSelectedIndex();
        List<GameObject> items = Inventory.Instance.GetItems();

        titleText.text = $"BAG  {count}/{max}";
        for (int i = 0; i < slotTexts.Count; i++)
        {
            string slotText = "[ empty ]";
            if (i < items.Count && items[i] != null)
            {
                PickupItem pickup = items[i].GetComponent<PickupItem>();
                Key key = items[i].GetComponent<Key>();
                if (pickup != null && !string.IsNullOrEmpty(pickup.itemName)) slotText = pickup.itemName;
                else if (key != null && !string.IsNullOrEmpty(key.itemName)) slotText = key.itemName;
                else slotText = items[i].name.Replace("(Clone)", "");
            }

            bool isSelected = i == selected;
            slotTexts[i].text = (isSelected ? "▶  " : "    ") + slotText;
            slotTexts[i].color = isSelected ? new Color(1f, 0.84f, 0.2f) : Color.white;
        }
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
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
