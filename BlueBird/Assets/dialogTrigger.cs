using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    public string speakerName;

    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Trigger Setting")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            DialogueManager.Instance.StartDialogue(speakerName, dialogueLines);
            hasTriggered = true;
        }
    }
}