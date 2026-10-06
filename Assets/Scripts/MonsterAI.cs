using UnityEngine;

// Put this on a Monster (together with Monster.cs).
// Patrols back and forth -> spots the player -> chases, jumps over walls, and shoots.
// Also hurts the player on touch.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Monster))]
public class MonsterAI : MonoBehaviour
{
    private enum State { Patrol, Chase }

    [Header("Detect")]
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float loseRange = 15f;
    [SerializeField] private bool needLineOfSight = true;

    [Header("Move")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float keepDistance = 4f;   // stops this far from the player to shoot (0 = run right into them)
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private bool avoidLedges = true;   // only while patrolling / when the player isn't below
    [SerializeField] private bool dropDownToPlayer = true; // walk off ledges when the player is lower
    [SerializeField] private float dropHeight = 1f;     // player must be at least this much lower to drop
    [SerializeField] private LayerMask groundLayer;

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

    [Header("Sound")]
    [SerializeField] private float footstepInterval = 0.45f;
    [SerializeField] private float footstepVolume = 0.4f;
    [SerializeField] private float hearRange = 12f;     // footsteps only play this close to the player

    private float footstepTimer;
    private Rigidbody2D rb;
    private Collider2D col;
    private Monster monster;
    private Transform player;
    private State state = State.Patrol;
    private int facing = 1;
    private bool grounded;
    private float nextFireTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        monster = GetComponent<Monster>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
        nextFireTime = Time.time + Random.Range(0.5f, fireInterval); // monsters don't all shoot in sync
    }

    private void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy) { state = State.Patrol; return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (state == State.Patrol && dist <= detectRange && CanSeePlayer())
            state = State.Chase;
        else if (state == State.Chase && dist > loseRange)
            state = State.Patrol;

        if (state == State.Chase && bulletPrefab != null && dist <= shootRange &&
            Time.time >= nextFireTime && CanSeePlayer())
        {
            Shoot();
            nextFireTime = Time.time + fireInterval;
        }

        if (grounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f && dist <= hearRange)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepTimer = footstepInterval;
                SoundManager.Instance.PlaySound2D("MonsWalk", footstepVolume);
            }
        }
    }

    private void FixedUpdate()
    {
        grounded = CheckGround();

        if (Time.time < monster.LastHitTime + hitStunTime) return;

        float speed = 0f;

        if (state == State.Chase && player != null)
        {
            float dx = player.position.x - transform.position.x;
            SetFacing(dx >= 0f ? 1 : -1);

            if (Mathf.Abs(dx) > keepDistance)
            {
                speed = chaseSpeed;

                if (grounded && WallAhead())
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                else if (avoidLedges && grounded && !PlayerIsBelow() && LedgeAhead())
                    speed = 0f;
            }
        }
        else
        {
            if (grounded && (WallAhead() || (avoidLedges && LedgeAhead())))
                SetFacing(-facing);
            speed = patrolSpeed;
        }

        rb.linearVelocity = new Vector2(facing * speed, rb.linearVelocity.y);
    }

    private void Shoot()
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)col.bounds.center;
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        dir = Quaternion.Euler(0f, 0f, Random.Range(-aimInaccuracy, aimInaccuracy)) * dir;

        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Bullet b = Instantiate(bulletPrefab, origin, Quaternion.Euler(0f, 0f, rot));
        b.Init(dir * bulletSpeed, bulletDamage, bulletScale, gameObject);
        SoundManager.Instance.PlaySound2D("MonsShoot");
    }

    private bool CanSeePlayer()
    {
        if (!needLineOfSight) return true;
        return Physics2D.Linecast(col.bounds.center, player.position, groundLayer).collider == null;
    }

    private bool PlayerIsBelow()
    {
        return dropDownToPlayer && player != null && player.position.y < col.bounds.min.y - dropHeight;
    }

    private bool CheckGround()
    {
        Bounds b = col.bounds;
        return Physics2D.OverlapBox(new Vector2(b.center.x, b.min.y - 0.05f),
                                    new Vector2(b.size.x * 0.9f, 0.1f), 0f, groundLayer);
    }

    private bool WallAhead()
    {
        Bounds b = col.bounds;
        return Physics2D.Raycast(b.center, Vector2.right * facing, b.extents.x + 0.2f, groundLayer);
    }

    private bool LedgeAhead()
    {
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x + facing * (b.extents.x + 0.2f), b.min.y + 0.1f);
        return !Physics2D.Raycast(origin, Vector2.down, 0.6f, groundLayer);
    }

    private void SetFacing(int dir)
    {
        if (dir == facing) return;
        facing = dir;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
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
    }
}
