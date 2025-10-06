using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoseCaseController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TMP_Text correctReportsText;
    [SerializeField] private TMP_Text wrongReportsText;
    [SerializeField] private TMP_Text timePlayedText;
    
    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene = "Game";

    private int correctReports = 0;
    private int wrongReports = 0;
    private float timePlayed = 0f;
    private bool gameOver = false;

    void Start()
    {
        if (losePanel) losePanel.SetActive(true);
    }

    void Update()
        {
            if (!gameOver) timePlayed += Time.deltaTime;
        }

    public void RetryGame()     { Time.timeScale = 1f; SceneManager.LoadScene(gameScene); }
    public void GoToMainMenu()  { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuScene); }

   
    public void AddCorrectReport()
    {
        correctReports++;
    }
    
    public void AddWrongReport()
    {
        wrongReports++;
    }
    public void TriggerLose() { TriggerGameOver(); }
    public void TriggerGameOver()
    {
        gameOver = true;
        Time.timeScale = 0f;
        
        if (losePanel) losePanel.SetActive(true);
        
        if (correctReportsText)
            correctReportsText.text = "Correct Reports: " + correctReports;
        
        if (wrongReportsText)
            wrongReportsText.text = "Wrong Reports: " + wrongReports;
        
        if (timePlayedText)
        {
            int m = Mathf.FloorToInt(timePlayed / 60);
            int s = Mathf.FloorToInt(timePlayed % 60);
            timePlayedText.text = "Time Survived: " + m.ToString("00") + ":" + s.ToString("00");
        }
    }
}