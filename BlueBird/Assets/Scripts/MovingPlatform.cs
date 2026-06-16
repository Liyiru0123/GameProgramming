using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float arriveDistance = 0.02f;

    private readonly Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();
    private Rigidbody2D rb;
    private Transform currentTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentTarget = pointB != null ? pointB : pointA;
    }

    private void FixedUpdate()
    {
        if (pointA == null || pointB == null || currentTarget == null)
        {
            return;
        }

        Vector3 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, currentTarget.position, speed * Time.fixedDeltaTime);

        if (rb != null && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            rb.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        if (Vector3.Distance(nextPosition, currentTarget.position) <= arriveDistance)
        {
            currentTarget = currentTarget == pointA ? pointB : pointA;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAttachRider(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAttachRider(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        DetachRider(collision.transform);
    }

    public void DetachAllRiders()
    {
        Transform[] riders = new Transform[originalParents.Count];
        originalParents.Keys.CopyTo(riders, 0);

        foreach (Transform rider in riders)
        {
            DetachRider(rider);
        }
    }

    private void TryAttachRider(Collision2D collision)
    {
        if (collision.transform.GetComponentInParent<PlayerRespawn>() == null)
        {
            return;
        }

        if (collision.transform.position.y < transform.position.y)
        {
            return;
        }

        if (!originalParents.ContainsKey(collision.transform))
        {
            originalParents.Add(collision.transform, collision.transform.parent);
        }

        collision.transform.SetParent(transform, true);
    }

    private void DetachRider(Transform rider)
    {
        if (rider == null)
        {
            return;
        }

        if (!originalParents.TryGetValue(rider, out Transform originalParent))
        {
            return;
        }

        if (rider.parent == transform)
        {
            rider.SetParent(originalParent, true);
        }

        originalParents.Remove(rider);
    }
}
