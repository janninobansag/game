// PURPOSE: Displays and saves a subtitle when its associated item is picked up.
using UnityEngine;

public class ItemSubtitleTrigger : MonoBehaviour
{
    // Text and timing shown only the first time this item is collected.
    [Header("Subtitle on Pickup")]
    [TextArea(2, 4)]
    public string subtitleText = "";
    public float displayDuration = 4f;
    public float delayBeforeShow = 0.5f;

    [Header("Debug")]
    [Tooltip("Logs why this item's subtitle does or does not show when the item is picked up. Turn this off after diagnosing the issue.")]
    public bool logPickupSubtitleDebug = true;

    [SerializeField, HideInInspector] private string subtitleId;
    private bool triggered = false;

    void OnValidate()
    {
        // Give this item a stable ID in the editor so its subtitle can be saved.
        EnsureSubtitleId();
    }

    private void EnsureSubtitleId()
    {
        if (string.IsNullOrEmpty(subtitleId))
            subtitleId = gameObject.scene.path + ":" + transform.GetSiblingIndex() + ":" + gameObject.name;
    }

    public string GetSubtitleId()
    {
        EnsureSubtitleId();
        return subtitleId;
    }

    public bool HasTriggered() => triggered;

    public void RestoreTriggeredState(bool wasTriggered)
    {
        triggered = wasTriggered;
    }

    // Call this from PickupItem when item is picked up.
    public void OnPickedUp()
    {
        string id = GetSubtitleId();
        SaveSystem saveSystem = SaveSystem.Instance;
        bool isSavedAsTriggered = saveSystem != null && saveSystem.HasSubtitleTriggered(id);

        if (logPickupSubtitleDebug)
        {
            string result = triggered
                ? "Subtitle blocked: this component already triggered during this play session."
                : isSavedAsTriggered
                    ? "Subtitle blocked: this subtitle ID is already saved as triggered."
                    : "Subtitle will show now: this is a new subtitle ID.";
            Debug.Log("[Item Subtitle Debug] Item='" + gameObject.name + "', Id='" + id +
                      "', InMemoryTriggered=" + triggered + ", SavedTriggered=" + isSavedAsTriggered +
                      ", SaveSystemAvailable=" + (saveSystem != null) + ". " + result, this);
        }

        if (triggered) return;

        if (isSavedAsTriggered)
        {
            triggered = true;
            return;
        }

        triggered = true;
        if (saveSystem != null)
        {
            saveSystem.MarkSubtitleTriggered(id);
            if (logPickupSubtitleDebug)
                Debug.Log("[Item Subtitle Debug] Subtitle record write result for Id='" + id +
                          "': SavedTriggered=" + saveSystem.HasSubtitleTriggered(id) + ".", this);
        }
        StartCoroutine(ShowSubtitle());
    }

    System.Collections.IEnumerator ShowSubtitle()
    {
        yield return new WaitForSeconds(delayBeforeShow);
        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowSubtitle(subtitleText, displayDuration);
    }
}
