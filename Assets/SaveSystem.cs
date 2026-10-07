// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
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

    // File name and automatic-save option used by this persistent game manager.
    [Header("Save Settings")]
    public string saveFileName = "gameSave_v2.db";
    public bool autoSaveOnQuit = true;

    [Header("Debug")]
    [Tooltip("Writes each drawer's saved and restored state to the Unity Console.")]
    public bool logDrawerSaveLoad = true;

    private const string GasRecordId = "Gas";

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

    private bool IsHardModeDatabase()
    {
        return GetCurrentDifficulty() == "Hard";
    }

    void Awake()
    {
        // Keep one save manager alive across scenes and prepare the selected database.
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
        string fileName = difficulty == "Hard" ? "gameSave_Hard_v2.db" : "gameSave_v2.db";
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
        // Create missing tables and update the database structure before saving or loading.
        try
        {
            connection = new SQLiteConnection(savePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            RelationalSaveSchema.Create(connection, IsHardModeDatabase());

            connection.CreateTable<PlayerData>();
            connection.CreateTable<InventoryData>();
            connection.CreateTable<DoorData>();
            connection.CreateTable<DrawerData>();
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

            if (IsHardModeDatabase())
            {
                connection.CreateTable<GeneratorCoverData>();
                connection.CreateTable<WrenchData>();
                connection.CreateTable<GasData>();
            }
            else
            {
                // These systems exist only in Chapter 2 Hard mode.
                connection.Execute("DROP TABLE IF EXISTS GeneratorCoverData");
                connection.Execute("DROP TABLE IF EXISTS WrenchData");
                connection.Execute("DROP TABLE IF EXISTS GasData");
            }

            // Existing saves created before IsTriggered was added need this column.
            try { connection.Execute("ALTER TABLE SubtitleData ADD COLUMN IsTriggered INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            try { connection.Execute("ALTER TABLE FlashlightData ADD COLUMN IsOn INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            // ── NEW: ProgressionData table ──
            connection.CreateTable<ProgressionData>();
            // Added fields for generic dropped items. Older databases may already have them.
            try { connection.Execute("ALTER TABLE DroppedItemData ADD COLUMN IsHeld INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            try { connection.Execute("ALTER TABLE DroppedItemData ADD COLUMN IsDropped INTEGER NOT NULL DEFAULT 1"); }
            catch (System.Exception) { }

            isDatabaseReady = true;

            if (File.Exists(savePath))
            {
            }
        }
        catch (System.Exception e)
        {
            isDatabaseReady = false;
            Debug.LogError("[SaveSystem] Could not initialize the relational save database at " + savePath + ": " + e.Message);
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
        string fileName = difficulty == "Hard" ? "gameSave_Hard_v2.db" : "gameSave_v2.db";
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

    public void MarkGeneratorCoverRemoved(string coverId)
    {
        if (string.IsNullOrEmpty(coverId))
            return;


        EnsureDatabaseReady();
        if (!isDatabaseReady)
        {
            Debug.LogWarning("[GeneratorCover] Could not save '" + coverId + "': save database is unavailable.");
            return;
        }

        try
        {
            connection.InsertOrReplace(new GeneratorCoverData
            {
                CoverId = coverId,
                IsRemoved = true
            });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GeneratorCover] Could not save '" + coverId + "': " + e.Message);
        }
    }

    public bool IsGeneratorCoverRemoved(string coverId)
    {
        if (string.IsNullOrEmpty(coverId))
            return false;


        EnsureDatabaseReady();
        if (!isDatabaseReady)
            return false;

        try
        {
            GeneratorCoverData data = connection.Table<GeneratorCoverData>()
                .Where(item => item.CoverId == coverId)
                .FirstOrDefault();
            return data != null && data.IsRemoved;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
    public void ClearGeneratorState(string saveId)
    {
        if (string.IsNullOrEmpty(saveId)) return;
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;
        try
        {
            GeneratorCoverData data = connection.Table<GeneratorCoverData>().Where(item => item.CoverId == saveId).FirstOrDefault();
            if (data != null) connection.Delete(data);
        }
        catch (System.Exception) { }
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
                    IsUsed = true
                };
                connection.Insert(newBatteryData);
            }
            else
            {
                existing.IsUsed = true;
                existing.IsHeld = false;
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
                    IsPlaced = false
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
                    IsPlaced = true
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                existing.IsPlaced = true;
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
                    IsPlaced = false
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                existing.IsPlaced = false;
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
        // Write player state, inventory, items, doors, drawers, AI, and progression to the database.
        EnsureDatabaseReady();
        
        if (isSaving) return;

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
            if (IsHardModeDatabase())
            {
                SaveWrenchState();
                SaveGasState();
            }

            connection.DeleteAll<InventoryData>();
            if (Inventory.Instance != null)
            {
                List<GameObject> inventoryItemsToSave = Inventory.Instance.GetItems();
                int equippedInventoryIndex = Inventory.Instance.GetSelectedIndex();
                for (int index = 0; index < inventoryItemsToSave.Count; index++)
                {
                    GameObject item = inventoryItemsToSave[index];
                    if (item != null)
                    {
                        string cleanName = item.name.Replace("(Clone)", "");
                        
                        InventoryData invData = new InventoryData
                        {
                            ItemName = cleanName,
                            Quantity = 1,
                            IsEquipped = index == equippedInventoryIndex
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

                    var existing = existingBatteryData.FirstOrDefault(b => b.BatteryName == cleanName);

                    if (existing != null)
                    {
                        existing.RechargeAmount = battery.rechargeAmount;
                        existing.IsHeld = isHeld;
                        existing.IsUsed = false;
                        connection.Update(existing);
                    }
                    else
                    {
                        BatteryData batteryData = new BatteryData
                        {
                            BatteryName = cleanName,
                            RechargeAmount = battery.rechargeAmount,
                            IsHeld = isHeld,
                            IsUsed = false
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

                RitualItemData ritualData = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = isRevealed,
                    IsPlaced = isPlaced
                };
                connection.Insert(ritualData);
            }

            // ── SAVE FLASHLIGHT STATE ──
            try
            {
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
                            IsOn = flashlight.IsOn,
                            IsHeld = flashlight.gameObject.transform.parent != null &&
                                     flashlight.gameObject.transform.parent.CompareTag("Player")
                        };
                        connection.Insert(flashlightData);
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[SaveSystem] Could not save flashlight state: " + exception.Message);
            }

            // ── SAVE DOORS WITH ROTATION ──
            connection.DeleteAll<DoorData>();
            connection.DeleteAll<DrawerData>();
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

            // ── SAVE DRAWERS ──
            connection.DeleteAll<DrawerData>();
            foreach (DrawerInteraction drawer in Object.FindObjectsOfType<DrawerInteraction>())
            {
                if (drawer == null) continue;

                Vector3 localPosition = drawer.transform.localPosition;
                connection.Insert(new DrawerData
                {
                    DrawerId = GenerateDrawerId(drawer),
                    DrawerName = drawer.name,
                    IsOpen = drawer.IsOpen(),
                    LocalPosX = localPosition.x,
                    LocalPosY = localPosition.y,
                    LocalPosZ = localPosition.z
                });

                if (logDrawerSaveLoad)
                {
                    Debug.Log($"[Drawer Save] {drawer.name} | id={GenerateDrawerId(drawer)} | open={drawer.IsOpen()} | localPosition={localPosition}", drawer);
                }
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
            // Keep records already stored for picked-up or destroyed items.
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

            // Save map ownership separately because the physical map is not kept in inventory.
            MapSystem mapSystem = FindObjectOfType<MapSystem>();
            if (mapSystem != null)
            {
                connection.Insert(new GameStateData
                {
                    Key = "HasMap",
                    Value = mapSystem.HasMap ? "true" : "false"
                });
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
                bool isRitualItem = cleanName == "LargeCandle" ||
                                    cleanName == "LargeCandle (1)" ||
                                    cleanName == "Cross" ||
                                    cleanName == "Bible";

                // Gas has its own Chapter 2 save record. Keeping it out of the
                // generic drop table prevents the loader from making a second can.
                if (IsHardModeDatabase() && pickup != null && IsGas(pickup))
                {
                    continue;
                }

                // The Hard-mode wrench has its own table because it restores through
                // a dedicated path. Do not duplicate its transform in DroppedItemData.
                if (IsHardModeDatabase() && pickup != null && IsWrench(pickup))
                {
                    continue;
                }

                if (inventoryItemNames.Contains(cleanName))
                {
                    continue;
                }

                if (candle != null && !isRitualItem)
                {
                    continue;
                }

                if (key != null && usedKeyNames.Contains(cleanName))
                {
                    continue;
                }

                if (candle != null && cleanName.ToLower().Contains("candle") && !isRitualItem)
                {
                    continue;
                }

                if (key != null && key.wasDropped && !key.IsPickedUp)
                {
                    isDropped = true;
                }

                if (pickup != null && pickup.wasDropped && !pickup.isPickedUp)
                {
                    isDropped = true;
                }

                // Batteries keep their detailed charge data in BatteryData, and also
                // get a generic drop record so they are visible in DroppedItemData.
                if (battery != null && battery.wasDropped)
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

                if (pickup != null && !pickup.isPickedUp && !pickup.wasDropped && key == null && flashlight == null && battery == null && candle == null)
                {
                    continue;
                }

                if (isDropped)
                {
                    DroppedItemData droppedData = new DroppedItemData
                    {
                        ItemName = cleanName,
                        IsHeld = false,
                        IsDropped = true,
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

    private PickupItem FindScenePickupByName(string itemName)
    {
        foreach (PickupItem pickup in Resources.FindObjectsOfTypeAll<PickupItem>())
        {
            if (pickup == null || !pickup.enabled ||
                pickup.gameObject.scene != SceneManager.GetActiveScene() ||
                pickup.gameObject.hideFlags != HideFlags.None ||
                pickup.GetComponent<Key>() != null || pickup.GetComponent<FlashlightPickup>() != null ||
                pickup.GetComponent<BatteryPickup>() != null || pickup.GetComponent<CandleItem>() != null)
                continue;

            string cleanObjectName = pickup.name.Replace("(Clone)", "");
            if (pickup.itemName == itemName || cleanObjectName == itemName)
                return pickup;
        }
        return null;
    }
    private void SaveWrenchState()
    {
        connection.DeleteAll<WrenchData>();

        foreach (PickupItem wrench in Object.FindObjectsOfType<PickupItem>(true))
        {
            if (wrench == null || !IsWrench(wrench))
                continue;

            Transform item = wrench.transform;
            connection.Insert(new WrenchData
            {
                WrenchId = GenerateAIId(wrench),
                IsHeld = wrench.isHeld,
                IsDropped = wrench.wasDropped,
                PosX = item.position.x,
                PosY = item.position.y,
                PosZ = item.position.z,
                RotX = item.rotation.x,
                RotY = item.rotation.y,
                RotZ = item.rotation.z,
                RotW = item.rotation.w
            });
        }
    }

    private void RestoreWrenchState()
    {
        foreach (WrenchData savedWrench in connection.Table<WrenchData>().ToList())
        {
            foreach (PickupItem wrench in Resources.FindObjectsOfTypeAll<PickupItem>())
            {
                if (wrench == null || !wrench.gameObject.scene.IsValid() || GenerateAIId(wrench) != savedWrench.WrenchId)
                    continue;

                wrench.wasDropped = savedWrench.IsDropped;
                wrench.isHeld = savedWrench.IsHeld;

                if (savedWrench.IsDropped)
                {
                    wrench.transform.position = new Vector3(savedWrench.PosX, savedWrench.PosY, savedWrench.PosZ);
                    wrench.transform.rotation = new Quaternion(
                        savedWrench.RotX, savedWrench.RotY, savedWrench.RotZ, savedWrench.RotW);
                    wrench.ResetItem();
                    wrench.wasDropped = true;
                    wrench.isHeld = false;
                }
                break;
            }
        }
    }
    private static bool IsWrench(PickupItem pickup)
    {
        return pickup.itemName.ToLowerInvariant().Contains("wrench") ||
               pickup.name.ToLowerInvariant().Contains("wrench");
    }

    private static bool IsGas(PickupItem pickup)
    {
        return pickup != null && string.Equals(pickup.itemName, "Gas", System.StringComparison.OrdinalIgnoreCase);
    }

    // Preview models live in DontDestroyOnLoad and have their scripts disabled.
    // Only the enabled Gas object belonging to the active Chapter 2 scene may be saved or restored.
    private static bool IsActiveSceneGas(PickupItem pickup)
    {
        return IsGas(pickup) && pickup.enabled && pickup.gameObject.hideFlags == HideFlags.None &&
               pickup.gameObject.scene == SceneManager.GetActiveScene();
    }

    private void SaveGasState()
    {
        connection.DeleteAll<GasData>();

        PickupItem gas = Object.FindObjectsOfType<PickupItem>(true).FirstOrDefault(IsActiveSceneGas);
        if (gas == null)
        {
            return;
        }

        Transform item = gas.transform;
        connection.Insert(new GasData
        {
            GasId = GasRecordId, IsHeld = gas.isHeld, IsDropped = gas.wasDropped,
            PosX = item.position.x, PosY = item.position.y, PosZ = item.position.z,
            RotX = item.rotation.x, RotY = item.rotation.y, RotZ = item.rotation.z, RotW = item.rotation.w
        });
    }

    private void RestoreGasState()
    {
        GasData savedGas = connection.Table<GasData>().FirstOrDefault(g => g.GasId == GasRecordId) ??
                           connection.Table<GasData>().FirstOrDefault(g => g.GasId == "Chapter2Gas");
        if (savedGas == null)
        {
            return;
        }

        PickupItem gas = Resources.FindObjectsOfTypeAll<PickupItem>().FirstOrDefault(IsActiveSceneGas);
        if (gas == null)
        {
            return;
        }
        gas.wasDropped = savedGas.IsDropped;
        gas.isHeld = savedGas.IsHeld;
        if (!savedGas.IsDropped) return;

        gas.transform.position = new Vector3(savedGas.PosX, savedGas.PosY, savedGas.PosZ);
        gas.transform.rotation = new Quaternion(savedGas.RotX, savedGas.RotY, savedGas.RotZ, savedGas.RotW);
        gas.ResetItem();
        gas.wasDropped = true;
        gas.isHeld = false;
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
    private string GenerateDrawerId(DrawerInteraction drawer)
    {
        // Do not use sibling indexes here. Picking up or disabling any scene object
        // can change those indexes, which makes the same drawer look like a new one.
        Transform parent = drawer.transform.parent;
        if (parent == null)
        {
            Vector3 position = drawer.transform.position;
            return $"{SceneManager.GetActiveScene().name}:Drawer:{drawer.name}:{position.x:F2}:{position.y:F2}:{position.z:F2}";
        }

        // A drawer's parent is static furniture, so its name and world position
        // remain the same whether the drawer is open, closed, or items are picked up.
        Vector3 parentPosition = parent.position;
        return $"{SceneManager.GetActiveScene().name}:Drawer:{parent.name}:{parentPosition.x:F2}:{parentPosition.y:F2}:{parentPosition.z:F2}:{drawer.name}";
    }

    private string GenerateDoorId(DoorInteraction door)
    {
        Vector3 pos = door.transform.position;
        return $"{door.name}_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
    }

    public bool LoadGame()
    {
        // Restore the saved world after the active scene has finished loading.
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

            // Restore the map ability and remove the world pickup when the player
            // had already collected it in the saved game.
            GameStateData mapState = connection.Table<GameStateData>()
                .Where(state => state.Key == "HasMap").FirstOrDefault();
            bool hasMap = mapState != null && mapState.Value == "true";
            MapSystem mapSystem = FindObjectOfType<MapSystem>();
            if (mapSystem != null)
            {
                mapSystem.RestoreMapOwnership(hasMap);
                if (hasMap)
                {
                    foreach (MapPickup mapPickup in FindObjectsOfType<MapPickup>())
                    {
                        mapPickup.RestoreCollected();
                    }
                }
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
            if (IsHardModeDatabase())
            {
                RestoreWrenchState();
                RestoreGasState();
            }

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

            // ── RESTORE DRAWERS ──
            Dictionary<string, DrawerInteraction> drawerLookup = new Dictionary<string, DrawerInteraction>();
            foreach (DrawerInteraction drawer in Object.FindObjectsOfType<DrawerInteraction>())
            {
                if (drawer == null) continue;
                string drawerId = GenerateDrawerId(drawer);
                if (!drawerLookup.ContainsKey(drawerId))
                    drawerLookup.Add(drawerId, drawer);
            }

            foreach (DrawerData drawerData in connection.Table<DrawerData>().ToList())
            {
                DrawerInteraction drawer;
                if (!drawerLookup.TryGetValue(drawerData.DrawerId, out drawer))
                {
                    if (logDrawerSaveLoad)
                    {
                        Debug.LogWarning($"[Drawer Load] No scene drawer matches saved id={drawerData.DrawerId} ({drawerData.DrawerName}).");
                    }
                    continue;
                }

                Vector3 savedLocalPosition = new Vector3(
                    drawerData.LocalPosX,
                    drawerData.LocalPosY,
                    drawerData.LocalPosZ);
                drawer.RestoreSavedState(drawerData.IsOpen, savedLocalPosition);

                if (logDrawerSaveLoad)
                {
                    Debug.Log($"[Drawer Load] {drawer.name} | id={drawerData.DrawerId} | open={drawerData.IsOpen} | localPosition={savedLocalPosition}", drawer);
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
            // Gas uses the dedicated Hard-mode table, not a generic drop record.
            if (IsHardModeDatabase())
            {
                droppedItems.RemoveAll(item => string.Equals(item.ItemName, "Gas", System.StringComparison.OrdinalIgnoreCase));
            }
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

            // DroppedItemData owns a dropped battery's world position and rotation.
            // BatteryData remains responsible only for its charge and usage state.
            var droppedBatteryData = droppedItems.Where(item =>
                item.IsDropped && batteryDataList.Any(battery =>
                    battery.BatteryName == item.ItemName && !battery.IsUsed && !battery.IsHeld))
                .ToList();
            List<string> droppedBatteryNames = new List<string>();
            foreach (DroppedItemData droppedBattery in droppedBatteryData)
            {
                droppedBatteryNames.Add(droppedBattery.ItemName);
            }

            // ── Get ritual items data ──
            var revealedRitualItems = ritualItemDataList.Where(r => r.IsRevealed).ToList();
            List<string> revealedItemNames = new List<string>();
            foreach (RitualItemData itemData in revealedRitualItems)
            {
                revealedItemNames.Add(itemData.ItemName);
            }

            // DroppedItemData owns the dropped transform for ritual items.
            var droppedRitualItems = droppedItems.Where(item => item.IsDropped &&
                (item.ItemName == "LargeCandle" ||
                 item.ItemName == "LargeCandle (1)" ||
                 item.ItemName == "Cross" ||
                 item.ItemName == "Bible"))
                .ToList();
            List<string> droppedRitualItemNames = new List<string>();
            foreach (DroppedItemData itemData in droppedRitualItems)
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
                
                var savedBattery = droppedBatteryData.FirstOrDefault(b => b.ItemName == cleanName);
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

                    // Hard Mode special pickups restore their original scene object.
                    // Do not destroy them before their saved dropped state is applied.
                    if (pickup != null && pickup.wasDropped && !pickup.isPickedUp &&
                        IsHardModeDatabase() && IsWrench(pickup))
                    {
                        isDroppedVersion = true;
                    }
                    else if (key != null && key.wasDropped && !key.IsPickedUp)
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
                var savedBattery = droppedBatteryData.FirstOrDefault(b => b.ItemName == cleanName);
                var savedBatteryState = batteryDataList.FirstOrDefault(b => b.BatteryName == cleanName);
                
                if (savedBattery != null && savedBatteryState != null)
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
                                batteryComp.rechargeAmount = savedBatteryState.RechargeAmount;
                                batteryComp.wasDropped = true;
                                batteryComp.isHeld = false;
                                batteryComp.wasUsed = false;
                            }

                            if (Inventory.Instance != null)
                            {
                                Inventory.Instance.RestoreDropLight(spawnedBattery);
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

                            if (Inventory.Instance != null)
                            {
                                Inventory.Instance.RestoreDropLight(spawnedItem);
                            }
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

                int savedEquippedIndex = -1;
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

                        FlashlightPickup flashlight = itemObject.GetComponent<FlashlightPickup>();
                        if (flashlight != null)
                        {
                            var savedFlashlight = flashlightData.FirstOrDefault(f => f.FlashlightName == invData.ItemName);
                            if (savedFlashlight != null)
                            {
                                flashlight.batteryLife = savedFlashlight.BatteryLife;
                                flashlight.SetBattery(savedFlashlight.CurrentBattery);
                            }
                            flashlight.SetHeld(true);
                            flashlight.SetLightOn(savedFlashlight != null && savedFlashlight.IsOn);
                            flashlight.ResetDroppedState();
                        }

                        if (Inventory.Instance.AddItem(itemObject) && invData.IsEquipped)
                        {
                            savedEquippedIndex = Inventory.Instance.GetItems().Count - 1;
                        }
                    }
                    else
                    {
                    }
                }

                if (savedEquippedIndex >= 0)
                {
                    Inventory.Instance.SelectItem(savedEquippedIndex);
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

                Vector3 position = new Vector3(droppedData.PosX, droppedData.PosY, droppedData.PosZ);
                Quaternion rotation = new Quaternion(droppedData.RotX, droppedData.RotY, droppedData.RotZ, droppedData.RotW);

                PickupItem scenePickup = FindScenePickupByName(droppedData.ItemName);
                if (scenePickup != null)
                {
                    scenePickup.transform.position = position;
                    scenePickup.transform.rotation = rotation;
                    scenePickup.ResetItem();
                    scenePickup.wasDropped = true;
                    scenePickup.isHeld = false;
                    if (Inventory.Instance != null)
                    {
                        Inventory.Instance.RestoreDropLight(scenePickup.gameObject);
                    }
                    continue;
                }

                if (PrefabManager.Instance != null)
                {
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

                        if (Inventory.Instance != null)
                        {
                            Inventory.Instance.RestoreDropLight(spawnedItem);
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

    public void ClearNewGameWorldState()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady)
            return;

        try
        {
            connection.DeleteAll<AIPositionData>();
            connection.DeleteAll<DroppedItemData>();

            if (IsHardModeDatabase())
            {
                connection.DeleteAll<WrenchData>();
                connection.DeleteAll<GeneratorCoverData>();
                connection.DeleteAll<GasData>();
            }
            connection.DeleteAll<GeneratorCoverData>();
        }
        catch (System.Exception)
        {
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
        // Delete the current save database and clear memory state for a new game.
        if (!isDatabaseReady) return;

        try
        {
            connection.DeleteAll<PlayerData>();
            connection.DeleteAll<AIPositionData>();
            if (IsHardModeDatabase())
            {
                connection.DeleteAll<WrenchData>();
                connection.DeleteAll<GeneratorCoverData>();
                connection.DeleteAll<GasData>();
            }

            connection.DeleteAll<InventoryData>();
            connection.DeleteAll<DoorData>();
            connection.DeleteAll<DrawerData>();
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
