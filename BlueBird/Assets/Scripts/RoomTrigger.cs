using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RoomTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector2 targetOffset;

    [Header("Checkpoint")]
    [SerializeField] private Checkpoint roomCheckpoint;
    [SerializeField] private bool activateCheckpointOnEnter = true;

    [Header("Move")]
    [SerializeField] private float moveDuration = -1f;
    [SerializeField] private RoomCameraController roomCameraController;

    private Collider2D triggerCollider;
    private bool playerInside;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }

        if (roomCameraController == null)
        {
            roomCameraController = FindObjectOfType<RoomCameraController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayerOverlap(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandlePlayerOverlap(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerRespawn>() == null)
        {
            return;
        }

        playerInside = false;
    }

    private Vector3 GetCameraTargetPosition()
    {
        if (cameraTarget != null)
        {
            Vector3 position = cameraTarget.position;
            position.x += targetOffset.x;
            position.y += targetOffset.y;
            return position;
        }

        if (triggerCollider != null)
        {
            Vector3 center = triggerCollider.bounds.center;
            center.x += targetOffset.x;
            center.y += targetOffset.y;
            center.z = 0f;
            return center;
        }

        Vector3 fallback = transform.position;
        fallback.x += targetOffset.x;
        fallback.y += targetOffset.y;
        fallback.z = 0f;
        return fallback;
    }

    private void TryHandlePlayerOverlap(Collider2D other)
    {
        PlayerRespawn playerRespawn = other.GetComponentInParent<PlayerRespawn>();
        if (playerRespawn == null || playerInside)
        {
            return;
        }

        playerInside = true;

        if (roomCameraController != null)
        {
            roomCameraController.MoveTo(GetCameraTargetPosition(), moveDuration);
        }

        if (activateCheckpointOnEnter && roomCheckpoint != null)
        {
            roomCheckpoint.ActivateForPlayer(playerRespawn, other.GetComponentInParent<PlayerAudio>());
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D currentCollider = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        if (currentCollider == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Bounds bounds = currentCollider.bounds;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Vector3 targetPosition = Application.isPlaying ? GetCameraTargetPosition() : GetEditorTargetPosition(currentCollider);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(targetPosition, 0.2f);
    }

    private Vector3 GetEditorTargetPosition(Collider2D currentCollider)
    {
        if (cameraTarget != null)
        {
            Vector3 position = cameraTarget.position;
            position.x += targetOffset.x;
            position.y += targetOffset.y;
            return position;
        }

        Vector3 center = currentCollider.bounds.center;
        center.x += targetOffset.x;
        center.y += targetOffset.y;
        center.z = 0f;
        return center;
    }
}
