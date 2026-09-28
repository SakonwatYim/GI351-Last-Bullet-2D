using UnityEngine;

// Health potion pickup. Same setup as AmmoPickup:
// SpriteRenderer + Rigidbody2D + two colliders:
//   1) a normal collider (so it doesn't slide through walls)
//   2) a slightly bigger collider with Is Trigger ON (to detect the player)
// Top-down: no gravity, it slides out and slows to a stop.
[RequireComponent(typeof(Rigidbody2D))]
public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;
    [SerializeField] private float pickupDelay = 0.5f;
    [SerializeField] private bool pickupAtFullHealth = false; // false = stays on the ground until you need it
    [SerializeField] private float lifetime = 0f;             // 0 = never disappears
    [SerializeField] private float slideDamping = 5f;         // higher = stops sooner after being thrown

    private float spawnTime;

    private void Awake()
    {
        spawnTime = Time.time;
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = slideDamping;
        rb.freezeRotation = true;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        // Let the player walk through the solid collider
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        foreach (var mine in GetComponents<Collider2D>())
        {
            if (mine.isTrigger) continue;
            foreach (var theirs in player.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(mine, theirs);
        }
    }

    public void Setup(Vector2 throwVelocity)
    {
        GetComponent<Rigidbody2D>().linearVelocity = throwVelocity;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < spawnTime + pickupDelay) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;
        if (!pickupAtFullHealth && health.CurrentHealth >= health.MaxHealth) return;

        health.Heal(healAmount);
        Destroy(gameObject);
    }
}
