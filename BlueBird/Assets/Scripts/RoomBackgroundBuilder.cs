using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RoomBackgroundBuilder : MonoBehaviour
{
    [SerializeField] private Color backgroundColor = new Color(0.37f, 0.52f, 0.72f, 1f);
    [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.14f);
    [SerializeField] private float zPosition = 1f;
    [SerializeField] private float gridScale = 0.7f;
    [Header("Decorations")]
    [SerializeField] private bool generateFlowers = true;
    [SerializeField] private int minFlowersPerRoom = 4;
    [SerializeField] private int maxFlowersPerRoom = 9;
    [SerializeField] private Vector2 flowerScaleRange = new Vector2(0.45f, 0.8f);
    [SerializeField] private float flowerBottomPadding = 0.55f;
    [SerializeField] private float flowerVerticalSpread = 1.45f;
    [SerializeField] private float flowerSidePadding = 0.85f;
    [SerializeField] private int flowerSortingOrder = -99;

    private GameObject container;
    private Sprite gridSprite;
    private Sprite[] flowerSprites;
    private static readonly Color[] FlowerPalette =
    {
        new Color(1f, 0.72f, 0.84f, 1f),
        new Color(0.94f, 0.82f, 0.44f, 1f),
        new Color(0.78f, 0.9f, 1f, 1f),
        new Color(0.87f, 0.77f, 1f, 1f),
        new Color(1f, 0.88f, 0.68f, 1f)
    };
    private static readonly Color StemColor = new Color(0.19f, 0.5f, 0.31f, 1f);
    private static readonly Color CenterColor = new Color(0.98f, 0.84f, 0.33f, 1f);

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
        EnsureFlowerSprites();

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

            if (generateFlowers)
            {
                CreateFlowerDecorations(room, bounds);
            }
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

    private void EnsureFlowerSprites()
    {
        if (flowerSprites != null && flowerSprites.Length > 0)
        {
            return;
        }

        flowerSprites = new Sprite[FlowerPalette.Length];
        for (int i = 0; i < FlowerPalette.Length; i++)
        {
            flowerSprites[i] = CreateFlowerSprite(FlowerPalette[i]);
        }
    }

    private void CreateFlowerDecorations(RoomTrigger room, Bounds bounds)
    {
        if (flowerSprites == null || flowerSprites.Length == 0)
        {
            return;
        }

        int seed = room.name.GetHashCode() ^ Mathf.RoundToInt(bounds.center.x * 100f) ^ Mathf.RoundToInt(bounds.center.y * 100f);
        Random.State previousState = Random.state;
        Random.InitState(seed);

        int flowerCount = Random.Range(minFlowersPerRoom, maxFlowersPerRoom + 1);
        float minX = bounds.min.x + flowerSidePadding;
        float maxX = bounds.max.x - flowerSidePadding;
        float minY = bounds.min.y + flowerBottomPadding;
        float maxY = Mathf.Min(bounds.max.y - 0.9f, minY + flowerVerticalSpread);

        if (minX >= maxX || minY >= maxY)
        {
            Random.state = previousState;
            return;
        }

        for (int i = 0; i < flowerCount; i++)
        {
            GameObject flower = new GameObject($"{room.name}_Flower_{i + 1}");
            flower.transform.SetParent(container.transform, false);
            flower.transform.position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                zPosition);

            float scale = Random.Range(flowerScaleRange.x, flowerScaleRange.y);
            flower.transform.localScale = new Vector3(
                Random.value > 0.5f ? scale : -scale,
                scale,
                1f);
            flower.transform.Rotate(0f, 0f, Random.Range(-6f, 6f));

            SpriteRenderer renderer = flower.AddComponent<SpriteRenderer>();
            renderer.sprite = flowerSprites[Random.Range(0, flowerSprites.Length)];
            renderer.sortingOrder = flowerSortingOrder;
            renderer.color = Color.white;
        }

        Random.state = previousState;
    }

    private Sprite CreateFlowerSprite(Color petalColor)
    {
        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        DrawStem(texture, size);
        DrawPetals(texture, size, petalColor);
        DrawCenter(texture, size);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.08f),
            size);
    }

    private void DrawStem(Texture2D texture, int size)
    {
        int stemX = size / 2;
        for (int x = stemX - 1; x <= stemX + 1; x++)
        {
            for (int y = 4; y <= 23; y++)
            {
                texture.SetPixel(x, y, StemColor);
            }
        }

        for (int x = stemX - 10; x <= stemX - 2; x++)
        {
            for (int y = 12; y <= 15; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    texture.SetPixel(x, y, StemColor);
                }
            }
        }

        for (int x = stemX + 2; x <= stemX + 10; x++)
        {
            for (int y = 16; y <= 19; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    texture.SetPixel(x, y, StemColor);
                }
            }
        }
    }

    private void DrawPetals(Texture2D texture, int size, Color petalColor)
    {
        Vector2Int center = new Vector2Int(size / 2, 30);
        FillCircle(texture, center + new Vector2Int(0, 8), 6, petalColor);
        FillCircle(texture, center + new Vector2Int(7, 2), 6, petalColor);
        FillCircle(texture, center + new Vector2Int(-7, 2), 6, petalColor);
        FillCircle(texture, center + new Vector2Int(4, -6), 6, petalColor);
        FillCircle(texture, center + new Vector2Int(-4, -6), 6, petalColor);
    }

    private void DrawCenter(Texture2D texture, int size)
    {
        FillCircle(texture, new Vector2Int(size / 2, 30), 5, CenterColor);
    }

    private void FillCircle(Texture2D texture, Vector2Int center, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radiusSquared)
                {
                    continue;
                }

                int pixelX = center.x + x;
                int pixelY = center.y + y;
                if (pixelX < 0 || pixelX >= texture.width || pixelY < 0 || pixelY >= texture.height)
                {
                    continue;
                }

                texture.SetPixel(pixelX, pixelY, color);
            }
        }
    }
}
