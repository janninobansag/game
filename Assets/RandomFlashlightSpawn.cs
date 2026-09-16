using UnityEngine;

/// <summary>
/// Places the Chapter 2 flashlight at one randomly selected spawn point for a new game.
/// The selected point is remembered for the current difficulty until a New Game is started.
/// Attach this component to the scene's Flashlight object, then assign its spawn points.
/// </summary>
[DisallowMultipleComponent]
public class RandomFlashlightSpawn : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Assign each possible flashlight spawn transform here.")]
    public Transform[] spawnPoints;

    [Tooltip("Keep this unique so it does not share a location choice with another item.")]
    public string saveKey = "FlashlightSpawnIndex";

    [Tooltip("Uses the selected spawn point's rotation as well as its position.")]
    public bool useSpawnRotation = true;

    [Header("Debug")]
    public bool logSpawnChoice = true;

    private void Awake()
    {
        PlaceAtSavedOrRandomSpawn();
    }

    private void PlaceAtSavedOrRandomSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Random Flashlight Spawn] No spawn points are assigned for " + name + ".", this);
            return;
        }

        string profileKey = saveKey + "_" + PlayerPrefs.GetString("GameDifficulty", "Normal");
        int index = PlayerPrefs.GetInt(profileKey, -1);

        if (index < 0 || index >= spawnPoints.Length || spawnPoints[index] == null)
        {
            index = GetRandomValidIndex();
            if (index < 0)
            {
                Debug.LogWarning("[Random Flashlight Spawn] All spawn-point entries are empty for " + name + ".", this);
                return;
            }

            PlayerPrefs.SetInt(profileKey, index);
            PlayerPrefs.Save();

            if (logSpawnChoice)
                Debug.Log("[Random Flashlight Spawn] New game chose " + spawnPoints[index].name + " for " + name + ".", this);
        }
        else if (logSpawnChoice)
        {
            Debug.Log("[Random Flashlight Spawn] Restored " + spawnPoints[index].name + " for " + name + ".", this);
        }

        Transform selectedSpawn = spawnPoints[index];
        transform.position = selectedSpawn.position;
        if (useSpawnRotation)
            transform.rotation = selectedSpawn.rotation;
    }

    private int GetRandomValidIndex()
    {
        int validCount = 0;
        foreach (Transform point in spawnPoints)
            if (point != null) validCount++;

        if (validCount == 0)
            return -1;

        int choice = Random.Range(0, validCount);
        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            if (choice-- == 0)
                return System.Array.IndexOf(spawnPoints, point);
        }

        return -1;
    }
}