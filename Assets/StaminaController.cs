using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class StaminaController : MonoBehaviour
{
    [Header("Hard Mode Stamina")]
    [Min(1f)] public float maxStamina = 100f;
    [Min(0f)] public float sprintDrainPerSecond = 12.5f;
    [Min(0f)] public float recoveryDelay = 0.5f;
    [Min(0f)] public float recoveryPerSecond = 20f;

    [Header("UI")]
    [Tooltip("Assign the StaminaBar Slider from the Chapter 2 Canvas.")]
    public Slider staminaBar;

    private float currentStamina;
    private float recoveryTimer;

    public float CurrentStamina => currentStamina;
    public bool IsHardMode => PlayerPrefs.GetString("GameDifficulty", "Normal") == "Hard";
    public bool CanSprint => !IsHardMode || currentStamina > 0.01f;

    void Awake()
    {
        currentStamina = maxStamina;
    }

    void Start()
    {
        RefreshUI();
    }

    public void UpdateStamina(bool isSprinting)
    {
        if (!IsHardMode)
        {
            currentStamina = maxStamina;
            recoveryTimer = 0f;
            RefreshUI();
            return;
        }

        if (isSprinting && currentStamina > 0f)
        {
            currentStamina = Mathf.Max(0f, currentStamina - sprintDrainPerSecond * Time.deltaTime);
            recoveryTimer = 0f;
        }
        else
        {
            recoveryTimer += Time.deltaTime;
            if (recoveryTimer >= recoveryDelay)
                currentStamina = Mathf.Min(maxStamina, currentStamina + recoveryPerSecond * Time.deltaTime);
        }

        RefreshUI();
    }

    public void RestoreStamina(float savedStamina)
    {
        currentStamina = Mathf.Clamp(savedStamina, 0f, maxStamina);
        recoveryTimer = 0f;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (staminaBar == null)
            return;

        staminaBar.gameObject.SetActive(IsHardMode);
        if (!IsHardMode)
            return;

        staminaBar.minValue = 0f;
        staminaBar.maxValue = maxStamina;
        staminaBar.value = currentStamina;
    }
}
