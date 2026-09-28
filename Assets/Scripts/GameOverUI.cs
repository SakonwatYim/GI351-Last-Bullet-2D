using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the Canvas (or any always-active object). Assign the Game Over panel,
// then hook the buttons' OnClick to PlayAgain() and BackToMainMenu().
// Counts monster kills, shows the wave reached, and saves the best of each (PlayerPrefs).
// Every text field is optional — leave empty to skip it.
public class GameOverUI : MonoBehaviour
{
    private const string BestWaveKey = "BestWave";
    private const string BestKillsKey = "BestKills";

    public static GameOverUI Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string mainMenuScene = "Main";
    [SerializeField] private bool pauseOnGameOver = true;
    [SerializeField] private WaveSpawner spawner;          // leave empty = find in scene

    [Header("Result Texts")]
    [SerializeField] private TMP_Text killsText;           // "Monsters Killed: 12"
    [SerializeField] private TMP_Text waveText;            // "Wave Reached: 4"
    [SerializeField] private TMP_Text bestText;            // "Best: Wave 6  |  Kills 30"
    [SerializeField] private TMP_Text newRecordText;       // "NEW HIGH SCORE!" (hidden if no record)

    public int Kills { get; private set; }
    public static int BestWave => PlayerPrefs.GetInt(BestWaveKey, 0);
    public static int BestKills => PlayerPrefs.GetInt(BestKillsKey, 0);

    private bool shown;

    private void Awake()
    {
        Instance = this;
        if (spawner == null) spawner = FindAnyObjectByType<WaveSpawner>();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void OnEnable() => Monster.OnAnyMonsterDied += HandleMonsterDied;

    private void OnDisable() => Monster.OnAnyMonsterDied -= HandleMonsterDied;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleMonsterDied(Monster monster)
    {
        if (!shown) Kills++;
    }

    public void Show()
    {
        if (shown) return;
        shown = true;

        int wave = spawner != null ? spawner.CurrentWave : 0;
        bool newWaveRecord = wave > BestWave;
        bool newKillRecord = Kills > BestKills;

        if (newWaveRecord) PlayerPrefs.SetInt(BestWaveKey, wave);
        if (newKillRecord) PlayerPrefs.SetInt(BestKillsKey, Kills);
        if (newWaveRecord || newKillRecord) PlayerPrefs.Save();

        if (killsText != null) killsText.text = $"Monsters Killed: {Kills}";
        if (waveText != null) waveText.text = $"Wave Reached: {wave}";
        if (bestText != null) bestText.text = $"Best: Wave {BestWave}  |  Kills {BestKills}";
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(newWaveRecord || newKillRecord);
            newRecordText.text = "NEW HIGH SCORE!";
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (pauseOnGameOver) Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
