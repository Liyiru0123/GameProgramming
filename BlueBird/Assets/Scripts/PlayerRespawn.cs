using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private float respawnDelay = 0.2f;
    [SerializeField] private float postRespawnInvulnerability = 0.15f;
    [SerializeField] private Behaviour[] extraBehavioursToDisable;

    [Header("Respawn Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer visualSpriteRenderer;
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private int fragmentColumns = 4;
    [SerializeField] private int fragmentRows = 4;
    [SerializeField] private float deathAnimationDuration = 0.32f;
    [SerializeField] private float respawnAnimationDuration = 0.4f;
    [SerializeField] private float explosionDistance = 1.2f;
    [SerializeField] private float explosionRandomness = 0.28f;
    [SerializeField] private float respawnSpreadDistance = 1.35f;
    [SerializeField] private float fragmentSpin = 220f;

    private Rigidbody2D rb;
    private Movement movement;
    private BetterJumping betterJumping;
    private PlayerAudio playerAudio;
    private Collider2D[] playerColliders;
    private SpriteRenderer[] visualRenderers;
    private Vector3 initialSpawnPoint;
    private Vector3 currentRespawnPoint;
    private float invulnerableUntil;
    private Checkpoint currentCheckpoint;

    public bool IsRespawning { get; private set; }
    public Vector3 CurrentRespawnPoint => currentRespawnPoint;
    public string CurrentCheckpointId => currentCheckpoint != null ? currentCheckpoint.CheckpointId : string.Empty;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();
        betterJumping = GetComponent<BetterJumping>();
        playerAudio = GetComponent<PlayerAudio>();
        playerColliders = GetComponents<Collider2D>();

        if (visualSpriteRenderer == null)
        {
            visualSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualRoot == null && visualSpriteRenderer != null)
        {
            visualRoot = visualSpriteRenderer.transform;
        }

        if (visualAnimator == null && visualRoot != null)
        {
            visualAnimator = visualRoot.GetComponent<Animator>();
        }

        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<Animator>();
        }

        if (visualRoot == null && visualAnimator != null)
        {
            visualRoot = visualAnimator.transform;
        }

        Transform visualSearchRoot = visualRoot != null ? visualRoot : transform;
        visualRenderers = visualSearchRoot.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        initialSpawnPoint = defaultSpawnPoint != null ? defaultSpawnPoint.position : transform.position;
        currentRespawnPoint = initialSpawnPoint;
    }

    public void Die()
    {
        if (IsRespawning || Time.time < invulnerableUntil)
        {
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    public bool SetCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null)
        {
            return false;
        }

        if (currentCheckpoint == checkpoint)
        {
            currentCheckpoint.SetActivated(true);
            currentRespawnPoint = currentCheckpoint.RespawnPosition;
            return false;
        }

        if (currentCheckpoint != null)
        {
            currentCheckpoint.SetActivated(false);
        }

        currentCheckpoint = checkpoint;
        currentRespawnPoint = checkpoint.RespawnPosition;
        currentCheckpoint.SetActivated(true);
        return true;
    }

    public void ResetToInitialSpawn()
    {
        currentCheckpoint = null;
        currentRespawnPoint = initialSpawnPoint;
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;

        if (rb != null)
        {
            rb.position = position;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        IsRespawning = true;
        playerAudio?.ResetFootstepState();
        playerAudio?.PlayDeath();
        FragmentSnapshot deathSnapshot = CaptureVisualSnapshot();
        SetPlayerState(false);
        SetVisualState(false);

        if (deathSnapshot.IsValid)
        {
            yield return PlayFragmentEffect(deathSnapshot, explodeOutward: true);
        }

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        transform.position = currentRespawnPoint;

        if (rb != null)
        {
            rb.position = currentRespawnPoint;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ResetVisualAnimator();

        FragmentSnapshot respawnSnapshot = CaptureVisualSnapshot();
        if (respawnSnapshot.IsValid)
        {
            yield return PlayFragmentEffect(respawnSnapshot, explodeOutward: false);
        }

        SetVisualState(true);
        yield return null;

        SetPlayerState(true);
        playerAudio?.ResetFootstepState();
        invulnerableUntil = Time.time + postRespawnInvulnerability;
        IsRespawning = false;
    }

    private void SetPlayerState(bool enabledState)
    {
        if (movement != null)
        {
            if (enabledState)
            {
                movement.ResumeControl();
            }
            else
            {
                movement.SuspendControl();
            }
        }

        if (betterJumping != null)
        {
            betterJumping.enabled = enabledState;
        }

        if (extraBehavioursToDisable != null)
        {
            foreach (Behaviour behaviour in extraBehavioursToDisable)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabledState;
                }
            }
        }

        if (rb != null)
        {
            rb.simulated = enabledState;
        }

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null)
            {
                playerCollider.enabled = enabledState;
            }
        }
    }

    private void SetVisualState(bool visible)
    {
        if (visualRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in visualRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void ResetVisualAnimator()
    {
        if (visualAnimator == null)
        {
            return;
        }

        visualAnimator.Rebind();
        visualAnimator.Update(0f);
    }

    private FragmentSnapshot CaptureVisualSnapshot()
    {
        if (visualSpriteRenderer == null || visualSpriteRenderer.sprite == null)
        {
            return default;
        }

        Vector3 worldScale = visualSpriteRenderer.transform.lossyScale;
        worldScale.x *= visualSpriteRenderer.flipX ? -1f : 1f;
        worldScale.y *= visualSpriteRenderer.flipY ? -1f : 1f;

        return new FragmentSnapshot
        {
            IsValid = true,
            Sprite = visualSpriteRenderer.sprite,
            Material = visualSpriteRenderer.sharedMaterial,
            Color = visualSpriteRenderer.color,
            SortingLayerID = visualSpriteRenderer.sortingLayerID,
            SortingOrder = visualSpriteRenderer.sortingOrder,
            Position = visualSpriteRenderer.transform.position,
            Rotation = visualSpriteRenderer.transform.rotation,
            Scale = worldScale
        };
    }

    private IEnumerator PlayFragmentEffect(FragmentSnapshot snapshot, bool explodeOutward)
    {
        int columns = Mathf.Max(1, fragmentColumns);
        int rows = Mathf.Max(1, fragmentRows);
        float duration = Mathf.Max(0.01f, explodeOutward ? deathAnimationDuration : respawnAnimationDuration);

        GameObject container = new GameObject(explodeOutward ? "DeathFragments" : "RespawnFragments");
        Transform containerTransform = container.transform;
        containerTransform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);
        containerTransform.localScale = snapshot.Scale;

        List<FragmentPiece> pieces = CreateFragmentPieces(snapshot, containerTransform, columns, rows);
        if (pieces.Count == 0)
        {
            Destroy(container);
            yield break;
        }

        foreach (FragmentPiece piece in pieces)
        {
            if (piece.Transform == null || piece.Renderer == null)
            {
                continue;
            }

            piece.Transform.localPosition = explodeOutward
                ? piece.AssembledLocalPosition
                : piece.AssembledLocalPosition + piece.RespawnOffset;

            piece.Transform.localRotation = explodeOutward
                ? Quaternion.identity
                : Quaternion.Euler(0f, 0f, piece.RotationOffset);

            Color initialColor = piece.Renderer.color;
            initialColor.a = explodeOutward ? 1f : 0f;
            piece.Renderer.color = initialColor;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(normalized);

            foreach (FragmentPiece piece in pieces)
            {
                if (piece.Transform == null || piece.Renderer == null)
                {
                    continue;
                }

                Vector3 assembledLocalPosition = piece.AssembledLocalPosition;
                Vector3 displacedLocalPosition = piece.AssembledLocalPosition
                    + (explodeOutward ? piece.DeathOffset : piece.RespawnOffset);

                Vector3 from = explodeOutward ? assembledLocalPosition : displacedLocalPosition;
                Vector3 to = explodeOutward ? displacedLocalPosition : assembledLocalPosition;
                float startRotation = explodeOutward ? 0f : piece.RotationOffset;
                float endRotation = explodeOutward ? piece.RotationOffset : 0f;

                piece.Transform.localPosition = Vector3.LerpUnclamped(from, to, eased);
                piece.Transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.LerpUnclamped(startRotation, endRotation, eased));

                Color color = piece.Renderer.color;
                color.a = explodeOutward ? 1f - normalized : normalized;
                piece.Renderer.color = color;
            }

            yield return null;
        }

        foreach (FragmentPiece piece in pieces)
        {
            if (piece.RuntimeSprite != null)
            {
                Destroy(piece.RuntimeSprite);
            }
        }

        Destroy(container);
    }

    private List<FragmentPiece> CreateFragmentPieces(
        FragmentSnapshot snapshot,
        Transform parent,
        int columns,
        int rows)
    {
        List<FragmentPiece> pieces = new List<FragmentPiece>(columns * rows);
        Sprite sourceSprite = snapshot.Sprite;
        Rect textureRect = sourceSprite.textureRect;
        float pixelsPerUnit = sourceSprite.pixelsPerUnit;
        Vector3 localSpriteCenter = new Vector3(
            (textureRect.center.x - sourceSprite.pivot.x) / pixelsPerUnit,
            (textureRect.center.y - sourceSprite.pivot.y) / pixelsPerUnit,
            0f);

        for (int y = 0; y < rows; y++)
        {
            int pixelBottom = Mathf.RoundToInt(textureRect.yMin + textureRect.height * y / rows);
            int pixelTop = Mathf.RoundToInt(textureRect.yMin + textureRect.height * (y + 1) / rows);
            int pixelHeight = pixelTop - pixelBottom;
            if (pixelHeight <= 0)
            {
                continue;
            }

            for (int x = 0; x < columns; x++)
            {
                int pixelLeft = Mathf.RoundToInt(textureRect.xMin + textureRect.width * x / columns);
                int pixelRight = Mathf.RoundToInt(textureRect.xMin + textureRect.width * (x + 1) / columns);
                int pixelWidth = pixelRight - pixelLeft;
                if (pixelWidth <= 0)
                {
                    continue;
                }

                Rect pieceRect = new Rect(pixelLeft, pixelBottom, pixelWidth, pixelHeight);
                Sprite runtimeSprite = Sprite.Create(
                    sourceSprite.texture,
                    pieceRect,
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);

                GameObject pieceObject = new GameObject($"Fragment_{x}_{y}");
                pieceObject.transform.SetParent(parent, false);

                SpriteRenderer pieceRenderer = pieceObject.AddComponent<SpriteRenderer>();
                pieceRenderer.sprite = runtimeSprite;
                pieceRenderer.sharedMaterial = snapshot.Material;
                pieceRenderer.sortingLayerID = snapshot.SortingLayerID;
                pieceRenderer.sortingOrder = snapshot.SortingOrder;
                pieceRenderer.color = snapshot.Color;

                float localCenterX = (pixelLeft - textureRect.xMin) + (pixelWidth * 0.5f);
                float localCenterY = (pixelBottom - textureRect.yMin) + (pixelHeight * 0.5f);
                Vector3 assembledLocalPosition = new Vector3(
                    (localCenterX - sourceSprite.pivot.x) / pixelsPerUnit,
                    (localCenterY - sourceSprite.pivot.y) / pixelsPerUnit,
                    0f);

                pieceObject.transform.localPosition = assembledLocalPosition;

                Vector2 centeredOffset = assembledLocalPosition - localSpriteCenter;
                Vector2 direction = centeredOffset.sqrMagnitude > 0.0001f
                    ? centeredOffset.normalized
                    : Random.insideUnitCircle.normalized;

                Vector2 randomOffset = Random.insideUnitCircle * explosionRandomness;

                float distanceScale = Random.Range(0.8f, 1.1f);

                pieces.Add(new FragmentPiece
                {
                    Transform = pieceObject.transform,
                    Renderer = pieceRenderer,
                    RuntimeSprite = runtimeSprite,
                    AssembledLocalPosition = assembledLocalPosition,
                    DeathOffset = (Vector3)(direction * (distanceScale * explosionDistance) + randomOffset),
                    RespawnOffset = (Vector3)(direction * (distanceScale * respawnSpreadDistance) + randomOffset),
                    RotationOffset = Random.Range(-fragmentSpin, fragmentSpin)
                });
            }
        }

        return pieces;
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    private struct FragmentSnapshot
    {
        public bool IsValid;
        public Sprite Sprite;
        public Material Material;
        public Color Color;
        public int SortingLayerID;
        public int SortingOrder;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    private sealed class FragmentPiece
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Sprite RuntimeSprite;
        public Vector3 AssembledLocalPosition;
        public Vector3 DeathOffset;
        public Vector3 RespawnOffset;
        public float RotationOffset;
    }
}
