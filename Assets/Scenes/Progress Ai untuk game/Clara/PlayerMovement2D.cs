
using UnityEngine;
using UnityEngine.InputSystem;

public enum MovePlane { XZ_TopDown, XY_SideScroll, YZ_SideView, XYZ_Free }
public enum FacingDirection { Down, Up, Left, Right }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController25D : MonoBehaviour
{
    [Header("=== MODE ===")]
    public MovePlane movePlane = MovePlane.XZ_TopDown;

    [Header("=== MOVEMENT ===")]
    public float moveSpeed = 5f;
    public float smoothTime = 0.05f;
    public bool canMove = true;

    [Header("=== ROTATION (matikan jika pakai sprite arah) ===")]
    public bool rotateToMovement = false;
    public float rotationSpeed = 15f;

    [Header("=== GROUND ===")]
    public float groundCheckExtra = 0.2f;
    public LayerMask groundLayer;

    [Header("=== SPRITES - IDLE ===")]
    [Tooltip("Fallback jika sprite arah idle kosong")]
    public Sprite defaultIdle;
    public Sprite idleDown;
    public Sprite idleUp;
    public Sprite idleLeft;
    public Sprite idleRight;

    [Header("=== SPRITES - WALK ===")]
    public Sprite walkDown;
    public Sprite walkUp;
    public Sprite walkLeft;
    public Sprite walkRight;

    [Header("=== SPRITE RENDERER ===")]
    [Tooltip("Kosongkan jika ingin auto-find di children")]
    public SpriteRenderer spriteRenderer;

    [Header("=== DEBUG ===")]
    public bool showDebug = true;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Vector2 inputVector;
    private Vector3 moveDirection;
    private Vector3 smoothVelocity;
    private bool isGrounded;

    private FacingDirection facing = FacingDirection.Down;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Auto-find SpriteRenderer kalau tidak di-assign
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                Debug.Log($"[Player] Auto-found SpriteRenderer: {spriteRenderer.gameObject.name}");
            else
                Debug.LogWarning("[Player] SpriteRenderer tidak ditemukan!");
        }

        // Set sprite idle awal
        UpdateSprite();

        Debug.Log("[Player] Start OK.");
    }

    void Update()
    {
        // ====== GROUND CHECK ======
        Vector3 rayStart = transform.position + Vector3.up * (col.height / 2f - col.radius);
        float rayDist = col.radius + groundCheckExtra;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, rayDist, groundLayer);
        Debug.DrawRay(rayStart, Vector3.down * rayDist, isGrounded ? Color.green : Color.red);

        // ====== INPUT ======
        if (!canMove)
        {
            moveDirection = Vector3.zero;
            UpdateSprite();
            return;
        }

        inputVector = ReadKeyboardInput();
        moveDirection = ConvertInput(inputVector);

        // ====== UPDATE FACING & SPRITE ======
        UpdateFacing();
        UpdateSprite();

        // ====== ROTASI (opsional, biasanya dimatikan saat pakai sprite arah) ======
        if (rotateToMovement && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        if (showDebug && inputVector.sqrMagnitude > 0.01f)
            Debug.Log($"[Player] Input={inputVector}, Facing={facing}, Grounded={isGrounded}");
    }

    // ================================================================
    // ====================== SPRITE LOGIC ============================
    // ================================================================

    void UpdateFacing()
    {
        // Kalau tidak ada gerakan, jangan ubah facing
        if (moveDirection.sqrMagnitude < 0.01f) return;

        float absX = Mathf.Abs(moveDirection.x);
        float absZ = Mathf.Abs(moveDirection.z);

        // Prioritas: horizontal kalau lebih dominan, kalau tidak vertikal
        if (absX > absZ)
        {
            facing = moveDirection.x > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            facing = moveDirection.z > 0 ? FacingDirection.Up : FacingDirection.Down;
        }
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        bool isMoving = moveDirection.sqrMagnitude > 0.01f;

        Sprite targetSprite = isMoving ? GetWalkSprite(facing) : GetIdleSprite(facing);

        if (targetSprite != null && spriteRenderer.sprite != targetSprite)
        {
            spriteRenderer.sprite = targetSprite;
        }
    }

    Sprite GetIdleSprite(FacingDirection dir)
    {
        Sprite s = null;

        switch (dir)
        {
            case FacingDirection.Down: s = idleDown; break;
            case FacingDirection.Up: s = idleUp; break;
            case FacingDirection.Left: s = idleLeft; break;
            case FacingDirection.Right: s = idleRight; break;
        }

        return s != null ? s : defaultIdle;
    }

    Sprite GetWalkSprite(FacingDirection dir)
    {
        Sprite s = null;

        switch (dir)
        {
            case FacingDirection.Down: s = walkDown; break;
            case FacingDirection.Up: s = walkUp; break;
            case FacingDirection.Left: s = walkLeft; break;
            case FacingDirection.Right: s = walkRight; break;
        }

        // Fallback: kalau walk sprite kosong, pakai idle arah yang sama
        return s != null ? s : GetIdleSprite(dir);
    }

    // ================================================================
    // ====================== INPUT ==================================
    // ================================================================

    Vector2 ReadKeyboardInput()
    {
        if (Keyboard.current == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;

        return new Vector2(x, y);
    }

    Vector3 ConvertInput(Vector2 input)
    {
        Vector3 dir = Vector3.zero;

        switch (movePlane)
        {
            case MovePlane.XZ_TopDown: dir = new Vector3(input.x, 0f, input.y); break;
            case MovePlane.XY_SideScroll: dir = new Vector3(input.x, input.y, 0f); break;
            case MovePlane.YZ_SideView: dir = new Vector3(0f, input.y, input.x); break;
            case MovePlane.XYZ_Free: dir = new Vector3(input.x, 0f, input.y); break;
        }

        return dir.normalized;
    }

    void FixedUpdate()
    {
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = Vector3.SmoothDamp(
            rb.linearVelocity,
            targetVelocity,
            ref smoothVelocity,
            smoothTime
        );
    }
}