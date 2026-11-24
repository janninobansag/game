using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SaveSystem
{
    private sealed class BananaLoadState
    {
        public BananaHarvestData Harvest;
        public List<BananaFruitData> Fruit;
        public GameObject Prefab;
        public Dictionary<string, DroppedItemData> Drops;
        public bool Completed;
    }

    private void SaveBananaState()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Inventory inventory = Inventory.Instance;
        List<GameObject> inventoryItems = inventory != null ? inventory.GetItems() : new List<GameObject>();
        List<DroppedItemData> drops = new List<DroppedItemData>();
        List<BananaFruitData> fruitRows = new List<BananaFruitData>();
        foreach (BananaUse banana in UnityEngine.Object.FindObjectsOfType<BananaUse>(true))
        {
            if (banana.IsConsumed || banana.gameObject.scene.name != sceneName)
                continue;
            Transform fruit = banana.transform;
            Vector3 scale = fruit.lossyScale;
            int inventoryIndex = inventoryItems.IndexOf(banana.gameObject);
            if (inventoryIndex < 0)
            {
                DroppedItemData drop = CreateDroppedItemData(banana.gameObject, "Banana");
                drop.BananaFruitId = banana.PersistentId;
                drops.Add(drop);
            }
            fruitRows.Add(new BananaFruitData
            {
                FruitId = banana.PersistentId,
                SceneName = sceneName,
                InventoryIndex = inventoryIndex,
                HasBeenPickedUp = banana.HasBeenPickedUp || inventoryIndex >= 0,
                UncollectedSecondsRemaining = banana.UncollectedSecondsRemaining,
                IsActive = banana.gameObject.activeSelf,
                HealingAmount = banana.healingAmount,
                ScaleX = scale.x, ScaleY = scale.y, ScaleZ = scale.z
            });
        }

        // An empty snapshot is significant: eaten scene fruit must not reappear.
        connection.RunInTransaction(() =>
        {
            connection.Execute("DELETE FROM BananaFruitData WHERE SceneName = ?", sceneName);
            connection.Execute("DELETE FROM BananaHarvestData WHERE SceneName = ?", sceneName);
            foreach (DroppedItemData drop in drops)
                connection.Insert(drop);
            foreach (BananaFruitData fruit in fruitRows)
                connection.Insert(fruit);
            connection.Insert(new BananaHarvestData
            {
                SceneName = sceneName,
                CooldownRemaining = BananaHarvestCycle.SecondsUntilReady,
                SelectedInventoryIndex = inventory != null ? inventory.GetSelectedIndex() : -1
            });
        });
    }

    private BananaLoadState PrepareBananaRestore()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        BananaHarvestData harvest = connection.Table<BananaHarvestData>().FirstOrDefault(row => row.SceneName == sceneName);
        if (harvest == null)
            return null; // Version 8 saves keep their scene's initial banana setup.

        List<BananaFruitData> fruit = connection.Table<BananaFruitData>().Where(row => row.SceneName == sceneName).ToList();
        GameObject prefab = null;
        if (fruit.Count > 0)
        {
            BananaTreeInteraction tree = UnityEngine.Object.FindObjectsOfType<BananaTreeInteraction>(true)
                .FirstOrDefault(candidate => candidate.gameObject.scene.name == sceneName &&
                    candidate.fruitPrefab != null && candidate.fruitPrefab.GetComponent<BananaUse>() != null);
            prefab = tree != null ? tree.fruitPrefab : null;
            if (prefab == null)
                throw new InvalidOperationException("Cannot restore bananas: assign the Banana Fruit prefab to a tree in " + sceneName + ".");
        }

        Dictionary<string, DroppedItemData> drops = connection.Table<DroppedItemData>().ToList()
            .Where(row => !string.IsNullOrEmpty(row.BananaFruitId))
            .ToDictionary(row => row.BananaFruitId);
        foreach (BananaFruitData row in fruit)
        {
            if (row.InventoryIndex < 0 && !drops.ContainsKey(row.FruitId))
                throw new InvalidOperationException("Missing dropped position for banana " + row.FruitId);
        }
        BananaLoadState state = new BananaLoadState { Harvest = harvest, Fruit = fruit, Prefab = prefab, Drops = drops };
        BananaHarvestCycle.BeginRestore();
        foreach (BananaUse banana in UnityEngine.Object.FindObjectsOfType<BananaUse>(true))
        {
            if (banana.gameObject.scene.name != sceneName)
                continue;
            if (Inventory.Instance != null)
                Inventory.Instance.RemoveWithoutDestroy(banana.gameObject);
            banana.RetireForRestore();
            banana.gameObject.SetActive(false);
            Destroy(banana.gameObject);
        }
        return state;
    }

    private void RestoreBananaState(BananaLoadState state)
    {
        if (state == null)
            return;
        Inventory inventory = Inventory.Instance;
        List<KeyValuePair<int, GameObject>> carriedFruit = new List<KeyValuePair<int, GameObject>>();
        foreach (BananaFruitData row in state.Fruit)
        {
            DroppedItemData drop;
            state.Drops.TryGetValue(row.FruitId, out drop);
            GameObject fruit = Instantiate(state.Prefab,
                drop != null ? new Vector3(drop.PosX, drop.PosY, drop.PosZ) : Vector3.zero,
                drop != null ? new Quaternion(drop.RotX, drop.RotY, drop.RotZ, drop.RotW) : Quaternion.identity);
            fruit.transform.localScale = new Vector3(row.ScaleX, row.ScaleY, row.ScaleZ);
            BananaUse banana = fruit.GetComponent<BananaUse>();
            banana.RestorePersistentId(row.FruitId);
            banana.healingAmount = row.HealingAmount;
            banana.RestoreCollectionState(row.HasBeenPickedUp || row.InventoryIndex >= 0, row.UncollectedSecondsRemaining);
            BananaHarvestCycle.RegisterFruit(banana);
            PickupItem pickup = fruit.GetComponent<PickupItem>();

            if (row.InventoryIndex >= 0 && inventory != null && inventory.AddItem(fruit))
            {
                carriedFruit.Add(new KeyValuePair<int, GameObject>(row.InventoryIndex, fruit));
                foreach (Collider collider in fruit.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            }
            else if (pickup != null)
            {
                pickup.isPickedUp = false;
                pickup.isHeld = false;
                pickup.wasDropped = true;
                fruit.SetActive(row.IsActive);
                if (inventory != null)
                    inventory.RestoreDropLight(fruit);
            }
        }

        if (inventory != null && inventory.GetCount() > 0)
        {
            List<GameObject> orderedItems = inventory.GetItems();
            foreach (KeyValuePair<int, GameObject> item in carriedFruit)
                orderedItems.Remove(item.Value);
            foreach (KeyValuePair<int, GameObject> item in carriedFruit.OrderBy(pair => pair.Key))
                orderedItems.Insert(Mathf.Clamp(item.Key, 0, orderedItems.Count), item.Value);

            foreach (GameObject item in orderedItems)
                inventory.SetItemHeld(item, false);
            int selected = Mathf.Clamp(state.Harvest.SelectedInventoryIndex, 0, orderedItems.Count - 1);
            inventory.SelectItem(selected);
            // SelectItem intentionally does nothing if this slot was already selected.
            inventory.SetItemHeld(orderedItems[selected], true);
        }

        BananaHarvestCycle.RestoreCooldown(state.Harvest.CooldownRemaining);
        state.Completed = true;
    }
}
