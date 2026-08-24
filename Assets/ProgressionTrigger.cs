using UnityEngine;
using UnityEngine.Events;

public class ProgressionTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string triggerName = "New Area";
    public int progressPoints = 5;
    public bool triggerOnce = true;
    public string playerTag = "Player";

    [Header("Events")]
    public UnityEvent OnTriggerActivated;

    private bool hasTriggered = false;
    private Collider triggerCollider;

    public delegate void ProgressionEvent(int points);
    public event ProgressionEvent OnProgressionTriggered;

    void Start()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }

        if (triggerOnce && PlayerPrefs.GetInt(GetSavedStateKey(), 0) == 1)
        {
            hasTriggered = true;
            triggerCollider.enabled = false;
        }

        RegisterSavedStateKey();
    }

    void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            return;
        }
        TriggerProgress();
    }

    void TriggerProgress()
    {

        // ── Check if ProgressionSystem exists ──
        if (ProgressionSystem.Instance == null)
        {
            return;
        }
        else
        {
        }

        // ── Call AddProgress ──
        ProgressionSystem.Instance.AddProgress(progressPoints);

        // ── Fire the event for any listeners ──
        OnProgressionTriggered?.Invoke(progressPoints);

        // ── Fire Unity Event ──
        OnTriggerActivated?.Invoke();

        // ── Disable the collider if trigger once ──
        if (triggerOnce && triggerCollider != null)
        {
            hasTriggered = true;
            PlayerPrefs.SetInt(GetSavedStateKey(), 1);
            PlayerPrefs.Save();
            triggerCollider.enabled = false;
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        PlayerPrefs.DeleteKey(GetSavedStateKey());
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    string GetSavedStateKey()
    {
        string difficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        return $"ProgressionTrigger_{difficulty}_{triggerName}";
    }

    void RegisterSavedStateKey()
    {
        string keys = PlayerPrefs.GetString("ProgressionTriggerKeys", "");
        string key = triggerName;

        if (string.IsNullOrEmpty(keys))
            keys = key;
        else if (!System.Array.Exists(keys.Split('|'), entry => entry == key))
            keys += "|" + key;

        PlayerPrefs.SetString("ProgressionTriggerKeys", keys);
        PlayerPrefs.Save();
    }

    public static void ClearSavedStates()
    {
        string keys = PlayerPrefs.GetString("ProgressionTriggerKeys", "");

        if (!string.IsNullOrEmpty(keys))
        {
            foreach (string key in keys.Split('|'))
            {
                PlayerPrefs.DeleteKey("ProgressionTrigger_Normal_" + key);
                PlayerPrefs.DeleteKey("ProgressionTrigger_Hard_" + key);
            }
        }

        PlayerPrefs.DeleteKey("ProgressionTriggerKeys");
        PlayerPrefs.Save();
    }

    void OnDrawGizmosSelected()
    {
        if (triggerCollider == null) return;

        Gizmos.color = Color.green;
        if (triggerCollider is BoxCollider box)
        {
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
        }
        else if (triggerCollider is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}