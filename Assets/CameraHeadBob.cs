// PURPOSE: Adds smooth movement-based camera bob, held-item sway, and external camera shake.
using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    // Camera motion values for walking, sprinting, and crouching.
    [Header("Head Bob Settings")]
    public float walkBobSpeed = 8.5f;
    public float walkBobAmount = 0.022f;
    public float sprintBobSpeed = 12f;
    public float sprintBobAmount = 0.04f;
    public float crouchBobSpeed = 6f;
    public float crouchBobAmount = 0.012f;

    [Header("Smoothing")]
    public float smoothSpeed = 14f;
    public float bobFadeInSpeed = 7f;
    public float bobFadeOutSpeed = 10f;

    [Header("Item Bob Settings")]
    public float itemWalkBobSpeed = 10f;
    public float itemWalkBobAmount = 0.025f;
    public float itemSprintBobSpeed = 15f;
    public float itemSprintBobAmount = 0.045f;
    public float itemBobSmooth = 8f;

    [Header("Item Sway Settings")]
    public float swayAmount = 0.012f;
    public float swaySmooth = 6f;
    public float swayClamp = 0.08f;

    [Header("External Shake")]
    public float externalShakeAmount = 0f;

    private float bobTimer;
    private float itemBobTimer;
    private float bobBlend;
    private Vector3 defaultPos;
    private CharacterController characterController;
    private PlayerController playerController;
    private StaminaController staminaController;
    private Transform currentItem;
    private Vector3 itemDefaultPos;
    private Quaternion itemDefaultRot;
    private Camera cam;
    private float defaultFOV;

    private void Start()
    {
        defaultPos = transform.localPosition;
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController>();
        staminaController = GetComponentInParent<StaminaController>();
        cam = GetComponent<Camera>();
        if (cam != null)
            defaultFOV = cam.fieldOfView;
    }

    private void Update()
    {
        // Calculate camera motion from the current player movement state.
        bool isCrouching = playerController != null
            ? playerController.IsCrouching
            : Input.GetKey(KeyCode.LeftControl);
        bool canSprint = staminaController == null || staminaController.CanSprint;
        bool isSprinting = playerController != null
            ? playerController.IsSprinting
            : Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical") > 0f && canSprint;
        bool playerCanMove = playerController == null || playerController.enabled;
        bool paused = PauseMenu.Instance != null && PauseMenu.Instance.isPaused;
        bool grounded = characterController == null || characterController.isGrounded;

        float movementAmount = 0f;
        if (playerCanMove && !paused && !PlayerController.IsReadingDocument && grounded)
        {
            if (characterController != null && playerController != null)
            {
                Vector3 horizontalVelocity = characterController.velocity;
                horizontalVelocity.y = 0f;
                float topSpeed = isCrouching
                    ? playerController.moveSpeed * playerController.crouchSpeedMultiplier
                    : isSprinting ? playerController.sprintSpeed : playerController.moveSpeed;
                movementAmount = topSpeed > 0f
                    ? Mathf.Clamp01(horizontalVelocity.magnitude / topSpeed)
                    : 0f;
            }
            else
            {
                Vector2 input = Vector2.ClampMagnitude(
                    new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
                movementAmount = input.magnitude;
            }
        }

        float blendSpeed = movementAmount > bobBlend ? bobFadeInSpeed : bobFadeOutSpeed;
        bobBlend = Mathf.MoveTowards(bobBlend, movementAmount, Time.deltaTime * blendSpeed);

        if (bobBlend > 0.01f)
        {
            float bobSpeed = isCrouching ? crouchBobSpeed
                : isSprinting ? sprintBobSpeed
                : walkBobSpeed;

            bobTimer += Time.deltaTime * bobSpeed * Mathf.Lerp(0.65f, 1f, bobBlend);
        }
        else
        {
            bobTimer = Mathf.MoveTowards(bobTimer, 0f, Time.deltaTime * 5f);
        }

        float bobY = Mathf.Sin(bobTimer) * (isCrouching ? crouchBobAmount
            : isSprinting ? sprintBobAmount
            : walkBobAmount) * bobBlend;
        float bobX = Mathf.Cos(bobTimer * 0.5f) * (isCrouching ? crouchBobAmount
            : isSprinting ? sprintBobAmount
            : walkBobAmount) * 0.35f * bobBlend;

        Vector3 shake = externalShakeAmount > 0f
            ? Random.insideUnitSphere * externalShakeAmount
            : Vector3.zero;
        Vector3 targetPosition = defaultPos + new Vector3(bobX, bobY, 0f) + shake;
        float positionBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothSpeed) * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, positionBlend);

        if (cam != null)
        {
            float targetFOV = defaultFOV;
            if (isSprinting && playerCanMove && !paused && playerController != null)
                targetFOV += playerController.sprintFOVIncrease;
            if (externalShakeAmount > 0f)
                targetFOV += Mathf.Sin(Time.time * 25f) * (externalShakeAmount * 5f);

            float fovBlend = 1f - Mathf.Exp(-5f * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovBlend);
        }

        UpdateHeldItem(bobBlend > 0.05f, isSprinting, isCrouching);
    }

    private void UpdateHeldItem(bool isMoving, bool isSprinting, bool isCrouching)
    {
        // Apply matching bob and mouse sway to the currently selected inventory item.
        if (Inventory.Instance == null)
            return;

        var items = Inventory.Instance.GetItems();
        int selected = Inventory.Instance.GetSelectedIndex();
        if (selected < 0 || selected >= items.Count)
        {
            currentItem = null;
            return;
        }

        GameObject heldObject = items[selected];
        if (heldObject == null)
            return;

        if (currentItem != heldObject.transform)
        {
            currentItem = heldObject.transform;
            itemDefaultPos = currentItem.localPosition;
            itemDefaultRot = currentItem.localRotation;
            itemBobTimer = 0f;
        }

        if (isMoving)
        {
            float speed = isCrouching ? itemWalkBobSpeed * 0.7f
                : isSprinting ? itemSprintBobSpeed : itemWalkBobSpeed;
            float amount = isCrouching ? itemWalkBobAmount * 0.5f
                : isSprinting ? itemSprintBobAmount : itemWalkBobAmount;
            itemBobTimer += Time.deltaTime * speed;

            Vector3 bobOffset = new Vector3(
                Mathf.Cos(itemBobTimer * 0.5f) * amount * 0.45f,
                Mathf.Sin(itemBobTimer) * amount,
                Mathf.Sin(itemBobTimer * 0.5f) * amount * 0.2f);
            Vector3 targetPosition = itemDefaultPos + bobOffset;
            currentItem.localPosition = Vector3.Lerp(
                currentItem.localPosition, targetPosition, Time.deltaTime * itemBobSmooth);
        }
        else
        {
            itemBobTimer = Mathf.MoveTowards(itemBobTimer, 0f, Time.deltaTime * 5f);
            currentItem.localPosition = Vector3.Lerp(
                currentItem.localPosition, itemDefaultPos, Time.deltaTime * itemBobSmooth);
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float swayX = Mathf.Clamp(-mouseX * swayAmount, -swayClamp, swayClamp);
        float swayY = Mathf.Clamp(-mouseY * swayAmount, -swayClamp, swayClamp);
        Quaternion swayRotation = Quaternion.Euler(swayY * 20f, swayX * 20f, swayX * 10f);
        currentItem.localRotation = Quaternion.Slerp(
            currentItem.localRotation,
            itemDefaultRot * swayRotation,
            Time.deltaTime * swaySmooth);
    }
}
