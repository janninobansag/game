#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VarenSaveAndGameplayTests
{
    [Test]
    public void SaveSchema_UsesVersionSix()
    {
        Assert.That(RelationalSaveSchema.SchemaVersion, Is.EqualTo(6));
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
}
#endif
