using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject pausePanel;      // assign your Pause Canvas (set inactive)

    [Header("Scene Names")]
    [SerializeField] private string rulesSceneName    = "Rules";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Animator recAnimator;

    private bool isPaused;

    void Start()
    {
        isPaused = false;
        if (pausePanel) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            SetPaused(!isPaused);
    }

    //Buttons
    public void Resume() => SetPaused(false);

    public void OpenRules()
    {
        if (pausePanel) pausePanel.SetActive(false);
        SceneManager.LoadScene(rulesSceneName, LoadSceneMode.Additive);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowPauseAgain()
    {
        if (pausePanel) pausePanel.SetActive(true);
    }

    //Core    
    private void SetPaused(bool pause)
    {
        isPaused = pause;
        if (pausePanel) pausePanel.SetActive(pause);

        Time.timeScale = pause ? 0f : 1f;
    }

    
}
