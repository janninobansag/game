// Q&A panel, answers, feedback, and player-lock behavior for Tikbalang.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class TikbalangAI
{
    private void FindAndPrepareQnAPanel()
    {
        if (qnaPanel == null)
        {
            foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate != null && candidate.name == "QnAPanel" && candidate.gameObject.scene.IsValid())
                {
                    qnaPanel = candidate.gameObject;
                    break;
                }
            }
        }

        if (qnaPanel == null)
        {
            return;
        }

        qnaPanel.SetActive(false);
        WireAnswerButton("OptionA", 0);
        WireAnswerButton("OptionB", 1);
        WireAnswerButton("OptionC", 2);
        WireAnswerButton("OptionD", 3);
    }

    // Displays a random question, waits for an answer or timeout, then applies damage if wrong.
    private IEnumerator ShowJumpscareQuestion(bool unlockPlayerAfterQuestion = true)
    {
        if (qnaPanel == null)
            yield break;

        if (questions == null || questions.Length == 0)
        {
            questions = new[]
            {
                new QnAEntry
                {
                    question = "What Philippine mythical creature is half-human and half-horse?",
                    options = new[] { "Tikbalang", "Aswang", "Kapre", "Manananggal" },
                    correctAnswerIndex = 0
                }
            };
        }

        currentQuestion = questions[Random.Range(0, questions.Length)];
        if (currentQuestion == null || currentQuestion.options == null || currentQuestion.options.Length < 4)
        {
            yield break;
        }

        SetPlayerQuestionLock(true);
        PopulateQuestionUI();
        qnaPanel.SetActive(true);
        isQuestionPanelOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetAnswerButtonsInteractable(true);

        waitingForAnswer = true;
        answerReceived = false;
        float elapsed = 0f;
        while (waitingForAnswer && elapsed < questionDisplayTime)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            elapsed += Time.unscaledDeltaTime;
            SetText("TimerText", string.Format(GameplayLocalization.TranslateSubtitle("Time: {0:F1}s"), Mathf.Max(0f, questionDisplayTime - elapsed)));
            yield return null;
        }

        bool correct = answerReceived && currentQuestion != null && selectedAnswerIndex == currentQuestion.correctAnswerIndex;
        if (correct)
        {
            SetFeedback(GameplayLocalization.TranslateSubtitle("Correct!"), Color.green);
        }
        else
        {
            SetFeedback(GameplayLocalization.TranslateSubtitle("Wrong!"), Color.red);
        }

        // Keep the feedback visible first. Applying damage afterward lets the shared
        // PlayerHealth blood/vignette effect remain visible as the Q&A panel closes.
        yield return new WaitForSecondsRealtime(0.8f);

        if (!correct)
        {
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health != null)
                health.TakeDamage(wrongAnswerDamage);

            yield return new WaitForSecondsRealtime(0.2f);
        }
        isQuestionPanelOpen = false;
        qnaPanel.SetActive(false);
        SetPlayerQuestionLock(false);
    }

    private int selectedAnswerIndex = -1;

    // Finds an answer button and connects it to the corresponding answer index.
    private void WireAnswerButton(string buttonName, int answerIndex)
    {
        Transform option = qnaPanel.transform.Find(buttonName);
        Button button = option != null ? option.GetComponentInChildren<Button>(true) : null;
        if (button == null) return;

        button.onClick.AddListener(() => SelectAnswer(answerIndex));
        qnaButtons.Add(button);
    }

    // Records the selected answer and disables the answer buttons.
    private void SelectAnswer(int answerIndex)
    {
        if (!waitingForAnswer || answerReceived) return;
        selectedAnswerIndex = answerIndex;
        answerReceived = true;
        waitingForAnswer = false;
        SetAnswerButtonsInteractable(false);
    }

    // Fills the question, answer choices, and feedback text in the Q and A panel.
    private void PopulateQuestionUI()
    {
        selectedAnswerIndex = -1;
        SetText("QuestionText", GameplayLocalization.TranslateSubtitle(currentQuestion.question));
        SetText("OptionA/Text (TMP)", "A. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[0]));
        SetText("OptionB/Text (TMP)", "B. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[1]));
        SetText("OptionC/Text (TMP)", "C. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[2]));
        SetText("OptionD/Text (TMP)", "D. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[3]));
        SetFeedback(string.Empty, Color.white);
    }

    // Sets a TextMeshPro label beneath the Q and A panel using its transform path.
    private void SetText(string path, string value)
    {
        Transform textTransform = qnaPanel.transform.Find(path);
        TextMeshProUGUI text = textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
        if (text != null) text.text = value;
    }

    // Updates the Q and A feedback message and its display color.
    private void SetFeedback(string message, Color color)
    {
        Transform feedbackTransform = qnaPanel.transform.Find("FeedbackText");
        TextMeshProUGUI feedback = feedbackTransform != null ? feedbackTransform.GetComponent<TextMeshProUGUI>() : null;
        if (feedback != null)
        {
            feedback.text = message;
            feedback.color = color;
        }
    }

    // Enables or disables every answer button connected to the Q and A panel.
    private void SetAnswerButtonsInteractable(bool value)
    {
        foreach (Button button in qnaButtons)
            if (button != null) button.interactable = value;
    }

    // Disables player controls during Q and A and restores them when the lock is released.
    private void SetPlayerQuestionLock(bool locked)
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<PlayerController>();

        if (playerController == null) return;

        if (locked)
        {
            if (playerController.enabled)
                wasPlayerControllerEnabled = true;
            playerController.enabled = false;
        }
        else if (wasPlayerControllerEnabled)
        {
            playerController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    // Resumes Tikbalang NavMesh chase toward the player after an encounter.
}
