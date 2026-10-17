// Q&A, player-lock, and answer result logic for MonsterAI_New.
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public partial class MonsterAI_New
{
    [Header("Question & Answer Settings")]
    public GameObject qnaPanel;
    public float questionDisplayTime = 30f;
    public int wrongAnswerDamage = 15;

    [Header("Q&A Questions")]
    public QnAEntry[] questions;

    private bool isQnAActive = false;
    private bool isClosingQnA = false;
    private bool isResolvingQnA = false;
    private QnAEntry currentQuestion;
    private int currentQuestionIndex = -1;
    private List<int> usedQuestionIndices = new List<int>();
    private float questionTimer = 0f;
    private bool isWaitingForAnswer = false;
    private bool hasAnswered = false;
    private CanvasGroup qnaCanvasGroup;
    private bool qnaPanelHiddenForPause = false;

    // ── Player Control References ──
    private PlayerController playerController;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private PauseMenu pauseMenu;
    private bool wasPlayerControllerEnabled = false;
    private bool isPlayerLocked = false;
    private bool isQnAComplete = false;
    private bool hasUnlockedAfterQnA = false;
    private readonly List<Button> qnaButtons = new List<Button>();
    private readonly List<UnityEngine.Events.UnityAction> qnaButtonActions = new List<UnityEngine.Events.UnityAction>();

    private void UnlockPlayer()
    {
        if (playerController != null)
        {
            if (pauseMenu == null || !pauseMenu.isPaused)
            {
                playerController.enabled = true;
                isPlayerLocked = false;
            }
            else
            {
                isPlayerLocked = false;
                hasUnlockedAfterQnA = true;
            }
        }
        else
        {
            isPlayerLocked = false;
        }
    }

    public void TriggerQnA()
    {
        if (isQnAActive || isResolvingQnA) return;
        if (questions == null || questions.Length == 0)
        {
            return;
        }

        if (!AIEncounterGate.IsOwnedBy(this) && !AIEncounterGate.TryBegin(this))
            return;

        StartCoroutine(QnASequence());
    }

    IEnumerator QnASequence()
    {
        isQnAActive = true;
        isClosingQnA = false;
        isResolvingQnA = false;
        isQnAComplete = false;
        hasUnlockedAfterQnA = false;
        currentState = State.QnA;
        isWaitingForAnswer = false;
        hasAnswered = false;
        SetQnAIdleAnimation();
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == chaseSound)
            audioSource.Stop();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;
        isPlayerLocked = true;

        LockAllPlayerControls(true);

        currentQuestionIndex = GetRandomQuestionIndex();
        if (currentQuestionIndex == -1)
        {
            usedQuestionIndices.Clear();
            currentQuestionIndex = Random.Range(0, questions.Length);
        }
        usedQuestionIndices.Add(currentQuestionIndex);
        currentQuestion = questions[currentQuestionIndex];

        if (qnaPanel != null)
        {
            RestoreQnAInput();
            qnaPanel.SetActive(true);
            qnaCanvasGroup.alpha = 0f;

            float fadeElapsed = 0f;
            while (fadeElapsed < 0.3f)
            {
                fadeElapsed += Time.deltaTime;
                qnaCanvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeElapsed / 0.3f);
                yield return null;
            }
            qnaCanvasGroup.alpha = 1f;

            UpdateQnAUI();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isWaitingForAnswer = true;
        questionTimer = 0f;

        while (isWaitingForAnswer && questionTimer < questionDisplayTime)
        {
            questionTimer += Time.deltaTime;
            UpdateQnATimer();

            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            if (playerController != null && playerController.enabled)
            {
                playerController.enabled = false;
                isPlayerLocked = true;
            }

            yield return null;
        }

        if (isWaitingForAnswer && !hasAnswered)
        {
            yield return StartCoroutine(WrongAnswerSequence());
        }

        // Answer/timeout sequences close Q&A only after the White Lady teleports.
        // Do not unlock the player here; that would allow another jumpscare first.
    }

    private void SetQnAIdleAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("jumpscare");
        animator.SetBool("walking", false);
        animator.SetBool("run", false);
    }
    void LockAllPlayerControls(bool lockControls)
    {
        if (playerController != null)
        {
            if (lockControls)
            {
                wasPlayerControllerEnabled = playerController.enabled;
                playerController.enabled = false;
                isPlayerLocked = true;
            }
            else
            {
                if (!isQnAActive)
                {
                    if (pauseMenu == null || !pauseMenu.isPaused)
                    {
                        playerController.enabled = true;
                        isPlayerLocked = false;
                    }
                    else
                    {
                        isPlayerLocked = false;
                    }
                }
                else
                {
                }
            }
        }

        if (characterController != null && lockControls)
        {
            characterController.Move(Vector3.zero);
        }

        if (playerRigidbody != null && lockControls)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (lockControls)
        {
            Input.ResetInputAxes();
        }

        if (pauseMenu != null && lockControls)
        {
            if (pauseMenu.isPaused)
            {
                pauseMenu.Resume();
            }
        }

        if (lockControls)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (pauseMenu == null || !pauseMenu.isPaused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    int GetRandomQuestionIndex()
    {
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < questions.Length; i++)
        {
            if (!usedQuestionIndices.Contains(i))
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
            return -1;

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    void UpdateQnAUI()
    {
        if (qnaPanel == null || currentQuestion == null) return;

        // ── Get TextMeshPro components from the children ──
        Transform questionText = qnaPanel.transform.Find("QuestionText");
        Transform optionAText = qnaPanel.transform.Find("OptionA/Text (TMP)");
        Transform optionBText = qnaPanel.transform.Find("OptionB/Text (TMP)");
        Transform optionCText = qnaPanel.transform.Find("OptionC/Text (TMP)");
        Transform optionDText = qnaPanel.transform.Find("OptionD/Text (TMP)");
        Transform timerText = qnaPanel.transform.Find("TimerText");
        Transform feedbackText = qnaPanel.transform.Find("FeedbackText");

        if (feedbackText != null)
        {
            var tmpro = feedbackText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = "";
        }

        if (questionText != null)
        {
            var tmpro = questionText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = GameplayLocalization.TranslateSubtitle(currentQuestion.question);
        }

        // ── Update option texts ──
        if (optionAText != null)
        {
            var tmpro = optionAText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 0)
                tmpro.text = "A. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[0]);
        }

        if (optionBText != null)
        {
            var tmpro = optionBText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 1)
                tmpro.text = "B. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[1]);
        }

        if (optionCText != null)
        {
            var tmpro = optionCText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 2)
                tmpro.text = "C. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[2]);
        }

        if (optionDText != null)
        {
            var tmpro = optionDText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 3)
                tmpro.text = "D. " + GameplayLocalization.TranslateSubtitle(currentQuestion.options[3]);
        }

        if (timerText != null)
        {
            var tmpro = timerText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = string.Format(GameplayLocalization.TranslateSubtitle("Time: {0:F1}s"), questionDisplayTime);
        }
    }

    void UpdateQnATimer()
    {
        if (qnaPanel == null) return;

        Transform timerText = qnaPanel.transform.Find("TimerText");
        if (timerText != null)
        {
            float remaining = Mathf.Max(0f, questionDisplayTime - questionTimer);
            string timerString = string.Format(GameplayLocalization.TranslateSubtitle("Time: {0:F1}s"), remaining);

            var tmpro = timerText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = timerString;
        }
    }

    void WireQnAButtons()
    {
        string[] buttonNames = { "OptionA", "OptionB", "OptionC", "OptionD" };

        for (int i = 0; i < buttonNames.Length; i++)
        {
            Transform option = qnaPanel.transform.Find(buttonNames[i]);
            Button button = option != null
                ? option.GetComponentInChildren<Button>(true)
                : null;

            if (button == null)
                continue;

            int answerIndex = i;
            UnityEngine.Events.UnityAction action = () => AnswerQnA(answerIndex);
            button.onClick.AddListener(action);
            qnaButtons.Add(button);
            qnaButtonActions.Add(action);
        }
    }

    private void RestoreQnAInput()
    {
        if (qnaCanvasGroup != null)
        {
            qnaCanvasGroup.interactable = true;
            qnaCanvasGroup.blocksRaycasts = true;
        }

        foreach (Button button in qnaButtons)
        {
            if (button != null)
                button.interactable = true;
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < qnaButtons.Count; i++)
        {
            if (qnaButtons[i] != null)
                qnaButtons[i].onClick.RemoveListener(qnaButtonActions[i]);
        }
    }

    public void AnswerQnA(int selectedIndex)
    {
        if (!isWaitingForAnswer || hasAnswered) return;
        hasAnswered = true;
        isWaitingForAnswer = false;
        isResolvingQnA = true;

        if (currentQuestion == null) return;

        bool isCorrect = (selectedIndex == currentQuestion.correctAnswerIndex);

        if (isCorrect)
        {
            StartCoroutine(CorrectAnswerSequence());
        }
        else
        {
            StartCoroutine(WrongAnswerSequence());
        }
    }

    IEnumerator CorrectAnswerSequence()
    {
        isResolvingQnA = true;
        ShowFeedback(GameplayLocalization.TranslateSubtitle("Correct!"), Color.green);
        yield return new WaitForSeconds(0.8f);

        TeleportMonsterAway();
        JumpscareSystemNEW.Instance?.ReleaseJumpscareLock();

        yield return StartCoroutine(WaitForJumpscareRelease());
        CloseQnA();
    }

    IEnumerator WrongAnswerSequence()
    {
        isResolvingQnA = true;
        ShowFeedback(GameplayLocalization.TranslateSubtitle("Wrong!"), Color.red);
        yield return new WaitForSeconds(0.8f);

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(wrongAnswerDamage);
            }
        }

        TeleportMonsterAway();
        JumpscareSystemNEW.Instance?.ReleaseJumpscareLock();

        yield return StartCoroutine(WaitForJumpscareRelease());
        CloseQnA();
    }

    private IEnumerator WaitForJumpscareRelease()
    {
        JumpscareSystemNEW jumpscareSystem = JumpscareSystemNEW.Instance;
        float releaseDeadline = Time.time + 2f;
        while (jumpscareSystem != null && jumpscareSystem.isActiveAndEnabled &&
            jumpscareSystem.IsJumpscareActive && Time.time < releaseDeadline)
            yield return null;
    }

    void ShowFeedback(string message, Color color)
    {
        if (qnaPanel == null) return;

        Transform feedbackText = qnaPanel.transform.Find("FeedbackText");
        if (feedbackText != null)
        {
            var tmpro = feedbackText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null)
            {
                tmpro.text = message;
                tmpro.color = color;
            }
        }
    }

    void CloseQnA()
    {
        if (isClosingQnA) return;
        isClosingQnA = true;

        isQnAActive = false;
        isResolvingQnA = false;
        qnaPanelHiddenForPause = false;
        isWaitingForAnswer = false;
        hasAnswered = false;
        currentState = State.Patrol;
        agent.isStopped = false;
        agent.speed = walkSpeed;

        if (qnaPanel != null)
        {
            qnaPanel.SetActive(false);
        }

        LockAllPlayerControls(false);
        isQnAComplete = true;

        ResetMonsterState();
        AIEncounterGate.End(this);

        StartCoroutine(ResetCloseFlag());
    }

    void ResetMonsterState()
    {
        isChasing = false;
        isIdle = false;
        playerInSight = false;
        hasTriggeredJumpscare = false;
        isJumpscaring = false;
        jumpscareAnimTimer = 0f;
        idleTimer = 0f;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.ResetTrigger("jumpscare");
            animator.SetBool("walking", false);
            animator.SetBool("run", false);
        }

        currentState = State.Patrol;

        SetNewPatrolTarget();

        StartCoroutine(DelayedAnimationReset());
    }

    IEnumerator DelayedAnimationReset()
    {
        yield return new WaitForSeconds(0.2f);

        if (animator != null)
        {
            if (agent != null && agent.velocity.magnitude > 0.1f)
            {
                animator.SetBool("walking", true);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
                if (agent != null && agent.velocity.magnitude > 0.1f)
                {
                    animator.SetBool("walking", true);
                }
                else
                {
                    animator.SetBool("walking", true);
                }
            }
        }
    }

    IEnumerator ResetCloseFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isClosingQnA = false;
    }

}
