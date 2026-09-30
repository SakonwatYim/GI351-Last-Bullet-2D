using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD for health, ammo and the Absorb skill. Put on the Canvas.
// Bars are UI Images with Image Type = Filled. Every field is optional.
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;          // leave empty = find Player in scene
    [SerializeField] private Gun gun;
    [SerializeField] private AbsorbSkill skill;

    [Header("Health")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;          // "3 / 5"
    [SerializeField] private Color healthColor = new Color(0.9f, 0.2f, 0.25f);
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.6f, 0.1f);
    [SerializeField, Range(0f, 1f)] private float lowHealthPercent = 0.3f;
    [SerializeField] private float barSmoothSpeed = 8f;

    [Header("Ammo")]
    [SerializeField] private Image ammoFill;
    [SerializeField] private TMP_Text ammoText;            // "12 / 30"
    [SerializeField] private TMP_Text powerText;           // "POWER 60%  x4" / "LASER READY"
    [SerializeField] private Color ammoColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color laserColor = new Color(1f, 0.2f, 0.2f);

    [Header("Skill")]
    [SerializeField] private Image skillFill;              // Filled / Radial 360 on top of the skill icon
    [SerializeField] private TMP_Text skillText;  // "READY" / "3.2" / "+4"
    [SerializeField] private TMP_Text skillCount;  // "READY" / "3.2" / "+4"
    [SerializeField] private Color skillReadyColor = new Color(0.4f, 0.9f, 1f);
    [SerializeField] private Color skillActiveColor = Color.white;
    [SerializeField] private Color skillCooldownColor = new Color(0.3f, 0.3f, 0.35f);

    [Header("Death")]
    [SerializeField] private GameObject deathPanel;        // shown when the player dies

    private float shownHealth = 1f;

    private void Awake()
    {
        if (health == null || gun == null || skill == null)
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null)
            {
                if (health == null) health = p.GetComponent<PlayerHealth>();
                if (gun == null) gun = p.GetComponent<Gun>();
                if (skill == null) skill = p.GetComponent<AbsorbSkill>();
            }
        }
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void Update()
    {
        UpdateHealth();
        UpdateAmmo();
        UpdateSkill();
        countSkillUpdate();
    }

    private void UpdateHealth()
    {
        if (health == null) return;

        float target = health.MaxHealth > 0 ? health.CurrentHealth / (float)health.MaxHealth : 0f;
        shownHealth = Mathf.MoveTowards(shownHealth, target, barSmoothSpeed * Time.deltaTime);

        if (healthFill != null)
        {
            healthFill.fillAmount = shownHealth;
            healthFill.color = target <= lowHealthPercent ? lowHealthColor : healthColor;
        }
        if (healthText != null)
            healthText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
        if (deathPanel != null && health.IsDead && !deathPanel.activeSelf)
            deathPanel.SetActive(true);
    }

    private void UpdateAmmo()
    {
        if (gun == null) return;

        bool laser = gun.IsLaserNext;
        Color c = laser ? laserColor : ammoColor;

        if (ammoFill != null)
        {
            ammoFill.fillAmount = gun.MaxAmmo > 0 ? gun.CurrentAmmo / (float)gun.MaxAmmo : 0f;
            ammoFill.color = c;
        }
        if (ammoText != null)
            ammoText.text = $"{gun.CurrentAmmo} / {gun.MaxAmmo}";
        if (powerText != null)
        {
            if (gun.CurrentAmmo <= 0) powerText.text = "NO AMMO";
            else if (laser) powerText.text = "LASER READY";
            else powerText.text = $"POWER {gun.Power * 100f:0}%  x{gun.PelletCount}";
            powerText.color = c;
        }
    }
    private void countSkillUpdate()
    {
        if (skill.count == 0)
        {
            skill.skill = "can use skill 2 time";
        }
        if (skill.count == 1)
        {
            skill.skill = "can use skill 1 time";
        }
        if (skill.count == 2)
        {
            skill.skill = "can use skill 0 time";
        }
        if (skill.count >= 3)
        {
            skill.skill = "cant use skill";
        }
    }
    private void UpdateSkill()
    {
        if (skill == null) return;

        float fill;
        string text;
        Color c;
        
        if (skill.IsActive)
        {
            fill = skill.Duration > 0f ? skill.ActiveTimeLeft / skill.Duration : 0f;
            text = $"+{skill.AbsorbedAmmo}";
            c = skillActiveColor;
        }
        else if (skill.CooldownLeft > 0f)
        {
            fill = skill.Cooldown > 0f ? 1f - skill.CooldownLeft / skill.Cooldown : 1f;
            text = $"{skill.CooldownLeft:0.0}";
            c = skillCooldownColor;
        }
        else
        {
            fill = 1f;
            text = "READY";
            c = skillReadyColor;
        }

        if (skillFill != null)
        {
            skillFill.fillAmount = fill;
            skillFill.color = c;
        }
        if (skillText != null) skillText.text = text;
    }
}
