using UnityEngine;

public class Collision : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Space]
    public bool onGround;
    public bool onWall;
    public bool onRightWall;
    public bool onLeftWall;
    public int wallSide;

    [Space]
    [Header("Collision")]
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.12f);
    public Vector2 wallCheckSize = new Vector2(0.12f, 0.7f);
    public Vector2 bottomOffset;
    public Vector2 rightOffset;
    public Vector2 leftOffset;
    public bool drawDebugGizmos = true;

    private LayerMask ActiveWallLayer => wallLayer.value == 0 ? groundLayer : wallLayer;

    void Update()
    {
        Vector2 position = transform.position;

        onGround = Physics2D.OverlapBox(position + bottomOffset, groundCheckSize, 0f, groundLayer);
        onRightWall = Physics2D.OverlapBox(position + rightOffset, wallCheckSize, 0f, ActiveWallLayer);
        onLeftWall = Physics2D.OverlapBox(position + leftOffset, wallCheckSize, 0f, ActiveWallLayer);

        onWall = onRightWall || onLeftWall;
        wallSide = onRightWall ? -1 : (onLeftWall ? 1 : 0);
    }

    void OnDrawGizmos()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + bottomOffset, groundCheckSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube((Vector2)transform.position + rightOffset, wallCheckSize);
        Gizmos.DrawWireCube((Vector2)transform.position + leftOffset, wallCheckSize);
    }
}
