using UnityEngine;
using System.Collections;

public class PickupTriggerSpawner : MonoBehaviour
{
    [Header("Watch This Item")]
    public PickupItem watchedItem;

    [Header("Object to Activate")]
    public GameObject objectToActivate;

    [Header("Objective Settings")]
    public bool setNewObjective = true;
    public ObjectiveTrigger objectiveTrigger;

    [Header("Settings")]
    public float checkInterval = 0.2f;
    public float delayBeforeActivate = 0f;

    [Header("Save Settings")]
    public bool persistThroughSaves = true;
    public string saveKey = "CandleOneRevealed";

    private bool activated = false;
    private float checkTimer = 0f;

    void Start()
    {

        if (persistThroughSaves && PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            activated = true;
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
                ForceMarkAsRevealed();
            }
            return;
        }

        if (objectToActivate != null)
            objectToActivate.SetActive(false);
    }

    void Update()
    {
        if (activated) return;
        if (watchedItem == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval) return;
        checkTimer = 0f;

        if (watchedItem.isPickedUp)
        {
            activated = true;

            if (persistThroughSaves)
            {
                PlayerPrefs.SetInt(saveKey, 1);
                PlayerPrefs.Save();
            }

            if (delayBeforeActivate > 0f)
                StartCoroutine(ActivateAfterDelay());
            else
                Activate();
        }
    }

    IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeActivate);
        Activate();
    }

    void Activate()
    {
        
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            ForceMarkAsRevealed();
        }
        else
        {
        }

        if (setNewObjective && objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();
    }

    void ForceMarkAsRevealed()
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        if (objectToActivate == null)
        {
            return;
        }

        string cleanName = objectToActivate.name.Replace("(Clone)", "");
        
        // ── FIX: Check if this is a ritual item ──
        bool isRitualItem = cleanName == "LargeCandle (1)" || 
                            cleanName == "Cross" || 
                            cleanName == "Bible";
        
        if (isRitualItem)
        {
            SaveSystem.Instance.MarkRitualItemAsRevealed(cleanName);
        }
        else
        {
        }
    }

    public void ResetActivation()
    {
        activated = false;
        if (objectToActivate != null)
            objectToActivate.SetActive(false);
        
        if (persistThroughSaves)
        {
            PlayerPrefs.SetInt(saveKey, 0);
            PlayerPrefs.Save();
        }
    }
}