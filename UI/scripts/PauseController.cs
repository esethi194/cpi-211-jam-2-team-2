using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel; 

    [Header("Scenes")]
    [SerializeField] private string rulesSceneName    = "Rules";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Shortcuts")]
    [SerializeField] private KeyCode pauseKey = KeyCode.P;       // toggle pause
    [SerializeField] private KeyCode winKey   = KeyCode.L;       // trigger win
    [SerializeField] private KeyCode loseKey  = KeyCode.K;       // trigger lose
    [SerializeField] private KeyCode retryKey = KeyCode.R;       // restart level
    [SerializeField] private KeyCode menuKey  = KeyCode.M;       // go to menu
    [SerializeField] private KeyCode nextKey  = KeyCode.Alpha1;  // next level
    [SerializeField] private KeyCode prevKey  = KeyCode.Alpha2;  // previous level

    private bool isPaused;

    void Awake()
    {
        if (!pausePanel)
        {
            var t = transform.Find("PauseMenu");
            if (t) pausePanel = t.gameObject;
        }

        isPaused = false;
        if (pausePanel) pausePanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Start()
    {
        if (!winController)  winController  = FindFirstObjectByType<WinCaseController>();
        if (!loseController) loseController = FindFirstObjectByType<LoseCaseController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey)) TogglePauseMenu();
        if (Input.GetKeyDown(winKey))   TriggerWin();
        if (Input.GetKeyDown(loseKey))  TriggerLose();
        if (Input.GetKeyDown(retryKey)) RestartLevel();
        if (Input.GetKeyDown(menuKey))  GoToMenu();
        if (Input.GetKeyDown(nextKey))  LoadByOffset(+1);
        if (Input.GetKeyDown(prevKey))  LoadByOffset(-1);
    }

    public void TogglePauseMenu() => SetPaused(!isPaused);
    public void OpenPauseMenu()   => SetPaused(true);
    public void Resume()          => SetPaused(false);

    public void OpenRules()
    {
        if (pausePanel) pausePanel.SetActive(false);
        SceneManager.LoadScene(rulesSceneName, LoadSceneMode.Additive);
    }

    public void ShowPauseAgain()
    {
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void TriggerWin()
    {
        if (!winController) winController = FindFirstObjectByType<WinCaseController>();
        if (winController)  winController.TriggerWin();
        else Debug.LogWarning("WinCaseController not found in scene.");
    }

    public void TriggerLose()
    {
        if (!loseController) loseController = FindFirstObjectByType<LoseCaseController>();
        if (loseController)  loseController.TriggerLose();
        else Debug.LogWarning("LoseCaseController not found in scene.");
    }

    private void SetPaused(bool pause)
    {
        isPaused = pause;
        if (pausePanel) pausePanel.SetActive(pause);

        Time.timeScale = pause ? 0f : 1f;
        Cursor.visible = pause;
        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        var idx = SceneManager.GetActiveScene().buildIndex;
        if (idx >= 0) SceneManager.LoadScene(idx);
    }

    private void LoadByOffset(int delta)
    {
        int cur  = SceneManager.GetActiveScene().buildIndex;
        int next = Mathf.Clamp(cur + delta, 0, SceneManager.sceneCountInBuildSettings - 1);
        if (next != cur)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(next);
        }
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
