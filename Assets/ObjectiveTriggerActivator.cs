using UnityEngine;

public class ObjectiveTriggerActivator : MonoBehaviour
{
    [Header("Target Objective Trigger")]
    public GameObject objectiveTriggerToActivate; // Drag your ObjectiveTrigger here

    [Header("Item Settings")]
    public string itemName = "Key"; // The name of the item that activates this

    [Header("Settings")]
    public bool disableAfterActivation = true;

    private bool isActivated = false;

    void Start()
    {
        // Disable the objective trigger at start
        if (objectiveTriggerToActivate != null)
        {
            objectiveTriggerToActivate.SetActive(false);
        }
    }

    // ── CALL THIS FROM YOUR ITEM PICKUP SCRIPT ──
    public void ActivateObjectiveTrigger()
    {
        if (isActivated) return;

        isActivated = true;

        if (objectiveTriggerToActivate != null)
        {
            objectiveTriggerToActivate.SetActive(true);
        }

        // Optional: Disable this script so it can't be called again
        if (disableAfterActivation)
        {
            enabled = false;
        }
    }

    // ── CHECK IF THE ITEM MATCHES ──
    public bool MatchesItem(string itemToCheck)
    {
        return itemName.ToLower() == itemToCheck.ToLower();
    }
}