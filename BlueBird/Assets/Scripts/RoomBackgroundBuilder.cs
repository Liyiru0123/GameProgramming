using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RoomBackgroundBuilder : MonoBehaviour
{
    [SerializeField] private Color backgroundColor = new Color(0.37f, 0.52f, 0.72f, 1f);
    [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.14f);
    [SerializeField] private float zPosition = 1f;
    [SerializeField] private float gridScale = 0.7f;

    private GameObject container;
    private Sprite gridSprite;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        Rebuild();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        if (container != null)
        {
            Destroy(container);
        }

        RoomTrigger[] rooms = FindObjectsOfType<RoomTrigger>();
        if (rooms == null || rooms.Length == 0)
        {
            return;
        }

        container = new GameObject("GeneratedRoomBackgrounds");
        SceneManager.MoveGameObjectToScene(container, SceneManager.GetActiveScene());

        EnsureGridSprite();

        foreach (RoomTrigger room in rooms)
        {
            BoxCollider2D roomBounds = room.GetComponent<BoxCollider2D>();
            if (roomBounds == null)
            {
                continue;
            }

            Bounds bounds = roomBounds.bounds;
            GameObject background = new GameObject($"{room.name}_Background");
            background.transform.SetParent(container.transform, false);
            background.transform.position = new Vector3(bounds.center.x, bounds.center.y, zPosition);
            background.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);

            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = gridSprite;
            renderer.color = backgroundColor;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(bounds.size.x / gridScale, bounds.size.y / gridScale);
            renderer.sortingOrder = -100;
        }
    }

    private void EnsureGridSprite()
    {
        if (gridSprite != null)
        {
            return;
        }

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        Color transparent = new Color(1f, 1f, 1f, 0f);
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                bool line = x == 0 || y == 0 || x == texture.width / 2 || y == texture.height / 2;
                texture.SetPixel(x, y, line ? gridLineColor : transparent);
            }
        }

        texture.Apply();
        gridSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 64f);
    }
}
