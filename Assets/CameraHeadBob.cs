using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [Header("Head Bob Settings")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 20f;
    public float sprintBobAmount = 0.09f;
    public float crouchBobSpeed = 8f;
    public float crouchBobAmount = 0.025f;

    [Header("Smoothing")]
    public float smoothSpeed = 10f;

    [Header("Item Bob Settings")]
    public float itemWalkBobSpeed = 12f;
    public float itemWalkBobAmount = 0.04f;
    public float itemSprintBobSpeed = 18f;
    public float itemSprintBobAmount = 0.08f;
    public float itemBobSmooth = 8f;

    // Item sway
    [Header("Item Sway Settings")]
    public float swayAmount = 0.02f;
    public float swaySmooth = 6f;
    public float swayClamp = 0.1f;

    [Header("External Shake")]
    public float externalShakeAmount = 0f;

    private float bobTimer = 0f;
    private float itemBobTimer = 0f;
    private Vector3 defaultPos;
    private CharacterController cc;
    private StaminaController staminaController;

    // Item reference
    private Transform currentItem;
    private Vector3 itemDefaultPos;
    private Quaternion itemDefaultRot;
    private float lastMouseX;
    private float lastMouseY;
    private Camera cam;
    private float defaultFOV;

    void Start()
    {
        defaultPos = transform.localPosition;
        cc = GetComponentInParent<CharacterController>();
        staminaController = GetComponentInParent<StaminaController>();
        cam = GetComponent<Camera>();
        if (cam != null) defaultFOV = cam.fieldOfView;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = (h != 0 || v != 0) && cc.isGrounded;
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);
        // Shift alone is not sprinting when Hard-mode stamina has run out.
        bool canSprint = staminaController == null || staminaController.CanSprint;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && v > 0f && canSprint;

        // ── CAMERA BOB ──
        if (isMoving)
        {
            float speed = isCrouching ? crouchBobSpeed
                : isSprinting ? sprintBobSpeed
                : walkBobSpeed;

            float amount = isCrouching ? crouchBobAmount
                : isSprinting ? sprintBobAmount
                : walkBobAmount;

            bobTimer += Time.deltaTime * speed;
            float bobY = Mathf.Sin(bobTimer) * amount;
            float bobX = Mathf.Cos(bobTimer / 2f) * amount * 0.5f;
            
            Vector3 shake = externalShakeAmount > 0f ? Random.insideUnitSphere * externalShakeAmount : Vector3.zero;
            transform.localPosition = defaultPos + new Vector3(bobX, bobY, 0f) + shake;
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 5f);
            
            Vector3 shake = externalShakeAmount > 0f ? Random.insideUnitSphere * externalShakeAmount : Vector3.zero;

            transform.localPosition = Vector3.Lerp(
                transform.localPosition, defaultPos,
                Time.deltaTime * smoothSpeed) + shake;
        }

        // FOV Distortion (Simulate blur/dizziness)
        if (cam != null)
        {
            if (externalShakeAmount > 0f)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFOV + Mathf.Sin(Time.time * 25f) * (externalShakeAmount * 5f), Time.deltaTime * 5f);
            else
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFOV, Time.deltaTime * 5f);
        }

        // ── ITEM BOB + SWAY ──
        UpdateHeldItem(isMoving, isSprinting, isCrouching);
    }

    void UpdateHeldItem(bool isMoving, bool isSprinting, bool isCrouching)
    {
        // Find current held item from inventory
        if (Inventory.Instance == null) return;

        var items = Inventory.Instance.GetItems();
        int selected = Inventory.Instance.GetSelectedIndex();

        if (selected < 0 || selected >= items.Count)
        {
            currentItem = null;
            return;
        }

        GameObject heldObj = items[selected];
        if (heldObj == null) return;

        // Track item reference
        if (currentItem != heldObj.transform)
        {
            currentItem = heldObj.transform;
            itemDefaultPos = currentItem.localPosition;
            itemDefaultRot = currentItem.localRotation;
            itemBobTimer = 0f;
        }

        // ── ITEM BOB ──
        if (isMoving)
        {
            float speed = isSprinting ? itemSprintBobSpeed : itemWalkBobSpeed;
            float amount = isSprinting ? itemSprintBobAmount : itemWalkBobAmount;

            itemBobTimer += Time.deltaTime * speed;

            float bobY = Mathf.Sin(itemBobTimer) * amount;
            float bobX = Mathf.Cos(itemBobTimer / 2f) * amount * 0.6f;
            float bobZ = Mathf.Sin(itemBobTimer / 2f) * amount * 0.3f;

            Vector3 targetPos = itemDefaultPos + new Vector3(bobX, bobY, bobZ);
            currentItem.localPosition = Vector3.Lerp(
                currentItem.localPosition, targetPos,
                Time.deltaTime * itemBobSmooth);
        }
        else
        {
            itemBobTimer = Mathf.Lerp(itemBobTimer, 0f, Time.deltaTime * 5f);
            currentItem.localPosition = Vector3.Lerp(
                currentItem.localPosition, itemDefaultPos,
                Time.deltaTime * itemBobSmooth);
        }

        // ── ITEM SWAY (mouse movement) ──
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float swayX = Mathf.Clamp(-mouseX * swayAmount, -swayClamp, swayClamp);
        float swayY = Mathf.Clamp(-mouseY * swayAmount, -swayClamp, swayClamp);

        Quaternion swayRot = Quaternion.Euler(swayY * 20f, swayX * 20f, swayX * 10f);
        currentItem.localRotation = Quaternion.Slerp(
            currentItem.localRotation,
            itemDefaultRot * swayRot,
            Time.deltaTime * swaySmooth);
    }
}
