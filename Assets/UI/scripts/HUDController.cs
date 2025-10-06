using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;


public class HUDController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("REC")]
    [SerializeField] private RawImage recDot;      
    [SerializeField] private TMP_Text recLabel;     
    [SerializeField] private float recBlinkRate = 0.6f;

    [Header("Clock")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private int startHour = 0;      // 0 = 12AM
    [SerializeField] private float secondsPerGameMinute = 1.0f; // speed of in-game time

    [Header("Camera Flash")]
    [SerializeField] private RawImage flashImage;        
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shutterClip; 
    [SerializeField] private float flashDuration = 0.3f;
    [Header("Report Alert System")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float alertDuration = 2.0f;
    [SerializeField] private TMPro.TMP_Text strikesText;
    [SerializeField] private TMPro.TMP_Text activeText;

    private float flashTimer;
    private bool isFlashing;
    private float gameMinutes;
    private float blinkTimer;
    private float alertTimer;
    private bool showingAlert;
    private Color color;

    void Start()
    {
        gameMinutes = startHour * 60;
        if (flashImage) flashImage.gameObject.SetActive(false);
        if (promptText)
        {
            promptText.text = "Press Enter (or Right-Click) to Report";
            promptText.color = Color.white;
            showingAlert = true;
            alertTimer = 0f;
        }
        UpdateClockUI();
    }

    void Update()
    {
        if (showingAlert)
        {
            alertTimer += Time.deltaTime;
            if (alertTimer >= alertDuration)
            {
                if (promptText) promptText.text = "";
                showingAlert = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(1))
        {
            TriggerFlash();
        }

        if (isFlashing)
        {
            flashTimer += Time.deltaTime;
            if (flashTimer >= flashDuration)
            {
                if (flashImage) flashImage.gameObject.SetActive(false);
                isFlashing = false;
                flashTimer = 0f;
            }
        }

        // REC blink
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= recBlinkRate)
        {
            blinkTimer = 0f;
            ToggleRec();
        }

        // Game clock tick
        gameMinutes += (Time.deltaTime / secondsPerGameMinute);
        UpdateClockUI();
    }
    
    public void CorrectReportMade()
    {
        Debug.Log("[HUDController] Correct report!");
        ShowAlert("CORRECT REPORT", Color.green);
    }

    public void WrongReportMade()
    {
        Debug.Log("[HUDController] Wrong report!");
        ShowAlert("WRONG REPORT", Color.red);
    }
    
    void ShowAlert(string message, Color color)
    {
        if (promptText)
        {
            promptText.text = message;
            promptText.color = color;
            showingAlert = true;
            alertTimer = 0f;
        }
    }

    void TriggerFlash()
    {
        Debug.Log("[HUDController] Camera flash triggered!");
        
        // Play shutter sound
        if (audioSource && shutterClip)
        {
            audioSource.PlayOneShot(shutterClip);
        }
        
        // Show flash
        if (flashImage)
        {
            flashImage.gameObject.SetActive(true);
            isFlashing = true;
            flashTimer = 0f;
        }
    }

    void ToggleRec()
    {
        if (recDot) recDot.enabled = !recDot.enabled;
        if (recLabel) recLabel.alpha = recLabel.alpha > 0.5f ? 0f : 1f;
    }

    void UpdateClockUI()
    {
        if (!timeText) return;
        int total = Mathf.FloorToInt(gameMinutes);
        int hh = (total / 60) % 24;
        int mm = total % 60;
        int dd = hh >= 12 ? 1 : 0;
        int displayH = hh % 12; if (displayH == 0) displayH = 12;
        timeText.text = $"{displayH:00}:{mm:00} {(dd==0 ? "AM" : "PM")}";
    }

    public void GoToMenu()
    {
        Debug.Log("[BackToMenu] Going back to main menu");
        Time.timeScale = 1f; // Reset time in case game was paused
        SceneManager.LoadScene(mainMenuSceneName);
    }
    // Minimal implementations so callers compile:
    public void PlayReportFlash(bool success)
    {
        TriggerFlash();
        Debug.Log(success ? "[HUD] Correct report!" : "[HUD] Wrong report!");
    }

    public void SetStrikes(int current, int max)
    {
        if (strikesText) strikesText.text = $"Strikes: {current}/{max}";
    }

    public void SetActive(int current, int max)
    {
        if (activeText) activeText.text = $"Active: {current}/{max}";
    }
}
