using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// All tree instances share the same supply, including bananas in the inventory.
public static class BananaHarvestCycle
{
    public const float RegrowthSeconds = 30f;

    private static readonly HashSet<BananaUse> remainingFruit = new HashSet<BananaUse>();
    private static bool spawningFruit;
    private static float readyAt;

    public static int RemainingFruitCount => remainingFruit.Count;
    public static float SecondsUntilReady => Mathf.Max(0f, readyAt - Time.time);
    public static bool CanHarvest => !spawningFruit && remainingFruit.Count == 0 && Time.time >= readyAt;

    public static void RegisterFruit(BananaUse fruit)
    {
        if (fruit != null && !fruit.IsConsumed)
            remainingFruit.Add(fruit);
    }

    public static void RemoveFruit(BananaUse fruit)
    {
        // Eating followed by OnDestroy must not restart the cooldown twice.
        if (remainingFruit.Remove(fruit) && remainingFruit.Count == 0)
            readyAt = Time.time + RegrowthSeconds;
    }

    public static bool TryBeginHarvest()
    {
        if (!CanHarvest)
            return false;
        spawningFruit = true;
        return true;
    }

    public static void FinishHarvest()
    {
        spawningFruit = false;
        if (remainingFruit.Count == 0)
            readyAt = Time.time + RegrowthSeconds;
    }

    internal static void BeginRestore()
    {
        remainingFruit.Clear();
        spawningFruit = true;
        readyAt = 0f;
    }

    internal static void RestoreCooldown(float secondsRemaining)
    {
        spawningFruit = false;
        readyAt = Time.time + Mathf.Clamp(secondsRemaining, 0f, RegrowthSeconds);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        ResetCycle();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        ResetCycle();
    }

    private static void ResetCycle()
    {
        remainingFruit.Clear();
        spawningFruit = false;
        readyAt = 0f;
    }
}
