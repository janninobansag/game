using System.Collections;
using UnityEngine;

public class PlayerSubtitleTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    [Tooltip("Kept for existing Inspector settings. Subtitles are now always shown only once per save.")]
    public bool triggerOnce = true;

    [Header("Subtitle")]
    [TextArea(2, 4)]
    public string subtitleText = "";
    public float displayDuration = 4f;
    public float delayBeforeShow = 0f;

    [SerializeField, HideInInspector] private string subtitleId;
    private bool hasTriggered = false;

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

    public bool HasTriggered() => hasTriggered;

    public void RestoreTriggeredState(bool wasTriggered)
    {
        hasTriggered = wasTriggered;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (hasTriggered) return;

        string id = GetSubtitleId();
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSubtitleTriggered(id))
        {
            hasTriggered = true;
            return;
        }

        hasTriggered = true;
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkSubtitleTriggered(id);
        StartCoroutine(ShowWithDelay());
    }

    IEnumerator ShowWithDelay()
    {
        if (delayBeforeShow > 0f)
            yield return new WaitForSeconds(delayBeforeShow);

        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowSubtitle(subtitleText, displayDuration);
    }

    public void ResetTrigger() => hasTriggered = false;
}