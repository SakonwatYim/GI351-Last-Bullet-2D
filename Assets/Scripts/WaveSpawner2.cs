using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Endless wave spawner. Put on an empty GameObject in the scene.
// Each wave spawns more monsters than the last; the next wave starts once
// every monster from the current wave is dead (or after maxWaveDuration).
public class WaveSpawner2 : MonoBehaviour
{
    [Serializable]
    private class MonsterEntry
    {
        public Monster prefab;
        [Tooltip("Higher = picked more often.")]
        public float weight = 1f;
        [Tooltip("This monster only appears from this wave on.")]
        public int unlockWave = 1;
    }
    [SerializeField] private int endGame;

    [Header("Monsters")]
    [SerializeField] private MonsterEntry[] monsters;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minDistanceFromPlayer = 6f; // don't spawn right on top of the player

    [Header("Wave Size")]
    [SerializeField] private int startCount;
    [SerializeField] private int extraPerWave = 2;
    [SerializeField] private int maxAlive = 15;           // cap on monsters alive at once

    [Header("Timing")]
    [SerializeField] private float firstWaveDelay = 2f;
    [SerializeField] private float timeBetweenWaves = 0;
    [SerializeField] private float spawnInterval = 0.6f;
    [SerializeField] private float minSpawnInterval = 0.15f;
    [SerializeField] private float spawnIntervalDecay = 0.95f; // interval *= this every wave
    [SerializeField] private float maxWaveDuration = 0f;       // 0 = wait until all dead

    public int CurrentWave { get; private set; }
    public int AliveCount => alive.Count;
    public bool IsBreak { get; private set; }             // true while waiting for the next wave
    public float BreakTimeLeft => IsBreak ? 0 : 0f;

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCleared;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Transform player;
    private PlayerHealth playerHealth;
    private float breakEndTime;

    private void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        IsBreak = true;
        breakEndTime = Time.time + firstWaveDelay;
        yield return new WaitForSeconds(firstWaveDelay);

        while (!PlayerDead())
        {
            CurrentWave++;
            IsBreak = false;
            OnWaveStarted?.Invoke(CurrentWave);

            int toSpawn = startCount + extraPerWave * (CurrentWave - 1);
            float interval = Mathf.Max(minSpawnInterval, spawnInterval * Mathf.Pow(spawnIntervalDecay, CurrentWave - 1));
            float waveStart = Time.time;

            while (toSpawn > 0 && !PlayerDead())
            {
                PruneDead();
                if (alive.Count < maxAlive && SpawnOne())
                    toSpawn--;
                yield return new WaitForSeconds(interval);
            }

            // Wait for the wave to be cleared
            while (!PlayerDead())
            {
                PruneDead();
                if (alive.Count == 0) break;
                if (maxWaveDuration > 0f && Time.time - waveStart >= maxWaveDuration) break;
                yield return null;
            }

            if (PlayerDead()) yield break;

            IsBreak = true;
            breakEndTime = Time.time + timeBetweenWaves;
            OnWaveCleared?.Invoke(CurrentWave);
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }
    private IEnumerator endWave()
    {
        yield return new WaitForSeconds(0f);
        if (passUI2.Instance != null) passUI2.Instance.Show();
    }
    private void Update()
    { 
      if (CurrentWave >= endGame)
        { StartCoroutine(endWave()); }
    }
    private bool SpawnOne()
    {
        Monster prefab = PickMonster();
        Transform point = PickSpawnPoint();
        if (prefab == null || point == null) return false;

        Monster m = Instantiate(prefab, point.position, Quaternion.identity);
        alive.Add(m.gameObject);
        return true;
    }

    private Monster PickMonster()
    {
        if (monsters == null) return null;

        float total = 0f;
        foreach (var e in monsters)
            if (e.prefab != null && CurrentWave >= e.unlockWave) total += e.weight;
        if (total <= 0f) return null;

        float r = Random.value * total;
        foreach (var e in monsters)
        {
            if (e.prefab == null || CurrentWave < e.unlockWave) continue;
            r -= e.weight;
            if (r <= 0f) return e.prefab;
        }
        return null;
    }

    private Transform PickSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        var valid = new List<Transform>();
        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;
            if (player == null || Vector2.Distance(sp.position, player.position) >= minDistanceFromPlayer)
                valid.Add(sp);
        }

        // Every point is too close -> just use any of them
        if (valid.Count == 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        return valid[Random.Range(0, valid.Count)];
    }

    private void PruneDead() => alive.RemoveAll(go => go == null);

    private bool PlayerDead() => playerHealth != null && playerHealth.IsDead;

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.magenta;
        foreach (var sp in spawnPoints)
            if (sp != null) Gizmos.DrawWireSphere(sp.position, 0.5f);
    }
}
