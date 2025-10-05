using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private HUDController hud;
    [Header("Scene Names")]
    [SerializeField] private string rulesSceneName = "Rules";
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    private bool isPaused;

    void Start()
    {
        isPaused = false;
        if (pausePanel) pausePanel.SetActive(false);
        
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)){
            SetPaused(!isPaused);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (hud) hud.CorrectReportMade();
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (hud) hud.WrongReportMade();
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void OpenRules()
    {
        if (pausePanel) pausePanel.SetActive(false);
        SceneManager.LoadScene(rulesSceneName, LoadSceneMode.Additive);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowPauseAgain()
    {
        if (pausePanel) pausePanel.SetActive(true);
    }

    private void SetPaused(bool pause)
    {
        isPaused = pause;
        if (pausePanel) pausePanel.SetActive(pause);

        Time.timeScale = pause ? 0f : 1f;
    }
}