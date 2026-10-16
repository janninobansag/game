#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VarenSaveAndGameplayTests
{
    [Test]
    public void SaveSchema_UsesVersionEight()
    {
        Assert.That(RelationalSaveSchema.SchemaVersion, Is.EqualTo(8));
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
