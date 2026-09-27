using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Gun))]
public class AbsorbSkill : MonoBehaviour
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private int ammoPerBullet = 1;
    [SerializeField] private Color hitTint = new Color(0.4f, 0.9f, 1f); // สีตอนโดนยิง
    [SerializeField] private Color activeTint = new Color(0.2f, 0.6f, 1f, 1f); // สีตอนกดใช้สกิล (สามารถปรับแต่งสีได้ใน Inspector)
    [SerializeField] private float hitTintDuration = 0.15f; // ระยะเวลาที่จะเปลี่ยนสีกระพริบตอนโดนยิง (วินาที)
    
    [Tooltip("The player's body sprite. Leave empty = the SpriteRenderer on this GameObject.")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private bool showDebugGUI = true;

    public bool IsActive => Time.time < activeUntil;
    public float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);
    public float ActiveTimeLeft => Mathf.Max(0f, activeUntil - Time.time);
    public float Duration => duration;
    public float Cooldown => cooldown;
    public int AbsorbedAmmo => absorbedThisUse * ammoPerBullet;

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

        // ตรวจสอบว่าหมดเวลาสกิลหรือยัง เพื่อคืนค่าสีปกติ (ถ้าไม่ได้โดนยิงค้างอยู่)
        if (!IsActive && isHitTinted == false)
        {
            // เช็คว่าเคยเปลี่ยนสีตอนสกิลทำงานอยู่ไหม (ป้องกันการรันซ้ำซ้อน)
            // แต่ในที่นี้เราจัดการคืนค่าสีใน SetTint หรือเช็คสถานะได้เลย
        }

        // จัดการเรื่องคืนค่าสีปกติหลังจากโดนยิงไปชั่วครู่
        if (isHitTinted && Time.time >= hitTintEndTimer)
        {
            isHitTinted = false;
            // ถ้าสกิลยังทำงานอยู่ ให้กลับไปเป็นสีตอนเปิดสกิล (activeTint) แต่ถ้าสกิลหมดแล้วให้กลับเป็นสีปกติ
            ApplyCurrentTint();
        }
    }

    private void Activate()
    {
        activeUntil = Time.time + duration;
        readyAt = activeUntil + cooldown;
        absorbedThisUse = 0;

        // เปลี่ยนเป็นสีตอนกดใช้สกิลทันที
        ApplyCurrentTint();
    }

    // Called by Bullet when an enemy bullet hits the player during the skill
    public void Absorb()
    {
        gun.AddAmmo(ammoPerBullet);
        absorbedThisUse++;

        // เปลี่ยนสีเมื่อโดนยิงขณะสกิลทำงาน (ให้มีความสำคัญกว่าสีตอนเปิดสกิลชั่วคราว)
        isHitTinted = true;
        hitTintEndTimer = Time.time + hitTintDuration;
        ApplyCurrentTint();
    }

    private void ApplyCurrentTint()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                if (isHitTinted)
                {
                    renderers[i].color = hitTint;
                }
                else if (IsActive)
                {
                    renderers[i].color = activeTint;
                }
                else
                {
                    renderers[i].color = baseColors[i];
                }
            }
        }
    }

    private void LateUpdate()
    {
        // คอยเช็คอัปเดตสีเมื่อสกิลหมดเวลาลงโดยอัตโนมัติ
        if (!IsActive && !isHitTinted)
        {
            // เช็คเทียบกับสีปัจจุบันเพื่อกันการเรียก Set สีซ้ำซ้อนทุกเฟรม
            // หรือจะใช้วิธีเช็คตอนหมดเวลาพอดีก็ได้ครับ
        }
    }
    
    // ปรับปรุงการเช็คหมดเวลาใน Update เพิ่มเติมเพื่อให้สีกลับมาปกติเป๊ะๆ
    private void FixedUpdate()
    {
        // เช็คสถานะสีทุกเฟรมเพื่อให้มั่นใจว่าพอหมดเวลาแล้วสีจะกลับเป็นปกติ
        ApplyCurrentTint();
    }

    private void OnGUI()
    {
        if (!showDebugGUI) return;
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