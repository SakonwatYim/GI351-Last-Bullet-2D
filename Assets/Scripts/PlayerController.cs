using UnityEngine;
using UnityEngine.InputSystem;

// Top-down movement:
// - WASD / arrow keys / left stick to move in 8 directions (no gravity)
// - Faces the mouse (or the move direction)
// - ApplyKnockback() is used by the gun (recoil) and by enemy hits
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 80f;          // how fast it reaches full speed
    [SerializeField] private float deceleration = 100f;         // how fast it stops when you let go

    [Header("Facing")]
    [SerializeField] private bool faceMouse = true;             // false = face the move direction instead
    [SerializeField] private float faceMouseDeadZone = 0.1f;    // don't flip-flop when the mouse is right above/below

    [Header("Knockback")]
    [SerializeField] private float knockbackControlLock = 0.15f;

    public int FacingDirection { get; private set; } = 1;
    public Vector2 MoveInput => moveInput;

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 moveInput;
    private float controlLockTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // No friction, so sliding along walls feels smooth.
        rb.sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
    }

    private void Update()
    {
        ReadInput();
        controlLockTimer -= Time.deltaTime;

        if (faceMouse && Mouse.current != null)
            FaceMouse();
        else if (moveInput.x != 0f && controlLockTimer <= 0f)
            SetFacing(moveInput.x > 0f ? 1 : -1);
    }

    private void FixedUpdate()
    {
        if (controlLockTimer > 0f) return; // let knockback play out

        Vector2 target = moveInput * moveSpeed;
        float accel = moveInput.sqrMagnitude > 0f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, accel * Time.fixedDeltaTime);
    }

    private void ReadInput()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;

        Vector2 input = Vector2.zero;

        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
        }

        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f) input = stick;
        }

        // Diagonals aren't faster than straight lines
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void FaceMouse()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        mouseScreen.z = -cam.transform.position.z;
        float dx = cam.ScreenToWorldPoint(mouseScreen).x - transform.position.x;
        if (Mathf.Abs(dx) > faceMouseDeadZone)
            SetFacing(dx > 0f ? 1 : -1);
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
}
