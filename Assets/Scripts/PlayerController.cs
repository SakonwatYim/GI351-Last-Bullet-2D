using UnityEngine;
using UnityEngine.InputSystem;

// Side-scrolling platformer movement (inspired by "The Last Bullet"):
// - A/D to move, W or Space to jump (hold for a higher jump)
// - Wall slide + wall jump
// - Coyote time and jump buffering so jumps feel responsive
// - ApplyKnockback() is ready for gun recoil / enemy hits later
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float groundAcceleration = 80f;
    [SerializeField] private float airAcceleration = 45f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;   // velocity kept when jump is released early
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Wall")]
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(9f, 14f);
    [SerializeField] private float wallJumpControlLock = 0.18f; // time the player can't steer after a wall jump

    [Header("Knockback")]
    [SerializeField] private float knockbackControlLock = 0.15f;

    [Header("Collision Checks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float checkDistance = 0.05f;

    public bool IsGrounded { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    private Rigidbody2D rb;
    private Collider2D col;
    private float baseGravity;

    private float moveInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float controlLockTimer;
    private int wallDirection; // -1 = wall on left, 1 = wall on right, 0 = none
    private bool isWallSliding;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        baseGravity = rb.gravityScale;
    }

    private void Update()
    {
        ReadInput();

        coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;
        controlLockTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
                Jump();
            else if (wallDirection != 0 && !IsGrounded)
                WallJump();
        }
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        ApplyHorizontalMovement();
        ApplyWallSlide();
        ApplyGravity();
    }

    private void ReadInput()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;

        moveInput = 0f;
        bool jumpPressed = false;
        bool jumpReleased = false;

        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput += 1f;
            jumpPressed |= kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            jumpReleased |= kb.wKey.wasReleasedThisFrame || kb.spaceKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame;
        }

        if (pad != null)
        {
            float stick = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > 0.2f) moveInput = Mathf.Sign(stick);
            jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
            jumpReleased |= pad.buttonSouth.wasReleasedThisFrame;
        }

        if (jumpPressed)
            jumpBufferTimer = jumpBufferTime;

        // Variable jump height: releasing early cuts the jump short
        if (jumpReleased && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        if (moveInput != 0f && controlLockTimer <= 0f)
            SetFacing((int)Mathf.Sign(moveInput));
    }

    private void CheckCollisions()
    {
        Bounds b = col.bounds;
        Vector2 center = b.center;

        // Slightly narrower boxes so a wall doesn't count as ground (and vice versa)
        Vector2 groundBox = new Vector2(b.size.x * 0.9f, checkDistance);
        Vector2 groundPos = new Vector2(center.x, b.min.y - checkDistance * 0.5f);
        IsGrounded = rb.linearVelocity.y <= 0.01f &&
                     Physics2D.OverlapBox(groundPos, groundBox, 0f, groundLayer);

        Vector2 wallBox = new Vector2(checkDistance, b.size.y * 0.8f);
        bool wallLeft = Physics2D.OverlapBox(new Vector2(b.min.x - checkDistance * 0.5f, center.y), wallBox, 0f, groundLayer);
        bool wallRight = Physics2D.OverlapBox(new Vector2(b.max.x + checkDistance * 0.5f, center.y), wallBox, 0f, groundLayer);
        wallDirection = wallRight ? 1 : wallLeft ? -1 : 0;
    }

    private void ApplyHorizontalMovement()
    {
        if (controlLockTimer > 0f) return;

        float targetSpeed = moveInput * moveSpeed;
        float accel = IsGrounded ? groundAcceleration : airAcceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void ApplyWallSlide()
    {
        // Slide only while pushing into the wall and falling
        isWallSliding = !IsGrounded && wallDirection != 0 &&
                        Mathf.Sign(moveInput) == wallDirection && moveInput != 0f &&
                        rb.linearVelocity.y < 0f;

        if (isWallSliding)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
    }

    private void ApplyGravity()
    {
        rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;

        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void WallJump()
    {
        int away = -wallDirection;
        rb.linearVelocity = new Vector2(away * wallJumpForce.x, wallJumpForce.y);
        SetFacing(away);
        controlLockTimer = wallJumpControlLock;
        jumpBufferTimer = 0f;
    }

    // Call this from the gun (recoil) or from enemies (hit knockback).
    public void ApplyKnockback(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        controlLockTimer = knockbackControlLock;
    }

    private void SetFacing(int dir)
    {
        if (dir == FacingDirection) return;
        FacingDirection = dir;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    private void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider2D>();
        if (c == null) return;
        Bounds b = c.bounds;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - checkDistance * 0.5f), new Vector3(b.size.x * 0.9f, checkDistance));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(b.min.x - checkDistance * 0.5f, b.center.y), new Vector3(checkDistance, b.size.y * 0.8f));
        Gizmos.DrawWireCube(new Vector3(b.max.x + checkDistance * 0.5f, b.center.y), new Vector3(checkDistance, b.size.y * 0.8f));
    }
}
