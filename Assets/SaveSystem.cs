using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine.AI;
using System.Collections.Generic;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    [Header("Save Settings")]
    public string saveFileName = "gameSave.db";
    public bool autoSaveOnQuit = true;

    private string savePath;
    private bool isLoading = false;
    private bool isSaving = false;
    private bool isQuitting = false;
    private bool isLoadComplete = false;

    private SQLiteConnection connection;
    private bool isDatabaseReady = false;

    private static string GetDifficultyForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "chapter 1")
        {
            PlayerPrefs.SetString("GameDifficulty", "Normal");
            PlayerPrefs.Save();
            return "Normal";
        }

        if (sceneName == "chapter 2")
        {
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            PlayerPrefs.Save();
            return "Hard";
        }

        return PlayerPrefs.GetString("GameDifficulty", "Normal");
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // ── FIX: Don't create database immediately ──
        // Just set the path based on difficulty, but don't initialize yet
        string difficulty = GetDifficultyForActiveScene();
        string fileName = difficulty == "Hard" ? "gameSave_Hard.db" : "gameSave.db";
        savePath = Path.Combine(Application.persistentDataPath, fileName);

        // ── Don't call InitializeDatabase() here ──
        // We will initialize only when needed
    }

    // ── Initialize database only when needed ──
    private void EnsureDatabaseReady()
    {
        if (isDatabaseReady) return;
        InitializeDatabase();
    }

    void InitializeDatabase()
    {
        try
        {
            connection = new SQLiteConnection(savePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

            connection.CreateTable<PlayerData>();
            connection.CreateTable<InventoryData>();
            connection.CreateTable<DoorData>();
            connection.CreateTable<RitualData>();
            connection.CreateTable<NoteData>();
            connection.CreateTable<GameStateData>();
            connection.CreateTable<DroppedItemData>();
            connection.CreateTable<FlashlightData>();
            connection.CreateTable<KeyData>();
            connection.CreateTable<BatteryData>();
            connection.CreateTable<RitualItemData>();
            connection.CreateTable<StaminaData>();
            connection.CreateTable<SubtitleData>();
            connection.CreateTable<IntroData>();
            connection.CreateTable<AIPositionData>();

            // Existing saves created before IsTriggered was added need this column.
            try { connection.Execute("ALTER TABLE SubtitleData ADD COLUMN IsTriggered INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            // ── NEW: ProgressionData table ──
            connection.CreateTable<ProgressionData>();

            isDatabaseReady = true;

            if (File.Exists(savePath))
            {
            }
        }
        catch (System.Exception e)
        {
            isDatabaseReady = false;
        }
    }

    // ── NEW: Reinitialize database (call this when switching difficulties) ──
    public void ReinitializeDatabase()
    {
        if (connection != null)
        {
            connection.Close();
            connection = null;
        }

        isDatabaseReady = false;

        // ── Use different database files for different difficulties ──
        string difficulty = GetDifficultyForActiveScene();
        string fileName = difficulty == "Hard" ? "gameSave_Hard.db" : "gameSave.db";
        savePath = Path.Combine(Application.persistentDataPath, fileName);

        // We'll initialize on the next operation
    }

    // ── Get current save file path ──
    public string GetSavePath()
    {
        return savePath;
    }

    // ── Get current difficulty from PlayerPrefs ──
    public string GetCurrentDifficulty()
    {
        return PlayerPrefs.GetString("GameDifficulty", "Normal");
    }

    public void MarkKeyAsUsed(string keyName)
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = keyName.Replace("(Clone)", "");

            var existing = connection.Table<KeyData>()
                .Where(k => k.KeyName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                KeyData keyData = new KeyData
                {
                    KeyName = cleanName,
                    WasUsed = true
                };
                connection.Insert(keyData);
            }
            else
            {
                existing.WasUsed = true;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkBatteryAsUsed(string batteryName)
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = batteryName.Replace("(Clone)", "");

            var existing = connection.Table<BatteryData>()
                .Where(b => b.BatteryName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                BatteryData newBatteryData = new BatteryData
                {
                    BatteryName = cleanName,
                    RechargeAmount = 50f,
                    IsHeld = false,
                    IsDropped = false,
                    IsUsed = true,
                    PosX = 0f,
                    PosY = 0f,
                    PosZ = 0f,
                    RotX = 0f,
                    RotY = 0f,
                    RotZ = 0f,
                    RotW = 1f
                };
                connection.Insert(newBatteryData);
            }
            else
            {
                existing.IsUsed = true;
                existing.IsHeld = false;
                existing.IsDropped = false;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkRitualItemAsRevealed(string itemName)
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = itemName.Replace("(Clone)", "");

            var existing = connection.Table<RitualItemData>()
                .Where(r => r.ItemName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                RitualItemData newItem = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = true,
                    IsPlaced = false,
                    IsDropped = false,
                    PosX = 0f,
                    PosY = 0f,
                    PosZ = 0f,
                    RotX = 0f,
                    RotY = 0f,
                    RotZ = 0f,
                    RotW = 1f
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkRitualItemAsPlaced(string itemName, Vector3 position, Quaternion rotation)
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = itemName.Replace("(Clone)", "");

            var existing = connection.Table<RitualItemData>()
                .Where(r => r.ItemName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                RitualItemData newItem = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = true,
                    IsPlaced = true,
                    IsDropped = false,
                    PosX = position.x,
                    PosY = position.y,
                    PosZ = position.z,
                    RotX = rotation.x,
                    RotY = rotation.y,
                    RotZ = rotation.z,
                    RotW = rotation.w
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                existing.IsPlaced = true;
                existing.IsDropped = false;
                existing.PosX = position.x;
                existing.PosY = position.y;
                existing.PosZ = position.z;
                existing.RotX = rotation.x;
                existing.RotY = rotation.y;
                existing.RotZ = rotation.z;
                existing.RotW = rotation.w;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkRitualItemAsDropped(string itemName, Vector3 position, Quaternion rotation)
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = itemName.Replace("(Clone)", "");

            var existing = connection.Table<RitualItemData>()
                .Where(r => r.ItemName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                RitualItemData newItem = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = true,
                    IsPlaced = false,
                    IsDropped = true,
                    PosX = position.x,
                    PosY = position.y,
                    PosZ = position.z,
                    RotX = rotation.x,
                    RotY = rotation.y,
                    RotZ = rotation.z,
                    RotW = rotation.w
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                existing.IsPlaced = false;
                existing.IsDropped = true;
                existing.PosX = position.x;
                existing.PosY = position.y;
                existing.PosZ = position.z;
                existing.RotX = rotation.x;
                existing.RotY = rotation.y;
                existing.RotZ = rotation.z;
                existing.RotW = rotation.w;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void ClearKeyData()
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            connection.DeleteAll<KeyData>();
        }
        catch (System.Exception e)
        {
        }
    }

    public void ClearBatteryData()
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            connection.DeleteAll<BatteryData>();
        }
        catch (System.Exception e)
        {
        }
    }

    public void ClearRitualItemData()
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            connection.DeleteAll<RitualItemData>();
        }
        catch (System.Exception e)
        {
        }
    }

    void Start()
    {
        if (!isLoadComplete)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != "menu" && currentScene != "MainMenu")
            {
                if (PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1)
                {
                    StartCoroutine(LoadAfterSceneLoad());
                }
            }
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "menu" || scene.name == "MainMenu")
        {
            PlayerPrefs.SetInt("ShouldLoadSave", 0);
            PlayerPrefs.Save();
            isLoadComplete = false;
            return;
        }

        if (!isLoadComplete)
        {
            if (PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1)
            {
                StartCoroutine(LoadAfterSceneLoad());
            }
        }
    }

    System.Collections.IEnumerator LoadAfterSceneLoad()
    {
        GameObject player = null;
        float waitTimer = 0f;
        while (player == null && waitTimer < 8f)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                yield return new WaitForSeconds(0.3f);
                waitTimer += 0.3f;
            }
        }

        if (player != null)
        {
            yield return new WaitForSeconds(0.3f);

            if (HasSaveFile())
            {
                LoadGame();
            }
            else
            {
            }
        }
        else
        {
        }

        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.Save();
        isLoadComplete = true;
    }

    public void SaveGame()
    {
        EnsureDatabaseReady();
        
        if (isSaving || isQuitting) return;

        if (!isDatabaseReady)
        {
            return;
        }

        isSaving = true;

        try
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                connection.DeleteAll<PlayerData>();

                PlayerData playerData = new PlayerData
                {
                    PosX = player.transform.position.x,
                    PosY = player.transform.position.y,
                    PosZ = player.transform.position.z,
                    RotX = player.transform.rotation.x,
                    RotY = player.transform.rotation.y,
                    RotZ = player.transform.rotation.z,
                    RotW = player.transform.rotation.w,
                    CurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                };

                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    playerData.Health = health.currentHealth;
                    playerData.MaxHealth = health.maxHealth;
                }

                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    playerData.Sensitivity = pc.mouseSensitivity;
                }

                connection.Insert(playerData);

                connection.DeleteAll<StaminaData>();
            connection.DeleteAll<SubtitleData>();
                StaminaController stamina = player.GetComponent<StaminaController>();
                if (stamina != null && stamina.IsHardMode)
                {
                    connection.Insert(new StaminaData
                    {
                        CurrentStamina = stamina.CurrentStamina
                    });
                }
            }
            else
            {
                isSaving = false;
                return;
            }

            // ── SAVE INVENTORY ──
            SaveAIPositions();

            connection.DeleteAll<InventoryData>();
            if (Inventory.Instance != null)
            {
                foreach (GameObject item in Inventory.Instance.GetItems())
                {
                    if (item != null)
                    {
                        string cleanName = item.name.Replace("(Clone)", "");
                        
                        InventoryData invData = new InventoryData
                        {
                            ItemName = cleanName,
                            Quantity = 1,
                            IsEquipped = false
                        };
                        connection.Insert(invData);
                    }
                }
            }

            // ── SAVE BATTERY STATE ──
            var existingBatteryData = connection.Table<BatteryData>().ToList();
            BatteryPickup[] batteries = Object.FindObjectsOfType<BatteryPickup>();
            List<string> sceneBatteryNames = new List<string>();

            foreach (BatteryPickup battery in batteries)
            {
                if (battery != null)
                {
                    if (battery.wasUsed)
                    {
                        continue;
                    }

                    string cleanName = battery.gameObject.name.Replace("(Clone)", "");
                    sceneBatteryNames.Add(cleanName);

                    bool isHeld = false;
                    bool isDropped = false;
                    
                    if (Inventory.Instance != null)
                    {
                        foreach (GameObject item in Inventory.Instance.GetItems())
                        {
                            if (item != null && item == battery.gameObject)
                            {
                                isHeld = true;
                                break;
                            }
                        }
                    }

                    if (!isHeld)
                    {
                        isDropped = battery.wasDropped;
                    }

                    var existing = existingBatteryData.FirstOrDefault(b => b.BatteryName == cleanName);

                    if (existing != null)
                    {
                        existing.RechargeAmount = battery.rechargeAmount;
                        existing.IsHeld = isHeld;
                        existing.IsDropped = isDropped;
                        existing.IsUsed = false;
                        existing.PosX = battery.transform.position.x;
                        existing.PosY = battery.transform.position.y;
                        existing.PosZ = battery.transform.position.z;
                        existing.RotX = battery.transform.rotation.x;
                        existing.RotY = battery.transform.rotation.y;
                        existing.RotZ = battery.transform.rotation.z;
                        existing.RotW = battery.transform.rotation.w;
                        connection.Update(existing);
                    }
                    else
                    {
                        BatteryData batteryData = new BatteryData
                        {
                            BatteryName = cleanName,
                            RechargeAmount = battery.rechargeAmount,
                            IsHeld = isHeld,
                            IsDropped = isDropped,
                            IsUsed = false,
                            PosX = battery.transform.position.x,
                            PosY = battery.transform.position.y,
                            PosZ = battery.transform.position.z,
                            RotX = battery.transform.rotation.x,
                            RotY = battery.transform.rotation.y,
                            RotZ = battery.transform.rotation.z,
                            RotW = battery.transform.rotation.w
                        };
                        connection.Insert(batteryData);
                    }
                }
            }

            // ── Mark batteries that are no longer in the scene as used ──
            foreach (var existing in existingBatteryData)
            {
                if (!sceneBatteryNames.Contains(existing.BatteryName) && !existing.IsUsed)
                {
                    existing.IsUsed = true;
                    existing.IsHeld = false;
                    existing.IsDropped = false;
                    connection.Update(existing);
                }
            }

            // ── SAVE RITUAL ITEMS STATE ──
            connection.DeleteAll<RitualItemData>();
            GameObject[] allRitualObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            List<GameObject> sceneRitualObjects = new List<GameObject>();

            foreach (GameObject obj in allRitualObjects)
            {
                if (obj == null) continue;
                if (obj.scene.IsValid() && obj.hideFlags == HideFlags.None)
                {
                    sceneRitualObjects.Add(obj);
                }
            }

            foreach (GameObject obj in sceneRitualObjects)
            {
                if (obj == null) continue;

                PickupItem pickup = obj.GetComponent<PickupItem>();
                if (pickup == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                bool isRitualItem = cleanName == "LargeCandle" || 
                                    cleanName == "LargeCandle (1)" || 
                                    cleanName == "Cross" || 
                                    cleanName == "Bible";
                
                if (!isRitualItem) continue;

                bool isRevealed = obj.activeSelf;
                bool isPlaced = false;
                bool isDropped = false;

                // ── Check ALL ancestors for holder components ──
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    TableHolder tableHolder = parent.GetComponent<TableHolder>();
                    CandleHolder candleHolder = parent.GetComponent<CandleHolder>();
                    if (tableHolder != null || candleHolder != null)
                    {
                        isPlaced = true;
                        break;
                    }
                    parent = parent.parent;
                }

                // ── If not held and not placed, it's dropped ──
                if (!isPlaced && obj.transform.parent == null)
                {
                    isDropped = true;
                }

                RitualItemData ritualData = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = isRevealed,
                    IsPlaced = isPlaced,
                    IsDropped = isDropped,
                    PosX = obj.transform.position.x,
                    PosY = obj.transform.position.y,
                    PosZ = obj.transform.position.z,
                    RotX = obj.transform.rotation.x,
                    RotY = obj.transform.rotation.y,
                    RotZ = obj.transform.rotation.z,
                    RotW = obj.transform.rotation.w
                };
                connection.Insert(ritualData);
            }

            // ── SAVE FLASHLIGHT STATE ──
            connection.DeleteAll<FlashlightData>();
            FlashlightPickup[] flashlights = Object.FindObjectsOfType<FlashlightPickup>();
            foreach (FlashlightPickup flashlight in flashlights)
            {
                if (flashlight != null)
                {
                    FlashlightData flashlightData = new FlashlightData
                    {
                        FlashlightName = flashlight.gameObject.name.Replace("(Clone)", ""),
                        BatteryLife = flashlight.batteryLife,
                        CurrentBattery = flashlight.GetBatteryPercent() * flashlight.batteryLife,
                        IsHeld = flashlight.gameObject.transform.parent != null && 
                                 flashlight.gameObject.transform.parent.CompareTag("Player"),
                        WasDropped = flashlight.wasDropped,
                        PosX = flashlight.transform.position.x,
                        PosY = flashlight.transform.position.y,
                        PosZ = flashlight.transform.position.z
                    };
                    connection.Insert(flashlightData);
                }
            }

            // ── SAVE DOORS WITH ROTATION ──
            connection.DeleteAll<DoorData>();
            DoorInteraction[] doors = Object.FindObjectsOfType<DoorInteraction>();
            int doorCount = 0;

            foreach (DoorInteraction door in doors)
            {
                if (door == null) continue;

                string doorId = GenerateDoorId(door);

                DoorData doorData = new DoorData
                {
                    DoorId = doorId,
                    DoorName = door.name,
                    IsUnlocked = !door.IsLocked(),
                    IsOpen = door.IsOpen(),
                    RotX = door.transform.rotation.x,
                    RotY = door.transform.rotation.y,
                    RotZ = door.transform.rotation.z,
                    RotW = door.transform.rotation.w
                };
                connection.Insert(doorData);
                doorCount++;
            }

            // ── SAVE RITUAL ──
            connection.DeleteAll<RitualData>();
            RitualManager ritual = Object.FindObjectsOfType<RitualManager>().FirstOrDefault();
            if (ritual != null)
            {
                RitualData ritualData = new RitualData
                {
                    IsComplete = ritual.IsRitualComplete()
                };
                connection.Insert(ritualData);
            }

            // ── SAVE NOTES ──
            connection.DeleteAll<NoteData>();
            Note[] notes = Object.FindObjectsOfType<Note>();
            foreach (Note note in notes)
            {
                if (note != null && note.HasBeenRead())
                {
                    NoteData noteData = new NoteData
                    {
                        NoteTitle = note.noteTitle,
                        IsRead = true
                    };
                    connection.Insert(noteData);
                }
            }

            // -- SAVE ONE-TIME SUBTITLES --
            connection.DeleteAll<SubtitleData>();
            foreach (ItemSubtitleTrigger trigger in Resources.FindObjectsOfTypeAll<ItemSubtitleTrigger>())
            {
                if (trigger != null && trigger.gameObject.scene.IsValid() && trigger.HasTriggered())
                {
                    connection.InsertOrReplace(new SubtitleData { SubtitleId = trigger.GetSubtitleId(), IsTriggered = true });
                }
            }
            foreach (PlayerSubtitleTrigger trigger in Resources.FindObjectsOfTypeAll<PlayerSubtitleTrigger>())
            {
                if (trigger != null && trigger.gameObject.scene.IsValid() && trigger.HasTriggered())
                {
                    connection.InsertOrReplace(new SubtitleData { SubtitleId = trigger.GetSubtitleId(), IsTriggered = true });
                }
            }
            // ── SAVE CHECKPOINT ──
            connection.DeleteAll<GameStateData>();
            if (CheckpointTrigger.HasCheckpointSaved)
            {
                GameStateData checkpointData = new GameStateData
                {
                    Key = "HasCheckpoint",
                    Value = "true"
                };
                connection.Insert(checkpointData);

                if (PlayerPrefs.HasKey("CheckpointPosX"))
                {
                    GameStateData checkpointPos = new GameStateData
                    {
                        Key = "CheckpointPos",
                        Value = $"{PlayerPrefs.GetFloat("CheckpointPosX")},{PlayerPrefs.GetFloat("CheckpointPosY")},{PlayerPrefs.GetFloat("CheckpointPosZ")}"
                    };
                    connection.Insert(checkpointPos);
                }
            }

            // ── SAVE USED KEYS ──
            var usedKeyCount = connection.Table<KeyData>().Where(k => k.WasUsed).Count();

            // ── SAVE DROPPED ITEMS ──
            connection.DeleteAll<DroppedItemData>();
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            int droppedCount = 0;

            List<string> inventoryItemNames = new List<string>();
            if (Inventory.Instance != null)
            {
                foreach (GameObject item in Inventory.Instance.GetItems())
                {
                    if (item != null)
                    {
                        string cleanName = item.name.Replace("(Clone)", "");
                        inventoryItemNames.Add(cleanName);
                    }
                }
            }

            var usedKeyData = connection.Table<KeyData>().Where(k => k.WasUsed).ToList();
            List<string> usedKeyNames = new List<string>();
            foreach (KeyData keyData in usedKeyData)
            {
                usedKeyNames.Add(keyData.KeyName);
            }

            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                PickupItem pickup = obj.GetComponent<PickupItem>();
                Key key = obj.GetComponent<Key>();
                FlashlightPickup flashlight = obj.GetComponent<FlashlightPickup>();
                CandleItem candle = obj.GetComponent<CandleItem>();
                BatteryPickup battery = obj.GetComponent<BatteryPickup>();

                bool isDropped = false;
                string cleanName = obj.name.Replace("(Clone)", "");

                if (inventoryItemNames.Contains(cleanName))
                {
                    continue;
                }

                if (candle != null)
                {
                    continue;
                }

                if (key != null && usedKeyNames.Contains(cleanName))
                {
                    continue;
                }

                if (battery != null)
                {
                    continue;
                }

                if (cleanName == "LargeCandle" || 
                    cleanName == "LargeCandle (1)" || 
                    cleanName == "Cross" || 
                    cleanName == "Bible")
                {
                    continue;
                }

                if (candle != null && cleanName.ToLower().Contains("candle"))
                {
                    continue;
                }

                if (key != null && key.wasDropped && !key.IsPickedUp)
                {
                    isDropped = true;
                }

                if (flashlight != null && flashlight.wasDropped)
                {
                    bool isHeld = obj.transform.parent != null && obj.transform.parent.CompareTag("Player");
                    if (!isHeld && !inventoryItemNames.Contains(cleanName))
                    {
                        isDropped = true;
                    }
                }

                if (pickup != null && !pickup.isPickedUp && key == null && flashlight == null && battery == null && candle == null)
                {
                    continue;
                }

                if (isDropped)
                {
                    DroppedItemData droppedData = new DroppedItemData
                    {
                        ItemName = cleanName,
                        PosX = obj.transform.position.x,
                        PosY = obj.transform.position.y,
                        PosZ = obj.transform.position.z,
                        RotX = obj.transform.rotation.x,
                        RotY = obj.transform.rotation.y,
                        RotZ = obj.transform.rotation.z,
                        RotW = obj.transform.rotation.w
                    };
                    connection.Insert(droppedData);
                    droppedCount++;
                }
            }

            // ── NEW: SAVE PROGRESSION ──
            if (ProgressionSystem.Instance != null)
            {
                connection.DeleteAll<ProgressionData>();
                ProgressionData progressionData = new ProgressionData
                {
                    ProgressValue = ProgressionSystem.Instance.GetProgressForSave(),
                    TotalPoints = ProgressionSystem.Instance.totalProgressPoints
                };
                connection.Insert(progressionData);
            }

            PlayerPrefs.SetInt("SkipIntro", 1);
            PlayerPrefs.SetString("SaveTime", System.DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
        }

        isSaving = false;
    }

    private void SaveAIPositions()
    {
        connection.DeleteAll<AIPositionData>();

        string sceneName = SceneManager.GetActiveScene().name;
        foreach (Component ai in FindSaveableAI())
        {
            if (ai == null) continue;

            Transform enemy = ai.transform;
            connection.Insert(new AIPositionData
            {
                AIId = GenerateAIId(ai),
                SceneName = sceneName,
                PosX = enemy.position.x,
                PosY = enemy.position.y,
                PosZ = enemy.position.z,
                RotX = enemy.rotation.x,
                RotY = enemy.rotation.y,
                RotZ = enemy.rotation.z,
                RotW = enemy.rotation.w
            });
        }
    }

    private void RestoreAIPositions()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Dictionary<string, AIPositionData> savedPositions = connection.Table<AIPositionData>()
            .Where(data => data.SceneName == sceneName)
            .ToDictionary(data => data.AIId, data => data);

        foreach (Component ai in FindSaveableAI())
        {
            if (ai == null) continue;

            AIPositionData savedPosition;
            if (!savedPositions.TryGetValue(GenerateAIId(ai), out savedPosition))
                continue;

            Transform enemy = ai.transform;
            Vector3 position = new Vector3(savedPosition.PosX, savedPosition.PosY, savedPosition.PosZ);
            Quaternion rotation = new Quaternion(
                savedPosition.RotX, savedPosition.RotY, savedPosition.RotZ, savedPosition.RotW);

            NavMeshAgent navAgent = enemy.GetComponent<NavMeshAgent>();
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                navAgent.Warp(position);
            else
                enemy.position = position;

            enemy.rotation = rotation;
        }
    }

    private IEnumerable<Component> FindSaveableAI()
    {
        foreach (MonsterAI_New ai in Object.FindObjectsOfType<MonsterAI_New>())
            yield return ai;
        foreach (MutantAI ai in Object.FindObjectsOfType<MutantAI>())
            yield return ai;
        foreach (TikbalangAI ai in Object.FindObjectsOfType<TikbalangAI>())
            yield return ai;
    }

    private string GenerateAIId(Component ai)
    {
        Transform current = ai.transform;
        string path = current.name + "[" + current.GetSiblingIndex() + "]";

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "[" + current.GetSiblingIndex() + "]/" + path;
        }

        return ai.GetType().Name + ":" + path;
    }
    private string GenerateDoorId(DoorInteraction door)
    {
        Vector3 pos = door.transform.position;
        return $"{door.name}_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
    }

    public bool LoadGame()
    {
        EnsureDatabaseReady();
        
        if (!isDatabaseReady)
        {
            return false;
        }

        try
        {
            PlayerData playerData = connection.Table<PlayerData>().FirstOrDefault();
            if (playerData == null)
            {
                return false;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return false;
            }

            Vector3 targetPosition = new Vector3(playerData.PosX, playerData.PosY, playerData.PosZ);

            GameStateData checkpointState = connection.Table<GameStateData>()
                .Where(x => x.Key == "HasCheckpoint").FirstOrDefault();

            if (checkpointState != null && checkpointState.Value == "true")
            {
                GameStateData checkpointPos = connection.Table<GameStateData>()
                    .Where(x => x.Key == "CheckpointPos").FirstOrDefault();
                if (checkpointPos != null)
                {
                    string[] pos = checkpointPos.Value.Split(',');
                    if (pos.Length == 3)
                    {
                        targetPosition = new Vector3(
                            float.Parse(pos[0]),
                            float.Parse(pos[1]),
                            float.Parse(pos[2])
                        );
                    }
                }
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = targetPosition;
            player.transform.rotation = new Quaternion(
                playerData.RotX, playerData.RotY, playerData.RotZ, playerData.RotW
            );

            if (cc != null) cc.enabled = true;

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.RestoreHealth(
                    playerData.Health,
                    playerData.MaxHealth
                );
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.mouseSensitivity = playerData.Sensitivity;
            }

            StaminaController stamina = player.GetComponent<StaminaController>();
            StaminaData staminaData = connection.Table<StaminaData>().FirstOrDefault();
            if (stamina != null && staminaData != null)
                stamina.RestoreStamina(staminaData.CurrentStamina);

            RestoreAIPositions();

            // ── RESTORE DOORS ──
            var doorDataList = connection.Table<DoorData>().ToList();
            DoorInteraction[] doorObjects = Object.FindObjectsOfType<DoorInteraction>();
            int restoredDoorCount = 0;

            Dictionary<string, DoorInteraction> doorLookup = new Dictionary<string, DoorInteraction>();
            foreach (DoorInteraction door in doorObjects)
            {
                if (door == null) continue;
                string doorId = GenerateDoorId(door);
                if (!doorLookup.ContainsKey(doorId))
                {
                    doorLookup.Add(doorId, door);
                }
                if (!doorLookup.ContainsKey(door.name))
                {
                    doorLookup.Add(door.name, door);
                }
            }

            foreach (DoorData doorData in doorDataList)
            {
                DoorInteraction matchedDoor = null;

                if (!string.IsNullOrEmpty(doorData.DoorId) && doorLookup.ContainsKey(doorData.DoorId))
                {
                    matchedDoor = doorLookup[doorData.DoorId];
                }

                if (matchedDoor == null && doorLookup.ContainsKey(doorData.DoorName))
                {
                    matchedDoor = doorLookup[doorData.DoorName];
                }

                if (matchedDoor != null)
                {
                    if (doorData.IsUnlocked)
                    {
                        if (matchedDoor.IsLocked())
                        {
                            matchedDoor.UnlockSilent();
                        }
                    }
                    else
                    {
                        if (!matchedDoor.IsLocked())
                        {
                            matchedDoor.Lock();
                        }
                    }

                    matchedDoor.transform.rotation = new Quaternion(
                        doorData.RotX,
                        doorData.RotY,
                        doorData.RotZ,
                        doorData.RotW
                    );

                    matchedDoor.SetOpenState(doorData.IsOpen);

                    restoredDoorCount++;
                }
                else
                {
                }
            }

            // ── Get data from database ──
            var inventoryItems = connection.Table<InventoryData>().ToList();
            List<string> inventoryItemNames = new List<string>();
            foreach (InventoryData invData in inventoryItems)
            {
                inventoryItemNames.Add(invData.ItemName);
            }

            var droppedItems = connection.Table<DroppedItemData>().ToList();
            List<string> droppedItemNames = new List<string>();
            foreach (DroppedItemData droppedData in droppedItems)
            {
                droppedItemNames.Add(droppedData.ItemName);
            }

            var flashlightData = connection.Table<FlashlightData>().ToList();
            var batteryDataList = connection.Table<BatteryData>().ToList();
            var ritualItemDataList = connection.Table<RitualItemData>().ToList();

            var usedKeyData = connection.Table<KeyData>().Where(k => k.WasUsed).ToList();
            List<string> usedKeyNames = new List<string>();
            foreach (KeyData keyData in usedKeyData)
            {
                usedKeyNames.Add(keyData.KeyName);
            }

            // ── Get used batteries from database ──
            var usedBatteryData = batteryDataList.Where(b => b.IsUsed).ToList();
            List<string> usedBatteryNames = new List<string>();
            foreach (BatteryData batteryData in usedBatteryData)
            {
                usedBatteryNames.Add(batteryData.BatteryName);
            }

            // ── Get batteries that are held in inventory ──
            var heldBatteryData = batteryDataList.Where(b => b.IsHeld && !b.IsUsed).ToList();
            List<string> heldBatteryNames = new List<string>();
            foreach (BatteryData batteryData in heldBatteryData)
            {
                heldBatteryNames.Add(batteryData.BatteryName);
            }

            // ── Get dropped batteries from database ──
            var droppedBatteryData = batteryDataList.Where(b => b.IsDropped && !b.IsUsed && !b.IsHeld).ToList();
            List<string> droppedBatteryNames = new List<string>();
            foreach (BatteryData batteryData in droppedBatteryData)
            {
                droppedBatteryNames.Add(batteryData.BatteryName);
            }

            // ── Get ritual items data ──
            var revealedRitualItems = ritualItemDataList.Where(r => r.IsRevealed).ToList();
            List<string> revealedItemNames = new List<string>();
            foreach (RitualItemData itemData in revealedRitualItems)
            {
                revealedItemNames.Add(itemData.ItemName);
            }

            var droppedRitualItems = ritualItemDataList.Where(r => r.IsDropped).ToList();
            List<string> droppedRitualItemNames = new List<string>();
            foreach (RitualItemData itemData in droppedRitualItems)
            {
                droppedRitualItemNames.Add(itemData.ItemName);
            }

            var placedRitualItems = ritualItemDataList.Where(r => r.IsPlaced).ToList();
            List<string> placedItemNames = new List<string>();
            foreach (RitualItemData itemData in placedRitualItems)
            {
                placedItemNames.Add(itemData.ItemName);
            }

            // ── Handle all items in scene ──
            GameObject[] sceneObjects = Object.FindObjectsOfType<GameObject>();
            List<GameObject> itemsToHide = new List<GameObject>();
            List<GameObject> itemsToDestroy = new List<GameObject>();
            List<GameObject> batteriesToDestroy = new List<GameObject>();
            List<GameObject> ritualItemsToRespawn = new List<GameObject>();

            // ── First pass: Destroy used keys ──
            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;
                Key key = obj.GetComponent<Key>();
                if (key == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                if (usedKeyNames.Contains(cleanName))
                {
                    itemsToDestroy.Add(obj);
                }
            }

            // ── Second pass: Destroy used batteries ──
            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;
                BatteryPickup battery = obj.GetComponent<BatteryPickup>();
                if (battery == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                if (usedBatteryNames.Contains(cleanName))
                {
                    itemsToDestroy.Add(obj);
                }
            }

            // ── Third pass: Handle dropped batteries ──
            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;
                BatteryPickup battery = obj.GetComponent<BatteryPickup>();
                if (battery == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                var savedBattery = droppedBatteryData.FirstOrDefault(b => b.BatteryName == cleanName);
                if (savedBattery != null)
                {
                    bool isInInventory = inventoryItemNames.Contains(cleanName);
                    if (!isInInventory)
                    {
                        batteriesToDestroy.Add(obj);
                    }
                }
            }

            // ── Fourth pass: Handle dropped ritual items ──
            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;
                
                PickupItem pickup = obj.GetComponent<PickupItem>();
                if (pickup == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                bool isRitualItem = cleanName == "LargeCandle" || 
                                    cleanName == "LargeCandle (1)" || 
                                    cleanName == "Cross" || 
                                    cleanName == "Bible";
                
                if (!isRitualItem) continue;

                if (droppedRitualItemNames.Contains(cleanName))
                {
                    bool isInInventory = inventoryItemNames.Contains(cleanName);
                    if (!isInInventory)
                    {
                        ritualItemsToRespawn.Add(obj);
                    }
                }
            }

            // ── Fifth pass: Restore ritual item visibility ──
            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;
                
                PickupItem pickup = obj.GetComponent<PickupItem>();
                if (pickup == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                bool isRitualItem = cleanName == "LargeCandle" || 
                                    cleanName == "LargeCandle (1)" || 
                                    cleanName == "Cross" || 
                                    cleanName == "Bible";
                
                if (!isRitualItem) continue;

                bool isRevealed = revealedItemNames.Contains(cleanName);

                if (isRevealed)
                {
                    obj.SetActive(true);
                }
                else
                {
                    obj.SetActive(false);
                }
            }

            // ── SIXTH PASS: Restore placed ritual items to their holders ──
            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;
                
                PickupItem pickup = obj.GetComponent<PickupItem>();
                if (pickup == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                
                bool isRitualItem = cleanName == "LargeCandle" || 
                                    cleanName == "LargeCandle (1)" || 
                                    cleanName == "Cross" || 
                                    cleanName == "Bible";
                
                if (!isRitualItem) continue;

                // ── If this item should be placed, find its holder ──
                if (placedItemNames.Contains(cleanName))
                {
                    // Find all CandleHolders and TableHolders in the scene
                    CandleHolder[] candleHolders = Object.FindObjectsOfType<CandleHolder>();
                    TableHolder[] tableHolders = Object.FindObjectsOfType<TableHolder>();
                    
                    bool wasPlaced = false;
                    
                    // Check candle holders
                    foreach (CandleHolder holder in candleHolders)
                    {
                        // Check if this holder already has a candle placed
                        if (holder.HasCandle()) continue;
                        
                        // Check if the item is a candle
                        if (cleanName == "LargeCandle" || cleanName == "LargeCandle (1)")
                        {
                            // Reparent the item to the holder's placement point
                            if (holder.placementPoint != null)
                            {
                                obj.transform.SetParent(holder.placementPoint);
                                obj.transform.localPosition = Vector3.zero;
                                obj.transform.localRotation = Quaternion.identity;
                                
                                // Disable physics and colliders
                                Rigidbody rb = obj.GetComponent<Rigidbody>();
                                if (rb != null)
                                {
                                    rb.isKinematic = true;
                                    rb.useGravity = false;
                                }
                                foreach (Collider c in obj.GetComponentsInChildren<Collider>())
                                    c.enabled = false;
                                
                                PickupItem pi = obj.GetComponent<PickupItem>();
                                if (pi != null) pi.enabled = false;
                                
                                CandleItem ci = obj.GetComponent<CandleItem>();
                                if (ci != null) ci.SetHeld(true);
                                
                                // Mark holder as having the item using reflection
                                var hasCandleField = typeof(CandleHolder).GetField("hasCandle", 
                                    System.Reflection.BindingFlags.NonPublic | 
                                    System.Reflection.BindingFlags.Instance);
                                if (hasCandleField != null)
                                    hasCandleField.SetValue(holder, true);
                                
                                var placedCandleField = typeof(CandleHolder).GetField("placedCandle", 
                                    System.Reflection.BindingFlags.NonPublic | 
                                    System.Reflection.BindingFlags.Instance);
                                if (placedCandleField != null)
                                    placedCandleField.SetValue(holder, obj);
                                
                                wasPlaced = true;
                                break;
                            }
                        }
                    }
                    
                    // If not placed yet, check table holders
                    if (!wasPlaced)
                    {
                        foreach (TableHolder holder in tableHolders)
                        {
                            if (holder.HasItem()) continue;
                            
                            // Check if the item matches this holder's required item
                            if (cleanName == holder.itemNameRequired || 
                                cleanName.ToLower().Contains(holder.itemNameRequired.ToLower()))
                            {
                                if (holder.placementPoint != null)
                                {
                                    obj.transform.SetParent(holder.placementPoint);
                                    obj.transform.localPosition = Vector3.zero;
                                    obj.transform.localRotation = Quaternion.identity;
                                    
                                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                                    if (rb != null)
                                    {
                                        rb.isKinematic = true;
                                        rb.useGravity = false;
                                    }
                                    foreach (Collider c in obj.GetComponentsInChildren<Collider>())
                                        c.enabled = false;
                                    
                                    PickupItem pi = obj.GetComponent<PickupItem>();
                                    if (pi != null) pi.enabled = false;
                                    
                                    // Mark holder as having the item using reflection
                                    var hasItemField = typeof(TableHolder).GetField("hasItem", 
                                        System.Reflection.BindingFlags.NonPublic | 
                                        System.Reflection.BindingFlags.Instance);
                                    if (hasItemField != null)
                                        hasItemField.SetValue(holder, true);
                                    
                                    var placedItemField = typeof(TableHolder).GetField("placedItem", 
                                        System.Reflection.BindingFlags.NonPublic | 
                                        System.Reflection.BindingFlags.Instance);
                                    if (placedItemField != null)
                                        placedItemField.SetValue(holder, obj);
                                    
                                    wasPlaced = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (!wasPlaced)
                    {
                    }
                }
            }

            foreach (GameObject obj in sceneObjects)
            {
                if (obj == null) continue;

                PickupItem pickup = obj.GetComponent<PickupItem>();
                Key key = obj.GetComponent<Key>();
                FlashlightPickup flashlight = obj.GetComponent<FlashlightPickup>();
                CandleItem candle = obj.GetComponent<CandleItem>();
                BatteryPickup battery = obj.GetComponent<BatteryPickup>();

                if (pickup == null && key == null && flashlight == null && battery == null && candle == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                bool isInInventory = inventoryItemNames.Contains(cleanName);
                bool isDroppedInSave = droppedItemNames.Contains(cleanName);
                bool isUsedKey = usedKeyNames.Contains(cleanName);
                bool isUsedBattery = usedBatteryNames.Contains(cleanName);
                bool isHeldBattery = heldBatteryNames.Contains(cleanName);
                bool isDroppedBattery = droppedBatteryNames.Contains(cleanName);
                bool isRitualItem = cleanName == "LargeCandle" || 
                                    cleanName == "LargeCandle (1)" || 
                                    cleanName == "Cross" || 
                                    cleanName == "Bible";

                if (isUsedKey || isUsedBattery)
                {
                    continue;
                }

                if (isRitualItem)
                {
                    continue;
                }

                if (isInInventory || isHeldBattery)
                {
                    if (obj.transform.parent == null || !obj.transform.parent.CompareTag("Player"))
                    {
                        itemsToHide.Add(obj);
                    }
                    continue;
                }

                if (candle != null)
                {
                    continue;
                }

                if (isDroppedInSave)
                {
                    bool isDroppedVersion = false;

                    if (key != null && key.wasDropped && !key.IsPickedUp)
                    {
                        isDroppedVersion = true;
                    }
                    else if (flashlight != null)
                    {
                        bool isHeld = obj.transform.parent != null && obj.transform.parent.CompareTag("Player");
                        if (flashlight.wasDropped && !isHeld)
                        {
                            isDroppedVersion = true;
                        }
                        else if (!isHeld && !flashlight.wasDropped)
                        {
                            isDroppedVersion = false;
                        }
                    }

                    if (!isDroppedVersion)
                    {
                        itemsToDestroy.Add(obj);
                    }
                }
                else
                {
                    if (flashlight != null && droppedItemNames.Contains(cleanName))
                    {
                        bool isHeld = obj.transform.parent != null && obj.transform.parent.CompareTag("Player");
                        if (!isHeld)
                        {
                            itemsToDestroy.Add(obj);
                        }
                    }
                }
            }

            // ── Destroy marked items ──
            foreach (GameObject obj in itemsToDestroy)
            {
                if (obj == null) continue;

                if (obj.transform.parent != null && obj.transform.parent.CompareTag("Player"))
                    continue;

                Destroy(obj);
            }

            // ── Destroy and respawn batteries at saved dropped positions ──
            foreach (GameObject obj in batteriesToDestroy)
            {
                if (obj == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                var savedBattery = droppedBatteryData.FirstOrDefault(b => b.BatteryName == cleanName);
                
                if (savedBattery != null)
                {
                    Destroy(obj);
                    
                    if (PrefabManager.Instance != null)
                    {
                        Vector3 position = new Vector3(savedBattery.PosX, savedBattery.PosY, savedBattery.PosZ);
                        Quaternion rotation = new Quaternion(savedBattery.RotX, savedBattery.RotY, savedBattery.RotZ, savedBattery.RotW);
                        
                        GameObject spawnedBattery = PrefabManager.Instance.SpawnDroppedItem(cleanName, position, rotation);
                        
                        if (spawnedBattery != null)
                        {
                            BatteryPickup batteryComp = spawnedBattery.GetComponent<BatteryPickup>();
                            if (batteryComp != null)
                            {
                                batteryComp.rechargeAmount = savedBattery.RechargeAmount;
                                batteryComp.wasDropped = true;
                                batteryComp.isHeld = false;
                                batteryComp.wasUsed = false;
                            }
                        }
                    }
                }
            }

            // ── Destroy and respawn ritual items at saved dropped positions ──
            foreach (GameObject obj in ritualItemsToRespawn)
            {
                if (obj == null) continue;

                string cleanName = obj.name.Replace("(Clone)", "");
                var savedItem = droppedRitualItems.FirstOrDefault(r => r.ItemName == cleanName);
                
                if (savedItem != null)
                {
                    Destroy(obj);
                    
                    if (PrefabManager.Instance != null)
                    {
                        Vector3 position = new Vector3(savedItem.PosX, savedItem.PosY, savedItem.PosZ);
                        Quaternion rotation = new Quaternion(savedItem.RotX, savedItem.RotY, savedItem.RotZ, savedItem.RotW);
                        
                        GameObject spawnedItem = PrefabManager.Instance.SpawnDroppedItem(cleanName, position, rotation);
                        
                        if (spawnedItem != null)
                        {
                            spawnedItem.SetActive(true);
                        }
                    }
                }
            }

            // ── Hide items that are in inventory ──
            foreach (GameObject obj in itemsToHide)
            {
                if (obj == null) continue;

                foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
                    r.enabled = false;

                foreach (Collider c in obj.GetComponentsInChildren<Collider>())
                    c.enabled = false;

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                PickupItem pickup = obj.GetComponent<PickupItem>();
                if (pickup != null)
                {
                    pickup.isPickedUp = true;
                }

                Key key = obj.GetComponent<Key>();
                if (key != null)
                {
                    key.isPickedUp = true;
                }

                FlashlightPickup flashlight = obj.GetComponent<FlashlightPickup>();
                if (flashlight != null)
                {
                    flashlight.SetHeld(false);
                }
            }

            // ── RESTORE INVENTORY ──
            if (inventoryItems.Count > 0 && Inventory.Instance != null)
            {
                while (Inventory.Instance.GetItems().Count > 0)
                {
                    Inventory.Instance.DropItem(0);
                }

                foreach (InventoryData invData in inventoryItems)
                {
                    if (usedKeyNames.Contains(invData.ItemName) || usedBatteryNames.Contains(invData.ItemName))
                    {
                        continue;
                    }

                    GameObject itemObject = null;

                    GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj == null) continue;
                        string cleanName = obj.name.Replace("(Clone)", "");
                        if (cleanName == invData.ItemName)
                        {
                            itemObject = obj;
                            break;
                        }
                    }

                    if (itemObject != null)
                    {
                        PickupItem pickup = itemObject.GetComponent<PickupItem>();
                        if (pickup != null)
                        {
                            pickup.isPickedUp = true;
                            pickup.RemoveGlowLight();
                        }

                        foreach (Collider c in itemObject.GetComponentsInChildren<Collider>())
                        {
                            c.enabled = false;
                        }

                        foreach (Renderer r in itemObject.GetComponentsInChildren<Renderer>())
                        {
                            r.enabled = false;
                        }

                        Rigidbody rb = itemObject.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }

                        Camera cam = Camera.main;
                        if (cam != null)
                        {
                            itemObject.transform.SetParent(cam.transform);

                            if (pickup != null)
                            {
                                itemObject.transform.localPosition = pickup.heldPositionOffset;
                                itemObject.transform.localRotation = Quaternion.Euler(pickup.heldRotation);
                            }
                            else
                            {
                                itemObject.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                                itemObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                            }
                        }

                        FlashlightPickup flashlight = itemObject.GetComponent<FlashlightPickup>();
                        if (flashlight != null)
                        {
                            var savedFlashlight = flashlightData.FirstOrDefault(f => f.FlashlightName == invData.ItemName);
                            if (savedFlashlight != null)
                            {
                                flashlight.SetBattery(savedFlashlight.CurrentBattery);
                                flashlight.batteryLife = savedFlashlight.BatteryLife;
                            }
                            flashlight.SetHeld(true);
                            flashlight.ResetDroppedState();
                        }

                        Inventory.Instance.AddItem(itemObject);
                    }
                    else
                    {
                    }
                }

                if (Inventory.Instance.GetItems().Count > 0)
                {
                }
            }

            // ── RESTORE DROPPED ITEMS ──
            foreach (DroppedItemData droppedData in droppedItems)
            {
                if (inventoryItemNames.Contains(droppedData.ItemName))
                {
                    continue;
                }

                if (usedKeyNames.Contains(droppedData.ItemName))
                {
                    continue;
                }

                if (usedBatteryNames.Contains(droppedData.ItemName))
                {
                    continue;
                }

                if (heldBatteryNames.Contains(droppedData.ItemName))
                {
                    continue;
                }

                if (droppedBatteryNames.Contains(droppedData.ItemName))
                {
                    continue;
                }

                if (droppedRitualItemNames.Contains(droppedData.ItemName))
                {
                    continue;
                }

                if (droppedData.ItemName == "LargeCandle" || 
                    droppedData.ItemName == "LargeCandle (1)" || 
                    droppedData.ItemName == "Cross" || 
                    droppedData.ItemName == "Bible")
                {
                    continue;
                }

                var batteryUsedCheck = batteryDataList.FirstOrDefault(b => b.BatteryName == droppedData.ItemName && b.IsUsed);
                if (batteryUsedCheck != null)
                {
                    continue;
                }

                if (PrefabManager.Instance != null)
                {
                    Vector3 position = new Vector3(droppedData.PosX, droppedData.PosY, droppedData.PosZ);
                    Quaternion rotation = new Quaternion(droppedData.RotX, droppedData.RotY, droppedData.RotZ, droppedData.RotW);

                    GameObject spawnedItem = PrefabManager.Instance.SpawnDroppedItem(droppedData.ItemName, position, rotation);

                    if (spawnedItem != null)
                    {
                        spawnedItem.transform.position = position;
                        spawnedItem.transform.rotation = rotation;

                        Key keyComp = spawnedItem.GetComponent<Key>();
                        if (keyComp != null)
                        {
                            keyComp.wasDropped = true;
                            keyComp.isPickedUp = false;
                        }

                        PickupItem pickupComp = spawnedItem.GetComponent<PickupItem>();
                        if (pickupComp != null)
                        {
                            pickupComp.isPickedUp = false;
                        }

                        FlashlightPickup flashlightComp = spawnedItem.GetComponent<FlashlightPickup>();
                        if (flashlightComp != null)
                        {
                            var savedFlashlight = flashlightData.FirstOrDefault(f => f.FlashlightName == droppedData.ItemName);
                            if (savedFlashlight != null)
                            {
                                flashlightComp.SetBattery(savedFlashlight.CurrentBattery);
                                flashlightComp.batteryLife = savedFlashlight.BatteryLife;
                            }
                            flashlightComp.wasDropped = true;
                            flashlightComp.SetHeld(false);
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }

            // -- LOAD ONE-TIME SUBTITLES --
            var shownSubtitleIds = new HashSet<string>(
                connection.Table<SubtitleData>().Where(data => data.IsTriggered).Select(data => data.SubtitleId));
            foreach (ItemSubtitleTrigger trigger in Resources.FindObjectsOfTypeAll<ItemSubtitleTrigger>())
            {
                if (trigger != null && trigger.gameObject.scene.IsValid())
                    trigger.RestoreTriggeredState(shownSubtitleIds.Contains(trigger.GetSubtitleId()));
            }
            foreach (PlayerSubtitleTrigger trigger in Resources.FindObjectsOfTypeAll<PlayerSubtitleTrigger>())
            {
                if (trigger != null && trigger.gameObject.scene.IsValid())
                    trigger.RestoreTriggeredState(shownSubtitleIds.Contains(trigger.GetSubtitleId()));
            }
            // ── NEW: LOAD PROGRESSION ──
            var progressionData = connection.Table<ProgressionData>().FirstOrDefault();
            if (progressionData != null)
            {
                if (ProgressionSystem.Instance != null)
                    ProgressionSystem.Instance.LoadProgressFromSave(progressionData.ProgressValue);
            }
            return true;
        }
        catch (System.Exception e)
        {
            return false;
        }
    }

    public void ClearProgressionData()
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
            return;

        try
        {
            connection.DeleteAll<ProgressionData>();
        }
        catch (System.Exception e)
        {
        }
    }

    public bool TryGetStoryIntroProgress(out int sectionIndex, out int lineIndex, out bool isComplete)
    {
        sectionIndex = 0;
        lineIndex = 0;
        isComplete = false;

        EnsureDatabaseReady();
        if (!isDatabaseReady) return false;

        try
        {
            IntroData data = connection.Table<IntroData>().FirstOrDefault();
            if (data == null) return false;

            sectionIndex = data.SectionIndex;
            lineIndex = data.LineIndex;
            isComplete = data.IsComplete;
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    public void SaveStoryIntroProgress(int sectionIndex, int lineIndex, bool isComplete)
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try
        {
            connection.InsertOrReplace(new IntroData
            {
                Id = 1,
                SectionIndex = sectionIndex,
                LineIndex = lineIndex,
                IsComplete = isComplete
            });
        }
        catch (System.Exception)
        {
        }
    }
    public void ClearStoryIntroData()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try { connection.DeleteAll<IntroData>(); }
        catch (System.Exception) { }
    }

    public void ClearSubtitleData()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try { connection.DeleteAll<SubtitleData>(); }
        catch (System.Exception) { }
    }
    public bool HasSubtitleTriggered(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId)) return false;

        EnsureDatabaseReady();
        if (!isDatabaseReady) return false;

        try
        {
            SubtitleData data = connection.Find<SubtitleData>(subtitleId);
            return data != null && data.IsTriggered;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    public void MarkSubtitleTriggered(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId)) return;

        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try
        {
            connection.InsertOrReplace(new SubtitleData { SubtitleId = subtitleId, IsTriggered = true });
        }
        catch (System.Exception)
        {
        }
    }
    public bool HasSaveFile()
    {
        // ── Check if the database file exists without creating it ──
        return File.Exists(savePath);
    }

    public void DeleteSave()
    {
        if (!isDatabaseReady) return;

        try
        {
            connection.DeleteAll<PlayerData>();
            SaveAIPositions();

            connection.DeleteAll<InventoryData>();
            connection.DeleteAll<DoorData>();
            connection.DeleteAll<RitualData>();
            connection.DeleteAll<NoteData>();
            connection.DeleteAll<GameStateData>();
            connection.DeleteAll<DroppedItemData>();
            connection.DeleteAll<FlashlightData>();
            connection.DeleteAll<KeyData>();
            connection.DeleteAll<BatteryData>();
            connection.DeleteAll<RitualItemData>();
            connection.DeleteAll<StaminaData>();
            connection.DeleteAll<SubtitleData>();
            connection.DeleteAll<IntroData>();
            // ── NEW: Delete ProgressionData ──
            connection.DeleteAll<ProgressionData>();

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    void OnApplicationQuit()
    {
        if (autoSaveOnQuit && !isQuitting)
        {
            isQuitting = true;
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "menu" && sceneName != "MainMenu")
            {
                SaveGame();
            }
            isQuitting = false;
        }

        if (connection != null)
        {
            connection.Close();
            connection = null;
        }
    }

    void OnDestroy()
    {
        if (connection != null)
        {
            connection.Close();
            connection = null;
        }
    }
}

// ── SQLITE DATA MODELS ──

[Table("AIPositionData")]
public class AIPositionData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string AIId { get; set; }
    public string SceneName { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}
[Table("PlayerData")]
public class PlayerData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Sensitivity { get; set; }
    public string CurrentScene { get; set; }
}

[Table("InventoryData")]
public class InventoryData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
}

[Table("DoorData")]
public class DoorData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string DoorId { get; set; }
    public string DoorName { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsOpen { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}

[Table("RitualData")]
public class RitualData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public bool IsComplete { get; set; }
}

[Table("NoteData")]
public class NoteData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string NoteTitle { get; set; }
    public bool IsRead { get; set; }
}

[Table("GameStateData")]
public class GameStateData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}

[Table("DroppedItemData")]
public class DroppedItemData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ItemName { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}

[Table("FlashlightData")]
public class FlashlightData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string FlashlightName { get; set; }
    public float BatteryLife { get; set; }
    public float CurrentBattery { get; set; }
    public bool IsHeld { get; set; }
    public bool WasDropped { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Table("KeyData")]
public class KeyData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string KeyName { get; set; }
    public bool WasUsed { get; set; }
}

[Table("BatteryData")]
public class BatteryData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string BatteryName { get; set; }
    public float RechargeAmount { get; set; }
    public bool IsHeld { get; set; }
    public bool IsDropped { get; set; }
    public bool IsUsed { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}

[Table("RitualItemData")]
public class RitualItemData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string ItemName { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsPlaced { get; set; }
    public bool IsDropped { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}

// ── NEW: ProgressionData table ──
[Table("ProgressionData")]
public class ProgressionData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int ProgressValue { get; set; }
    public int TotalPoints { get; set; }
}

[Table("SubtitleData")]
public class SubtitleData
{
    [PrimaryKey]
    public string SubtitleId { get; set; }
    public bool IsTriggered { get; set; }
}
[Table("StaminaData")]
public class StaminaData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public float CurrentStamina { get; set; }
}

[Table("IntroData")]
public class IntroData
{
    [PrimaryKey]
    public int Id { get; set; }
    public int SectionIndex { get; set; }
    public int LineIndex { get; set; }
    public bool IsComplete { get; set; }
}