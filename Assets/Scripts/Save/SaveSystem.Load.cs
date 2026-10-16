// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public partial class SaveSystem : MonoBehaviour
{
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
            RestoreTikbalangState();
            RestoreGeneratorLightState();
            RestoreGeneratorRunState();
            RestorePathGuideProgress();
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
                    continue;
                }

                Vector3 savedLocalPosition = new Vector3(
                    drawerData.LocalPosX,
                    drawerData.LocalPosY,
                    drawerData.LocalPosZ);
                drawer.RestoreSavedState(drawerData.IsOpen, savedLocalPosition);

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
                        IsHardModeDatabase() && (IsWrench(pickup) || IsGas(pickup)))
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

                // A Gas record from before it was poured must not recreate Gas
                // while the generator is fueled or its lights are still running.
                if (IsHardModeDatabase() &&
                    string.Equals(droppedData.ItemName, "Gas", System.StringComparison.OrdinalIgnoreCase) &&
                    IsGeneratorActiveOrFueled())
                {
                    continue;
                }

                Vector3 position = new Vector3(droppedData.PosX, droppedData.PosY, droppedData.PosZ);
                Quaternion rotation = new Quaternion(droppedData.RotX, droppedData.RotY, droppedData.RotZ, droppedData.RotW);

                PickupItem scenePickup = FindScenePickupByName(droppedData.ItemName);
                if (scenePickup != null)
                {
                    if (IsHardModeDatabase() && (IsWrench(scenePickup) || IsGas(scenePickup)))
                        scenePickup.transform.SetParent(null, true);

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

            // Reconcile after generic item restoration. The original Gas may
            // remain while lights are on until the player pours it for the first time.
            GasData savedGas = IsHardModeDatabase()
                ? connection.Table<GasData>().FirstOrDefault(g => g.GasId == GasRecordId) ??
                  connection.Table<GasData>().FirstOrDefault(g => g.GasId == "Chapter2Gas")
                : null;
            bool savedGasWasHeldOrDropped = (savedGas != null && (savedGas.IsHeld || savedGas.IsDropped)) ||
                inventoryItemNames.Any(name => name != null &&
                    string.Equals(name.Replace("(Clone)", "").Trim(), "Gas", System.StringComparison.OrdinalIgnoreCase)) ||
                droppedItemNames.Any(name => name != null &&
                    string.Equals(name.Replace("(Clone)", "").Trim(), "Gas", System.StringComparison.OrdinalIgnoreCase));
            foreach (GeneratorKeySlot generator in Object.FindObjectsOfType<GeneratorKeySlot>())
                generator.ReconcileGasWithLightState(savedGasWasHeldOrDropped);
            return true;
        }
        catch (System.Exception e)
        {
            return false;
        }
    }
}
