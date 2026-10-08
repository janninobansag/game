using UnityEngine;

/// <summary>
/// Places the scene's Wrench at one randomly selected spawn point for a new game.
/// The selected point is remembered for the current difficulty until New Game is started.
/// Attach this component to the Wrench object and assign its possible spawn points.
/// </summary>
[DisallowMultipleComponent]
public class RandomWrenchSpawn : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Assign each possible Wrench spawn transform here.")]
    public Transform[] spawnPoints;

    [Tooltip("Keep this unique so it does not share a location choice with another item.")]
    public string saveKey = "WrenchSpawnIndex";

    [Tooltip("Use the selected spawn point's rotation as well as its position.")]
    public bool useSpawnRotation = true;

    [Header("Testing")]
    [Tooltip("When checked, the Wrench stays at its scene position and does not choose a random spawn point.")]
    public bool disableRandomSpawn;

    [Header("Debug")]
    public bool logSpawnChoice = true;

    private void Awake()
    {
        if (disableRandomSpawn)
            return;

        PlaceAtSavedOrRandomSpawn();
    }

    private void PlaceAtSavedOrRandomSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            if (logSpawnChoice)
                Debug.LogWarning("RandomWrenchSpawn has no spawn points assigned.", this);
            return;
        }

        string profileKey = saveKey + "_" + PlayerPrefs.GetString("GameDifficulty", "Normal");
        int index = PlayerPrefs.GetInt(profileKey, -1);

        if (index < 0 || index >= spawnPoints.Length || spawnPoints[index] == null)
        {
            index = GetRandomValidIndex();
            if (index < 0)
            {
                if (logSpawnChoice)
                    Debug.LogWarning("RandomWrenchSpawn has no valid spawn transforms assigned.", this);
                return;
            }

            PlayerPrefs.SetInt(profileKey, index);
            PlayerPrefs.Save();
        }

        Transform selectedSpawn = spawnPoints[index];
        DrawerInteraction drawer = selectedSpawn.GetComponentInParent<DrawerInteraction>();
        if (drawer != null)
        {
            // Keep the wrench in the drawer's local space so it moves with the drawer.
            transform.SetParent(drawer.transform, true);
        }

        transform.position = selectedSpawn.position;
        if (useSpawnRotation)
            transform.rotation = selectedSpawn.rotation;

        if (logSpawnChoice)
            Debug.Log("Wrench spawned at point '" + selectedSpawn.name + "'.", this);
    }

    private int GetRandomValidIndex()
    {
        int validCount = 0;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                validCount++;
        }

        if (validCount == 0)
            return -1;

        int choice = Random.Range(0, validCount);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                continue;

            if (choice-- == 0)
                return i;
        }

        return -1;
    }
}
