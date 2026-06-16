using UnityEngine;
using TMPro;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea]
    public string message;

    public GameObject tutorialPanel;
    public TMP_Text tutorialText;

    public bool onlyShowOnce = true;

    private bool hasShown = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (onlyShowOnce && hasShown) return;

        tutorialText.text = message;
        tutorialPanel.SetActive(true);
        hasShown = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        tutorialPanel.SetActive(false);
    }
}