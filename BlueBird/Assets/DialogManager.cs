using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI")]
    private Action onDialogueEnd;
    public GameObject dialoguePanel;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;

    [Header("Typing")]
    public float typingSpeed = 0.03f;

    private string speakerName;
    private string[] dialogueLines;
    private int currentLineIndex;

    private bool isDialogueActive;
    private bool isTyping;

    private Coroutine typingCoroutine;

    private void Awake()
    {
        Instance = this;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                FinishCurrentLineImmediately();
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartDialogue(string speaker, string[] lines, Action endCallback = null)
    {
        onDialogueEnd = endCallback;
        Debug.Log("StartDialogue 被调用了");

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Dialogue lines 是空的");
            return;
        }

        if (lines == null || lines.Length == 0) return;

        speakerName = speaker;
        dialogueLines = lines;
        currentLineIndex = 0;

        isDialogueActive = true;

        dialoguePanel.SetActive(true);
        speakerNameText.text = speakerName;

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(dialogueLines[currentLineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void FinishCurrentLineImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = dialogueLines[currentLineIndex];
        isTyping = false;
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);

        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}