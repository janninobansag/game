using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FlashlightBatteryUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider batteryBar;
    public Image batteryFill;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            CreateBatteryBarIfNeeded();
    }

    void Start()
    {
        CreateBatteryBarIfNeeded();
        Refresh();
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        Refresh();
    }

    private void Refresh()
    {
        FlashlightPickup flashlight = FlashlightPickup.HeldFlashlight;
        bool shouldShow = flashlight != null && flashlight.IsHeld;

        if (batteryBar == null)
            return;

        batteryBar.gameObject.SetActive(shouldShow);
        if (!shouldShow)
            return;

        float batteryPercent = flashlight.GetBatteryPercent();
        batteryBar.minValue = 0f;
        batteryBar.maxValue = 1f;
        batteryBar.value = batteryPercent;

        if (batteryFill != null)
        {
            batteryFill.color = batteryPercent > 0.5f ? new Color(0.35f, 0.9f, 0.45f) :
                batteryPercent > 0.25f ? new Color(1f, 0.78f, 0.2f) :
                new Color(0.95f, 0.25f, 0.2f);
        }
    }

    private void CreateBatteryBarIfNeeded()
    {
        if (batteryBar != null)
            return;

        GameObject barObject = new GameObject("FlashlightBatteryBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        barObject.transform.SetParent(transform, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = Vector2.zero;
        barRect.anchorMax = Vector2.zero;
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(145f, 78f);
        barRect.sizeDelta = new Vector2(220f, 18f);

        Image background = barObject.GetComponent<Image>();
        background.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
        background.raycastTarget = false;

        batteryBar = barObject.GetComponent<Slider>();
        batteryBar.interactable = false;
        batteryBar.transition = Selectable.Transition.None;
        batteryBar.targetGraphic = background;
        batteryBar.minValue = 0f;
        batteryBar.maxValue = 1f;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(barObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        batteryFill = fillObject.GetComponent<Image>();
        batteryFill.raycastTarget = false;
        batteryBar.fillRect = fillRect;
        barObject.SetActive(false);
    }
}
