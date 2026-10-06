using UnityEngine;

// Optional door. Put on an object with a Collider2D (Is Trigger ON).
// Once every monster is dead, the player walking in goes to the next scene.
// Turn off "Load Next Scene When Cleared" on the MonsterSpawner when using this.
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private MonsterSpawner spawner; // leave empty = find in scene

    private void Awake()
    {
        if (spawner == null) spawner = FindAnyObjectByType<MonsterSpawner>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (spawner != null && !spawner.IsCleared) return;
        SceneFader.FadeToNext();
    }
}
