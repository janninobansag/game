#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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

            InvokePrivate(whiteLady, "WireQnAButtons");
            InvokePrivate(whiteLady, "RestoreQnAInput");

            Assert.That(buttons.All(button => button.interactable), Is.True);
            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);
        }
        finally
        {
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

    private static void AssertMissingProperties(Type type, params string[] propertyNames)
    {
        string[] actualProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();

        foreach (string propertyName in propertyNames)
            Assert.That(actualProperties, Does.Not.Contain(propertyName), type.Name + " must store dropped transforms in DroppedItemData.");
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Could not find " + methodName + " on " + target.GetType().Name + ".");
        method.Invoke(target, null);
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
