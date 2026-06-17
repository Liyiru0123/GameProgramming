using UnityEngine;
using TMPro;

public class TutorialUIManager : MonoBehaviour
{
    public GameObject tutorialPanel;
    public TMP_Text tutorialText;

    public void ShowMessage(string message)
    {
        tutorialText.text = message;
        tutorialPanel.SetActive(true);
    }

    public void HideMessage()
    {
        tutorialPanel.SetActive(false);
    }
}