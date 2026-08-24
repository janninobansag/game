using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// NarratorController - Displays narrator text on a black screen, then fades out.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas (UI > Canvas) - set Render Mode to "Screen Space - Overlay"
/// 2. Create a Panel inside the Canvas - name it "NarratorPanel"
///    - Set its color to black (R:0, G:0, B:0, A:255)
///    - Stretch it to fill the entire canvas (anchor: stretch-stretch)
/// 3. Create a TextMeshPro Text inside NarratorPanel - name it "NarratorText"
///    - Center it, set font size ~36, color white
/// 4. Create an empty GameObject - name it "NarratorController"
/// 5. Attach this script to it
/// 6. Drag the Panel into "Narrator Panel" and the Text into "Narrator Text" in the Inspector
/// </summary>
public class NarratorController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The black background Panel")]
    public Image narratorPanel;

    [Tooltip("The TextMeshPro text component")]
    public TextMeshProUGUI narratorText;

    [Header("Narrator Settings")]
    [Tooltip("How long each character takes to appear (typewriter effect)")]
    public float typewriterSpeed = 0.05f;

    [Tooltip("How long to wait after text is fully shown before fading")]
    public float displayDuration = 2.5f;

    [Tooltip("How long the fade-out takes in seconds")]
    public float fadeDuration = 2.0f;

    [Header("Narration Lines")]
    [Tooltip("Add all your narrator lines here")]
    [TextArea(2, 5)]
    public string[] narratorLines = {
        "In a world forgotten by time...",
        "One hero rises from the darkness.",
        "Your journey begins now."
    };

    private CanvasGroup canvasGroup;
    private WaitForSeconds typewriterWait;
    private WaitForSeconds displayWait;
    private Color originalTextColor;

    void Start()
    {
        // Add a CanvasGroup to the panel for smooth fading
        canvasGroup = narratorPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = narratorPanel.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        narratorText.text = "";
        
        // Cache waits to avoid GC allocations in coroutines
        typewriterWait = new WaitForSeconds(typewriterSpeed);
        displayWait = new WaitForSeconds(displayDuration);
        originalTextColor = narratorText.color;

        StartCoroutine(PlayNarration());
    }

    IEnumerator PlayNarration()
    {
        // Play each line one by one
        foreach (string line in narratorLines)
        {
            yield return StartCoroutine(TypewriterEffect(line));
            yield return displayWait;
            yield return StartCoroutine(ClearText());
        }

        // After all lines, fade out the black overlay
        yield return StartCoroutine(FadeOut());

        // Optional: disable the narrator panel entirely after fade
        narratorPanel.gameObject.SetActive(false);
    }

    IEnumerator TypewriterEffect(string line)
    {
        narratorText.text = line;
        narratorText.maxVisibleCharacters = 0;
        
        for (int i = 0; i <= line.Length; i++)
        {
            narratorText.maxVisibleCharacters = i;
            yield return typewriterWait;
        }
    }

    IEnumerator ClearText()
    {
        // Quick fade out the text only before showing next line
        float elapsed = 0f;
        float clearDuration = 0.4f;

        while (elapsed < clearDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / clearDuration);
            narratorText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
            yield return null;
        }

        narratorText.text = "";
        narratorText.color = originalTextColor;
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Call this from anywhere to trigger narration manually.
    /// e.g.: FindObjectOfType<NarratorController>().TriggerNarration("New text here");
    /// </summary>
    public void TriggerNarration(string line)
    {
        StopAllCoroutines();
        canvasGroup.alpha = 1f;
        narratorPanel.gameObject.SetActive(true);
        StartCoroutine(SingleLineNarration(line));
    }

    IEnumerator SingleLineNarration(string line)
    {
        yield return StartCoroutine(TypewriterEffect(line));
        yield return displayWait;
        yield return StartCoroutine(FadeOut());
        narratorPanel.gameObject.SetActive(false);
    }
}
