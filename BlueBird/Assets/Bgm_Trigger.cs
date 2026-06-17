using UnityEngine;

public class RoomBGMTrigger : MonoBehaviour
{
    public AudioClip roomBGM;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("进入 BGM 触发器：" + collision.name);

        if (!collision.CompareTag("Player")) return;

        if (roomBGM == null)
        {
            Debug.LogError("这个房间触发器没有设置 roomBGM！");
            return;
        }

        Debug.Log("准备切换 BGM：" + roomBGM.name);

        BGMManager.Instance.ChangeBGM(roomBGM);
    }
}