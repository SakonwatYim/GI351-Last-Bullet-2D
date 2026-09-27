using UnityEngine;
using UnityEngine.InputSystem;

// Put this on the Main Camera. Smoothly follows the player,
// leans a little toward the mouse (so you can see where you aim), and can shake.
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Follow")]
    [SerializeField] private Transform target;          // leave empty = auto-find the Player
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Look Ahead (toward mouse)")]
    [SerializeField] private float mouseLookAhead = 0.2f; // 0 = off, 0.2 = move 20% of the way toward the mouse
    [SerializeField] private float maxLookAhead = 3f;

    [Header("Bounds (optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minPosition = new Vector2(-50f, -10f);
    [SerializeField] private Vector2 maxPosition = new Vector2(50f, 30f);

    private Camera cam;
    private Vector3 velocity;
    private Vector3 basePosition;   // position without shake
    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    private void Start()
    {
        if (target == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }

        // Start already on the player instead of sliding in from far away
        if (target != null)
        {
            basePosition = GetDesiredPosition();
            transform.position = basePosition;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        basePosition = Vector3.SmoothDamp(basePosition, GetDesiredPosition(), ref velocity, smoothTime);

        Vector3 shake = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float strength = shakeStrength * (shakeTimer / shakeDuration);
            shake = (Vector3)(Random.insideUnitCircle * strength);
        }

        transform.position = basePosition + shake;
    }

    private Vector3 GetDesiredPosition()
    {
        Vector2 pos = (Vector2)target.position + offset;

        if (mouseLookAhead > 0f && cam != null && Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            mouseScreen.z = -transform.position.z;
            Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
            Vector2 lean = (mouseWorld - (Vector2)target.position) * mouseLookAhead;
            pos += Vector2.ClampMagnitude(lean, maxLookAhead);
        }

        if (useBounds)
        {
            pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);
        }

        return new Vector3(pos.x, pos.y, transform.position.z);
    }

    public void Shake(float strength, float duration)
    {
        // Keep the stronger shake if one is already playing
        if (shakeTimer > 0f && strength * duration < shakeStrength * shakeTimer) return;
        shakeStrength = strength;
        shakeDuration = shakeTimer = duration;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Vector3 center = (minPosition + maxPosition) * 0.5f;
        Gizmos.DrawWireCube(center, maxPosition - minPosition);
    }
}
