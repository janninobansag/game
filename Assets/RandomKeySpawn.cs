using UnityEngine;

/// <summary>
/// Places this key at one randomly chosen spawn point for each new game.
/// Attach it to the Mansion key and assign the available spawn transforms.
/// </summary>
public class RandomKeySpawn : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Assign the three Mansion key spawn transforms here.")]
    public Transform[] spawnPoints;
    [Tooltip("A unique name for this random item. Leave this as MansionKeySpawnIndex.")]
    public string saveKey = "MansionKeySpawnIndex";
    public bool useSpawnRotation = true;

    [Header("Debug")]
    public bool logSpawnChoice = true;

    private void Awake()
    {
        PlaceAtSavedOrRandomSpawn();
    }

    private void PlaceAtSavedOrRandomSpawn()
    {
        int validCount = 0;
        if (spawnPoints != null)
        {
            foreach (Transform point in spawnPoints)
                if (point != null) validCount++;
        }

        if (validCount == 0)
        {
            return;
        }

        string profileKey = GetProfileSaveKey();
        int index = PlayerPrefs.GetInt(profileKey, -1);
        if (index < 0 || index >= spawnPoints.Length || spawnPoints[index] == null)
        {
            index = GetRandomValidIndex();
            PlayerPrefs.SetInt(profileKey, index);
            PlayerPrefs.Save();
        }

        Transform pointToUse = spawnPoints[index];
        transform.position = pointToUse.position;
        if (useSpawnRotation)
            transform.rotation = pointToUse.rotation;
    }

    private int GetRandomValidIndex()
    {
        int[] validIndices = new int[spawnPoints.Length];
        int count = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            validIndices[count++] = i;
        }
        return validIndices[Random.Range(0, count)];
    }

    private string GetProfileSaveKey()
    {
        string difficulty = PlayerPrefs.GetString("GameDifficulty", "Normal");
        return saveKey + "_" + difficulty;
    }
}