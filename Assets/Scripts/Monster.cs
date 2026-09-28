using System.Collections;
using UnityEngine;

// Simple test monster: takes damage from bullets, flashes, gets knocked back, dies.
// Monster: SpriteRenderer + Rigidbody2D + Collider2D (Is Trigger OFF) + this script.
public class Monster : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 20f;
    [SerializeField] private float knockbackPerDamage = 0.5f;
    [SerializeField] private AmmoPickup ammoDropPrefab; // optional
    [SerializeField, Range(0f, 1f)] private float ammoDropChance = 0.33f;
    [SerializeField] private HealthPickup potionDropPrefab; // optional
    [SerializeField, Range(0f, 1f)] private float potionDropChance = 0.1f;

    public static event System.Action<Monster> OnAnyMonsterDied;

    public float LastHitTime { get; private set; } = -999f;

    private float health;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color baseColor;
    private bool isDead;

    private void Awake()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (isDead) return;
        health -= amount;
        LastHitTime = Time.time;

        if (rb != null)
            rb.AddForce(hitDirection * amount * knockbackPerDamage, ForceMode2D.Impulse);

        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(Flash());
        }

        if (health <= 0f)
            Die();
    }

    private IEnumerator Flash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = baseColor;
    }

    private void Die()
    {
        isDead = true;
        OnAnyMonsterDied?.Invoke(this);

        if (ammoDropPrefab != null && Random.value < ammoDropChance)
        {
            AmmoPickup p = Instantiate(ammoDropPrefab, transform.position, Quaternion.identity);
            p.Setup(5, Vector2.up * 4f);
        }
        if (potionDropPrefab != null && Random.value < potionDropChance)
        {
            HealthPickup h = Instantiate(potionDropPrefab, transform.position, Quaternion.identity);
            h.Setup(new Vector2(Random.Range(-1.5f, 1.5f), 5f)); // pop out sideways so it doesn't overlap the ammo
        }
        Destroy(gameObject);
    }
}
