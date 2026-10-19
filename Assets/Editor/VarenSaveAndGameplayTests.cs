#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SQLite4Unity3d;
using UnityEngine;
using UnityEngine.UI;

public class VarenSaveAndGameplayTests
{
    [Test]
    public void SaveSchema_UsesVersionEight()
    {
        Assert.That(RelationalSaveSchema.SchemaVersion, Is.EqualTo(8));
    }

    [Test]
    public void AIEncounterGate_OnlyFirstAICanControlThePlayer()
    {
        GameObject firstAI = new GameObject("First AI");
        GameObject secondAI = new GameObject("Second AI");

        try
        {
            Assert.That(AIEncounterGate.TryBegin(firstAI), Is.True);
            Assert.That(AIEncounterGate.IsBlocked(secondAI), Is.True);
            Assert.That(AIEncounterGate.TryBegin(secondAI), Is.False);

            AIEncounterGate.End(secondAI);
            Assert.That(AIEncounterGate.IsBlocked(secondAI), Is.True);

            AIEncounterGate.End(firstAI);
            Assert.That(AIEncounterGate.IsBlocked(secondAI), Is.False);
            Assert.That(AIEncounterGate.TryBegin(secondAI), Is.True);
        }
        finally
        {
            AIEncounterGate.End(firstAI);
            AIEncounterGate.End(secondAI);
            UnityEngine.Object.DestroyImmediate(firstAI);
            UnityEngine.Object.DestroyImmediate(secondAI);
        }
    }

    [Test]
    public void AIEncounterPause_RestoresOtherAIAnimationAfterRelease()
    {
        GameObject owner = new GameObject("Encounter Owner");
        GameObject otherAI = new GameObject("Paused AI");
        Animator animator = otherAI.AddComponent<Animator>();
        animator.speed = 1.25f;
        AIEncounterPause pause = new AIEncounterPause();

        try
        {
            Assert.That(AIEncounterGate.TryBegin(owner), Is.True);
            Assert.That(pause.Update(otherAI, null, animator, null), Is.True);
            Assert.That(animator.speed, Is.EqualTo(0f));

            AIEncounterGate.End(owner);
            Assert.That(pause.Update(otherAI, null, animator, null), Is.False);
            Assert.That(animator.speed, Is.EqualTo(1.25f));
        }
        finally
        {
            pause.Restore(null, animator, null);
            AIEncounterGate.End(owner);
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(otherAI);
        }
    }

    [Test]
    public void WhiteLadyQnA_RestoresSharedChoicesAfterTikbalangAnswer()
    {
        GameObject tikbalangObject = new GameObject("Tikbalang Encounter Test");
        GameObject whiteLadyObject = new GameObject("White Lady Q&A Test");
        GameObject panel = new GameObject("QnAPanel", typeof(RectTransform), typeof(CanvasGroup));
        whiteLadyObject.SetActive(false);

        try
        {
            MonsterAI_New whiteLady = whiteLadyObject.AddComponent<MonsterAI_New>();
            whiteLady.qnaPanel = panel;
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            SetPrivateField(whiteLady, "qnaCanvasGroup", canvasGroup);

            string[] optionNames = { "OptionA", "OptionB", "OptionC", "OptionD" };
            Button[] buttons = new Button[optionNames.Length];
            for (int i = 0; i < optionNames.Length; i++)
            {
                GameObject option = new GameObject(optionNames[i], typeof(RectTransform), typeof(Button));
                option.transform.SetParent(panel.transform, false);
                buttons[i] = option.GetComponent<Button>();
                buttons[i].interactable = false;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Assert.That(AIEncounterGate.TryBegin(tikbalangObject), Is.True);
            Assert.That(AIEncounterGate.TryBegin(whiteLady), Is.False);
            AIEncounterGate.End(tikbalangObject);
            Assert.That(AIEncounterGate.TryBegin(whiteLady), Is.True);

            InvokePrivate(whiteLady, "WireQnAButtons");
            InvokePrivate(whiteLady, "RestoreQnAInput");

            Assert.That(buttons.All(button => button.interactable), Is.True);
            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);

            SetPrivateField(whiteLady, "isWaitingForAnswer", true);
            buttons[0].onClick.Invoke();
            Assert.That(GetPrivateField<bool>(whiteLady, "hasAnswered"), Is.True);
            Assert.That(GetPrivateField<bool>(whiteLady, "isWaitingForAnswer"), Is.False);
        }
        finally
        {
            AIEncounterGate.End(tikbalangObject);
            AIEncounterGate.End(whiteLadyObject.GetComponent<MonsterAI_New>());
            UnityEngine.Object.DestroyImmediate(tikbalangObject);
            UnityEngine.Object.DestroyImmediate(whiteLadyObject);
            UnityEngine.Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void OneShotEvents_RestoreNoteAndWhiteLadyWithoutReplaying()
    {
        GameObject firstNoteObject = new GameObject("Note A");
        GameObject secondNoteObject = new GameObject("Note B");
        GameObject audioObject = new GameObject("Note Audio");
        GameObject triggerObject = new GameObject("White Lady Trigger", typeof(BoxCollider));
        GameObject whiteLadyObject = new GameObject("White Lady Jumpscare");

        try
        {
            Note firstNote = firstNoteObject.AddComponent<Note>();
            Note secondNote = secondNoteObject.AddComponent<Note>();
            firstNote.noteTitle = "Same Title";
            secondNote.noteTitle = "Same Title";
            AudioTrigger audioTrigger = audioObject.AddComponent<AudioTrigger>();
            firstNote.onCloseAudio = audioTrigger;

            Assert.That(firstNote.SaveStateKey, Is.Not.EqualTo(secondNote.SaveStateKey));
            firstNote.RestoreReadState(true);
            Assert.That(firstNote.HasBeenRead(), Is.True);
            Assert.That(audioTrigger.HasPlayed, Is.True);

            WhiteLadyTeleportTrigger trigger = triggerObject.AddComponent<WhiteLadyTeleportTrigger>();
            trigger.whiteLady = whiteLadyObject.transform;
            trigger.RestoreTriggeredState(true);
            Assert.That(trigger.HasTriggered, Is.True);
            Assert.That(whiteLadyObject.activeSelf, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstNoteObject);
            UnityEngine.Object.DestroyImmediate(secondNoteObject);
            UnityEngine.Object.DestroyImmediate(audioObject);
            UnityEngine.Object.DestroyImmediate(triggerObject);
            UnityEngine.Object.DestroyImmediate(whiteLadyObject);
        }
    }

    [Test]
    public void WhiteLadyOneShot_LoadRestoresAndNewGameClearsTrigger()
    {
        string databasePath = Path.Combine(Path.GetTempPath(),
            "VarenOneShotTest_" + Guid.NewGuid().ToString("N") + ".db");
        SaveSystem originalInstance = SaveSystem.Instance;
        GameObject saveObject = new GameObject("One Shot Save Test");
        GameObject triggerObject = new GameObject("White Lady Save Trigger", typeof(BoxCollider));
        GameObject whiteLadyObject = new GameObject("White Lady Save Jumpscare");
        SQLiteConnection database = null;

        try
        {
            saveObject.SetActive(false);
            SaveSystem saveSystem = saveObject.AddComponent<SaveSystem>();
            saveSystem.autoSaveOnQuit = false;
            database = CreateTemporarySaveDatabase(saveSystem, databasePath, false);
            SaveSystem.Instance = saveSystem;

            WhiteLadyTeleportTrigger trigger = triggerObject.AddComponent<WhiteLadyTeleportTrigger>();
            trigger.whiteLady = whiteLadyObject.transform;
            saveSystem.MarkOneShotEventCompleted(trigger.SaveStateKey);

            Assert.That(database.Table<GameStateData>().ToList()
                .Any(state => state.Key == trigger.SaveStateKey && state.Value == "true"), Is.True);
            InvokePrivate(saveSystem, "RestoreOneShotEventStates");
            Assert.That(trigger.HasTriggered, Is.True);
            Assert.That(whiteLadyObject.activeSelf, Is.False);

            saveSystem.ClearOneShotEventStates();
            trigger.RestoreTriggeredState(false);
            whiteLadyObject.SetActive(true);
            InvokePrivate(saveSystem, "RestoreOneShotEventStates");
            Assert.That(trigger.HasTriggered, Is.False);
            Assert.That(whiteLadyObject.activeSelf, Is.True);
        }
        finally
        {
            SaveSystem.Instance = originalInstance;
            UnityEngine.Object.DestroyImmediate(saveObject);
            if (database != null) database.Close();
            if (File.Exists(databasePath)) File.Delete(databasePath);
            UnityEngine.Object.DestroyImmediate(triggerObject);
            UnityEngine.Object.DestroyImmediate(whiteLadyObject);
        }
    }

    [Test]
    public void ItemSpecificModels_DoNotDuplicateDroppedTransforms()
    {
        AssertMissingProperties(typeof(FlashlightData), "WasDropped", "PosX", "PosY", "PosZ", "RotX", "RotY", "RotZ", "RotW");
        AssertMissingProperties(typeof(BatteryData), "IsDropped", "PosX", "PosY", "PosZ", "RotX", "RotY", "RotZ", "RotW");
        AssertMissingProperties(typeof(RitualItemData), "IsDropped", "PosX", "PosY", "PosZ", "RotX", "RotY", "RotZ", "RotW");
    }

    [Test]
    public void DisabledRandomKeySpawn_LeavesKeyAtItsScenePosition()
    {
        GameObject keyObject = new GameObject("Test Key");
        keyObject.SetActive(false);
        RandomKeySpawn spawn = keyObject.AddComponent<RandomKeySpawn>();
        Vector3 expectedPosition = new Vector3(10f, 2f, -4f);
        keyObject.transform.position = expectedPosition;
        spawn.disableRandomSpawn = true;

        InvokePrivate(spawn, "Awake");

        Assert.That(keyObject.transform.position, Is.EqualTo(expectedPosition));
        UnityEngine.Object.DestroyImmediate(keyObject);
    }

    [Test]
    public void DisabledRandomWrenchSpawn_LeavesWrenchAtItsScenePosition()
    {
        GameObject wrenchObject = new GameObject("Test Wrench");
        wrenchObject.SetActive(false);
        RandomWrenchSpawn spawn = wrenchObject.AddComponent<RandomWrenchSpawn>();
        Vector3 expectedPosition = new Vector3(-7f, 1f, 12f);
        wrenchObject.transform.position = expectedPosition;
        spawn.disableRandomSpawn = true;

        InvokePrivate(spawn, "Awake");

        Assert.That(wrenchObject.transform.position, Is.EqualTo(expectedPosition));
        UnityEngine.Object.DestroyImmediate(wrenchObject);
    }

    [Test]
    public void RespawnedGas_MarksItsWorldPositionForSaving()
    {
        const string markerName = "Respawned Gas Test Marker";
        GameObject slotObject = new GameObject("Test Generator Slot", typeof(BoxCollider));
        GameObject gasTemplate = new GameObject("Test Gas Template");
        GameObject spawnPoint = new GameObject("Test Gas Spawn Point");

        try
        {
            slotObject.SetActive(false);
            gasTemplate.SetActive(false);
            GeneratorKeySlot slot = slotObject.AddComponent<GeneratorKeySlot>();
            PickupItem templatePickup = gasTemplate.AddComponent<PickupItem>();
            templatePickup.itemName = "Gas";
            new GameObject(markerName).transform.SetParent(gasTemplate.transform);

            Vector3 expectedPosition = new Vector3(7f, 1f, -3f);
            spawnPoint.transform.position = expectedPosition;
            slot.gasSpawnPoints = new[] { spawnPoint.transform };
            slot.gasSpawnHeightOffset = 0f;
            SetPrivateField(slot, "gasRespawnTemplate", gasTemplate);

            InvokePrivate(slot, "SpawnGasAtRandomPoint");

            PickupItem spawnedGas = Resources.FindObjectsOfTypeAll<PickupItem>()
                .FirstOrDefault(item => item != null && item.gameObject.activeInHierarchy &&
                    item.transform.Find(markerName) != null);
            Assert.That(spawnedGas, Is.Not.Null);
            Assert.That(spawnedGas.transform.position, Is.EqualTo(expectedPosition));
            Assert.That(spawnedGas.wasDropped, Is.True);
            Assert.That(spawnedGas.isPickedUp, Is.False);
            Assert.That(spawnedGas.isHeld, Is.False);
        }
        finally
        {
            foreach (PickupItem item in Resources.FindObjectsOfTypeAll<PickupItem>())
            {
                if (item != null && item.gameObject != gasTemplate &&
                    item.transform.Find(markerName) != null)
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(slotObject);
            UnityEngine.Object.DestroyImmediate(gasTemplate);
            UnityEngine.Object.DestroyImmediate(spawnPoint);
        }
    }

    [Test]
    public void IncorrectVaultPin_ShowsGuideBookHint()
    {
        GameObject vaultObject = new GameObject("Test Vault");
        VaultDoorInteraction vault = vaultObject.AddComponent<VaultDoorInteraction>();
        SetPrivateField(vault, "correctPin", "1234");
        SetPrivateField(vault, "enteredPin", "0000");

        InvokePrivate(vault, "SubmitPin");

        Assert.That(GetPrivateField<string>(vault, "pinFeedback"),
            Is.EqualTo("Incorrect PIN. Look for the PIN in the Guide Book in the bedroom."));
        Assert.That(GetPrivateField<float>(vault, "pinFeedbackTimer"), Is.EqualTo(3f));
        UnityEngine.Object.DestroyImmediate(vaultObject);
    }

    [Test]
    public void Note_Localization_UsesSelectedLanguageAndFallsBackToEnglish()
    {
        bool hadSettings = SettingsDatabase.TryLoad(out SettingsData originalSettings);
        GameObject noteObject = new GameObject("Localized Note Test");
        Note note = noteObject.AddComponent<Note>();
        note.noteTitle = "English Title";
        note.noteContent = "English Content";
        note.koreanTitle = "한국어 제목";
        note.koreanContent = "한국어 내용";
        note.tagalogTitle = "Tagalog na Pamagat";
        note.tagalogContent = "Tagalog na Nilalaman";

        try
        {
            SaveLanguage(GameLanguage.Korean);
            Assert.That(note.GetLocalizedTitle(), Is.EqualTo(note.koreanTitle));
            Assert.That(note.GetLocalizedContent(), Is.EqualTo(note.koreanContent));

            SaveLanguage(GameLanguage.Tagalog);
            Assert.That(note.GetLocalizedTitle(), Is.EqualTo(note.tagalogTitle));
            Assert.That(note.GetLocalizedContent(), Is.EqualTo(note.tagalogContent));

            note.tagalogContent = string.Empty;
            Assert.That(note.GetLocalizedContent(), Is.EqualTo(note.noteContent));
        }
        finally
        {
            RestoreSettings(originalSettings, hadSettings);
            UnityEngine.Object.DestroyImmediate(noteObject);
        }
    }

    [Test]
    public void SaveSystem_ReinitializeDatabase_SelectsCurrentDifficultySaveFile()
    {
        string originalDifficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        bool hadDifficulty = PlayerPrefs.HasKey("GameDifficulty");
        SaveSystem originalInstance = SaveSystem.Instance;
        GameObject saveObject = new GameObject("Save System Test");
        SaveSystem saveSystem = saveObject.AddComponent<SaveSystem>();

        try
        {
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            saveSystem.ReinitializeDatabase();
            Assert.That(Path.GetFileName(saveSystem.GetSavePath()), Is.EqualTo("gameSave_Hard_v2.db"));

            PlayerPrefs.SetString("GameDifficulty", "Normal");
            saveSystem.ReinitializeDatabase();
            Assert.That(Path.GetFileName(saveSystem.GetSavePath()), Is.EqualTo("gameSave_v2.db"));
        }
        finally
        {
            if (hadDifficulty) PlayerPrefs.SetString("GameDifficulty", originalDifficulty);
            else PlayerPrefs.DeleteKey("GameDifficulty");
            PlayerPrefs.Save();
            SaveSystem.Instance = originalInstance;
            UnityEngine.Object.DestroyImmediate(saveObject);
        }
    }

    [Test]
    public void Inventory_AddItem_RejectsNullAndItemsBeyondCapacity()
    {
        Inventory originalInventory = Inventory.Instance;
        GameObject inventoryObject = new GameObject("Inventory Capacity Test");
        inventoryObject.SetActive(false);
        Inventory inventory = inventoryObject.AddComponent<Inventory>();
        GameObject firstItem = new GameObject("First Item(Clone)");
        GameObject secondItem = new GameObject("Second Item");
        PickupItem firstPickup = firstItem.AddComponent<PickupItem>();

        try
        {
            Inventory.Instance = inventory;
            inventory.maxCapacity = 1;

            Assert.That(inventory.AddItem(null), Is.False);
            Assert.That(inventory.GetCount(), Is.Zero);
            Assert.That(inventory.AddItem(firstItem), Is.True);
            Assert.That(firstItem.name, Is.EqualTo("First Item"));
            Assert.That(firstPickup.isPickedUp, Is.True);
            Assert.That(firstPickup.isHeld, Is.True);
            Assert.That(inventory.IsFull(), Is.True);

            Assert.That(inventory.AddItem(secondItem), Is.False);
            Assert.That(inventory.GetCount(), Is.EqualTo(1));
            Assert.That(inventory.GetSelectedIndex(), Is.EqualTo(0));
        }
        finally
        {
            Inventory.Instance = originalInventory;
            UnityEngine.Object.DestroyImmediate(inventoryObject);
            UnityEngine.Object.DestroyImmediate(firstItem);
            UnityEngine.Object.DestroyImmediate(secondItem);
        }
    }

    [Test]
    public void Inventory_RemoveWithoutDestroy_UpdatesSelectionAndKeepsItemAlive()
    {
        Inventory originalInventory = Inventory.Instance;
        GameObject inventoryObject = new GameObject("Inventory Removal Test");
        inventoryObject.SetActive(false);
        Inventory inventory = inventoryObject.AddComponent<Inventory>();
        GameObject firstItem = new GameObject("First Item");
        GameObject secondItem = new GameObject("Second Item");

        try
        {
            Inventory.Instance = inventory;
            inventory.maxCapacity = 2;
            Assert.That(inventory.AddItem(firstItem), Is.True);
            Assert.That(inventory.AddItem(secondItem), Is.True);
            Assert.That(inventory.GetSelectedIndex(), Is.EqualTo(1));

            inventory.RemoveWithoutDestroy(secondItem);

            Assert.That(inventory.GetCount(), Is.EqualTo(1));
            Assert.That(inventory.GetSelectedIndex(), Is.Zero);
            Assert.That(secondItem != null, Is.True);

            inventory.RemoveWithoutDestroy(firstItem);
            Assert.That(inventory.GetCount(), Is.Zero);
            Assert.That(inventory.GetSelectedIndex(), Is.EqualTo(-1));
        }
        finally
        {
            Inventory.Instance = originalInventory;
            UnityEngine.Object.DestroyImmediate(inventoryObject);
            UnityEngine.Object.DestroyImmediate(firstItem);
            UnityEngine.Object.DestroyImmediate(secondItem);
        }
    }

    [Test]
    public void FlashlightPickup_BatteryOperations_StayWithinBatteryLife()
    {
        GameObject flashlightObject = new GameObject("Flashlight Battery Test");
        flashlightObject.SetActive(false);
        FlashlightPickup flashlight = flashlightObject.AddComponent<FlashlightPickup>();

        try
        {
            flashlight.batteryLife = 100f;
            flashlight.SetBattery(125f);
            Assert.That(flashlight.GetBatteryPercent(), Is.EqualTo(1f));
            Assert.That(flashlight.IsFullBattery(), Is.True);

            flashlight.SetBattery(-10f);
            Assert.That(flashlight.GetBatteryPercent(), Is.Zero);
            Assert.That(flashlight.IsFullBattery(), Is.False);

            flashlight.SetBattery(80f);
            flashlight.Recharge(50f);
            Assert.That(flashlight.GetBatteryPercent(), Is.EqualTo(1f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(flashlightObject);
        }
    }

    [Test]
    public void PlayerHealth_RestoreHealth_ClampsValuesAndSupportsLegacySave()
    {
        GameObject playerObject = new GameObject("Player Health Restore Test");
        playerObject.SetActive(false);
        PlayerHealth health = playerObject.AddComponent<PlayerHealth>();

        try
        {
            health.RestoreHealth(150f, 100f);
            Assert.That(health.currentHealth, Is.EqualTo(100f));
            Assert.That(health.maxHealth, Is.EqualTo(100f));
            Assert.That(health.isDead, Is.False);

            health.RestoreHealth(-5f, 80f);
            Assert.That(health.currentHealth, Is.Zero);
            Assert.That(health.maxHealth, Is.EqualTo(80f));

            health.maxHealth = 120f;
            health.RestoreHealth(0f, 0f);
            Assert.That(health.currentHealth, Is.EqualTo(120f));
            Assert.That(health.maxHealth, Is.EqualTo(120f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void StaminaController_RestoreStamina_ClampsSavedValue()
    {
        GameObject playerObject = new GameObject("Stamina Restore Test");
        playerObject.SetActive(false);
        StaminaController stamina = playerObject.AddComponent<StaminaController>();
        GameObject sliderObject = new GameObject("Test Stamina Slider", typeof(RectTransform), typeof(Slider));
        Slider slider = sliderObject.GetComponent<Slider>();

        try
        {
            stamina.maxStamina = 100f;
            stamina.staminaBar = slider;

            stamina.RestoreStamina(-5f);
            Assert.That(stamina.CurrentStamina, Is.Zero);

            stamina.RestoreStamina(150f);
            Assert.That(stamina.CurrentStamina, Is.EqualTo(100f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(sliderObject);
        }
    }

    [Test]
    public void DoorInteraction_UnlockWithoutOpening_UnlocksLinkedDoor()
    {
        GameObject firstDoorObject = new GameObject("First Door");
        GameObject linkedDoorObject = new GameObject("Linked Door");
        firstDoorObject.SetActive(false);
        linkedDoorObject.SetActive(false);
        DoorInteraction firstDoor = firstDoorObject.AddComponent<DoorInteraction>();
        DoorInteraction linkedDoor = linkedDoorObject.AddComponent<DoorInteraction>();
        firstDoor.isLocked = true;
        linkedDoor.isLocked = true;
        firstDoor.linkedDoor = linkedDoor;

        try
        {
            firstDoor.Unlock(false);

            Assert.That(firstDoor.IsLocked(), Is.False);
            Assert.That(linkedDoor.IsLocked(), Is.False);
            Assert.That(firstDoor.IsOpen(), Is.False);
            Assert.That(linkedDoor.IsOpen(), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstDoorObject);
            UnityEngine.Object.DestroyImmediate(linkedDoorObject);
        }
    }

    [Test]
    public void VaultDoorInteraction_MatchesKeyAndOnlyUnlocksKeyStageOnce()
    {
        GameObject vaultObject = new GameObject("Vault Key Test");
        vaultObject.SetActive(false);
        VaultDoorInteraction vault = vaultObject.AddComponent<VaultDoorInteraction>();
        SetPrivateField(vault, "requiredKeyTarget", "vault door");

        try
        {
            Assert.That(vault.MatchesKey("VAULT DOOR"), Is.True);
            Assert.That(vault.MatchesKey("bedroom door"), Is.False);
            Assert.That(vault.TryUnlockWithKey(), Is.True);
            Assert.That(vault.NeedsKey, Is.False);
            Assert.That(vault.TryUnlockWithKey(), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(vaultObject);
        }
    }

    [Test]
    public void ObjectiveTriggerActivator_MatchesItemIgnoringCaseAndActivatesOnce()
    {
        GameObject activatorObject = new GameObject("Objective Activator Test");
        GameObject targetObject = new GameObject("Objective Target Test");
        activatorObject.SetActive(false);
        targetObject.SetActive(false);
        ObjectiveTriggerActivator activator = activatorObject.AddComponent<ObjectiveTriggerActivator>();
        activator.itemName = "Generator Key";
        activator.objectiveTriggerToActivate = targetObject;

        try
        {
            Assert.That(activator.MatchesItem("generator key"), Is.True);
            Assert.That(activator.MatchesItem("House Key"), Is.False);

            activator.ActivateObjectiveTrigger();
            activator.ActivateObjectiveTrigger();

            Assert.That(targetObject.activeSelf, Is.True);
            Assert.That(activator.enabled, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(activatorObject);
            UnityEngine.Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void ObjectiveTrigger_OneShotDoesNotRestartItsDisplayTimer()
    {
        string originalDifficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        bool hadDifficulty = PlayerPrefs.HasKey("GameDifficulty");
        string testId = "UnitTestObjective_" + Guid.NewGuid().ToString("N");
        string stateKey = "ObjectiveTrigger_Normal_" + testId;
        GameObject objectiveObject = new GameObject("Objective One Shot Test");
        ObjectiveTrigger objective = objectiveObject.AddComponent<ObjectiveTrigger>();

        try
        {
            PlayerPrefs.SetString("GameDifficulty", "Normal");
            objective.saveId = testId;
            objective.triggerOnce = true;
            objective.displayDuration = 4f;
            objective.TriggerObjective();

            Assert.That(GetPrivateField<bool>(objective, "hasTriggered"), Is.True);
            Assert.That(PlayerPrefs.GetInt(stateKey, 0), Is.EqualTo(1));

            SetPrivateField(objective, "displayTimer", 1.25f);
            objective.displayDuration = 8f;
            objective.TriggerObjective();

            Assert.That(GetPrivateField<float>(objective, "displayTimer"), Is.EqualTo(1.25f));
        }
        finally
        {
            PlayerPrefs.DeleteKey(stateKey);
            if (hadDifficulty) PlayerPrefs.SetString("GameDifficulty", originalDifficulty);
            else PlayerPrefs.DeleteKey("GameDifficulty");
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(objectiveObject);
        }
    }

    [Test]
    public void MapSystem_RestoreMapOwnership_RespectsSavedAndDefaultOwnership()
    {
        GameObject mapObject = new GameObject("Map Ownership Test");
        mapObject.SetActive(false);
        MapSystem map = mapObject.AddComponent<MapSystem>();

        try
        {
            map.mapStartsUnlocked = false;
            map.RestoreMapOwnership(false);
            Assert.That(map.HasMap, Is.False);

            map.RestoreMapOwnership(true);
            Assert.That(map.HasMap, Is.True);

            map.mapStartsUnlocked = true;
            map.RestoreMapOwnership(false);
            Assert.That(map.HasMap, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mapObject);
        }
    }

    [Test]
    public void ProgressionSystem_SetProgress_ClampsToConfiguredRange()
    {
        string saveKey = "VarenProgressionTest_" + Guid.NewGuid().ToString("N");
        int originalValue = PlayerPrefs.GetInt(saveKey, 0);
        bool hadValue = PlayerPrefs.HasKey(saveKey);
        ProgressionSystem originalInstance = ProgressionSystem.Instance;
        GameObject progressionObject = new GameObject("Progression Clamp Test");
        progressionObject.SetActive(false);
        ProgressionSystem progression = progressionObject.AddComponent<ProgressionSystem>();
        progression.saveKey = saveKey;
        progression.totalProgressPoints = 100;

        try
        {
            progression.SetProgress(-20);
            Assert.That(progression.GetCurrentProgress(), Is.Zero);
            Assert.That(progression.GetProgressPercentage(), Is.Zero);

            progression.SetProgress(150);
            Assert.That(progression.GetCurrentProgress(), Is.EqualTo(100));
            Assert.That(progression.GetProgressPercentage(), Is.EqualTo(100f));
            Assert.That(progression.GetProgressString(), Is.EqualTo("100%"));
        }
        finally
        {
            if (hadValue) PlayerPrefs.SetInt(saveKey, originalValue);
            else PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
            ProgressionSystem.Instance = originalInstance;
            UnityEngine.Object.DestroyImmediate(progressionObject);
        }
    }

    [Test]
    public void PathGuideIndicator_RestoreProgress_ClampsToAvailableWaypoints()
    {
        GameObject guideObject = new GameObject("Path Guide Restore Test");
        guideObject.SetActive(false);
        PathGuideIndicator guide = guideObject.AddComponent<PathGuideIndicator>();
        GameObject firstLocation = new GameObject("First Guide Location");
        GameObject secondLocation = new GameObject("Second Guide Location");

        try
        {
            guide.useChildLocations = false;
            guide.indicatorLocations = new[] { firstLocation.transform, secondLocation.transform };

            guide.RestoreProgress(-1);
            Assert.That(guide.CurrentLocationIndex, Is.Zero);

            guide.RestoreProgress(20);
            Assert.That(guide.CurrentLocationIndex, Is.EqualTo(2));
            Assert.That(guide.CurrentLocation, Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(guideObject);
            UnityEngine.Object.DestroyImmediate(firstLocation);
            UnityEngine.Object.DestroyImmediate(secondLocation);
        }
    }

    [Test]
    public void DrawerInteraction_RestoreSavedState_AppliesOpenStateAndLocalPosition()
    {
        GameObject drawerObject = new GameObject("Drawer Restore Test");
        drawerObject.SetActive(false);
        DrawerInteraction drawer = drawerObject.AddComponent<DrawerInteraction>();
        Vector3 savedPosition = new Vector3(0.2f, 0.35f, -0.1f);

        try
        {
            drawer.RestoreSavedState(true, savedPosition);
            Assert.That(drawer.IsOpen(), Is.True);
            Assert.That(drawer.transform.localPosition, Is.EqualTo(savedPosition));

            drawer.RestoreSavedState(false, Vector3.zero);
            Assert.That(drawer.IsOpen(), Is.False);
            Assert.That(drawer.transform.localPosition, Is.EqualTo(Vector3.zero));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(drawerObject);
        }
    }

    [Test]
    public void KeyAndBattery_MarkAsUsed_ClearPickupAndHeldStates()
    {
        GameObject keyObject = new GameObject("Used Key Test");
        GameObject batteryObject = new GameObject("Used Battery Test");
        keyObject.SetActive(false);
        batteryObject.SetActive(false);
        Key key = keyObject.AddComponent<Key>();
        BatteryPickup battery = batteryObject.AddComponent<BatteryPickup>();

        try
        {
            key.isPickedUp = true;
            key.wasDropped = true;
            key.MarkAsUsed();
            Assert.That(key.wasUsed, Is.True);
            Assert.That(key.IsPickedUp, Is.False);
            Assert.That(key.wasDropped, Is.False);

            battery.wasDropped = true;
            battery.isHeld = true;
            battery.MarkAsUsed();
            Assert.That(battery.wasUsed, Is.True);
            Assert.That(battery.wasDropped, Is.False);
            Assert.That(battery.isHeld, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(keyObject);
            UnityEngine.Object.DestroyImmediate(batteryObject);
        }
    }

    [Test]
    public void GeneratorFuelInteraction_AcceptsGasNamesAndRejectsUnrelatedItems()
    {
        GameObject generatorObject = new GameObject("Gas Name Validation Test", typeof(BoxCollider));
        generatorObject.SetActive(false);
        GeneratorFuelInteraction fuel = generatorObject.AddComponent<GeneratorFuelInteraction>();
        fuel.requiredItemName = "Gas";

        try
        {
            Assert.That(InvokePrivate(fuel, "IsAcceptedGasName", "Gas"), Is.True);
            Assert.That(InvokePrivate(fuel, "IsAcceptedGasName", "gas can"), Is.True);
            Assert.That(InvokePrivate(fuel, "IsAcceptedGasName", "Gasoline"), Is.False);
            Assert.That(InvokePrivate(fuel, "IsAcceptedGasName", string.Empty), Is.False);
            Assert.That(InvokePrivate(fuel, "IsAcceptedGasName", (object)null), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(generatorObject);
        }
    }

    [TestCase(false, false, false, false, "KeepExistingGas")]
    [TestCase(false, true, false, true, "KeepExistingGas")]
    [TestCase(true, true, false, false, "RemoveGas")]
    [TestCase(true, false, true, false, "RemoveGas")]
    [TestCase(true, false, false, true, "RemoveGas")]
    [TestCase(true, false, false, false, "RespawnGas")]
    public void GeneratorKeySlot_GasRestorePolicy_MatchesPourAndPowerState(
        bool hasEverPouredGas, bool lightsOn, bool isRunning, bool hasFuel, string expectedAction)
    {
        object action = InvokePrivateStatic(typeof(GeneratorKeySlot), "DetermineGasRestoreAction",
            hasEverPouredGas, lightsOn, isRunning, hasFuel);
        Assert.That(action.ToString(), Is.EqualTo(expectedAction));
    }

    [Test]
    public void GeneratorKeySlot_AreAnyGeneratorLightsOn_UsesActiveEnabledLights()
    {
        GameObject slotObject = new GameObject("Generator Light State Test", typeof(BoxCollider));
        slotObject.SetActive(false);
        GeneratorKeySlot slot = slotObject.AddComponent<GeneratorKeySlot>();
        GameObject firstLightObject = new GameObject("Generator Test Light A");
        GameObject secondLightObject = new GameObject("Generator Test Light B");
        Light firstLight = firstLightObject.AddComponent<Light>();
        Light secondLight = secondLightObject.AddComponent<Light>();
        slot.additionalGeneratorLights = new[] { firstLight, secondLight };

        try
        {
            firstLight.enabled = false;
            secondLight.enabled = true;
            Assert.That(slot.AreAnyGeneratorLightsOn, Is.True);

            secondLightObject.SetActive(false);
            Assert.That(slot.AreAnyGeneratorLightsOn, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(slotObject);
            UnityEngine.Object.DestroyImmediate(firstLightObject);
            UnityEngine.Object.DestroyImmediate(secondLightObject);
        }
    }

    [Test]
    public void GeneratorLightsOffTrigger_RestoresBlackoutWithoutReplayingTrigger()
    {
        GameObject triggerObject = new GameObject("Generator Blackout Test", typeof(BoxCollider));
        GeneratorLightsOffTrigger trigger = triggerObject.AddComponent<GeneratorLightsOffTrigger>();
        GameObject lightObject = new GameObject("Blackout Test Light");
        Light light = lightObject.AddComponent<Light>();
        trigger.generatorLights = new[] { light };

        try
        {
            trigger.SetControlledLights(true);
            Assert.That(trigger.HasConfiguredLights, Is.True);
            Assert.That(trigger.AreAnyControlledLightsOn, Is.True);

            trigger.RestoreLightsOffState();

            Assert.That(light.enabled, Is.False);
            Assert.That(trigger.AreAnyControlledLightsOn, Is.False);
            Assert.That(triggerObject.GetComponent<Collider>().enabled, Is.False);
            Assert.That(GetPrivateField<bool>(trigger, "hasTriggered"), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(triggerObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }

    [TestCase(true, true, true, true, true)]
    [TestCase(false, true, true, true, false)]
    [TestCase(true, false, true, true, false)]
    [TestCase(true, true, false, true, false)]
    [TestCase(true, true, true, false, false)]
    public void RitualManager_RequiresBothCandlesBibleAndCross(bool candleOne, bool candleTwo,
        bool bible, bool cross, bool expectedComplete)
    {
        object result = InvokePrivateStatic(typeof(RitualManager), "AreAllRitualItemsPlaced",
            candleOne, candleTwo, bible, cross);
        Assert.That(result, Is.EqualTo(expectedComplete));
    }

    [Test]
    public void SaveSystemState_GeneratorSubtitleAndIntroProgress_RoundTripAndClear()
    {
        string databasePath = Path.Combine(Path.GetTempPath(),
            "VarenStateTest_" + Guid.NewGuid().ToString("N") + ".db");
        string originalDifficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        bool hadDifficulty = PlayerPrefs.HasKey("GameDifficulty");
        SaveSystem originalInstance = SaveSystem.Instance;
        GameObject saveObject = new GameObject("Save State Test");
        saveObject.SetActive(false);
        SaveSystem saveSystem = saveObject.AddComponent<SaveSystem>();
        SQLiteConnection database = null;

        try
        {
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            database = CreateTemporarySaveDatabase(saveSystem, databasePath, true);
            SaveSystem.Instance = saveSystem;

            saveSystem.MarkGeneratorCoverRemoved("UnitTestCover");
            Assert.That(saveSystem.IsGeneratorCoverRemoved("UnitTestCover"), Is.True);
            saveSystem.ClearGeneratorState("UnitTestCover");
            Assert.That(saveSystem.IsGeneratorCoverRemoved("UnitTestCover"), Is.False);

            saveSystem.MarkSubtitleTriggered("unit-test-subtitle");
            Assert.That(saveSystem.HasSubtitleTriggered("unit-test-subtitle"), Is.True);
            saveSystem.ClearSubtitleData();
            Assert.That(saveSystem.HasSubtitleTriggered("unit-test-subtitle"), Is.False);

            saveSystem.SaveStoryIntroProgress(3, 7, true);
            Assert.That(saveSystem.TryGetStoryIntroProgress(out int section, out int line, out bool complete), Is.True);
            Assert.That(section, Is.EqualTo(3));
            Assert.That(line, Is.EqualTo(7));
            Assert.That(complete, Is.True);
            saveSystem.ClearStoryIntroData();
            Assert.That(saveSystem.TryGetStoryIntroProgress(out _, out _, out _), Is.False);
        }
        finally
        {
            SaveSystem.Instance = originalInstance;
            if (database != null) database.Close();
            UnityEngine.Object.DestroyImmediate(saveObject);
            if (File.Exists(databasePath)) File.Delete(databasePath);
            if (hadDifficulty) PlayerPrefs.SetString("GameDifficulty", originalDifficulty);
            else PlayerPrefs.DeleteKey("GameDifficulty");
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void SaveSystem_ClearNewGameWorldState_RemovesHardModeDroppedAndGeneratorData()
    {
        string databasePath = Path.Combine(Path.GetTempPath(),
            "VarenNewGameStateTest_" + Guid.NewGuid().ToString("N") + ".db");
        string originalDifficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        bool hadDifficulty = PlayerPrefs.HasKey("GameDifficulty");
        SaveSystem originalInstance = SaveSystem.Instance;
        GameObject saveObject = new GameObject("New Game World State Test");
        saveObject.SetActive(false);
        SaveSystem saveSystem = saveObject.AddComponent<SaveSystem>();
        SQLiteConnection database = null;

        try
        {
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            database = CreateTemporarySaveDatabase(saveSystem, databasePath, true);
            SaveSystem.Instance = saveSystem;
            database.Insert(new AIPositionData { AIId = "TestAI", SceneName = "test" });
            database.Insert(new DroppedItemData { ItemName = "Gas", IsDropped = true });
            database.Insert(new GasData { GasId = "Gas", IsDropped = true });
            database.Insert(new WrenchData { WrenchId = "Wrench", IsDropped = true });
            database.Insert(new GeneratorCoverData { CoverId = "Cover", IsRemoved = true });

            saveSystem.ClearNewGameWorldState();

            Assert.That(database.Table<AIPositionData>().Count(), Is.Zero);
            Assert.That(database.Table<DroppedItemData>().Count(), Is.Zero);
            Assert.That(database.Table<GasData>().Count(), Is.Zero);
            Assert.That(database.Table<WrenchData>().Count(), Is.Zero);
            Assert.That(database.Table<GeneratorCoverData>().Count(), Is.Zero);
        }
        finally
        {
            SaveSystem.Instance = originalInstance;
            if (database != null) database.Close();
            UnityEngine.Object.DestroyImmediate(saveObject);
            if (File.Exists(databasePath)) File.Delete(databasePath);
            if (hadDifficulty) PlayerPrefs.SetString("GameDifficulty", originalDifficulty);
            else PlayerPrefs.DeleteKey("GameDifficulty");
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void GasDroppedItemData_PreservesWorldPositionAndRotationOnRestore()
    {
        GameObject gasToSaveObject = new GameObject("Gas To Save");
        GameObject restoredGasObject = new GameObject("Gas To Restore");
        PickupItem restoredGas = restoredGasObject.AddComponent<PickupItem>();
        Vector3 expectedPosition = new Vector3(14.5f, 2.25f, -8f);
        Quaternion expectedRotation = Quaternion.Euler(12f, 83f, -4f);
        gasToSaveObject.transform.SetPositionAndRotation(expectedPosition, expectedRotation);

        try
        {
            DroppedItemData data = (DroppedItemData)InvokePrivateStatic(typeof(SaveSystem),
                "CreateDroppedItemData", gasToSaveObject, "Gas");

            Assert.That(data.ItemName, Is.EqualTo("Gas"));
            Assert.That(data.IsDropped, Is.True);
            Assert.That(data.PosX, Is.EqualTo(expectedPosition.x));
            Assert.That(data.PosY, Is.EqualTo(expectedPosition.y));
            Assert.That(data.PosZ, Is.EqualTo(expectedPosition.z));

            InvokePrivateStatic(typeof(SaveSystem), "ApplyDroppedItemData", restoredGas, data);

            Assert.That(restoredGas.transform.position, Is.EqualTo(expectedPosition));
            Assert.That(Quaternion.Angle(restoredGas.transform.rotation, expectedRotation), Is.LessThan(0.01f));
            Assert.That(restoredGas.wasDropped, Is.True);
            Assert.That(restoredGas.isHeld, Is.False);
            Assert.That(restoredGas.isPickedUp, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gasToSaveObject);
            UnityEngine.Object.DestroyImmediate(restoredGasObject);
        }
    }

    [Test]
    public void SettingsDatabase_SaveAndLoad_PreservesSettingsInIsolatedDatabase()
    {
        string databasePath = Path.Combine(Path.GetTempPath(),
            "VarenSettingsTest_" + Guid.NewGuid().ToString("N") + ".db");

        try
        {
            object[] loadArguments = { databasePath, null };
            Assert.That(InvokePrivateStatic(typeof(SettingsDatabase), "TryLoad", loadArguments), Is.False);
            Assert.That(InvokePrivateStatic(typeof(SettingsDatabase), "Save", databasePath, null), Is.False);

            SettingsData expected = new SettingsData
            {
                Volume = 0.73f,
                Sensitivity = 1.8f,
                Brightness = 0.62f,
                QualityLevel = 3,
                Language = (int)GameLanguage.Tagalog
            };
            Assert.That(InvokePrivateStatic(typeof(SettingsDatabase), "Save", databasePath, expected), Is.True);

            loadArguments = new object[] { databasePath, null };
            Assert.That(InvokePrivateStatic(typeof(SettingsDatabase), "TryLoad", loadArguments), Is.True);
            SettingsData actual = (SettingsData)loadArguments[1];
            Assert.That(actual.Volume, Is.EqualTo(expected.Volume));
            Assert.That(actual.Sensitivity, Is.EqualTo(expected.Sensitivity));
            Assert.That(actual.Brightness, Is.EqualTo(expected.Brightness));
            Assert.That(actual.QualityLevel, Is.EqualTo(expected.QualityLevel));
            Assert.That(actual.Language, Is.EqualTo(expected.Language));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static void AssertMissingProperties(Type type, params string[] propertyNames)
    {
        string[] actualProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();

        foreach (string propertyName in propertyNames)
            Assert.That(actualProperties, Does.Not.Contain(propertyName), type.Name + " must store dropped transforms in DroppedItemData.");
    }

    private static SQLiteConnection CreateTemporarySaveDatabase(SaveSystem saveSystem,
        string databasePath, bool hardMode)
    {
        SQLiteConnection database = new SQLiteConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        try
        {
            RelationalSaveSchema.Create(database, hardMode);
            SetPrivateField(saveSystem, "connection", database);
            SetPrivateField(saveSystem, "isDatabaseReady", true);
            return database;
        }
        catch
        {
            database.Close();
            throw;
        }
    }

    private static object InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Could not find " + methodName + " on " + target.GetType().Name + ".");
        return method.Invoke(target, arguments);
    }

    private static object InvokePrivateStatic(Type targetType, string methodName, params object[] arguments)
    {
        MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Could not find " + methodName + " on " + targetType.Name + ".");
        return method.Invoke(null, arguments);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Could not find " + fieldName + " on " + target.GetType().Name + ".");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Could not find " + fieldName + " on " + target.GetType().Name + ".");
        return (T)field.GetValue(target);
    }

    private static void SaveLanguage(GameLanguage language)
    {
        Assert.That(SettingsDatabase.Save(new SettingsData { Language = (int)language }), Is.True);
    }

    private static void RestoreSettings(SettingsData settings, bool existed)
    {
        if (existed)
        {
            SettingsDatabase.Save(settings);
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, "settings.db");
        if (File.Exists(path)) File.Delete(path);
    }
}
#endif
