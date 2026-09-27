using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Gun))]
public class AbsorbSkill : MonoBehaviour
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private int ammoPerBullet = 1;
    [SerializeField] private Color hitTint = new Color(0.4f, 0.9f, 1f); // สีที่จะเปลี่ยนตอนโดนยิง
    [SerializeField] private float hitTintDuration = 0.15f; // ระยะเวลาที่จะเปลี่ยนสีกระพริบตอนโดนยิง (วินาที)
    [Tooltip("The player's body sprite. Leave empty = the SpriteRenderer on this GameObject.")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    public bool IsActive => Time.time < activeUntil;
    public float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

    private Gun gun;
    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float activeUntil = -999f;
    private float readyAt;
    private int absorbedThisUse;

    // สำหรับจัดการเรื่องเปลี่ยนสีชั่วคราวตอนโดนยิง
    private float hitTintEndTimer = 0f;
    private bool isHitTinted = false;

    private void Awake()
    {
        gun = GetComponent<Gun>();
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        renderers = bodyRenderer != null ? new[] { bodyRenderer } : new SpriteRenderer[0];
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].color;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame && Time.time >= readyAt)
            Activate();

        // จัดการเรื่องคืนค่าสีปกติหลังจากโดนยิงไปชั่วครู่
        if (isHitTinted && Time.time >= hitTintEndTimer)
        {
            SetTint(false);
            isHitTinted = false;
        }
    }

    private void Activate()
    {
        activeUntil = Time.time + duration;
        readyAt = activeUntil + cooldown;
        absorbedThisUse = 0;
    }

    // Called by Bullet when an enemy bullet hits the player during the skill
    public void Absorb()
    {
        gun.AddAmmo(ammoPerBullet);
        absorbedThisUse++;

        // เปลี่ยนสีเมื่อโดนยิงขณะสกิลทำงาน
        SetTint(true);
        isHitTinted = true;
        hitTintEndTimer = Time.time + hitTintDuration;
    }

    private void SetTint(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = on ? hitTint : baseColors[i];
            }
        }
    }

    private void OnGUI()
    {
        string text;
        if (IsActive)
            text = $"Skill [F]: ABSORBING {activeUntil - Time.time:0.0}s   (+{absorbedThisUse * ammoPerBullet} ammo)";
        else if (CooldownLeft > 0f)
            text = $"Skill [F]: cooldown {CooldownLeft:0.0}s";
        else
            text = "Skill [F]: READY";
        GUI.Label(new Rect(10, 70, 400, 25), text);
    }
}