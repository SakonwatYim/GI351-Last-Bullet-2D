using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the Canvas (or any always-active object). Assign the Game Over panel,
// then hook the buttons' OnClick to PlayAgain() and BackToMainMenu().
// Counts monster kills and saves the best (PlayerPrefs).
// Every text field is optional — leave empty to skip it.
public class GameOverUI : MonoBehaviour
{
    private const string BestKillsKey = "BestKills";

    public static GameOverUI Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string mainMenuScene = "Main";
    [SerializeField] private bool pauseOnGameOver = true;

    [Header("Result Texts")]
    [SerializeField] private TMP_Text killsText;           // "Monsters Killed: 12"
    [SerializeField] private TMP_Text bestText;            // "Best: Kills 30"
    [SerializeField] private TMP_Text newRecordText;       // "NEW HIGH SCORE!" (hidden if no record)

    public int Kills { get; private set; }
    public static int BestKills => PlayerPrefs.GetInt(BestKillsKey, 0);

    private bool shown;

    private void Awake()
    {
        Instance = this;
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

        bool newKillRecord = Kills > BestKills;

        if (newKillRecord)
        {
            PlayerPrefs.SetInt(BestKillsKey, Kills);
            PlayerPrefs.Save();
        }

        if (killsText != null) killsText.text = $"Monsters Killed: {Kills}";
        if (bestText != null) bestText.text = $"Best: Kills {BestKills}";
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(newKillRecord);
            newRecordText.text = "NEW HIGH SCORE!";
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (pauseOnGameOver) Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        SoundManager.Instance.PlaySound2D("Button");
        SceneFader.Reload();
    }

    public void BackToMainMenu()
    {
        SoundManager.Instance.PlaySound2D("Button");
        SceneFader.FadeTo(mainMenuScene);
    }
}
