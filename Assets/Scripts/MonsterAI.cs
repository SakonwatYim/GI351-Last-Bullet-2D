using UnityEngine;
using UnityEngine.Serialization;

// Top-down monster. Put this on a Monster (together with Monster.cs).
// Wanders around its spawn point -> spots the player -> chases (steering around walls) and shoots.
// Also hurts the player on touch. Ignores gravity.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Monster))]
public class MonsterAI : MonoBehaviour
{
    private enum State { Wander, Chase }

    [Header("Detect")]
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float loseRange = 15f;
    [SerializeField] private bool needLineOfSight = true;
    [Tooltip("Walls / obstacles that block sight and movement.")]
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Move")]
    [FormerlySerializedAs("patrolSpeed")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float keepDistance = 4f;   // stops this far from the player to shoot (0 = run right into them)
    [SerializeField] private float acceleration = 20f;

    [Header("Shoot")]
    [SerializeField] private Bullet bulletPrefab;       // leave empty = melee-only monster
    [SerializeField] private Transform firePoint;       // optional
    [SerializeField] private float shootRange = 8f;
    [SerializeField] private float fireInterval = 1.5f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float bulletScale = 1f;
    [SerializeField] private float aimInaccuracy = 5f;  // random degrees added to each shot

    [Header("Touch Damage")]
    [SerializeField] private float contactDamage = 1f;

    [Header("Hit Stun")]
    [SerializeField] private float hitStunTime = 0.25f; // stop moving briefly after being shot so knockback shows

    // Directions tried (in degrees from the wanted one) when a wall is in the way
    private static readonly float[] SteerAngles = { 0f, 35f, -35f, 70f, -70f, 105f, -105f };

    private Rigidbody2D rb;
    private Collider2D col;
    private Monster monster;
    private Transform player;
    private State state = State.Wander;
    private Vector2 home;
    private Vector2 wanderTarget;
    private float nextWanderTime;
    private float nextFireTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        monster = GetComponent<Monster>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;

        home = transform.position;
        wanderTarget = home;
        nextFireTime = Time.time + Random.Range(0.5f, fireInterval); // monsters don't all shoot in sync
    }

    private void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy) { state = State.Wander; return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (state == State.Wander && dist <= detectRange && CanSeePlayer())
            state = State.Chase;
        else if (state == State.Chase && dist > loseRange)
        {
            state = State.Wander;
            home = transform.position;
        }

        if (state == State.Chase && bulletPrefab != null && dist <= shootRange &&
            Time.time >= nextFireTime && CanSeePlayer())
        {
            Shoot();
            nextFireTime = Time.time + fireInterval;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time < monster.LastHitTime + hitStunTime) return; // let knockback play out

        Vector2 pos = rb.position;
        Vector2 desired;

        if (state == State.Chase && player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - pos;
            desired = toPlayer.magnitude > keepDistance ? toPlayer.normalized * chaseSpeed : Vector2.zero;
            FaceX(toPlayer.x);
        }
        else
        {
            // Pick a new random point near home every few seconds (or once reached)
            if (Vector2.Distance(pos, wanderTarget) < 0.3f || Time.time >= nextWanderTime)
            {
                wanderTarget = home + Random.insideUnitCircle * wanderRadius;
                nextWanderTime = Time.time + Random.Range(2f, 4f);
            }
            Vector2 toTarget = wanderTarget - pos;
            desired = toTarget.magnitude > 0.3f ? toTarget.normalized * wanderSpeed : Vector2.zero;
            FaceX(desired.x);
        }

        desired = SteerAroundWalls(desired);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);
    }

    // If a wall is in the way, try turning a bit left/right until a free direction is found
    private Vector2 SteerAroundWalls(Vector2 desired)
    {
        if (desired.sqrMagnitude < 0.01f) return desired;

        float probe = col.bounds.extents.magnitude + 0.4f;
        foreach (float angle in SteerAngles)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * desired;
            if (!Physics2D.CircleCast(col.bounds.center, col.bounds.extents.x * 0.8f, dir.normalized, probe, obstacleLayer))
                return dir;
        }

        if (state == State.Wander) nextWanderTime = 0f; // boxed in -> pick another wander point
        return Vector2.zero;
    }

    private void Shoot()
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)col.bounds.center;
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        dir = Quaternion.Euler(0f, 0f, Random.Range(-aimInaccuracy, aimInaccuracy)) * dir;

        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Bullet b = Instantiate(bulletPrefab, origin, Quaternion.Euler(0f, 0f, rot));
        b.Init(dir * bulletSpeed, bulletDamage, bulletScale, gameObject);
    }

    private bool CanSeePlayer()
    {
        if (!needLineOfSight) return true;
        return Physics2D.Linecast(col.bounds.center, player.position, obstacleLayer).collider == null;
    }

    private void FaceX(float x)
    {
        if (Mathf.Abs(x) < 0.05f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (x > 0f ? 1f : -1f);
        transform.localScale = s;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (contactDamage <= 0f) return;
        var health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        Vector2 dir = (collision.transform.position - transform.position).normalized;
        health.TakeDamage(contactDamage, dir); // PlayerHealth ignores hits while invincible
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)home : transform.position, wanderRadius);
    }
}
