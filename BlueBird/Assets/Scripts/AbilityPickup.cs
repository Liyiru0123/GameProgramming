using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class AbilityPickup : MonoBehaviour
{
    [SerializeField] private bool unlockDash = true;
    [SerializeField] private string tutorialText = "You learned Dash.";
    [SerializeField] private float tutorialDuration = 2.5f;
    [SerializeField] private GameObject visualToDisable;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }

        if (visualToDisable == null)
        {
            visualToDisable = gameObject;
        }
    }

    private void Start()
    {
        PlayerAbilities abilities = FindObjectOfType<PlayerAbilities>();
        if (unlockDash && abilities != null && abilities.DashUnlocked)
        {
            if (visualToDisable != null)
            {
                visualToDisable.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerAbilities abilities = other.GetComponentInParent<PlayerAbilities>();
        if (abilities == null)
        {
            return;
        }

        if (unlockDash)
        {
            abilities.SetDashUnlocked(true);
        }

        if (!string.IsNullOrWhiteSpace(tutorialText))
        {
            RuntimeGameUI.Instance?.ShowTutorialPrompt(tutorialText, tutorialDuration);
        }

        if (visualToDisable != null)
        {
            visualToDisable.SetActive(false);
        }
    }
}
