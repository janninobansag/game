// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
public partial class SaveSystem : MonoBehaviour
{
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
            SavePathGuideProgress();
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

    private void SavePathGuideProgress()
    {
        foreach (PathGuideIndicator guide in Resources.FindObjectsOfTypeAll<PathGuideIndicator>())
        {
            if (guide == null || guide.gameObject.scene != SceneManager.GetActiveScene() ||
                guide.gameObject.hideFlags != HideFlags.None)
                continue;

            connection.Insert(new GameStateData
            {
                Key = guide.SaveKey,
                Value = guide.CurrentLocationIndex.ToString()
            });
        }
    }

    private void RestorePathGuideProgress()
    {
        foreach (PathGuideIndicator guide in Resources.FindObjectsOfTypeAll<PathGuideIndicator>())
        {
            if (guide == null || guide.gameObject.scene != SceneManager.GetActiveScene() ||
                guide.gameObject.hideFlags != HideFlags.None)
                continue;

            GameStateData savedProgress = connection.Table<GameStateData>()
                .Where(state => state.Key == guide.SaveKey)
                .FirstOrDefault();

            if (savedProgress != null && int.TryParse(savedProgress.Value, out int savedIndex))
                guide.RestoreProgress(savedIndex);
        }
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
}
