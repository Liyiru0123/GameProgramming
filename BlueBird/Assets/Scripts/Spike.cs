using UnityEngine;

[DisallowMultipleComponent]
public class Spike : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn playerRespawn = other.GetComponentInParent<PlayerRespawn>();
        if (playerRespawn == null)
        {
            return;
        }

        playerRespawn.Die();
    }
}
