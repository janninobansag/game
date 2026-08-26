using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[ExecuteAlways]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("UI References")]
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public GameObject damageVignette;

    [Header("Health Bar UI")]
    [Tooltip("Created as a rectangular Canvas Slider when no Slider is assigned.")]
    public Slider healthBar;
    [SerializeField] private GameObject healthBarPanel;
    public Vector2 healthBarSize = new Vector2(260f, 22f);
    public Vector2 healthBarPosition = new Vector2(171f, 165f);
    [Header("Damage Effects")]
    public float damageFlashDuration = 0.3f;
    public float invincibilityDuration = 1f;
    public Color damageColor = new Color(0.8f, 0f, 0f, 0.5f);
    [Range(0f, 1f)] public float bloodEffectIntensity = 0.9f;

    [Header("Death Settings")]
    public float deathDelay = 2f;
    public GameObject deathScreen;

    public bool isDead = false;
    private bool isInvincible = false;
    private float damageFlashTimer = 0f;
    private Color originalVignetteColor;
    private Image vignetteImage;
    private Material bloodEffectMaterial;
    private bool hasBloodShader;

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureHealthBarSlider();
            EnsureDamageOverlayInCanvas();
        }
    }

    void Start()
    {
        EnsureHealthBarSlider();
        currentHealth = maxHealth;
        UpdateHealthUI();

        SetupReliableBloodOverlay();

        // Get vignette image if exists
        if (damageVignette != null)
        {
            vignetteImage = damageVignette.GetComponent<Image>();
            if (vignetteImage != null)
            {
                originalVignetteColor = vignetteImage.color;
                damageVignette.SetActive(false);
            }
        }

        if (deathScreen != null)
            deathScreen.SetActive(false);
    }

    void Update()
    {
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (hasBloodShader && bloodEffectMaterial != null)
            {
                float fade = Mathf.Clamp01(damageFlashTimer / Mathf.Max(0.01f, damageFlashDuration));
                bloodEffectMaterial.SetFloat("_Intensity", fade * bloodEffectIntensity);
            }

            if (damageFlashTimer <= 0f && damageVignette != null)
                damageVignette.SetActive(false);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Show damage effects
        ShowDamageEffects();

        // Update UI
        UpdateHealthUI();


        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            // Start invincibility frames
            StartCoroutine(InvincibilityFrames());
        }
    }

    // Used by Varen's jumpscare. The jumpscare system decides whether to
    // return to a checkpoint or restart the chapter after its sequence ends.
    public void TakeDamageForEnemyCatch(float damage)
    {
        if (isDead || isInvincible) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        ShowDamageEffects();
        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            isDead = true;
            return;
        }

        StartCoroutine(InvincibilityFrames());
    }
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();

    }

    void ShowDamageEffects()
    {
        ShowBloodDamageOverlay(damageFlashDuration);
    }

    public void ShowBloodDamageOverlay(float duration)
    {
        damageFlashTimer = Mathf.Max(damageFlashTimer, Mathf.Max(0.05f, duration));
        if (damageVignette == null) return;

        damageVignette.transform.SetAsLastSibling();
        damageVignette.SetActive(true);
        if (vignetteImage == null) return;

        if (hasBloodShader && bloodEffectMaterial != null)
        {
            vignetteImage.color = Color.white;
            bloodEffectMaterial.SetFloat("_Intensity", bloodEffectIntensity);
        }
        else
        {
            vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, Mathf.Max(0.35f, damageColor.a));
        }
    }

    private void EnsureDamageOverlayInCanvas()
    {
        if (damageVignette == null) return;

        Image image = damageVignette.GetComponent<Image>();
        if (image == null) return;

        Canvas canvas = image.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        RectTransform rect = image.rectTransform;
        if (rect.parent != canvas.transform)
        {
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        image.raycastTarget = false;
    }
    private void SetupReliableBloodOverlay()
    {
        if (damageVignette == null) return;

        vignetteImage = damageVignette.GetComponent<Image>();
        if (vignetteImage == null) return;

        EnsureDamageOverlayInCanvas();
        vignetteImage.raycastTarget = false;

        Canvas overlayCanvas = vignetteImage.GetComponent<Canvas>();
        if (overlayCanvas == null) overlayCanvas = vignetteImage.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 900;

        Material template = Resources.Load<Material>("UI/ReliableBloodDamage");
        if (template == null) return;

        bloodEffectMaterial = new Material(template);
        vignetteImage.material = bloodEffectMaterial;
        hasBloodShader = true;
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void UpdateHealthUI()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        float healthPercent = currentHealth / maxHealth;
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }


        // Update health bar
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = healthPercent;

            // Change color based on health
            healthBarFill.color = Color.Lerp(new Color(0.92f, 0.12f, 0.12f), new Color(0.18f, 0.90f, 0.30f), healthPercent);
        }

        // Update health text
        if (healthText != null)
        {
            healthText.text = $"HP  {Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }
    }

    public void RestoreHealth(float savedCurrentHealth, float savedMaxHealth)
    {
        // Saves made before HP support contain zero values. Keep the Inspector maximum for those saves.
        if (savedMaxHealth > 0f)
            maxHealth = savedMaxHealth;

        currentHealth = savedMaxHealth > 0f ? savedCurrentHealth : maxHealth;
        isDead = false;
        isInvincible = false;
        UpdateHealthUI();
    }

    void Die()
    {
        isDead = true;

        if (deathScreen != null)
            deathScreen.SetActive(true);

        // Disable player controller
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        // Show death screen effect
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathDelay);

        // Respawn at checkpoint or reload scene
        CheckpointTrigger checkpoint = FindObjectOfType<CheckpointTrigger>();
        if (checkpoint != null && checkpoint.HasCheckpoint())
        {
            checkpoint.Respawn();
            // Reset health after respawn
            currentHealth = maxHealth;
            isDead = false;
            UpdateHealthUI();
            if (deathScreen != null)
                deathScreen.SetActive(false);
        }
        else
        {
            // Reload scene if no checkpoint
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    private void EnsureHealthBarSlider()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Slider existingCanvasHealthBar = FindCanvasHealthBar(canvas);
        if (existingCanvasHealthBar != null)
        {
            healthBar = existingCanvasHealthBar;
            healthBarPanel = healthBar.gameObject;
            if (healthBar.fillRect != null)
                healthBarFill = healthBar.fillRect.GetComponent<Image>();
            healthText = healthBar.GetComponentInChildren<TextMeshProUGUI>(true);
            return;
        }

        if (healthBar != null && healthBar.gameObject.name == "HealthBar UI")
        {
            healthBarPanel = healthBar.gameObject;
            return;
        }

        if (healthBar != null)
        {
            GameObject oldGeneratedBar = healthBar.gameObject;
            healthBar = null;
            if (oldGeneratedBar.name == "HealthBarSlider")
            {
                if (Application.isPlaying) Destroy(oldGeneratedBar);
                else DestroyImmediate(oldGeneratedBar);
            }
        }

        RemoveLegacyHealthBar(canvas);
        healthText = null;

        healthBarPanel = new GameObject("HealthBar UI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        healthBarPanel.layer = 5;
        healthBarPanel.transform.SetParent(canvas.transform, false);

        RectTransform barRect = healthBarPanel.GetComponent<RectTransform>();
        barRect.anchorMin = Vector2.zero;
        barRect.anchorMax = Vector2.zero;
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = healthBarPosition;
        barRect.sizeDelta = healthBarSize;

        Image background = healthBarPanel.GetComponent<Image>();
        background.raycastTarget = false;
        background.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

        healthBar = healthBarPanel.GetComponent<Slider>();
        healthBar.transition = Selectable.Transition.None;
        healthBar.interactable = false;
        healthBar.targetGraphic = background;

        GameObject fillObject = CreateUiObject("Health Fill", healthBarPanel.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        healthBarFill = fillObject.AddComponent<Image>();
        healthBarFill.raycastTarget = false;
        healthBar.fillRect = fillRect;

        GameObject labelObject = CreateUiObject("Health Text", healthBarPanel.transform);
        healthText = labelObject.AddComponent<TextMeshProUGUI>();
        healthText.font = TMP_Settings.defaultFontAsset;
        healthText.fontSize = 13f;
        healthText.fontStyle = FontStyles.Bold;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.raycastTarget = false;
        healthText.color = Color.white;
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        healthText.text = "HP  100 / 100";
    }

    private static Slider FindCanvasHealthBar(Canvas canvas)
    {
        foreach (Slider slider in canvas.GetComponentsInChildren<Slider>(true))
            if (slider.gameObject.name == "HealthBar UI")
                return slider;

        return null;
    }

    private void RemoveLegacyHealthBar(Canvas canvas)
    {
        if (healthBarFill == null || healthBarFill.gameObject.name != "HealthBar") return;

        GameObject legacyBar = healthBarFill.gameObject;
        if (damageVignette != null && damageVignette.transform.IsChildOf(legacyBar.transform))
            damageVignette.transform.SetParent(canvas.transform, false);

        healthBarFill = null;
        if (Application.isPlaying) Destroy(legacyBar);
        else DestroyImmediate(legacyBar);
    }

    [ContextMenu("Rebuild HealthBar UI in Canvas")]
    public void RebuildHealthBarUiInCanvas()
    {
        if (healthBarPanel != null)
        {
            if (Application.isPlaying) Destroy(healthBarPanel);
            else DestroyImmediate(healthBarPanel);
        }

        healthBarPanel = null;
        healthBar = null;
        healthBarFill = null;
        healthText = null;
        EnsureHealthBarSlider();
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
    public bool IsDead() => isDead;
    public float GetHealthPercent() => currentHealth / maxHealth;
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();
        if (deathScreen != null)
            deathScreen.SetActive(false);
    }
}