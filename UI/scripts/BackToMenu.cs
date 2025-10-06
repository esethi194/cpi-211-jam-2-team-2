using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void GoToMenu()
    {
        Debug.Log("[BackToMenu] Going back to main menu");
        Time.timeScale = 1f; // Reset time in case game was paused
        SceneManager.LoadScene(mainMenuSceneName);
    }
}