// PURPOSE: Handles first-person player movement, camera input, and movement actions such as sprinting and crouching.
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Used by readable books and notes so they can reliably stop both movement and mouse look.
    public static bool IsReadingDocument { get; private set; }

    public static void SetDocumentReading(bool isReading)
    {
        IsReadingDocument = isReading;
    }

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;      
    public float sprintFOVIncrease = 4f;
    public float gravity = -20f;
    [Range(30f, 75f)] public float maxWalkableSlope = 55f;

    [Header("Movement Feel")]
    [Range(0f, 50f)] public float groundAcceleration = 20f;
    [Range(0f, 60f)] public float groundDeceleration = 26f;
    [Range(0f, 30f)] public float airAcceleration = 8f;

    private bool isSprinting = false;
    private bool wasSprintingBeforeJump = false;

    [Header("Jump")]
    public float jumpHeight = 1.5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    [Header("Crouch")]
    public float crouchSpeedMultiplier = 0.5f;
    public float crouchHeight = 1f;
    public float crouchCenterY = 0.5f;
    private float normalHeight;
    private float normalCenterY;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraHolder;
    private float xRotation = 0f;

    private CharacterController cc;
    private StaminaController staminaController;
    private Vector3 velocity;
    private Vector3 horizontalVelocity;
    private bool isGrounded;
    private bool isCrouching;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.slopeLimit = maxWalkableSlope;
        staminaController = GetComponent<StaminaController>();
        normalHeight = cc.height;
        normalCenterY = cc.center.y;
    }

    void Start()
    {
        // Lock cursor at start only if game is not paused
        if (PauseMenu.Instance == null || !PauseMenu.Instance.isPaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        horizontalVelocity = Vector3.zero;
    }
    void Update()
    {
        if (IsReadingDocument)
        {
            horizontalVelocity = Vector3.zero;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // Handle cursor state based on pause
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused)
        {
            // Game is paused - cursor should be visible and free
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return; // Don't process movement or look when paused
        }
        
        // Game is not paused - ensure cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        HandleMouseLook();
        CheckGround();
        HandleCrouch();
        HandleMove();
        HandleJump();
        ApplyGravity();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        
        if (isGrounded && !wasGrounded && wasSprintingBeforeJump)
        {
            isSprinting = true;
            wasSprintingBeforeJump = false;
        }
    }

    void HandleMove()
    {
        Vector2 input = Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);

        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && input.y > 0.05f && !isCrouching;
        bool canSprint = staminaController == null || staminaController.CanSprint;

        isSprinting = wantsToSprint && canSprint;

        // Do not retain movement momentum while crouching after the player
        // releases the movement keys. This must not depend on the ground check.
        if (isCrouching && input.sqrMagnitude <= 0.001f)
        {
            horizontalVelocity = Vector3.zero;

            if (staminaController != null)
                staminaController.UpdateStamina(false);

            return;
        }

        float speed = isSprinting ? sprintSpeed
            : isCrouching ? moveSpeed * crouchSpeedMultiplier
            : moveSpeed;

        Vector3 targetVelocity = moveDir * speed;
        if (!isGrounded && input.sqrMagnitude <= 0.001f)
            targetVelocity = horizontalVelocity;

        float acceleration = isGrounded
            ? (input.sqrMagnitude > 0.001f ? groundAcceleration : groundDeceleration)
            : (input.sqrMagnitude > 0.001f ? airAcceleration : 0f);

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (staminaController != null)
            staminaController.UpdateStamina(isSprinting);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            wasSprintingBeforeJump = isSprinting;
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        CollisionFlags collisionFlags = cc.Move((horizontalVelocity + velocity) * Time.deltaTime);

        // Stop upward jump momentum immediately when the character hits a ceiling.
        if ((collisionFlags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            velocity.y = 0f;
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();

        if (Input.GetKeyUp(KeyCode.LeftControl))
            StandUp();
    }

    void StartCrouch()
    {
        isCrouching = true;
        isSprinting = false;
        horizontalVelocity = Vector3.zero;
        cc.height = crouchHeight;
        cc.center = new Vector3(cc.center.x, crouchCenterY, cc.center.z);
    }

    void StandUp()
    {
        isCrouching = false;
        cc.height = normalHeight;
        cc.center = new Vector3(cc.center.x, normalCenterY, cc.center.z);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) 
            return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
