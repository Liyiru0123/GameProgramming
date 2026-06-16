using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class RoomCameraController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform initialTarget;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Coroutine moveCoroutine;
    private Vector3 currentTargetPosition;

    private Transform CameraTransform => mainCamera != null ? mainCamera.transform : transform;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (initialTarget != null)
        {
            SnapTo(initialTarget.position);
            return;
        }

        Vector3 position = CameraTransform.position;
        currentTargetPosition = new Vector3(position.x, position.y, 0f);
        CameraTransform.position = currentTargetPosition + offset;
    }

    public void MoveTo(Transform cameraTarget, float overrideDuration = -1f)
    {
        if (cameraTarget == null)
        {
            return;
        }

        MoveTo(cameraTarget.position, overrideDuration);
    }

    public void MoveTo(Vector3 worldPosition, float overrideDuration = -1f)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        float duration = overrideDuration >= 0f ? overrideDuration : moveDuration;
        currentTargetPosition = new Vector3(worldPosition.x, worldPosition.y, 0f);
        moveCoroutine = StartCoroutine(MoveRoutine(currentTargetPosition + offset, duration));
    }

    public void SnapTo(Vector3 worldPosition)
    {
        currentTargetPosition = new Vector3(worldPosition.x, worldPosition.y, 0f);
        CameraTransform.position = currentTargetPosition + offset;
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition, float duration)
    {
        Transform cameraTransform = CameraTransform;
        Vector3 startPosition = cameraTransform.position;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            cameraTransform.position = targetPosition;
            moveCoroutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        cameraTransform.position = targetPosition;
        moveCoroutine = null;
    }
}
