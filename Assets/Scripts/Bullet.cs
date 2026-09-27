using UnityEngine;

// Bullet prefab: SpriteRenderer + Rigidbody2D + Collider2D (Is Trigger ON) + this script.
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;

    private Rigidbody2D rb;
    private float damage;
    private GameObject owner;
    private bool ownerIsMonster;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Init(Vector2 velocity, float damage, float scale, GameObject owner)
    {
        this.damage = damage;
        this.owner = owner;
        ownerIsMonster = owner != null && owner.GetComponent<Monster>() != null;
        transform.localScale *= scale;
        rb.linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore other triggers (pickups, other bullets) and whoever fired us
        if (other.isTrigger) return;
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;
        // Monster bullets fly through other monsters (no friendly fire)
        if (ownerIsMonster && other.GetComponentInParent<Monster>() != null) return;

        // Player's absorb skill turns enemy bullets into ammo
        AbsorbSkill absorb = other.GetComponentInParent<AbsorbSkill>();
        if (absorb != null && absorb.IsActive)
        {
            absorb.Absorb();
            Destroy(gameObject);
            return;
        }

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
            target.TakeDamage(damage, rb.linearVelocity.normalized);

        Destroy(gameObject);
    }
}
