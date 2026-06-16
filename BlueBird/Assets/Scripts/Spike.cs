using UnityEngine;

[DisallowMultipleComponent]
public class Spike : MonoBehaviour
{
    [Header("Forgiveness")]
    [SerializeField] private Vector2 forgivenessInset = new Vector2(0.08f, 0.08f);

    private Collider2D trapCollider;

    private void Awake()
    {
        trapCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKill(other);
    }

    private void TryKill(Collider2D other)
    {
        PlayerRespawn playerRespawn = other.GetComponentInParent<PlayerRespawn>();
        if (playerRespawn == null)
        {
            return;
        }

        if (!IsInsideKillZone(other))
        {
            return;
        }

        playerRespawn.Die();
    }

    private bool IsInsideKillZone(Collider2D other)
    {
        if (trapCollider == null)
        {
            return true;
        }

        Bounds killBounds = trapCollider.bounds;
        Vector3 shrinkAmount = new Vector3(forgivenessInset.x * 2f, forgivenessInset.y * 2f, 0f);
        killBounds.Expand(-shrinkAmount);

        // If the inset is larger than the current collider, fall back to the original bounds.
        if (killBounds.size.x <= 0f || killBounds.size.y <= 0f)
        {
            killBounds = trapCollider.bounds;
        }

        return killBounds.Intersects(other.bounds);
    }
}
