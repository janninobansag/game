using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class VarenIntegrationPlayModeTests
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags StaticAny = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly HashSet<int> originalSceneObjectIds = new HashSet<int>();
    private readonly Dictionary<string, SavedPreference> savedPreferences = new Dictionary<string, SavedPreference>();
    private readonly Dictionary<FieldInfo, object> savedStaticFields = new Dictionary<FieldInfo, object>();
    private readonly List<string> temporaryDatabasePaths = new List<string>();
    private string testId;
    private float originalTimeScale;
    private float originalAudioListenerVolume;
    private CursorLockMode originalCursorLockState;
    private bool originalCursorVisibility;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        testId = Guid.NewGuid().ToString("N");
        originalTimeScale = Time.timeScale;
        originalAudioListenerVolume = AudioListener.volume;
        originalCursorLockState = Cursor.lockState;
        originalCursorVisibility = Cursor.visible;

        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject != null && gameObject.scene.IsValid())
                originalSceneObjectIds.Add(gameObject.GetInstanceID());
        }

        CaptureAndClearStatic(typeof(Inventory), "Instance");
        CaptureAndClearStatic(typeof(SaveSystem), "Instance");
        CaptureAndClearStatic(typeof(AudioManager), "Instance");
        CaptureAndClearStatic(typeof(PauseMenu), "Instance");
        CaptureAndClearStatic(typeof(ProgressionSystem), "Instance");
        CaptureAndClearStatic(typeof(Note), "activeNote");
        CaptureAndClearStatic(typeof(NoteUIController), "instance");
        CaptureAndClearStatic(typeof(AIEncounterGate), "owner");
        CaptureAndClearStatic(typeof(PlayerController), "<IsReadingDocument>k__BackingField");

        CapturePreference("GameDifficulty", PreferenceKind.String);
        CapturePreference("SaveTime", PreferenceKind.String);
        CapturePreference("ObjectiveTriggerKeys", PreferenceKind.String);
        CapturePreference("ShouldLoadSave", PreferenceKind.Int);
        CapturePreference("SkipIntro", PreferenceKind.Int);
        CapturePreference("GameLanguage", PreferenceKind.Int);
        CapturePreference("MasterVolume", PreferenceKind.Float);
        CapturePreference("MouseSensitivity", PreferenceKind.Float);
        CapturePreference("Brightness", PreferenceKind.Float);
        CapturePreference("QualityLevel", PreferenceKind.Int);
        CapturePreference("SettingsRepairVersion", PreferenceKind.Int);
        CaptureExistingObjectivePreferences();

        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        Time.timeScale = 1f;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;

        GameObject[] currentObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int index = currentObjects.Length - 1; index >= 0; index--)
        {
            GameObject gameObject = currentObjects[index];
            if (gameObject == null || !gameObject.scene.IsValid() ||
                originalSceneObjectIds.Contains(gameObject.GetInstanceID()))
                continue;

            Transform parent = gameObject.transform.parent;
            if (parent == null || originalSceneObjectIds.Contains(parent.gameObject.GetInstanceID()))
                UnityEngine.Object.Destroy(gameObject);
        }

        yield return null;

        foreach (string databasePath in temporaryDatabasePaths)
            DeleteTemporaryDatabase(databasePath);

        foreach (KeyValuePair<string, SavedPreference> entry in savedPreferences)
            entry.Value.Restore(entry.Key);
        PlayerPrefs.Save();

        AudioListener.volume = originalAudioListenerVolume;
        Cursor.lockState = originalCursorLockState;
        Cursor.visible = originalCursorVisibility;
        Time.timeScale = originalTimeScale;

        foreach (KeyValuePair<FieldInfo, object> entry in savedStaticFields)
            entry.Key.SetValue(null, entry.Value);

        ClearMenuLocalizationCache();
    }

    [UnityTest]
    public IEnumerator IT001_InventoryKeyUnlocksMatchingDoorAndConsumesKey()
    {
        CreateMainCamera();
        Inventory inventory = CreateInventory();

        GameObject doorObject = CreateObject("Integration Test Door");
        doorObject.transform.position = new Vector3(0f, 0f, 2f);
        doorObject.AddComponent<BoxCollider>();
        DoorInteraction door = doorObject.AddComponent<DoorInteraction>();
        door.isLocked = true;
        door.animationSpeed = 100f;

        GameObject keyObject = CreateObject("Integration Test Key");
        Key key = keyObject.AddComponent<Key>();
        key.unlocksTag = doorObject.name;
        keyObject.AddComponent<KeyUse>();
        Assert.That(inventory.AddItem(keyObject), Is.True);

        yield return null;

        InvokePrivate(keyObject.GetComponent<KeyUse>(), "TryUnlockDoor");

        Assert.That(door.IsLocked(), Is.False);
        Assert.That(door.IsOpen(), Is.True);
        Assert.That(key.wasUsed, Is.True);
        Assert.That(inventory.GetCount(), Is.Zero);
    }

    [UnityTest]
    public IEnumerator IT002_InventoryItemPlacement_PersistsRitualPlacement()
    {
        SaveSystem saveSystem = CreateSaveSystem(false);
        Inventory inventory = CreateInventory();

        GameObject holderObject = CreateObject("Integration Test Bible Holder");
        TableHolder holder = holderObject.AddComponent<TableHolder>();
        // This test invokes PlaceItem directly; the camera/raycast prompt loop is a manual interaction check.
        holder.enabled = false;
        holder.itemNameRequired = "Bible";
        GameObject placementPoint = CreateObject("Bible Placement Point");
        placementPoint.transform.SetParent(holderObject.transform, false);
        holder.placementPoint = placementPoint.transform;

        GameObject bible = CreateObject("Bible");
        PickupItem pickup = bible.AddComponent<PickupItem>();
        pickup.itemName = "Bible";
        bible.AddComponent<BoxCollider>();
        Assert.That(inventory.AddItem(bible), Is.True);

        yield return null;
        InvokePrivate(holder, "PlaceItem");

        Assert.That(holder.HasItem(), Is.True);
        Assert.That(holder.GetPlacedItem(), Is.SameAs(bible));
        Assert.That(bible.transform.parent, Is.EqualTo(placementPoint.transform));
        Assert.That(inventory.GetItems().Contains(bible), Is.False);
        Assert.That(pickup.enabled, Is.False);
        Assert.That(saveSystem, Is.SameAs(SaveSystem.Instance));
    }

    [UnityTest]
    public IEnumerator IT003_SaveLoad_RestoresPlayerInventoryAndDoorState()
    {
        SaveSystem saveSystem = CreateSaveSystem(false);
        GameObject player = CreatePlayer();
        Inventory inventory = CreateInventory();

        GameObject item = CreateObject("Integration Test Item");
        PickupItem pickup = item.AddComponent<PickupItem>();
        pickup.itemName = item.name;
        Assert.That(inventory.AddItem(item), Is.True);

        GameObject doorObject = CreateObject("Integration Test Save Door");
        doorObject.transform.position = new Vector3(4f, 0f, 2f);
        doorObject.AddComponent<BoxCollider>();
        DoorInteraction door = doorObject.AddComponent<DoorInteraction>();
        door.isLocked = false;

        Vector3 savedPlayerPosition = new Vector3(2f, 3f, 4f);
        player.transform.position = savedPlayerPosition;
        yield return null;
        door.SetOpenState(true);

        saveSystem.SaveGame();
        player.transform.position = new Vector3(-9f, 1f, 7f);
        door.SetOpenState(false);
        inventory.RemoveWithoutDestroy(item);

        Assert.That(saveSystem.LoadGame(), Is.True);

        Assert.That(Vector3.Distance(player.transform.position, savedPlayerPosition), Is.LessThan(0.001f));
        Assert.That(inventory.GetItems(), Does.Contain(item));
        Assert.That(door.IsOpen(), Is.True);
    }

    [UnityTest]
    public IEnumerator IT004_TikbalangQnA_CorrectAnswerKeepsHealthAndReleasesEncounter()
    {
        GameObject player = CreatePlayer();
        CreateMainCamera();
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;
        health.currentHealth = 100f;

        GameObject panel = CreateQnAPanel(out Button correctAnswerButton);
        GameObject tikbalangObject = CreateObject("Integration Test Tikbalang");
        tikbalangObject.SetActive(false);
        NavMeshAgent agent = tikbalangObject.AddComponent<NavMeshAgent>();
        // Q&A ownership and health behavior are under test here, not NavMesh pathing.
        agent.enabled = false;
        TikbalangAI tikbalang = tikbalangObject.AddComponent<TikbalangAI>();
        tikbalang.player = player.transform;
        tikbalang.qnaPanel = panel;
        tikbalang.questionDisplayTime = 3f;
        tikbalang.jumpscareBeforeTeleportDelay = 0f;
        tikbalang.cameraLookDuration = 0f;
        tikbalang.cameraReturnDuration = 0f;
        tikbalang.showGeneratorObjectiveAfterIntro = false;
        tikbalang.questions = new[]
        {
            new QnAEntry
            {
                question = "Integration test question?",
                options = new[] { "Correct", "Wrong 1", "Wrong 2", "Wrong 3" },
                correctAnswerIndex = 0
            }
        };
        tikbalangObject.SetActive(true);

        yield return null;

        Assert.That(tikbalang.TriggerDetectionJumpscare(null), Is.True);
        Assert.That(AIEncounterGate.IsOwnedBy(tikbalang), Is.True);

        for (int frame = 0; frame < 30 && !panel.activeSelf; frame++)
            yield return null;

        Assert.That(panel.activeSelf, Is.True, "Tikbalang should open its Q&A panel during the encounter.");
        Assert.That(correctAnswerButton.interactable, Is.True);
        correctAnswerButton.onClick.Invoke();

        for (int frame = 0; frame < 120 && AIEncounterGate.IsActive; frame++)
            yield return new WaitForSecondsRealtime(0.02f);

        Assert.That(health.currentHealth, Is.EqualTo(100f).Within(0.001f));
        Assert.That(panel.activeSelf, Is.False);
        Assert.That(AIEncounterGate.IsActive, Is.False);
    }

    [UnityTest]
    public IEnumerator IT005_GeneratorReload_RestoresInsertedKeyFuelAndLights()
    {
        PlayerPrefs.SetString("GameDifficulty", "Hard");
        SaveSystem saveSystem = CreateSaveSystem(true);
        CreatePlayer();
        saveSystem.MarkGeneratorCoverRemoved("GeneratorFuel");
        saveSystem.MarkGeneratorCoverRemoved("GeneratorKeyInserted");

        Inventory inventory = CreateInventory();
        GameObject keyObject = CreateObject("Generator key");
        keyObject.AddComponent<Key>().itemName = "Generator key";
        PickupItem keyPickup = keyObject.AddComponent<PickupItem>();
        keyPickup.itemName = "Generator key";
        Assert.That(inventory.AddItem(keyObject), Is.True);

        GeneratorFixture fixture = CreateGeneratorFixture(null, false);
        yield return null;

        Assert.That(fixture.Fuel.HasFuel(), Is.True);
        Assert.That(keyObject.transform.parent, Is.EqualTo(fixture.KeyInsertPoint));
        Assert.That(inventory.GetCount(), Is.Zero);
        Assert.That(fixture.Slot.AreAnyGeneratorLightsOn, Is.True);

        saveSystem.SaveGame();
        fixture.Slot.RestoreGeneratorLights(false);
        Assert.That(fixture.Slot.AreAnyGeneratorLightsOn, Is.False);
        Assert.That(saveSystem.LoadGame(), Is.True);

        Assert.That(fixture.Fuel.HasFuel(), Is.True);
        Assert.That(keyObject.transform.parent, Is.EqualTo(fixture.KeyInsertPoint));
        Assert.That(fixture.Slot.AreAnyGeneratorLightsOn, Is.True);
    }

    [UnityTest]
    public IEnumerator IT006_SettingsDatabase_AppliesAudioAndMenuLocalization()
    {
        string settingsPath = CreateTemporaryDatabasePath();
        SettingsData expected = new SettingsData
        {
            Volume = 37f,
            Sensitivity = 3.5f,
            Brightness = 0.62f,
            QualityLevel = 1,
            Language = (int)GameLanguage.Tagalog
        };

        Assert.That(InvokePrivateStatic(typeof(SettingsDatabase), "Save", settingsPath, expected), Is.True);
        object[] loadArguments = { settingsPath, null };
        Assert.That(InvokePrivateStatic(typeof(SettingsDatabase), "TryLoad", loadArguments), Is.True);
        SettingsData loaded = (SettingsData)loadArguments[1];

        GameObject managerObject = CreateObject("Integration Test Audio Manager");
        AudioManager audioManager = managerObject.AddComponent<AudioManager>();
        audioManager.enabled = false;
        InvokePrivate(audioManager, "LoadAndApplySettingsFromDatabase", settingsPath);

        GameObject labelObject = CreateObject("Integration Test Menu Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "BACK";
        MenuLocalization.Apply((GameLanguage)loaded.Language, null);

        Assert.That(audioManager.GetVolume(), Is.EqualTo(expected.Volume).Within(0.001f));
        Assert.That(audioManager.GetSensitivity(), Is.EqualTo(expected.Sensitivity).Within(0.001f));
        Assert.That(audioManager.GetBrightness(), Is.EqualTo(expected.Brightness).Within(0.001f));
        Assert.That(AudioListener.volume, Is.EqualTo(expected.Volume / 100f).Within(0.001f));
        Assert.That(label.text, Is.EqualTo("BUMALIK"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator IT007_SequentialEncounters_PausesAndResumesOtherAI()
    {
        GameObject tikbalang = CreateObject("Tikbalang Encounter Owner");
        GameObject whiteLady = CreateObject("White Lady Encounter Contender");
        Animator whiteLadyAnimator = whiteLady.AddComponent<Animator>();
        whiteLadyAnimator.speed = 0.75f;
        AIEncounterPause pause = new AIEncounterPause();

        Assert.That(AIEncounterGate.TryBegin(tikbalang), Is.True);
        Assert.That(pause.Update(whiteLady, null, whiteLadyAnimator, null), Is.True);
        Assert.That(whiteLadyAnimator.speed, Is.Zero);
        Assert.That(AIEncounterGate.TryBegin(whiteLady), Is.False);

        AIEncounterGate.End(tikbalang);
        Assert.That(pause.Update(whiteLady, null, whiteLadyAnimator, null), Is.False);
        Assert.That(whiteLadyAnimator.speed, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(AIEncounterGate.TryBegin(whiteLady), Is.True);
        AIEncounterGate.End(whiteLady);
        yield return null;
    }

    [UnityTest]
    public IEnumerator IT008_WhiteLadyOneShot_LoadRestoresAndNewGameClearsTrigger()
    {
        SaveSystem saveSystem = CreateSaveSystem(false);
        CreatePlayer();

        GameObject whiteLady = CreateObject("Integration Test White Lady");
        GameObject teleportPoint = CreateObject("Integration Test Teleport Point");
        teleportPoint.transform.position = new Vector3(12f, 0f, -3f);

        GameObject triggerObject = CreateObject("Integration Test White Lady Trigger");
        BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        WhiteLadyTeleportTrigger trigger = triggerObject.AddComponent<WhiteLadyTeleportTrigger>();
        trigger.whiteLady = whiteLady.transform;
        trigger.teleportPoints = new[] { teleportPoint.transform };
        trigger.teleportInterval = 0.001f;
        trigger.hideWhiteLadyAfterSequence = false;
        trigger.saveId = "PlayModeWhiteLady_" + testId;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        BoxCollider playerCollider = playerObject.AddComponent<BoxCollider>();
        yield return null;

        InvokePrivate(trigger, "TryStartTeleport", playerCollider);
        Assert.That(trigger.HasTriggered, Is.True);
        saveSystem.SaveGame();

        trigger.RestoreTriggeredState(false);
        Assert.That(trigger.HasTriggered, Is.False);
        Assert.That(saveSystem.LoadGame(), Is.True);
        Assert.That(trigger.HasTriggered, Is.True);

        saveSystem.ClearOneShotEventStates();
        trigger.RestoreTriggeredState(true);
        Assert.That(saveSystem.LoadGame(), Is.True);
        Assert.That(trigger.HasTriggered, Is.False, "The New Game one-shot reset should be loadable from the isolated save.");
    }

    [UnityTest]
    public IEnumerator IT009_NoteCloseEvents_LoadDoesNotReplayAndNewGameResets()
    {
        SaveSystem saveSystem = CreateSaveSystem(false);
        CreatePlayer();
        CreateNoteCanvas();

        GameObject noteObject = CreateObject("Integration Test Note");
        Note note = noteObject.AddComponent<Note>();
        note.saveId = "PlayModeNote_" + testId;
        note.noteTitle = "Integration Test Note";

        AudioTrigger audioTrigger = noteObject.AddComponent<AudioTrigger>();
        audioTrigger.fadeIn = false;
        audioTrigger.audioClip = AudioClip.Create("PlayMode Silent Note", 128, 1, 44100, false);
        audioTrigger.audioClip.SetData(new float[128], 0);
        note.onCloseAudio = audioTrigger;

        ObjectiveTrigger objective = noteObject.AddComponent<ObjectiveTrigger>();
        objective.saveId = "PlayModeNoteObjective_" + testId;
        objective.objectiveText = "Integration test objective";
        note.onCloseObjective = objective;
        SnapshotObjectivePreference(objective.saveId, "Normal");
        SnapshotObjectivePreference(objective.saveId, "Hard");

        yield return null;

        note.OpenNote();
        note.CloseNote();
        Assert.That(note.HasBeenRead(), Is.True);
        Assert.That(audioTrigger.HasPlayed, Is.True);
        Assert.That(GetPrivateField<bool>(objective, "hasTriggered"), Is.True);
        saveSystem.SaveGame();

        Assert.That(saveSystem.LoadGame(), Is.True);
        Assert.That(note.HasBeenRead(), Is.True);
        Assert.That(audioTrigger.HasPlayed, Is.True);

        // Reset these local flags as a test probe: a repeated close should not call either event.
        audioTrigger.RestorePlayedState(false);
        SetPrivateField(objective, "hasTriggered", false);
        note.OpenNote();
        note.CloseNote();
        Assert.That(audioTrigger.HasPlayed, Is.False);
        Assert.That(GetPrivateField<bool>(objective, "hasTriggered"), Is.False);

        saveSystem.ClearOneShotEventStates();
        ObjectiveTrigger.ClearSavedStates();
        note.RestoreReadState(false);
        SetPrivateField(objective, "hasTriggered", false);
        Assert.That(saveSystem.LoadGame(), Is.True);
        Assert.That(note.HasBeenRead(), Is.False);
        Assert.That(audioTrigger.HasPlayed, Is.False);

        objective.TriggerObjective();
        Assert.That(GetPrivateField<bool>(objective, "hasTriggered"), Is.True);
    }

    [UnityTest]
    public IEnumerator IT010_UnpouredGas_LoadKeepsOriginalGasWhileLightsAreOn()
    {
        PlayerPrefs.SetString("GameDifficulty", "Hard");
        SaveSystem saveSystem = CreateSaveSystem(true);
        CreatePlayer();
        CreateMainCamera();

        GameObject gas = CreateGas("Gas", new Vector3(1f, 0.2f, 2f));
        gas.GetComponent<PickupItem>().wasDropped = true;
        GeneratorFixture fixture = CreateGeneratorFixture(gas, false);
        yield return null;
        fixture.Slot.RestoreGeneratorLights(true);

        Assert.That(fixture.Fuel.HasEverPouredGas, Is.False);
        saveSystem.SaveGame();
        gas.transform.position = new Vector3(40f, 3f, -20f);

        Assert.That(saveSystem.LoadGame(), Is.True);

        Assert.That(gas.activeInHierarchy, Is.True);
        Assert.That(fixture.Slot.AreAnyGeneratorLightsOn, Is.True);
        Assert.That(Vector3.Distance(gas.transform.position, new Vector3(1f, 0.2f, 2f)), Is.LessThan(0.001f));
    }

    [UnityTest]
    public IEnumerator IT011_RespawnedGas_LoadRestoresSpawnPointWorldPosition()
    {
        PlayerPrefs.SetString("GameDifficulty", "Hard");
        SaveSystem saveSystem = CreateSaveSystem(true);
        CreatePlayer();
        CreateMainCamera();

        GameObject originalGas = CreateGas("Gas", new Vector3(0f, 0.2f, 0f));
        GeneratorFixture fixture = CreateGeneratorFixture(originalGas, true);
        GameObject firstSpawn = CreateObject("gas spawn point A");
        firstSpawn.transform.position = new Vector3(10f, 0f, 4f);
        GameObject secondSpawn = CreateObject("gas spawn point B");
        secondSpawn.transform.position = new Vector3(-8f, 0f, 6f);
        fixture.Slot.gasSpawnPoints = new[] { firstSpawn.transform, secondSpawn.transform };
        fixture.Slot.gasSpawnHeightOffset = 0.25f;
        fixture.Slot.runningSeconds = 0.05f;
        fixture.Slot.showLightsShutdownWarning = false;

        yield return null;

        SetPrivateField(fixture.Fuel, "isFueled", true);
        SetPrivateField(fixture.Fuel, "hasEverPouredGas", true);
        fixture.Slot.RestoreGeneratorLights(false);
        originalGas.SetActive(false);
        fixture.Slot.SendMessage("RunGenerator", SendMessageOptions.RequireReceiver);

        for (int frame = 0; frame < 60 && fixture.Slot.IsRunning; frame++)
            yield return new WaitForSecondsRealtime(0.02f);

        PickupItem replacementGas = FindActiveGas();
        Assert.That(replacementGas, Is.Not.Null);
        Vector3 savedSpawnPosition = replacementGas.transform.position;
        Vector3 expectedA = firstSpawn.transform.position + Vector3.up * fixture.Slot.gasSpawnHeightOffset;
        Vector3 expectedB = secondSpawn.transform.position + Vector3.up * fixture.Slot.gasSpawnHeightOffset;
        Assert.That(Mathf.Min(Vector3.Distance(savedSpawnPosition, expectedA),
            Vector3.Distance(savedSpawnPosition, expectedB)), Is.LessThan(0.001f));
        Assert.That(fixture.Slot.AreAnyGeneratorLightsOn, Is.False);
        Assert.That(fixture.Fuel.HasEverPouredGas, Is.True);

        saveSystem.SaveGame();
        replacementGas.transform.position = new Vector3(100f, 20f, 100f);
        Assert.That(saveSystem.LoadGame(), Is.True);

        Assert.That(Vector3.Distance(replacementGas.transform.position, savedSpawnPosition), Is.LessThan(0.001f));
        Assert.That(FindActiveGas(), Is.SameAs(replacementGas), "Loading must retain the saved Gas instance, not choose another spawn point.");
    }

    private SaveSystem CreateSaveSystem(bool hardMode)
    {
        PlayerPrefs.SetString("GameDifficulty", hardMode ? "Hard" : "Normal");
        GameObject managerObject = CreateObject("Integration Test Save System");
        SaveSystem saveSystem = managerObject.AddComponent<SaveSystem>();
        saveSystem.autoSaveOnQuit = false;
        SetPrivateField(saveSystem, "savePath", CreateTemporaryDatabasePath());
        return saveSystem;
    }

    private string CreateTemporaryDatabasePath()
    {
        string directory = Path.Combine(Application.temporaryCachePath, "VarenPlayModeTests");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, testId + "_" + temporaryDatabasePaths.Count + ".db");
        temporaryDatabasePaths.Add(path);
        return path;
    }

    private Inventory CreateInventory()
    {
        return CreateObject("Integration Test Inventory").AddComponent<Inventory>();
    }

    private GameObject CreatePlayer()
    {
        GameObject player = CreateObject("Player");
        player.tag = "Player";
        return player;
    }

    private Camera CreateMainCamera()
    {
        GameObject cameraObject = CreateObject("Integration Test Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private GameObject CreateObject(string name, params Type[] componentTypes)
    {
        return componentTypes != null && componentTypes.Length > 0
            ? new GameObject(name, componentTypes)
            : new GameObject(name);
    }

    private GameObject CreateGas(string name, Vector3 position)
    {
        GameObject gas = CreateObject(name);
        gas.transform.position = position;
        PickupItem pickup = gas.AddComponent<PickupItem>();
        pickup.itemName = "Gas";
        gas.AddComponent<BoxCollider>();
        return gas;
    }

    private GeneratorFixture CreateGeneratorFixture(GameObject gasPrefab, bool respawnGas)
    {
        GameObject generatorObject = CreateObject("Integration Test Generator");
        generatorObject.SetActive(false);
        generatorObject.AddComponent<BoxCollider>();
        GeneratorFuelInteraction fuel = generatorObject.AddComponent<GeneratorFuelInteraction>();
        GeneratorKeySlot slot = generatorObject.AddComponent<GeneratorKeySlot>();
        slot.fuelInteraction = fuel;
        slot.respawnGasWhenLightsTurnOff = respawnGas;
        slot.gasPrefab = gasPrefab;

        GameObject keyPoint = CreateObject("Integration Test Key Insert Point");
        keyPoint.transform.SetParent(generatorObject.transform, false);
        slot.keyInsertPoint = keyPoint.transform;

        GameObject lightObject = CreateObject("Integration Test Generator Light");
        slot.generatorLight1 = lightObject.AddComponent<Light>();
        generatorObject.SetActive(true);
        return new GeneratorFixture(fuel, slot, keyPoint.transform);
    }

    private GameObject CreateQnAPanel(out Button correctAnswerButton)
    {
        GameObject panel = CreateObject("Integration Test QnAPanel");
        panel.SetActive(false);
        correctAnswerButton = null;
        string[] optionNames = { "OptionA", "OptionB", "OptionC", "OptionD" };
        for (int index = 0; index < optionNames.Length; index++)
        {
            GameObject option = CreateObject(optionNames[index], typeof(RectTransform));
            option.transform.SetParent(panel.transform, false);
            GameObject buttonObject = CreateObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            buttonObject.transform.SetParent(option.transform, false);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            if (index == 0)
                correctAnswerButton = button;
        }
        return panel;
    }

    private void CreateNoteCanvas()
    {
        GameObject canvasObject = CreateObject("Integration Test Note Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private PickupItem FindActiveGas()
    {
        foreach (PickupItem pickup in Resources.FindObjectsOfTypeAll<PickupItem>())
        {
            if (pickup != null && pickup.enabled && pickup.gameObject.activeInHierarchy &&
                pickup.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene() &&
                string.Equals(pickup.itemName, "Gas", StringComparison.OrdinalIgnoreCase))
                return pickup;
        }
        return null;
    }

    private static object InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, InstancePrivate);
        Assert.That(method, Is.Not.Null, "Could not find private method " + methodName + ".");
        return method.Invoke(target, arguments);
    }

    private static object InvokePrivateStatic(Type type, string methodName, params object[] arguments)
    {
        MethodInfo method = type.GetMethod(methodName, StaticPrivate);
        Assert.That(method, Is.Not.Null, "Could not find private static method " + methodName + ".");
        return method.Invoke(null, arguments);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
        Assert.That(field, Is.Not.Null, "Could not find private field " + fieldName + ".");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
        Assert.That(field, Is.Not.Null, "Could not find private field " + fieldName + ".");
        field.SetValue(target, value);
    }

    private void CaptureAndClearStatic(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, StaticAny);
        if (field == null)
            return;

        savedStaticFields[field] = field.GetValue(null);
        field.SetValue(null, field.FieldType.IsValueType
            ? Activator.CreateInstance(field.FieldType)
            : null);
    }

    private void CapturePreference(string key, PreferenceKind kind)
    {
        if (savedPreferences.ContainsKey(key))
            return;

        bool exists = PlayerPrefs.HasKey(key);
        object value = null;
        if (exists)
        {
            switch (kind)
            {
                case PreferenceKind.String:
                    value = PlayerPrefs.GetString(key);
                    break;
                case PreferenceKind.Int:
                    value = PlayerPrefs.GetInt(key);
                    break;
                case PreferenceKind.Float:
                    value = PlayerPrefs.GetFloat(key);
                    break;
            }
        }
        savedPreferences[key] = new SavedPreference(exists, kind, value);
    }

    private void CaptureExistingObjectivePreferences()
    {
        string registeredIds = PlayerPrefs.GetString("ObjectiveTriggerKeys", string.Empty);
        if (string.IsNullOrEmpty(registeredIds))
            return;

        foreach (string id in registeredIds.Split('|'))
        {
            if (string.IsNullOrEmpty(id))
                continue;
            SnapshotObjectivePreference(id, "Normal");
            SnapshotObjectivePreference(id, "Hard");
        }
    }

    private void SnapshotObjectivePreference(string id, string difficulty)
    {
        CapturePreference("ObjectiveTrigger_" + difficulty + "_" + id, PreferenceKind.Int);
    }

    private static void DeleteTemporaryDatabase(string databasePath)
    {
        string[] relatedFiles = { databasePath, databasePath + "-journal", databasePath + "-wal", databasePath + "-shm" };
        foreach (string path in relatedFiles)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void ClearMenuLocalizationCache()
    {
        FieldInfo cacheField = typeof(MenuLocalization).GetField("originals", StaticAny);
        if (cacheField != null && cacheField.GetValue(null) is System.Collections.IDictionary cache)
            cache.Clear();
    }

    private enum PreferenceKind
    {
        String,
        Int,
        Float
    }

    private sealed class SavedPreference
    {
        private readonly bool exists;
        private readonly PreferenceKind kind;
        private readonly object value;

        public SavedPreference(bool exists, PreferenceKind kind, object value)
        {
            this.exists = exists;
            this.kind = kind;
            this.value = value;
        }

        public void Restore(string key)
        {
            if (!exists)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            switch (kind)
            {
                case PreferenceKind.String:
                    PlayerPrefs.SetString(key, (string)value);
                    break;
                case PreferenceKind.Int:
                    PlayerPrefs.SetInt(key, (int)value);
                    break;
                case PreferenceKind.Float:
                    PlayerPrefs.SetFloat(key, (float)value);
                    break;
            }
        }
    }

    private readonly struct GeneratorFixture
    {
        public readonly GeneratorFuelInteraction Fuel;
        public readonly GeneratorKeySlot Slot;
        public readonly Transform KeyInsertPoint;

        public GeneratorFixture(GeneratorFuelInteraction fuel, GeneratorKeySlot slot, Transform keyInsertPoint)
        {
            Fuel = fuel;
            Slot = slot;
            KeyInsertPoint = keyInsertPoint;
        }
    }
}
