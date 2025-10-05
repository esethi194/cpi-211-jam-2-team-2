using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("REC")]
    [SerializeField] private Graphic recDot;      
    [SerializeField] private TMP_Text recLabel;     
    [SerializeField] private float recBlinkRate = 0.6f;

    [Header("Clock")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private int startHour = 0;      // 0 = 12AM
    [SerializeField] private float secondsPerGameMinute = 1.0f; // speed of in-game time


    float gameMinutes;
    float blinkTimer;

    void Start()
    {
        gameMinutes = startHour * 60;
        // if (flash) flash.gameObject.SetActive(false);
        UpdateClockUI();     // initial draw
    }

    void Update()
    {
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
    
    void ToggleRec()
    {
        if (recDot)   recDot.canvasRenderer.SetAlpha(recDot.canvasRenderer.GetAlpha() > 0.5f ? 0f : 1f);
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
}
