using System.Collections;
using UnityEngine;

public class PlayerSubtitleTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Subtitle")]
    [TextArea(2, 4)]
    public string subtitleText = "";
    public float displayDuration = 4f;
    public float delayBeforeShow = 0f;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        StartCoroutine(ShowWithDelay());
    }

    IEnumerator ShowWithDelay()
    {
        if (delayBeforeShow > 0f)
            yield return new WaitForSeconds(delayBeforeShow);

        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowSubtitle(
                subtitleText, displayDuration);
    }

    public void ResetTrigger() => hasTriggered = false;
}
