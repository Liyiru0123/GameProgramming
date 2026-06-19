using UnityEngine;
using UnityEngine.SceneManagement;

public class EndPanelManager : MonoBehaviour
{
    public GameObject endPanel;

    private void Start()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }
    }

    public void ShowEndPanel()
    {
        endPanel.SetActive(true);

        // 暂停游戏
        Time.timeScale = 0f;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("menu");
    }
}