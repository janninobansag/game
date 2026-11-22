// PURPOSE: Tracks sprint stamina, restores it, and updates the stamina bar for the active difficulty.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerController))]
public class StaminaController : MonoBehaviour
{
    [Header("Hard Mode Stamina")]
    [Min(1f)] public float maxStamina = 100f;
    [Min(0f)] public float sprintDrainPerSecond = 12.5f;
    [Min(0f)] public float recoveryDelay = 0.5f;
    [Min(0f)] public float recoveryPerSecond = 20f;

    [Header("Exhaustion")]
    [Min(0f)] public float exhaustionStunDuration = 0.6f;
    [Range(0.01f, 1f)] public float sprintRecoveryThreshold = 0.3f;
    [Min(0f)] public float exhaustionDamage = 8f;

    [Header("UI")]
    [Tooltip("Assign the StaminaBar Slider from the Chapter 2 Canvas.")]
    public Slider staminaBar;

    private float currentStamina;
    private float recoveryTimer;
    private float exhaustionStunTimer;
    private bool isExhausted;
    private PlayerHealth playerHealth;
    private float nextStaminaBarSearchTime;
    private float lastDisplayedStamina = float.NaN;

    public float CurrentStamina => currentStamina;
    public bool IsHardMode => SceneManager.GetActiveScene().name == "chapter 2" || PlayerPrefs.GetString("GameDifficulty", "Normal") == "Hard";
    public bool IsExhaustionStunned => IsHardMode && exhaustionStunTimer > 0f;
    public bool CanSprint => !IsHardMode || (!isExhausted && !IsExhaustionStunned && currentStamina > 0.01f);

    void Awake()
    {
        currentStamina = maxStamina;
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        FindStaminaBar();
        RefreshUI();
    }

    public void UpdateStamina(bool isSprinting)
    {
        if (!IsHardMode)
        {
            currentStamina = maxStamina;
            recoveryTimer = 0f;
            exhaustionStunTimer = 0f;
            isExhausted = false;
            RefreshUI();
            return;
        }

        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        exhaustionStunTimer = Mathf.Max(0f, exhaustionStunTimer - Time.deltaTime);

        if (isSprinting && !isExhausted && currentStamina > 0f)
        {
            currentStamina = Mathf.Max(0f, currentStamina - sprintDrainPerSecond * Time.deltaTime);
            recoveryTimer = 0f;
            if (currentStamina <= 0f)
            {
                isExhausted = true;
                exhaustionStunTimer = exhaustionStunDuration;
                // Charge once on depletion, never every frame at zero or on load.
                if (playerHealth == null)
                    playerHealth = GetComponent<PlayerHealth>();
                if (playerHealth != null && exhaustionDamage > 0f)
                    playerHealth.TakeDamage(exhaustionDamage, true);
            }
        }
        // Exhaustion must recover even while Shift is held, otherwise a player
        // could remain stuck at zero instead of reaching the sprint threshold.
        else if (isHoldingShift && !isExhausted)
        {
            recoveryTimer = 0f;
        }
        else
        {
            recoveryTimer += Time.deltaTime;
            if (recoveryTimer >= recoveryDelay)
                currentStamina = Mathf.Min(maxStamina, currentStamina + recoveryPerSecond * Time.deltaTime);
        }

        if (isExhausted && currentStamina >= maxStamina * sprintRecoveryThreshold)
            isExhausted = false;

        RefreshUI();
    }

    public void RestoreStamina(float savedStamina)
    {
        currentStamina = Mathf.Clamp(savedStamina, 0f, maxStamina);
        recoveryTimer = 0f;
        // Saves contain the stamina value, so conservatively retain the sprint
        // lock below the recovery threshold after loading or respawning.
        isExhausted = currentStamina < maxStamina * sprintRecoveryThreshold;
        exhaustionStunTimer = currentStamina <= 0f ? exhaustionStunDuration : 0f;
        RefreshUI();
    }

    private void FindStaminaBar()
    {
        // Chapter 1 has no stamina bar. Avoid searching the whole scene every frame
        // while the player is in that chapter.
        if (Time.unscaledTime < nextStaminaBarSearchTime)
            return;

        nextStaminaBarSearchTime = Time.unscaledTime + 1f;
        GameObject barObject = GameObject.Find("StaminaBar");
        if (barObject != null)
        {
            staminaBar = barObject.GetComponent<Slider>();
            lastDisplayedStamina = float.NaN;
        }
    }

    private void RefreshUI()
    {
        bool isHardMode = IsHardMode;

        // Normal mode has no stamina HUD, so never search Chapter 1 for one.
        if (staminaBar == null)
        {
            if (!isHardMode)
                return;
            FindStaminaBar();
        }

        if (staminaBar == null)
            return;

        if (staminaBar.gameObject.activeSelf != isHardMode)
            staminaBar.gameObject.SetActive(isHardMode);

        if (!isHardMode)
            return;

        if (staminaBar.minValue != 0f)
            staminaBar.minValue = 0f;
        if (!Mathf.Approximately(staminaBar.maxValue, maxStamina))
            staminaBar.maxValue = maxStamina;
        if (!Mathf.Approximately(lastDisplayedStamina, currentStamina))
        {
            staminaBar.value = currentStamina;
            lastDisplayedStamina = currentStamina;
        }
    }
}
