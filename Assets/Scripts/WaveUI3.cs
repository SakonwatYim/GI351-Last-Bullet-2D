using System.Collections;
using TMPro;
using UnityEngine;

// Shows wave info from WaveSpawner. Put on the Canvas (or any UI object).
// Every text field is optional — leave empty to skip it.
public class WaveUI3 : MonoBehaviour
{
    [SerializeField] private WaveSpawner3 spawner;          // leave empty = find in scene

    [Header("HUD")]
    [SerializeField] private TMP_Text waveText;            // "WAVE 3"
    [SerializeField] private TMP_Text aliveText;           // "Monsters: 5"  /  "Next wave in 3"

    [Header("Announcement")]
    [SerializeField] private TMP_Text announceText;        // big center text, fades in/out
    [SerializeField] private float announceHold = 1.5f;
    [SerializeField] private float announceFade = 0.4f;
    [SerializeField] private Color waveStartColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private Color waveClearColor = new Color(0.4f, 1f, 0.5f);

    private Coroutine announceRoutine;

    private void Awake()
    {
        if (spawner == null) spawner = FindAnyObjectByType<WaveSpawner3>();
        if (announceText != null) SetAlpha(announceText, 0f);
    }

    private void OnEnable()
    {
        if (spawner == null) return;
        spawner.OnWaveStarted += HandleWaveStarted;
        spawner.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        if (spawner == null) return;
        spawner.OnWaveStarted -= HandleWaveStarted;
        spawner.OnWaveCleared -= HandleWaveCleared;
    }

    private void Update()
    {
        if (spawner == null) return;

        if (waveText != null)
            waveText.text = "Stage 3";

        if (aliveText != null)
        {
            aliveText.text = spawner.IsBreak
                ? "ready"
                : $"Monsters: {spawner.AliveCount}";
        }
    }

    private void HandleWaveStarted(int wave) => Announce("gO", waveStartColor);

    private void HandleWaveCleared(int wave) => Announce("CLEARED!", waveClearColor);

    private void Announce(string message, Color color)
    {
        if (announceText == null) return;
        if (announceRoutine != null) StopCoroutine(announceRoutine);
        announceText.text = message;
        announceText.color = color;
        announceRoutine = StartCoroutine(AnnounceRoutine());
    }

    private IEnumerator AnnounceRoutine()
    {
        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(announceHold);
        yield return Fade(1f, 0f);
        announceRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < announceFade; t += Time.deltaTime)
        {
            SetAlpha(announceText, Mathf.Lerp(from, to, t / announceFade));
            yield return null;
        }
        SetAlpha(announceText, to);
    }

    private static void SetAlpha(TMP_Text text, float a)
    {
        Color c = text.color;
        c.a = a;
        text.color = c;
    }
}
