using UnityEngine;

[RequireComponent(typeof(PickupItem))]
public class BananaUse : MonoBehaviour
{
    [Header("Eating")]
    [Min(0f)] public float healingAmount = 20f;

    private PickupItem pickup;
    private PlayerHealth playerHealth;
    private bool consumed;
    private string persistentId;
    private bool hasBeenPickedUp;
    private float uncollectedSecondsRemaining = 30f;

    internal bool HasBeenPickedUp => hasBeenPickedUp;
    internal float UncollectedSecondsRemaining => uncollectedSecondsRemaining;

    internal void MarkPickedUp()
    {
        hasBeenPickedUp = true;
    }

    internal void RestoreCollectionState(bool pickedUp, float secondsRemaining)
    {
        hasBeenPickedUp = pickedUp;
        uncollectedSecondsRemaining = Mathf.Clamp(secondsRemaining, 0f, 30f);
    }

    internal bool AdvanceUncollectedTimer(float deltaTime)
    {
        if (consumed || hasBeenPickedUp)
            return false;
        uncollectedSecondsRemaining = Mathf.Max(0f, uncollectedSecondsRemaining - Mathf.Max(0f, deltaTime));
        return uncollectedSecondsRemaining <= 0f;
    }

    internal bool IsConsumed => consumed;
    internal string PersistentId
    {
        get
        {
            if (string.IsNullOrEmpty(persistentId))
                persistentId = System.Guid.NewGuid().ToString("N");
            return persistentId;
        }
    }

    internal void RestorePersistentId(string id)
    {
        persistentId = id;
    }

    internal void RetireForRestore()
    {
        // Destroy is deferred until end-of-frame. Exclude the replaced object
        // immediately if another save happens during that same frame.
        consumed = true;
    }

    private void Awake()
    {
        // Inventory icons clone the prefab too, but are never edible world fruit.
        // Mark before registration so previews cannot affect counts or save snapshots.
        if (InventoryItemPreview.IsCreatingPreview)
        {
            consumed = true;
            return;
        }
        pickup = GetComponent<PickupItem>();
        BananaHarvestCycle.RegisterFruit(this);
    }

    private void OnDestroy()
    {
        // Removed fruit must not permanently lock every tree. Picking up,
        // hiding, or dropping fruit does not remove it from the shared supply.
        BananaHarvestCycle.RemoveFruit(this);
    }

    private bool CanUseBanana()
    {
        if (consumed || pickup == null || !pickup.isPickedUp || !pickup.isHeld ||
            Inventory.Instance == null || !Inventory.Instance.GetItems().Contains(gameObject))
            return false;

        if (Time.timeScale <= 0f || PlayerController.IsReadingDocument || AIEncounterGate.IsActive ||
            (PauseMenu.Instance != null && PauseMenu.Instance.isPaused) ||
            Cursor.lockState != CursorLockMode.Locked)
            return false;

        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = FindObjectOfType<PlayerHealth>();
        }

        return playerHealth != null && !playerHealth.isDead;
    }

    private void Update()
    {
        // Successful collection permanently cancels expiry, including later drops.
        if (pickup != null && pickup.isPickedUp)
            MarkPickedUp();
        if (AdvanceUncollectedTimer(Time.deltaTime))
        {
            consumed = true;
            BananaHarvestCycle.RemoveFruit(this);
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        // Match the existing battery item's left-click use control.
        if (!CanUseBanana() || !Input.GetMouseButtonDown(0))
            return;
        if (healingAmount <= 0f || playerHealth.currentHealth >= playerHealth.maxHealth)
            return;

        consumed = true;
        playerHealth.Heal(healingAmount);
        BananaHarvestCycle.RemoveFruit(this);
        Inventory.Instance.RemoveAndDestroy(gameObject);
    }

    private void OnGUI()
    {
        if (!CanUseBanana())
            return;

        string message = playerHealth.currentHealth >= playerHealth.maxHealth
            ? "Health is full"
            : $"Left Click to eat Banana (+{healingAmount:0.#} HP)";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18
        };
        Rect rect = new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f + 80f, 500f, 30f);
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, style);
        style.normal.textColor = Color.yellow;
        GUI.Label(rect, message, style);
    }
}
