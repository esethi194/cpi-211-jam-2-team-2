using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class WinCaseController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text correctReportsText;
    [SerializeField] private TMP_Text wrongReportsText;
    [SerializeField] private TMP_Text timePlayedText;

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene     = "Game";

    private int   correctReports = 0;
    private int   wrongReports   = 0;
    private float timePlayed     = 0f;
    private bool  gameOver       = false;

    void Awake()
    {
        // Start hidden so it doesn't show at runtime
        if (winPanel) winPanel.SetActive(false);
        gameOver   = false;
        timePlayed = 0f;
    }

    void Update()
    {
        if (!gameOver) timePlayed += Time.deltaTime;
    }

    public void AddCorrectReport() => correctReports++;
    public void AddWrongReport()   => wrongReports++;

    public void TriggerWin()
    {
        Debug.Log("TriggerWin called!");
        gameOver = true;
        Time.timeScale = 0f;

        // update UI text
        if (correctReportsText) correctReportsText.text = $"Correct Reports: {correctReports}";
        if (wrongReportsText)   wrongReportsText.text   = $"Wrong Reports: {wrongReports}";
        if (timePlayedText)     timePlayedText.text     = FormatTime(timePlayed);

        ShowWinPanel();
    }

    void ShowWinPanel()
    {
        if (!winPanel) { Debug.LogError("Win panel is NULL"); return; }

        var canvasGroup = winPanel.GetComponent<CanvasGroup>();
        if (!canvasGroup)
        {
            canvasGroup = winPanel.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1; canvasGroup.interactable = true; canvasGroup.blocksRaycasts = true;

        var cv = winPanel.GetComponent<Canvas>();
        if (!cv)
        {
            cv = winPanel.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.overrideSorting = true; cv.sortingOrder = 1000;
        }

        if (!winPanel.GetComponent<GraphicRaycaster>())
            winPanel.AddComponent<GraphicRaycaster>();

        var rt = winPanel.GetComponent<RectTransform>();
        if (rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.localScale = Vector3.one; }

        winPanel.transform.SetAsLastSibling();
        winPanel.SetActive(true);
        Debug.Log("Win panel activated!");
    }

    string FormatTime(float s)
    {
        int ss = (int)s % 60, mm = (int)(s / 60) % 60, hh = (int)(s / 3600);
        return $"{hh:00}:{mm:00}:{ss:00}";
    }

    public void TryAgain()     { Time.timeScale = 1f; SceneManager.LoadScene(gameScene);         Debug.Log("try again"); }
    public void NextLevel()    { Time.timeScale = 1f; SceneManager.LoadScene(gameScene);         Debug.Log("nextLVL");} // replace with real next-level
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuScene);         Debug.Log("MainMenu");}
}
