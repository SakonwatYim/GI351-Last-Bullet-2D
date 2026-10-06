using UnityEngine;
using UnityEngine.InputSystem;

// "The Last Bullet" style gun: the LESS ammo you have, the STRONGER each shot is.
// Put this on the Player. Aim with the mouse, Left Click to shoot, E to throw away ammo.
// Each shot pushes the player the opposite way (shoot down = rocket jump).
[RequireComponent(typeof(PlayerController))]
public class Gun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gunPivot;   // child of Player, rotates toward the mouse
    [SerializeField] private Transform firePoint;  // child of gunPivot, at the barrel tip
    [SerializeField] private Bullet bulletPrefab;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo ;
    [SerializeField] private int startAmmo;
    [SerializeField] private int dropAmount = 5;
    [SerializeField] private float fireCooldown = 0.2f;

    [Header("Power: full ammo -> last bullet")]
    [Tooltip("X = power (0 at full ammo, 1 at last bullet). Reshape to make power ramp up late or early.")]
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 10f;
    [SerializeField] private int minPellets = 1;
    [SerializeField] private int maxPellets = 7;
    [SerializeField] private float maxSpreadAngle = 45f;
    [SerializeField] private float minKnockback = 3f;
    [SerializeField] private float maxKnockback = 18f;
    [SerializeField] private float minBulletScale = 1f;
    [SerializeField] private float maxBulletScale = 2f;
    [SerializeField] private float bulletSpeed = 22f;

    [Header("Last Bullet = Laser")]
    [SerializeField] private bool lastBulletIsLaser = true;
    [SerializeField] private float laserDamage = 30f;
    [SerializeField] private float laserRange = 30f;
    [SerializeField] private float laserWidth = 0.5f;
    [SerializeField] private float laserDuration = 0.3f;
    [SerializeField] private float laserKnockback = 25f;
    [SerializeField] private Color laserColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Layers that stop the laser (e.g. Ground). Everything else is pierced.")]
    [SerializeField] private LayerMask laserBlockLayers;

    [Header("Sound")]
    [Tooltip("At this much ammo or less the shot uses the Shoot2-20 sound, above it Shoot21-30.")]
    [SerializeField] private int heavyShotAmmo = 20;

    [Header("Debug")]
    [SerializeField] private bool showAmmoGUI = true;

    public int CurrentAmmo { get; private set; }
    public int MaxAmmo => maxAmmo;

    // 0 = full ammo (weakest), 1 = last bullet (strongest)
    public float Power
    {
        get
        {
            if (maxAmmo <= 1) return 1f;
            float t = 1f - (CurrentAmmo - 1) / (float)(maxAmmo - 1);
            return powerCurve.Evaluate(Mathf.Clamp01(t));
        }
    }

    public int PelletCount => Mathf.RoundToInt(Mathf.Lerp(minPellets, maxPellets, Power));
    public float Damage => Mathf.Lerp(minDamage, maxDamage, Power);

    private PlayerController player;
    private Camera cam;
    private Vector2 aimDirection = Vector2.right;
    private float nextFireTime;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        cam = Camera.main;
        CurrentAmmo = Mathf.Clamp(startAmmo, 0, maxAmmo);
    }

    private void Update()
    {
        if (PauseMenu.IsPaused || EndGameUI.IsShown) return; // clicking menu buttons must not fire the gun

        UpdateAim();

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            TryShoot();

        if (kb != null && kb.eKey.wasPressedThisFrame)
            DropAmmo();
    }

    private void UpdateAim()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || Mouse.current == null) return;

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        mouseScreen.z = -cam.transform.position.z;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        Vector2 origin = gunPivot != null ? (Vector2)gunPivot.position : (Vector2)transform.position;
        Vector2 dir = mouseWorld - origin;
        if (dir.sqrMagnitude > 0.0001f)
            aimDirection = dir.normalized;

        if (gunPivot == null) return;

        // The player flips by scaling X to -1, so convert the aim into the parent's local space
        float parentSign = gunPivot.parent != null ? Mathf.Sign(gunPivot.parent.lossyScale.x) : 1f;
        Vector2 localDir = new Vector2(aimDirection.x * parentSign, aimDirection.y);
        float angle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;
        gunPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Keep the gun sprite from being upside-down when aiming backwards
        Vector3 s = gunPivot.localScale;
        s.y = Mathf.Abs(s.y) * (localDir.x < 0f ? -1f : 1f);
        gunPivot.localScale = s;
    }

    private void TryShoot()
    {
        if (Time.time < nextFireTime || CurrentAmmo <= 0) return;

        if (IsLaserNext)
        {
            nextFireTime = Time.time + fireCooldown;
            FireLaser();
            SoundManager.Instance.PlaySound2D("ShootRazor");
            CurrentAmmo--;
            player.ApplyKnockback(-aimDirection * laserKnockback);
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.5f, 0.35f);
            return;
        }

        if (bulletPrefab == null) return;
        nextFireTime = Time.time + fireCooldown;

        // Power is read BEFORE using the bullet, so the very last bullet gets full power
        float power = Power;
        int pellets = PelletCount;
        float damage = Damage;
        float spread = pellets > 1 ? maxSpreadAngle * power : 0f;
        float scale = Mathf.Lerp(minBulletScale, maxBulletScale, power);
        float knockback = Mathf.Lerp(minKnockback, maxKnockback, power);

        Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        for (int i = 0; i < pellets; i++)
        {
            float offset = pellets == 1 ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (float)(pellets - 1));
            Vector2 dir = Quaternion.Euler(0f, 0f, offset) * aimDirection;
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Bullet b = Instantiate(bulletPrefab, spawnPos, Quaternion.Euler(0f, 0f, rot));
            b.Init(dir * bulletSpeed, damage, scale, gameObject);
        }

        SoundManager.Instance.PlaySound2D(CurrentAmmo <= heavyShotAmmo ? "Shoot2-20" : "Shoot21-30");
        CurrentAmmo--;
        player.ApplyKnockback(-aimDirection * knockback);

        // Stronger shots shake the screen more
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(Mathf.Lerp(0.05f, 0.3f, power), 0.15f);
    }

    public bool IsLaserNext => lastBulletIsLaser && CurrentAmmo == 1;

    private void FireLaser()
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 end = origin + aimDirection * laserRange;

        // Pierce through everything until something on laserBlockLayers (e.g. a wall) is hit
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, laserWidth * 0.5f, aimDirection, laserRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var alreadyHit = new System.Collections.Generic.HashSet<IDamageable>();
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;

            if ((laserBlockLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                end = origin + aimDirection * hit.distance;
                break;
            }

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null && alreadyHit.Add(target))
                target.TakeDamage(laserDamage, aimDirection);
        }

        StartCoroutine(ShowLaser(origin, end));
    }

    private System.Collections.IEnumerator ShowLaser(Vector2 start, Vector2 end)
    {
        var go = new GameObject("Laser");
        var line = go.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = line.endColor = laserColor;
        line.numCapVertices = 4;
        line.sortingOrder = 100;

        // Beam starts thick and shrinks away
        for (float t = 0f; t < laserDuration; t += Time.deltaTime)
        {
            float w = laserWidth * (1f - t / laserDuration);
            line.startWidth = line.endWidth = w;
            yield return null;
        }

        Destroy(line.material);
        Destroy(go);
    }

    private void DropAmmo()
    {
        int amount = Mathf.Min(dropAmount, CurrentAmmo);
        if (amount <= 0) return;
        CurrentAmmo -= amount; // thrown away: nothing is dropped to pick back up
    }

    // Returns how many bullets were actually added (0 if already full)
    public int AddAmmo(int amount)
    {
        int added = Mathf.Clamp(amount, 0, maxAmmo - CurrentAmmo);
        CurrentAmmo += added;
        return added;
    }

    private void OnGUI()
    {
        if (!showAmmoGUI) return;
        GUI.Label(new Rect(10, 10, 400, 25), $"Ammo: {CurrentAmmo} / {maxAmmo}");
        if (IsLaserNext)
            GUI.Label(new Rect(10, 30, 400, 25), $"LAST BULLET: LASER   Damage: {laserDamage:0.0}");
        else
            GUI.Label(new Rect(10, 30, 400, 25), $"Power: {Power * 100f:0}%   Pellets: {PelletCount}   Damage: {Damage:0.0}");
    }
}
