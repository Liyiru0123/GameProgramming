using UnityEngine;

public class RoomBGMTrigger : MonoBehaviour
{
    public AudioClip roomBGM;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            BGMManager.Instance.ChangeBGM(roomBGM);
        }
    }
}