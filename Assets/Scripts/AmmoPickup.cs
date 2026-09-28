using UnityEngine;

// Ammo pickup prefab: SpriteRenderer + Rigidbody2D + two colliders:
//   1) a normal collider (so it doesn't slide through walls)
//   2) a slightly bigger collider with Is Trigger ON (to detect the player)
// Top-down: no gravity, it slides out and slows to a stop.
[RequireComponent(typeof(Rigidbody2D))]
public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private int amount = 5;
    [SerializeField] private float pickupDelay = 0.8f; // so a dropped pickup isn't grabbed back instantly
    [SerializeField] private float slideDamping = 5f;  // higher = stops sooner after being thrown

    private float spawnTime;

    private void Awake()
    {
        spawnTime = Time.time;
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = slideDamping;
        rb.freezeRotation = true;
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

    public void Setup(int amount, Vector2 throwVelocity)
    {
        this.amount = amount;
        GetComponent<Rigidbody2D>().linearVelocity = throwVelocity;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < spawnTime + pickupDelay) return;

        Gun gun = other.GetComponentInParent<Gun>();
        if (gun == null) return;

        amount -= gun.AddAmmo(amount);
        if (amount <= 0)
            Destroy(gameObject);
    }
}
