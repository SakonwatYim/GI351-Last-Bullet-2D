using UnityEngine;

// Anything bullets can hurt (monsters, breakable objects, ...)
public interface IDamageable
{
    void TakeDamage(float amount, Vector2 hitDirection);
}
