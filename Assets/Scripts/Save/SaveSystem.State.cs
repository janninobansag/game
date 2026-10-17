// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
using System.IO;
using System.Linq;
using UnityEngine;
public partial class SaveSystem : MonoBehaviour
{
    public static string GetOneShotStateKey(Component source, string eventType, string saveId)
    {
        string identity = string.IsNullOrWhiteSpace(saveId) ? string.Empty : saveId.Trim();
        if (identity.Length == 0)
        {
            Transform current = source.transform;
            while (current != null)
            {
                identity = current.name + "[" + current.GetSiblingIndex() + "]/" + identity;
                current = current.parent;
            }
        }

        return "OneShot:" + eventType + ":" + source.gameObject.scene.path + ":" + identity;
    }

    public void ClearOneShotEventStates()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady)
            return;

        connection.DeleteAll<NoteData>();
        foreach (GameStateData state in connection.Table<GameStateData>().ToList())
        {
            if (state.Key != null && state.Key.StartsWith("OneShot:", System.StringComparison.Ordinal))
                connection.Delete(state);
        }
    }

    public void MarkOneShotEventCompleted(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        EnsureDatabaseReady();
        if (!isDatabaseReady)
            return;

        GameStateData savedState = connection.Table<GameStateData>()
            .FirstOrDefault(state => state.Key == key);
        if (savedState == null)
            connection.Insert(new GameStateData { Key = key, Value = "true" });
        else if (savedState.Value != "true")
        {
            savedState.Value = "true";
            connection.Update(savedState);
        }
    }

    public void MarkGeneratorCoverRemoved(string coverId)
    {
        if (string.IsNullOrEmpty(coverId))
            return;


        EnsureDatabaseReady();
        if (!isDatabaseReady)
        {
            Debug.LogWarning("[GeneratorCover] Could not save '" + coverId + "': save database is unavailable.");
            return;
        }

        try
        {
            connection.InsertOrReplace(new GeneratorCoverData
            {
                CoverId = coverId,
                IsRemoved = true
            });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GeneratorCover] Could not save '" + coverId + "': " + e.Message);
        }
    }

    public bool IsGeneratorCoverRemoved(string coverId)
    {
        if (string.IsNullOrEmpty(coverId))
            return false;


        EnsureDatabaseReady();
        if (!isDatabaseReady)
            return false;

        try
        {
            GeneratorCoverData data = connection.Table<GeneratorCoverData>()
                .Where(item => item.CoverId == coverId)
                .FirstOrDefault();
            return data != null && data.IsRemoved;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
    public void ClearGeneratorState(string saveId)
    {
        if (string.IsNullOrEmpty(saveId)) return;
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;
        try
        {
            GeneratorCoverData data = connection.Table<GeneratorCoverData>().Where(item => item.CoverId == saveId).FirstOrDefault();
            if (data != null) connection.Delete(data);
        }
        catch (System.Exception) { }
    }
    public void MarkKeyAsUsed(string keyName)
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = keyName.Replace("(Clone)", "");

            var existing = connection.Table<KeyData>()
                .Where(k => k.KeyName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                KeyData keyData = new KeyData
                {
                    KeyName = cleanName,
                    WasUsed = true
                };
                connection.Insert(keyData);
            }
            else
            {
                existing.WasUsed = true;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkBatteryAsUsed(string batteryName)
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = batteryName.Replace("(Clone)", "");

            var existing = connection.Table<BatteryData>()
                .Where(b => b.BatteryName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                BatteryData newBatteryData = new BatteryData
                {
                    BatteryName = cleanName,
                    RechargeAmount = 50f,
                    IsHeld = false,
                    IsUsed = true
                };
                connection.Insert(newBatteryData);
            }
            else
            {
                existing.IsUsed = true;
                existing.IsHeld = false;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkRitualItemAsRevealed(string itemName)
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = itemName.Replace("(Clone)", "");

            var existing = connection.Table<RitualItemData>()
                .Where(r => r.ItemName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                RitualItemData newItem = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = true,
                    IsPlaced = false
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkRitualItemAsPlaced(string itemName, Vector3 position, Quaternion rotation)
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = itemName.Replace("(Clone)", "");

            var existing = connection.Table<RitualItemData>()
                .Where(r => r.ItemName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                RitualItemData newItem = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = true,
                    IsPlaced = true
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                existing.IsPlaced = true;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void MarkRitualItemAsDropped(string itemName, Vector3 position, Quaternion rotation)
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            string cleanName = itemName.Replace("(Clone)", "");

            var existing = connection.Table<RitualItemData>()
                .Where(r => r.ItemName == cleanName).FirstOrDefault();

            if (existing == null)
            {
                RitualItemData newItem = new RitualItemData
                {
                    ItemName = cleanName,
                    IsRevealed = true,
                    IsPlaced = false
                };
                connection.Insert(newItem);
            }
            else
            {
                existing.IsRevealed = true;
                existing.IsPlaced = false;
                connection.Update(existing);
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void ClearKeyData()
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            connection.DeleteAll<KeyData>();
        }
        catch (System.Exception e)
        {
        }
    }

    public void ClearBatteryData()
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            connection.DeleteAll<BatteryData>();
        }
        catch (System.Exception e)
        {
        }
    }

    public void ClearRitualItemData()
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
        {
            return;
        }

        try
        {
            connection.DeleteAll<RitualItemData>();
        }
        catch (System.Exception e)
        {
        }
    }
    public void ClearNewGameWorldState()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady)
            return;

        try
        {
            connection.DeleteAll<AIPositionData>();
            connection.DeleteAll<DroppedItemData>();

            if (IsHardModeDatabase())
            {
                connection.DeleteAll<WrenchData>();
                connection.DeleteAll<GeneratorCoverData>();
                connection.DeleteAll<GasData>();
            }
            connection.DeleteAll<GeneratorCoverData>();
        }
        catch (System.Exception)
        {
        }
    }
    public void ClearProgressionData()
    {
        EnsureDatabaseReady();

        if (!isDatabaseReady)
            return;

        try
        {
            connection.DeleteAll<ProgressionData>();
        }
        catch (System.Exception e)
        {
        }
    }

    public bool TryGetStoryIntroProgress(out int sectionIndex, out int lineIndex, out bool isComplete)
    {
        sectionIndex = 0;
        lineIndex = 0;
        isComplete = false;

        EnsureDatabaseReady();
        if (!isDatabaseReady) return false;

        try
        {
            IntroData data = connection.Table<IntroData>().FirstOrDefault();
            if (data == null) return false;

            sectionIndex = data.SectionIndex;
            lineIndex = data.LineIndex;
            isComplete = data.IsComplete;
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    public void SaveStoryIntroProgress(int sectionIndex, int lineIndex, bool isComplete)
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try
        {
            connection.InsertOrReplace(new IntroData
            {
                Id = 1,
                SectionIndex = sectionIndex,
                LineIndex = lineIndex,
                IsComplete = isComplete
            });
        }
        catch (System.Exception)
        {
        }
    }
    public void ClearStoryIntroData()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try { connection.DeleteAll<IntroData>(); }
        catch (System.Exception) { }
    }

    public void ClearSubtitleData()
    {
        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try { connection.DeleteAll<SubtitleData>(); }
        catch (System.Exception) { }
    }
    public bool HasSubtitleTriggered(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId)) return false;


        EnsureDatabaseReady();
        if (!isDatabaseReady) return false;

        try
        {
            SubtitleData data = connection.Find<SubtitleData>(subtitleId);
            return data != null && data.IsTriggered;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    public void MarkSubtitleTriggered(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId)) return;

        EnsureDatabaseReady();
        if (!isDatabaseReady) return;

        try
        {
            connection.InsertOrReplace(new SubtitleData { SubtitleId = subtitleId, IsTriggered = true });
        }
        catch (System.Exception)
        {
        }
    }
    public bool HasSaveFile()
    {
        // ── Check if the database file exists without creating it ──
        return File.Exists(savePath);
    }

    public void DeleteSave()
    {
        // Delete the current save database and clear memory state for a new game.
        if (!isDatabaseReady) return;

        try
        {
            connection.DeleteAll<PlayerData>();
            connection.DeleteAll<AIPositionData>();
            if (IsHardModeDatabase())
            {
                connection.DeleteAll<WrenchData>();
                connection.DeleteAll<GeneratorCoverData>();
                connection.DeleteAll<GasData>();
            }

            connection.DeleteAll<InventoryData>();
            connection.DeleteAll<DoorData>();
            connection.DeleteAll<DrawerData>();
            connection.DeleteAll<RitualData>();
            connection.DeleteAll<NoteData>();
            connection.DeleteAll<GameStateData>();
            connection.DeleteAll<DroppedItemData>();
            connection.DeleteAll<FlashlightData>();
            connection.DeleteAll<KeyData>();
            connection.DeleteAll<BatteryData>();
            connection.DeleteAll<RitualItemData>();
            connection.DeleteAll<StaminaData>();
            connection.DeleteAll<SubtitleData>();
            connection.DeleteAll<IntroData>();
            // ── NEW: Delete ProgressionData ──
            connection.DeleteAll<ProgressionData>();

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
        catch (System.Exception e)
        {
        }
    }
}
