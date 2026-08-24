using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("UI References")]
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public GameObject damageVignette;

    [Header("Damage Effects")]
    public float damageFlashDuration = 0.3f;
    public float invincibilityDuration = 1f;
    public Color damageColor = new Color(0.8f, 0f, 0f, 0.5f);

    [Header("Death Settings")]
    public float deathDelay = 2f;
    public GameObject deathScreen;

    public bool isDead = false;
    private bool isInvincible = false;
    private float damageFlashTimer = 0f;
    private Color originalVignetteColor;
    private Image vignetteImage;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

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
        // Handle damage flash timer
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0f)
            {
                if (damageVignette != null)
                    damageVignette.SetActive(false);
            }
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

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();

    }

    void ShowDamageEffects()
    {
        // Show damage flash
        damageFlashTimer = damageFlashDuration;
        if (damageVignette != null)
        {
            damageVignette.SetActive(true);
            if (vignetteImage != null)
            {
                vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, damageColor.a);
            }
        }
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void UpdateHealthUI()
    {
        float healthPercent = currentHealth / maxHealth;

        // Update health bar
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = healthPercent;

            // Change color based on health
            if (healthPercent > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }

        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }
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