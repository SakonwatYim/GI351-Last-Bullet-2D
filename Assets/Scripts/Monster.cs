using System.Collections;
using UnityEngine;

// Simple test monster: takes damage from bullets, flashes, gets knocked back, dies.
// Monster: SpriteRenderer + Rigidbody2D + Collider2D (Is Trigger OFF) + this script.
public class Monster : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 20f;
    [SerializeField] private float knockbackPerDamage = 0.5f;
    [SerializeField] private AmmoPickup ammoDropPrefab; // optional

    public float LastHitTime { get; private set; } = -999f;

    private float health;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color baseColor;

    private void Awake()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
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
        if (ammoDropPrefab != null)
        {
            AmmoPickup p = Instantiate(ammoDropPrefab, transform.position, Quaternion.identity);
            p.Setup(5, Vector2.up * 4f);
        }
        Destroy(gameObject);
    }
}
