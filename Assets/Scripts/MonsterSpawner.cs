using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

// Put on an empty GameObject in each level.
// Spawns one monster at every spawn point as soon as the player leaves the ammo fill area.
// Each spawn point chooses whether it spawns a normal (ground) or a flying monster.
public class MonsterSpawner : MonoBehaviour
{
    public enum MonsterType { Normal, Fly }

    [Serializable]
    private class SpawnPoint
    {
        public Transform point;
        public MonsterType type = MonsterType.Normal;
    }

    [Header("Monsters")]
    [Tooltip("Picked at random for Normal spawn points.")]
    [SerializeField] private Monster[] normalMonsters;
    [Tooltip("Picked at random for Fly spawn points.")]
    [SerializeField] private Monster[] flyMonsters;

    [Header("Spawn Points")]
    [SerializeField] private SpawnPoint[] spawnPoints;

    [Header("When All Monsters Are Dead")]
    [Tooltip("ON = go to the next scene right away. OFF = use a LevelExit door instead.")]
    [SerializeField] private bool loadNextSceneWhenCleared = true;
    [SerializeField] private UnityEvent onAllMonstersDead; // e.g. open a door

    public int AliveCount => alive.Count;
    public bool HasSpawned { get; private set; }
    public bool IsCleared { get; private set; }

    private readonly List<GameObject> alive = new List<GameObject>();

    private void OnEnable() => fillBullet_area.OnPlayerLeft += SpawnAll;

    private void OnDisable() => fillBullet_area.OnPlayerLeft -= SpawnAll;

    private void SpawnAll()
    {
        if (HasSpawned) return;
        HasSpawned = true;

        if (spawnPoints == null) return;
        foreach (var sp in spawnPoints)
        {
            if (sp == null || sp.point == null) continue;

            Monster prefab = PickMonster(sp.type == MonsterType.Fly ? flyMonsters : normalMonsters);
            if (prefab == null) continue;

            Monster m = Instantiate(prefab, sp.point.position, Quaternion.identity);
            alive.Add(m.gameObject);
        }
    }

    private void Update()
    {
        if (!HasSpawned || IsCleared) return;

        alive.RemoveAll(go => go == null);
        if (alive.Count > 0) return;

        IsCleared = true;
        onAllMonstersDead?.Invoke();
        if (loadNextSceneWhenCleared) SceneFader.FadeToNext();
    }

    private static Monster PickMonster(Monster[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        return prefabs[Random.Range(0, prefabs.Length)];
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        foreach (var sp in spawnPoints)
        {
            if (sp == null || sp.point == null) continue;
            Gizmos.color = sp.type == MonsterType.Fly ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(sp.point.position, 0.5f);
        }
    }
}
