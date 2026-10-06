using UnityEngine;

// Flying monster. Put on a Monster (together with Monster.cs) instead of MonsterAI.
// Hovers around its spawn point -> spots the player -> flies above them,
// shoots and/or dives at them. Ignores gravity. Also hurts the player on touch.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Monster))]
public class FlyingMonsterAI : MonoBehaviour
{
    private enum State { Idle, Chase, Dive, Recover }

    [Header("Detect")]
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float loseRange = 18f;
    [SerializeField] private bool needLineOfSight = false;
    [SerializeField] private LayerMask groundLayer;          // walls / ground to fly around

    [Header("Idle")]
    [SerializeField] private float wanderRadius = 2.5f;
    [SerializeField] private float idleSpeed = 1.5f;
    [SerializeField] private float bobAmount = 0.3f;         // up/down wobble
    [SerializeField] private float bobSpeed = 3f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float acceleration = 8f;        // how fast it turns / changes speed
    [SerializeField] private Vector2 hoverOffset = new Vector2(3f, 3.5f); // where it hangs around the player

    [Header("Shoot")]
    [SerializeField] private Bullet bulletPrefab;            // leave empty = no shooting
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootRange = 9f;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float bulletScale = 1f;
    [SerializeField] private float aimInaccuracy = 5f;

    [Header("Dive Attack")]
    [SerializeField] private bool canDive = true;
    [SerializeField] private float diveRange = 7f;
    [SerializeField] private float diveSpeed = 12f;
    [SerializeField] private float diveTime = 0.5f;
    [SerializeField] private float diveWindup = 0.4f;        // pauses and shakes before diving
    [SerializeField] private float diveCooldown = 3f;
    [SerializeField] private float recoverTime = 0.6f;

    [Header("Touch Damage")]
    [SerializeField] private float contactDamage = 1f;

    [Header("Hit Stun")]
    [SerializeField] private float hitStunTime = 0.25f;

    [Header("Sound")]
    [SerializeField] private float flapInterval = 0.6f;
    [SerializeField] private float flapVolume = 0.4f;
    [SerializeField] private float hearRange = 12f;          // wing flaps only play this close to the player

    private float flapTimer;
    private Rigidbody2D rb;
    private Collider2D col;
    private Monster monster;
    private Transform player;
    private State state = State.Idle;
    private Vector2 home;
    private Vector2 wanderTarget;
    private Vector2 diveDir;
    private float stateTime;
    private float nextFireTime;
    private float nextDiveTime;
    private float bobOffset;
    private int hoverSide = 1;

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
        bobOffset = Random.value * 10f;
        hoverSide = Random.value < 0.5f ? -1 : 1;
        nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
        nextDiveTime = Time.time + Random.Range(1f, diveCooldown);
    }

    private void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            SetState(State.Idle);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= hearRange)
        {
            flapTimer -= Time.deltaTime;
            if (flapTimer <= 0f)
            {
                flapTimer = flapInterval;
                SoundManager.Instance.PlaySound2D("MonsFly", flapVolume);
            }
        }

        switch (state)
        {
            case State.Idle:
                if (dist <= detectRange && CanSeePlayer()) SetState(State.Chase);
                break;

            case State.Chase:
                if (dist > loseRange) { home = transform.position; SetState(State.Idle); break; }

                if (bulletPrefab != null && dist <= shootRange && Time.time >= nextFireTime && CanSeePlayer())
                {
                    Shoot();
                    nextFireTime = Time.time + fireInterval;
                }

                if (canDive && dist <= diveRange && Time.time >= nextDiveTime && CanSeePlayer())
                {
                    diveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    SetState(State.Dive);
                }
                break;

            case State.Dive:
                if (stateTime >= diveWindup + diveTime)
                {
                    nextDiveTime = Time.time + diveCooldown;
                    SetState(State.Recover);
                }
                break;

            case State.Recover:
                if (stateTime >= recoverTime) SetState(State.Chase);
                break;
        }

        stateTime += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (Time.time < monster.LastHitTime + hitStunTime) return; // let knockback play out

        Vector2 pos = rb.position;
        Vector2 desired = Vector2.zero;

        switch (state)
        {
            case State.Idle:
                if (Vector2.Distance(pos, wanderTarget) < 0.3f)
                    wanderTarget = home + Random.insideUnitCircle * wanderRadius;
                desired = (wanderTarget - pos).normalized * idleSpeed;
                break;

            case State.Chase:
            case State.Recover:
                // Hang out above and to one side of the player; swap sides if the player walks past
                float dx = player.position.x - pos.x;
                if (Mathf.Abs(dx) > hoverOffset.x * 2f) hoverSide = dx > 0f ? -1 : 1;
                Vector2 target = (Vector2)player.position + new Vector2(hoverOffset.x * hoverSide, hoverOffset.y);
                Vector2 toTarget = target - pos;
                float speed = state == State.Recover ? chaseSpeed * 0.6f : chaseSpeed;
                desired = Vector2.ClampMagnitude(toTarget * 2f, 1f) * speed;
                break;

            case State.Dive:
                if (stateTime < diveWindup)
                {
                    // Windup: stop and shake as a warning
                    rb.linearVelocity = Vector2.zero;
                    rb.position = pos + Random.insideUnitCircle * 0.03f;
                    FaceX(diveDir.x);
                    return;
                }
                rb.linearVelocity = diveDir * diveSpeed;
                FaceX(diveDir.x);
                return;
        }

        desired = AvoidWalls(desired);
        desired.y += Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);

        if (state != State.Idle && player != null) FaceX(player.position.x - pos.x);
        else FaceX(rb.linearVelocity.x);
    }

    // If a wall is in the way, slide along it (try up first, then down)
    private Vector2 AvoidWalls(Vector2 desired)
    {
        if (desired.sqrMagnitude < 0.01f) return desired;

        float probe = col.bounds.extents.magnitude + 0.5f;
        Vector2 dir = desired.normalized;
        if (!Physics2D.Raycast(col.bounds.center, dir, probe, groundLayer)) return desired;

        Vector2 up = new Vector2(dir.x * 0.3f, 1f).normalized;
        if (!Physics2D.Raycast(col.bounds.center, up, probe, groundLayer)) return up * desired.magnitude;

        Vector2 down = new Vector2(dir.x * 0.3f, -1f).normalized;
        if (!Physics2D.Raycast(col.bounds.center, down, probe, groundLayer)) return down * desired.magnitude;

        return -desired;
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

    private void SetState(State s)
    {
        if (state == s) return;
        state = s;
        stateTime = 0f;
    }

    private void FaceX(float x)
    {
        if (Mathf.Abs(x) < 0.05f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (x > 0f ? 1f : -1f);
        transform.localScale = s;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Diving into a wall ends the dive early
        if (state == State.Dive && (groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            nextDiveTime = Time.time + diveCooldown;
            SetState(State.Recover);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (contactDamage <= 0f) return;
        var health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        Vector2 dir = (collision.transform.position - transform.position).normalized;
        health.TakeDamage(contactDamage, dir);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, diveRange);
    }
}
