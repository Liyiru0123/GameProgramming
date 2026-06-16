using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string checkpointId;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private int sortingOrder = -1;
    [SerializeField] private Color inactiveColor = new Color(0.6f, 0.82f, 1f, 0.72f);
    [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color activationFlashColor = new Color(0.5f, 0.98f, 1f, 1f);
    [SerializeField] private float activateAnimationDuration = 0.34f;
    [SerializeField] private float activateScaleMultiplier = 1.38f;
    [SerializeField] private float activeBobAmount = 0.12f;
    [SerializeField] private float activeBobSpeed = 2.6f;

    private static readonly System.Collections.Generic.Dictionary<string, Checkpoint> Registry =
        new System.Collections.Generic.Dictionary<string, Checkpoint>();

    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;
    public string CheckpointId => string.IsNullOrWhiteSpace(checkpointId) ? name : checkpointId;

    private Coroutine activationCoroutine;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseLocalPosition;
    private bool isActivated;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualRoot == null)
        {
            visualRoot = targetRenderer != null ? targetRenderer.transform : transform;
        }

        ApplyRendererSorting();
        baseScale = visualRoot.localScale;
        baseLocalPosition = visualRoot.localPosition;
        SetActivated(false);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(checkpointId))
        {
            checkpointId = gameObject.name;
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (respawnPoint == null)
        {
            Transform respawnChild = transform.Find("RespawnPoint");
            if (respawnChild != null)
            {
                respawnPoint = respawnChild;
            }
        }

        if (visualRoot == null)
        {
            visualRoot = targetRenderer != null ? targetRenderer.transform : transform;
        }

        ApplyRendererSorting();

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null && !trigger.isTrigger)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        Registry[CheckpointId] = this;
    }

    private void OnDisable()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        if (visualRoot != null)
        {
            visualRoot.localScale = baseScale;
            visualRoot.localPosition = baseLocalPosition;
        }

        if (Registry.TryGetValue(CheckpointId, out Checkpoint registered) && registered == this)
        {
            Registry.Remove(CheckpointId);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn playerRespawn = other.GetComponentInParent<PlayerRespawn>();
        if (playerRespawn == null)
        {
            return;
        }

        ActivateForPlayer(playerRespawn, other.GetComponentInParent<PlayerAudio>());
    }

    public bool ActivateForPlayer(PlayerRespawn playerRespawn, PlayerAudio playerAudio = null)
    {
        if (playerRespawn == null)
        {
            return false;
        }

        bool activatedNewCheckpoint = playerRespawn.SetCheckpoint(this);
        if (activatedNewCheckpoint)
        {
            playerAudio?.PlayCheckpoint();
        }

        return activatedNewCheckpoint;
    }

    public void SetActivated(bool isActive)
    {
        if (isActivated == isActive)
        {
            ApplyVisualState(isActive);
            return;
        }

        isActivated = isActive;

        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        if (!isActive)
        {
            ApplyVisualState(false);
            return;
        }

        activationCoroutine = StartCoroutine(PlayActivationAnimation());
    }

    private IEnumerator PlayActivationAnimation()
    {
        ApplyVisualState(true);

        if (visualRoot == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, activateAnimationDuration);
        float elapsed = 0f;
        Vector3 startScale = baseScale;
        Vector3 peakScale = baseScale * activateScaleMultiplier;
        Color startColor = activationFlashColor;
        Color endColor = activeColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float scaleT = Mathf.Sin(eased * Mathf.PI);

            visualRoot.localScale = Vector3.LerpUnclamped(startScale, peakScale, scaleT);
            visualRoot.localPosition = baseLocalPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * (activeBobAmount * 1.5f);

            if (targetRenderer != null)
            {
                targetRenderer.color = Color.Lerp(startColor, endColor, eased);
            }

            yield return null;
        }

        if (targetRenderer != null)
        {
            targetRenderer.color = activeColor;
        }

        visualRoot.localScale = baseScale;
        visualRoot.localPosition = baseLocalPosition + Vector3.up * activeBobAmount;
        activationCoroutine = null;
    }

    private void ApplyVisualState(bool active)
    {
        ApplyRendererSorting();

        if (targetRenderer != null)
        {
            targetRenderer.color = active ? activeColor : inactiveColor;
        }

        if (visualRoot != null)
        {
            visualRoot.localScale = baseScale;
            visualRoot.localPosition = active
                ? baseLocalPosition + Vector3.up * activeBobAmount
                : baseLocalPosition;
        }
    }

    private void Update()
    {
        if (!isActivated || visualRoot == null || activationCoroutine != null)
        {
            return;
        }

        Vector3 position = baseLocalPosition;
        position.y += Mathf.Sin(Time.time * activeBobSpeed) * activeBobAmount;
        visualRoot.localPosition = position;
    }

    private void ApplyRendererSorting()
    {
        if (targetRenderer != null)
        {
            targetRenderer.sortingOrder = sortingOrder;
        }
    }

    public static Checkpoint FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        Registry.TryGetValue(id, out Checkpoint checkpoint);
        return checkpoint;
    }
}
