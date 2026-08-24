using UnityEngine;

public class ItemSubtitleTrigger : MonoBehaviour
{
    [Header("Subtitle on Pickup")]
    [TextArea(2, 4)]
    public string subtitleText = "";
    public float displayDuration = 4f;
    public float delayBeforeShow = 0.5f;

    [SerializeField, HideInInspector] private string subtitleId;
    private bool triggered = false;

    void OnValidate()
    {
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
        if (triggered) return;

        string id = GetSubtitleId();
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSubtitleTriggered(id))
        {
            triggered = true;
            return;
        }

        triggered = true;
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkSubtitleTriggered(id);
        StartCoroutine(ShowSubtitle());
    }

    System.Collections.IEnumerator ShowSubtitle()
    {
        yield return new WaitForSeconds(delayBeforeShow);
        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowSubtitle(subtitleText, displayDuration);
    }
}