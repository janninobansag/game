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
    public float sprintFOVIncrease = 10f; 
    public float gravity = -9.81f;

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
    private bool isGrounded;
    private bool isCrouching;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
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

    void Update()
    {
        if (IsReadingDocument)
        {
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
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = transform.right * h + transform.forward * v;
        if (moveDir.magnitude > 1f) 
            moveDir.Normalize();

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && v > 0f && !isCrouching;
        bool canSprint = staminaController == null || staminaController.CanSprint;
        
        if (isGrounded)
        {
            isSprinting = wantsToSprint && canSprint;
        }
        else
        {
            if (wantsToSprint && canSprint)
                isSprinting = true;
            else if ((!wantsToSprint || !canSprint) && isSprinting)
                isSprinting = false;
        }

        float speed = isSprinting ? sprintSpeed
            : isCrouching ? moveSpeed * crouchSpeedMultiplier
            : moveSpeed;

        cc.Move(moveDir * speed * Time.deltaTime);

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
        cc.Move(velocity * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded)
            StartCrouch();

        if (Input.GetKeyUp(KeyCode.LeftControl))
            StandUp();
    }

    void StartCrouch()
    {
        isCrouching = true;
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
