using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalDialogueTrigger : MonoBehaviour
{
    [Header("Final Dialogue")]
    public string speakerName = " ";

    [TextArea(2, 5)]
    public string[] finalDialogue;

    [Header("Scene")]
    public string mainMenuSceneName = "menu";

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            DialogueManager.Instance.StartDialogue(
                speakerName,
                finalDialogue,
                ReturnToMainMenu
            );
        }
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}