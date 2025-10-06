using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameplayScene = "Game";
    [SerializeField] private string rulesScene    = "Rules";

    void Start()
{

}

    void Awake()
    {
        Time.timeScale = 1f;
    }

    void Play()
    {
        Debug.Log($"[MenuController] Loading gameplay scene: {gameplayScene}");
        SceneManager.LoadScene(gameplayScene);
    }

    public void OpenRules()
    {
        Debug.Log($"[MenuController] Loading rules scene: {rulesScene}");
        SceneManager.LoadScene(rulesScene);
    }

    public void QuitApp()
    {
        Debug.Log("[MenuController] Quitting application...");
    #if UNITY_EDITOR
        Debug.Log("[MenuController] Running in Editor - Stopping play mode");
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Debug.Log("[MenuController] Running in Build - Calling Application.Quit()");
        Application.Quit();
    #endif
    }
}
