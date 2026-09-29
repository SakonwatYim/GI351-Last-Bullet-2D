using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the Canvas in the Main scene.
// Hook the buttons' OnClick to PlayGame() and QuitGame().
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "Gameplay";

    private void Start()
    {
        Time.timeScale = 1f; // in case we came back from a paused Game Over
        MusicManager.Instance.PlayMusic("Main");
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(gameplayScene);
        MusicManager.Instance.PlayMusic("GamePlay");
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
