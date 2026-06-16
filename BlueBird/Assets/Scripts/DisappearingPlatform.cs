using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DisappearingPlatform : MonoBehaviour
{
    [SerializeField] private float disappearDelay = 0.5f;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private bool flashBeforeDisappear = true;
    [SerializeField] private int flashCount = 3;

    private Renderer[] cachedRenderers;
    private Collider2D[] cachedColliders;
    private MovingPlatform movingPlatform;
    private bool cycleRunning;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponents<Collider2D>();
        movingPlatform = GetComponent<MovingPlatform>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartCycle(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryStartCycle(collision.collider);
    }

    private void TryStartCycle(Collider2D other)
    {
        if (cycleRunning)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerRespawn>() == null)
        {
            return;
        }

        StartCoroutine(DisappearCycle());
    }

    private IEnumerator DisappearCycle()
    {
        cycleRunning = true;

        yield return StartCoroutine(PlayWarningFlash());

        SetPlatformState(false);
        movingPlatform?.DetachAllRiders();

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        SetPlatformState(true);
        cycleRunning = false;
    }

    private IEnumerator PlayWarningFlash()
    {
        if (disappearDelay <= 0f)
        {
            yield break;
        }

        if (!flashBeforeDisappear || flashCount <= 0 || cachedRenderers.Length == 0)
        {
            yield return new WaitForSeconds(disappearDelay);
            yield break;
        }

        float flashWindow = Mathf.Min(disappearDelay, 0.2f * flashCount);
        float stableWindow = Mathf.Max(0f, disappearDelay - flashWindow);

        if (stableWindow > 0f)
        {
            yield return new WaitForSeconds(stableWindow);
        }

        float interval = flashWindow / (flashCount * 2f);
        for (int i = 0; i < flashCount; i++)
        {
            SetRenderersEnabled(false);
            yield return new WaitForSeconds(interval);
            SetRenderersEnabled(true);
            yield return new WaitForSeconds(interval);
        }
    }

    private void SetPlatformState(bool visible)
    {
        SetRenderersEnabled(visible);

        foreach (Collider2D platformCollider in cachedColliders)
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = visible;
            }
        }
    }

    private void SetRenderersEnabled(bool visible)
    {
        foreach (Renderer cachedRenderer in cachedRenderers)
        {
            if (cachedRenderer != null)
            {
                cachedRenderer.enabled = visible;
            }
        }
    }
}
