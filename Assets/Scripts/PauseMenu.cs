using UnityEngine;
using UnityEngine.InputSystem;

// Put this on the Canvas in each gameplay scene (not on the pause panel itself,
// or Esc stops working once the panel is hidden).
// Esc opens/closes the pause panel. Hook the buttons' OnClick to Resume() and BackToMainMenu().
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingPanel; // optional: panel with VolumeSettings
    [SerializeField] private string mainMenuScene = "Main";

    private bool leaving;

    private void Awake()
    {
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        IsPaused = false;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (leaving || EndGameUI.IsShown || kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        // Esc on the settings panel goes back to the pause panel first
        if (settingPanel != null && settingPanel.activeSelf) ShowSettings(false);
        else SetPaused(!IsPaused);
    }

    public void Resume()
    {
        SoundManager.Instance.PlaySound2D("Button");
        SetPaused(false);
    }

    public void BackToMainMenu()
    {
        SoundManager.Instance.PlaySound2D("Button");
        leaving = true; // ignore Esc during the fade
        SceneFader.FadeTo(mainMenuScene); // the fader sets Time.timeScale back to 1
    }

    public void OpenSettings()
    {
        SoundManager.Instance.PlaySound2D("Button");
        ShowSettings(true);
    }

    public void CloseSettings()
    {
        SoundManager.Instance.PlaySound2D("Button");
        ShowSettings(false);
    }

    // Swaps between the pause panel and the settings panel (the game stays paused)
    private void ShowSettings(bool show)
    {
        if (settingPanel != null) settingPanel.SetActive(show);
        if (pausePanel != null) pausePanel.SetActive(!show);
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (settingPanel != null) settingPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }
}
