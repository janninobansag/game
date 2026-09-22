using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Objective Settings")]
    public string objectiveText = "Search the guard house.";
    public float displayDuration = 4f;
    public bool triggerOnce = true;
    public string playerTag = "Player";

    [Tooltip("Optional stable ID for this trigger. Leave empty to use its scene hierarchy path.")]
    public string saveId;

    [Header("UI References")]
    public GameObject objectivePanel;        // The panel that holds the objective text
    public TextMeshProUGUI objectiveLabel;   // The text component for objective

    [Header("Animation Settings")]
    public float fadeSpeed = 3f;
    public float slideDistance = 50f;

    private bool hasTriggered = false;
    private bool isShowing = false;
    private float currentAlpha = 0f;
    private float displayTimer = 0f;
    private Vector3 originalPosition;
    private RectTransform panelRect;

    void Start()
    {
        RegisterSavedStateKey();

        // A saved game must not replay objectives that were already shown.
        if (triggerOnce && PlayerPrefs.GetInt(GetSavedStateKey(), 0) == 1)
            hasTriggered = true;

        if (objectivePanel != null)
        {
            panelRect = objectivePanel.GetComponent<RectTransform>();
            originalPosition = panelRect.localPosition;
            objectivePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isShowing) return;

        // Countdown timer
        displayTimer -= Time.deltaTime;

        if (displayTimer > 1f)
        {
            // Fade in
            currentAlpha = Mathf.Lerp(currentAlpha, 1f, Time.deltaTime * fadeSpeed);
            if (panelRect != null)
            {
                panelRect.localPosition = Vector3.Lerp(
                    panelRect.localPosition,
                    originalPosition,
                    Time.deltaTime * fadeSpeed
                );
            }
        }
        else if (displayTimer > 0f)
        {
            // Hold
            currentAlpha = 1f;
        }
        else
        {
            // Fade out and hide
            currentAlpha = Mathf.Lerp(currentAlpha, 0f, Time.deltaTime * fadeSpeed);
            
            if (currentAlpha <= 0.05f)
            {
                isShowing = false;
                if (objectivePanel != null)
                    objectivePanel.SetActive(false);
                currentAlpha = 0f;
            }
        }

        // Apply alpha to panel
        if (objectivePanel != null && objectivePanel.activeSelf)
        {
            CanvasGroup canvasGroup = objectivePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = currentAlpha;
            }
        }
    }

    public void TriggerObjective()
    {
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;

        if (triggerOnce)
        {
            PlayerPrefs.SetInt(GetSavedStateKey(), 1);
            PlayerPrefs.Save();
        }

        isShowing = true;
        displayTimer = displayDuration;
        currentAlpha = 0f;

        if (objectivePanel != null)
        {
            objectivePanel.SetActive(true);
            
            // Reset position for slide effect
            if (panelRect != null)
            {
                Vector3 startPos = originalPosition;
                startPos.y += slideDistance;
                panelRect.localPosition = startPos;
            }

            // Set text
            if (objectiveLabel != null)
            {
                objectiveLabel.text = GameplayLocalization.TranslateObjective(objectiveText);
            }

            // Ensure canvas group exists
            CanvasGroup canvasGroup = objectivePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = objectivePanel.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        TriggerObjective();
    }
    private string GetSavedStateKey()
    {
        string difficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        return $"ObjectiveTrigger_{difficulty}_{GetTriggerId()}";
    }

    private string GetTriggerId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
            return saveId.Trim();

        // Stable unless this trigger is moved or renamed in the scene hierarchy.
        string path = SceneManager.GetActiveScene().name;
        Transform current = transform;
        while (current != null)
        {
            path = current.name + "_" + current.GetSiblingIndex() + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void RegisterSavedStateKey()
    {
        const string registryKey = "ObjectiveTriggerKeys";
        string key = GetTriggerId();
        string keys = PlayerPrefs.GetString(registryKey, "");

        if (string.IsNullOrEmpty(keys))
            keys = key;
        else if (!System.Array.Exists(keys.Split('|'), entry => entry == key))
            keys += "|" + key;

        PlayerPrefs.SetString(registryKey, keys);
        PlayerPrefs.Save();
    }

    public static void ClearSavedStates()
    {
        const string registryKey = "ObjectiveTriggerKeys";
        string keys = PlayerPrefs.GetString(registryKey, "");

        if (!string.IsNullOrEmpty(keys))
        {
            foreach (string key in keys.Split('|'))
            {
                PlayerPrefs.DeleteKey("ObjectiveTrigger_Normal_" + key);
                PlayerPrefs.DeleteKey("ObjectiveTrigger_Hard_" + key);
            }
        }

        PlayerPrefs.DeleteKey(registryKey);
        PlayerPrefs.Save();
    }
}
