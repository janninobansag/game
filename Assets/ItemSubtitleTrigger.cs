using UnityEngine;

public class ItemSubtitleTrigger : MonoBehaviour
{
    [Header("Subtitle on Pickup")]
    [TextArea(2, 4)]
    public string subtitleText = "";
    public float displayDuration = 4f;
    public float delayBeforeShow = 0.5f;

    private bool triggered = false;

    // Call this from PickupItem when item is picked up
    public void OnPickedUp()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(ShowSubtitle());
    }

    System.Collections.IEnumerator ShowSubtitle()
    {
        yield return new WaitForSeconds(delayBeforeShow);
        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowSubtitle(
                subtitleText, displayDuration);
    }
}