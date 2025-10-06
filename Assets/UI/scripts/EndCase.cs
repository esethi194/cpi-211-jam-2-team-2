// Assets/UI/Scripts/EndCase.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndCase : MonoBehaviour
{
    [Header("HUD (optional)")]
    [SerializeField] private HUDController hud;

    [Header("End Scenes")]
    [SerializeField] private string winSceneName  = "WinScreen";
    [SerializeField] private string loseSceneName = "LoseScreen";

#if UNITY_EDITOR
    // lets you drag scenes in the Inspector; we store the name for runtime
    [SerializeField] private UnityEditor.SceneAsset winSceneAsset;
    [SerializeField] private UnityEditor.SceneAsset loseSceneAsset;
    void OnValidate()
    {
        if (winSceneAsset)  winSceneName  = winSceneAsset.name;
        if (loseSceneAsset) loseSceneName = loseSceneAsset.name;
    }
#endif

    [Header("Rules")]
    [SerializeField] private int maxStrikes = 3;
    [SerializeField] private int maxActive  = 3;

    private int strikes;

    void Awake()
    {
        if (!hud) hud = FindFirstObjectByType<HUDController>();
        hud?.SetStrikes(0, maxStrikes);
        hud?.SetActive(0,  maxActive);
    }

    void OnEnable()
    {
        GameSignals.Reported      += OnReported;
        GameSignals.ActiveChanged += OnActiveChanged;
        GameSignals.Win           += OnWinSignal;
        GameSignals.Lose          += OnLoseSignal;
    }
    void OnDisable()
    {
        GameSignals.Reported      -= OnReported;
        GameSignals.ActiveChanged -= OnActiveChanged;
        GameSignals.Win           -= OnWinSignal;
        GameSignals.Lose          -= OnLoseSignal;
    }

    // --- events -> logic
    void OnReported(bool ok)
    {
        hud?.PlayReportFlash(ok);

        if (!ok)
        {
            strikes++;
            hud?.SetStrikes(strikes, maxStrikes);
            if (strikes >= maxStrikes) LoadLose();
        }
    }

    void OnActiveChanged(int count)
    {
        hud?.SetActive(count, maxActive);
        if (count >= maxActive) LoadLose();
    }

    void OnWinSignal()            => LoadWin();
    void OnLoseSignal(string _)   => LoadLose();

    // --- scene loads
    public void LoadWin()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(winSceneName);
    }

    public void LoadLose()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(loseSceneName);
    }
}
