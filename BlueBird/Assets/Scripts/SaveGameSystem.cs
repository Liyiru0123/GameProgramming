using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SaveGameSystem : MonoBehaviour
{
    private enum PendingRestoreMode
    {
        None,
        FreshGame,
        SlotData
    }

    [Serializable]
    public class SlotData
    {
        public string checkpointId;
        public float respawnX;
        public float respawnY;
        public float respawnZ;
        public bool dashUnlocked;
        public string savedAt;
    }

    private const string SlotKeyPrefix = "save_slot_";

    private static SaveGameSystem instance;
    [Header("Fresh Game Spawn")]
    [SerializeField] private Vector3 freshGameSpawnPosition = new Vector2(-9.14f, -3.91f);
    private int activeSlot = -1;
    private bool suppressSave;
    private PendingRestoreMode pendingRestoreMode;
    private SlotData pendingSlotData;

    public static SaveGameSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SaveGameSystem>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public bool HasSlotData(int slotIndex)
    {
        return PlayerPrefs.HasKey(GetSlotKey(slotIndex));
    }

    public SlotData LoadSlotData(int slotIndex)
    {
        if (!HasSlotData(slotIndex))
        {
            return null;
        }

        return JsonUtility.FromJson<SlotData>(PlayerPrefs.GetString(GetSlotKey(slotIndex)));
    }

    public string GetSlotSummary(int slotIndex)
    {
        SlotData data = LoadSlotData(slotIndex);
        if (data == null)
        {
            return "Empty";
        }

        string checkpointName = string.IsNullOrWhiteSpace(data.checkpointId) ? "Start" : data.checkpointId;
        return $"{checkpointName} | {data.savedAt}";
    }

    public void StartNewGameWithoutSlot()
    {
        activeSlot = -1;
        pendingRestoreMode = PendingRestoreMode.FreshGame;
        pendingSlotData = null;
        TryApplyPendingRestore();
    }

    public void StartNewGameInSlot(int slotIndex)
    {
        DeleteSlot(slotIndex);
        activeSlot = slotIndex;
        pendingRestoreMode = PendingRestoreMode.FreshGame;
        pendingSlotData = null;
        if (TryApplyPendingRestore())
        {
            SaveCurrentSlot();
        }
    }

    public bool ContinueFromSlot(int slotIndex)
    {
        SlotData data = LoadSlotData(slotIndex);
        if (data == null)
        {
            return false;
        }

        activeSlot = slotIndex;
        pendingRestoreMode = PendingRestoreMode.SlotData;
        pendingSlotData = data;
        TryApplyPendingRestore();
        return true;
    }

    public void DeleteSlot(int slotIndex)
    {
        PlayerPrefs.DeleteKey(GetSlotKey(slotIndex));
        PlayerPrefs.Save();
    }

    public void SaveCheckpoint(PlayerRespawn playerRespawn, Checkpoint checkpoint)
    {
        if (suppressSave || activeSlot < 0 || playerRespawn == null)
        {
            return;
        }

        PlayerAbilities abilities = FindPlayerAbilities();
        SlotData data = new SlotData
        {
            checkpointId = checkpoint != null ? checkpoint.CheckpointId : string.Empty,
            respawnX = playerRespawn.CurrentRespawnPoint.x,
            respawnY = playerRespawn.CurrentRespawnPoint.y,
            respawnZ = playerRespawn.CurrentRespawnPoint.z,
            dashUnlocked = abilities == null || abilities.DashUnlocked,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        WriteSlotData(activeSlot, data);
    }

    public void SaveCurrentSlot()
    {
        if (suppressSave || activeSlot < 0)
        {
            return;
        }

        PlayerRespawn playerRespawn = FindPlayerRespawn();
        if (playerRespawn == null)
        {
            return;
        }

        Checkpoint checkpoint = Checkpoint.FindById(playerRespawn.CurrentCheckpointId);
        SaveCheckpoint(playerRespawn, checkpoint);
    }

    private bool ApplyFreshGameState()
    {
        PlayerRespawn playerRespawn = FindPlayerRespawn();
        
        if (playerRespawn != null)
        {
            playerRespawn.TeleportTo(freshGameSpawnPosition);
        }

        suppressSave = true;

        PlayerAbilities abilities = FindPlayerAbilities();
        RoomCameraController cameraController = FindObjectOfType<RoomCameraController>();

        foreach (Checkpoint checkpoint in FindObjectsOfType<Checkpoint>())
        {
            checkpoint.SetActivated(false);
        }

        if (abilities != null)
        {
            abilities.SetDashUnlocked(false, save: false);
        }

        if (playerRespawn != null)
        {
            playerRespawn.ResetToInitialSpawn();
            playerRespawn.TeleportTo(playerRespawn.CurrentRespawnPoint);
        }

        if (cameraController != null && playerRespawn != null)
        {
            cameraController.SnapTo(playerRespawn.CurrentRespawnPoint);
        }

        suppressSave = false;
        return true;
    }
    

    private bool ApplySlotData(SlotData data)
    {
        PlayerRespawn playerRespawn = FindPlayerRespawn();
        if (playerRespawn == null)
        {
            return false;
        }

        suppressSave = true;

        PlayerAbilities abilities = FindPlayerAbilities();
        RoomCameraController cameraController = FindObjectOfType<RoomCameraController>();

        foreach (Checkpoint checkpoint in FindObjectsOfType<Checkpoint>())
        {
            checkpoint.SetActivated(false);
        }

        if (abilities != null)
        {
            abilities.SetDashUnlocked(data != null && data.dashUnlocked, save: false);
        }

        if (playerRespawn != null)
        {
            Vector3 respawnPosition = new Vector3(data.respawnX, data.respawnY, data.respawnZ);
            Checkpoint checkpoint = Checkpoint.FindById(data.checkpointId);
            if (checkpoint != null)
            {
                playerRespawn.SetCheckpoint(checkpoint);
            }
            else
            {
                playerRespawn.ResetToInitialSpawn();
            }

            playerRespawn.TeleportTo(respawnPosition);
        }

        if (cameraController != null && playerRespawn != null)
        {
            cameraController.SnapTo(playerRespawn.transform.position);
        }

        suppressSave = false;
        return true;
    }

    private static string GetSlotKey(int slotIndex)
    {
        return $"{SlotKeyPrefix}{slotIndex}";
    }

    private static void WriteSlotData(int slotIndex, SlotData data)
    {
        PlayerPrefs.SetString(GetSlotKey(slotIndex), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private static PlayerRespawn FindPlayerRespawn()
    {
        return FindObjectOfType<PlayerRespawn>();
    }

    private static PlayerAbilities FindPlayerAbilities()
    {
        return FindObjectOfType<PlayerAbilities>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        suppressSave = false;
        TryApplyPendingRestore();
    }

    private bool TryApplyPendingRestore()
    {
        bool applied = false;

        switch (pendingRestoreMode)
        {
            case PendingRestoreMode.FreshGame:
                applied = ApplyFreshGameState();
                break;
            case PendingRestoreMode.SlotData:
                applied = ApplySlotData(pendingSlotData);
                break;
        }

        if (applied)
        {
            pendingRestoreMode = PendingRestoreMode.None;
            pendingSlotData = null;
        }

        return applied;
    }
}
