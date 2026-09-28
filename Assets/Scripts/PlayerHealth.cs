using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the Player. Takes damage from monster bullets / touching monsters,
// blinks while invincible, and restarts the scene on death.
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibleTime = 1f;
    [SerializeField] private Vector2 hurtKnockback = new Vector2(7f, 6f);
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private float restartDelay = 1.5f;
    [SerializeField] private bool showDebugGUI = true;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private PlayerController controller;
    private SpriteRenderer[] renderers;
    private float invincibleUntil;
    private AbsorbSkill absorbSkill;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        absorbSkill = GetComponent<AbsorbSkill>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (IsDead || Time.time < invincibleUntil) return;
        if (absorbSkill != null && absorbSkill.IsActive) return; // invincible while absorbing

        CurrentHealth -= Mathf.Max(1, Mathf.RoundToInt(amount));

        float side = hitDirection.x >= 0f ? 1f : -1f;
        controller.ApplyKnockback(new Vector2(side * hurtKnockback.x, hurtKnockback.y));
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.3f, 0.2f);

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        invincibleUntil = Time.time + invincibleTime;
        StartCoroutine(Blink());
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    private IEnumerator Blink()
    {
        while (Time.time < invincibleUntil)
        {
            SetVisible(false);
            yield return new WaitForSeconds(blinkInterval);
            SetVisible(true);
            yield return new WaitForSeconds(blinkInterval);
        }
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        foreach (var r in renderers)
            if (r != null) r.enabled = visible;
    }

    private void Die()
    {
        IsDead = true;
        CurrentHealth = 0;
        controller.enabled = false;
        var gun = GetComponent<Gun>();
        if (gun != null) gun.enabled = false;
        SetVisible(false);
        StartCoroutine(ShowGameOver());
    }

    private IEnumerator ShowGameOver()
    {
        yield return new WaitForSeconds(restartDelay);
        if (GameOverUI.Instance != null) GameOverUI.Instance.Show();
    }


    private void OnGUI()
    {
        if (!showDebugGUI) return;
        GUI.Label(new Rect(10, 50, 400, 25), IsDead ? "YOU DIED" : $"HP: {CurrentHealth} / {maxHealth}");
    }
}
