using UnityEngine;

// Put this on the Canvas of the LAST level only (not on the panel itself).
// When that level is cleared, the panel is shown instead of loading another scene.
// Hook the buttons' OnClick to BackToMainMenu() and PlayAgain().
public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance { get; private set; }
    public static bool IsShown { get; private set; }

    [SerializeField] private GameObject endPanel;
    [SerializeField] private string mainMenuScene = "Main";
    [SerializeField] private string firstLevelScene = "Gameplay";

    private void Awake()
    {
        Instance = this;
        IsShown = false;
        if (endPanel != null) endPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsShown = false;
    }

    // Called when a level is cleared. Levels without an EndGameUI just go to the next scene.
    public static void LevelCleared()
    {
        if (Instance != null) Instance.Show();
        else SceneFader.FadeToNext();
    }

    private void Show()
    {
        if (IsShown) return;
        IsShown = true;
        if (endPanel != null) endPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void BackToMainMenu()
    {
        SoundManager.Instance.PlaySound2D("Button");
        SceneFader.FadeTo(mainMenuScene); // the fader sets Time.timeScale back to 1
    }

    public void PlayAgain()
    {
        SoundManager.Instance.PlaySound2D("Button");
        SceneFader.FadeTo(firstLevelScene);
    }
}
