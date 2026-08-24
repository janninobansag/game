using System.Collections;
using UnityEngine;

public class MutantRespawnTrigger : MonoBehaviour
{
    [Header("Mutant Settings")]
    public GameObject mutantPrefab;
    public Transform spawnPoint;
    public string playerTag = "Player";

    [Header("Trigger Settings")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;
    private GameObject spawnedMutant;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        SpawnMutant();
    }

    void SpawnMutant()
    {
        if (mutantPrefab == null || spawnPoint == null)
        {
            return;
        }

        // Destroy old mutant if exists
        if (spawnedMutant != null)
            Destroy(spawnedMutant);

        spawnedMutant = Instantiate(mutantPrefab,
            spawnPoint.position, spawnPoint.rotation);

    }
}