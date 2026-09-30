using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class passUI : MonoBehaviour
{
    public static passUI Instance { get; private set; }
    [SerializeField] private GameObject PassPanel;
    [SerializeField] private bool pauseOnGameOver = true;
    [SerializeField] private string mainMenuScene = "Main";
    [SerializeField] private WaveSpawner spawner;
    private bool shown;

    private void Awake()
    {
        Instance = this;
        if (spawner == null) spawner = FindAnyObjectByType<WaveSpawner>();
        if (PassPanel != null) PassPanel.SetActive(false);
    }
    public void Show()
    {
        if (shown) return;
        shown = true;
       
            PassPanel.SetActive(true);
            if (pauseOnGameOver) Time.timeScale = 0f;
        
    }
    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
    public void nextGame()
    {
        Time.timeScale = 1f;
        //ทำเป็นด่านต่อไป
       // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
