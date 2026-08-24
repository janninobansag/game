using UnityEngine;
using System.Collections.Generic;

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance;

    [Header("Item Prefabs")]
    public GameObject batteryPrefab;
    public GameObject bedroomKeyPrefab;
    public GameObject biblePrefab;
    public GameObject churchKeyPrefab;
    public GameObject crossPrefab;
    public GameObject flashlightPrefab;
    public GameObject house2KeyPrefab;
    public GameObject house2CRKeyPrefab;
    public GameObject house3KeyPrefab;
    public GameObject house1KeyPrefab;
    public GameObject largeCandlePrefab;

    [Header("Legacy/Backward Compatible")]
    public GameObject keyPrefab;
    public GameObject candlePrefab;
    public GameObject notePrefab;

    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        BuildPrefabDictionary();
    }

    void BuildPrefabDictionary()
    {
        prefabDictionary.Clear();

        AddPrefab(batteryPrefab, "battery");
        AddPrefab(bedroomKeyPrefab, "Bedroom key");
        AddPrefab(biblePrefab, "Bible");
        AddPrefab(churchKeyPrefab, "Church key");
        AddPrefab(crossPrefab, "Cross");
        AddPrefab(flashlightPrefab, "Flashlight");
        AddPrefab(house2KeyPrefab, "House 2 key");
        AddPrefab(house2CRKeyPrefab, "House 2 cr key");
        AddPrefab(house3KeyPrefab, "House 3 key");
        AddPrefab(house1KeyPrefab, "Key house 1");
        AddPrefab(largeCandlePrefab, "LargeCandle");

        AddPrefab(keyPrefab, "Key");
        AddPrefab(candlePrefab, "LargeCandle");
        AddPrefab(notePrefab, "Note");

        AddPrefab(bedroomKeyPrefab, "BedroomKey");
        AddPrefab(churchKeyPrefab, "ChurchKey");
        AddPrefab(house2KeyPrefab, "House2Key");
        AddPrefab(house2CRKeyPrefab, "House2CRKey");
        AddPrefab(house3KeyPrefab, "House3Key");
        AddPrefab(house1KeyPrefab, "KeyHouse1");
    }

    void AddPrefab(GameObject prefab, string name)
    {
        if (prefab == null) return;
        
        string key = name.ToLower();
        if (!prefabDictionary.ContainsKey(key))
        {
            prefabDictionary.Add(key, prefab);
        }
    }

    public GameObject GetPrefab(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return null;
        }

        string lowerName = itemName.ToLower();

        if (prefabDictionary.ContainsKey(lowerName))
        {
            return prefabDictionary[lowerName];
        }

        string noSpaces = lowerName.Replace(" ", "");
        if (prefabDictionary.ContainsKey(noSpaces))
        {
            return prefabDictionary[noSpaces];
        }

        foreach (KeyValuePair<string, GameObject> entry in prefabDictionary)
        {
            if (lowerName.Contains(entry.Key))
            {
                return entry.Value;
            }
            if (entry.Key.Contains(lowerName))
            {
                return entry.Value;
            }
        }

        if (lowerName.Contains("key"))
        {
            foreach (KeyValuePair<string, GameObject> entry in prefabDictionary)
            {
                if (entry.Key.Contains("key") && entry.Value != null)
                {
                    return entry.Value;
                }
            }
        }

        if (lowerName.Contains("candle") || lowerName.Contains("largecandle"))
        {
            foreach (KeyValuePair<string, GameObject> entry in prefabDictionary)
            {
                if (entry.Key.Contains("candle") && entry.Value != null)
                {
                    return entry.Value;
                }
            }
        }
        return null;
    }

    public GameObject SpawnDroppedItem(string itemName, Vector3 position, Quaternion rotation, float batteryValue = -1f)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return null;
        }

        GameObject prefab = GetPrefab(itemName);
        if (prefab != null)
        {
            GameObject newItem = Instantiate(prefab, position, rotation);
            newItem.name = itemName;

            // ── Mark flashlight as spawned from prefab AND set battery ──
            FlashlightPickup flashlight = newItem.GetComponent<FlashlightPickup>();
            if (flashlight != null)
            {
                flashlight.isSpawnedFromPrefab = true;
                if (batteryValue >= 0f)
                {
                    // ── Set battery before Start() runs ──
                    flashlight.SetBattery(batteryValue);
                }
                else
                {
                    flashlight.SetBattery(flashlight.batteryLife);
                }
            }

            PickupItem pickup = newItem.GetComponent<PickupItem>();
            if (pickup != null)
            {
                pickup.isPickedUp = false;
            }

            foreach (Collider c in newItem.GetComponentsInChildren<Collider>())
                c.enabled = true;
            foreach (Renderer r in newItem.GetComponentsInChildren<Renderer>())
                r.enabled = true;

            Rigidbody rb = newItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            return newItem;
        }
        return null;
    }

    public void RegisterPrefab(GameObject prefab, string name)
    {
        AddPrefab(prefab, name);
    }

    public bool HasPrefab(string itemName)
    {
        return GetPrefab(itemName) != null;
    }
}