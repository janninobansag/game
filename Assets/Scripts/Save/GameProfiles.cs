using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Keeps the original save in its existing location and gives each new player
/// a separate directory for Normal and Hard saves.
/// </summary>
public static class GameProfiles
{
    public const string OriginalProfileId = "original";
    private const string ActiveProfileKey = "ActiveProfileId";
    private const string ProfilesFolder = "Profiles";
    private const string ProfileFile = "profile.json";
    private const string KnownKeysSuffix = "_KnownKeys";

    [Serializable]
    public sealed class Profile
    {
        public string id;
        public string name;
    }

    public static string ActiveId
    {
        get
        {
            string id = PlayerPrefs.GetString(ActiveProfileKey, OriginalProfileId);
            return IsKnownProfile(id) ? id : OriginalProfileId;
        }
    }

    public static string ActiveName
    {
        get
        {
            string activeId = ActiveId;
            foreach (Profile profile in GetProfiles())
            {
                if (profile.id == activeId)
                    return profile.name;
            }

            return "Original Save";
        }
    }

    public static List<Profile> GetProfiles()
    {
        var profiles = new List<Profile>
        {
            new Profile { id = OriginalProfileId, name = "Original Save" }
        };

        string root = Path.Combine(Application.persistentDataPath, ProfilesFolder);
        if (!Directory.Exists(root))
            return profiles;

        foreach (string directory in Directory.GetDirectories(root))
        {
            string id = Path.GetFileName(directory);
            if (!Guid.TryParseExact(id, "N", out _))
                continue;

            try
            {
                string metadataPath = Path.Combine(directory, ProfileFile);
                if (!File.Exists(metadataPath))
                    continue;

                Profile profile = JsonUtility.FromJson<Profile>(File.ReadAllText(metadataPath));
                if (profile != null && profile.id == id && !string.IsNullOrWhiteSpace(profile.name))
                    profiles.Add(profile);
            }
            catch (Exception)
            {
                // An invalid profile file must not hide the other profiles.
            }
        }

        profiles.Sort((left, right) =>
        {
            if (left.id == right.id) return 0;
            if (left.id == OriginalProfileId) return -1;
            if (right.id == OriginalProfileId) return 1;
            return StringComparer.OrdinalIgnoreCase.Compare(left.name, right.name);
        });
        return profiles;
    }

    public static bool TryCreate(string requestedName, out Profile created, out string error)
    {
        created = null;
        string name = requestedName == null ? "" : requestedName.Trim();
        if (name.Length == 0 || name.Length > 24)
        {
            error = "Enter a name from 1 to 24 characters.";
            return false;
        }

        foreach (Profile existing in GetProfiles())
        {
            if (string.Equals(existing.name, name, StringComparison.OrdinalIgnoreCase))
            {
                error = "That profile name is already in use.";
                return false;
            }
        }

        created = new Profile { id = Guid.NewGuid().ToString("N"), name = name };
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, ProfilesFolder, created.id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, ProfileFile), JsonUtility.ToJson(created));
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            created = null;
            error = "Could not create the profile: " + exception.Message;
            return false;
        }
    }

    public static bool Select(string id)
    {
        if (!IsKnownProfile(id))
            return false;

        PlayerPrefs.SetString(ActiveProfileKey, id);
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.Save();
        return true;
    }

    public static string GetSavePath(string fileName)
    {
        string root = ActiveId == OriginalProfileId
            ? Application.persistentDataPath
            : Path.Combine(Application.persistentDataPath, ProfilesFolder, ActiveId);
        return Path.Combine(root, fileName);
    }

    public static string ProgressKey(string key)
    {
        string id = ActiveId;
        if (id == OriginalProfileId)
            return key;

        string registry = "Profile_" + id + KnownKeysSuffix;
        string knownKeys = PlayerPrefs.GetString(registry, "");
        if (!Array.Exists(knownKeys.Split('|'), entry => entry == key))
            PlayerPrefs.SetString(registry, string.IsNullOrEmpty(knownKeys) ? key : knownKeys + "|" + key);

        return ProfileKey(id, key);
    }

    public static bool TryDelete(string id, out string error)
    {
        if (id == OriginalProfileId)
        {
            error = "Original Save cannot be deleted here.";
            return false;
        }

        if (id == ActiveId)
        {
            error = "Switch to another profile before deleting this one.";
            return false;
        }

        if (!IsKnownProfile(id))
        {
            error = "That profile no longer exists.";
            return false;
        }

        string profilesRoot = Path.GetFullPath(Path.Combine(Application.persistentDataPath, ProfilesFolder));
        string profileDirectory = Path.GetFullPath(Path.Combine(profilesRoot, id));
        if (!string.Equals(Path.GetDirectoryName(profileDirectory), profilesRoot, StringComparison.OrdinalIgnoreCase))
        {
            error = "Invalid profile directory.";
            return false;
        }

        try
        {
            Directory.Delete(profileDirectory, true);
            ClearProgressKeys(id);
            PlayerPrefs.Save();
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = "Could not delete the profile: " + exception.Message;
            return false;
        }
    }

    private static string ProfileKey(string id, string key)
    {
        return "Profile_" + id + "_" + key;
    }

    private static void ClearProgressKeys(string id)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        string registry = "Profile_" + id + KnownKeysSuffix;
        foreach (string key in PlayerPrefs.GetString(registry, "").Split('|'))
        {
            if (!string.IsNullOrEmpty(key))
                keys.Add(key);
        }

        foreach (string category in new[] { "ProgressionTrigger", "ObjectiveTrigger" })
        {
            string recorded = PlayerPrefs.GetString(ProfileKey(id, category + "Keys"), "");
            foreach (string trigger in recorded.Split('|'))
            {
                if (string.IsNullOrEmpty(trigger))
                    continue;

                keys.Add(category + "_Normal_" + trigger);
                keys.Add(category + "_Hard_" + trigger);
            }
            keys.Add(category + "Keys");
        }

        foreach (string key in new[]
        {
            "GameProgress", "CandleOneRevealed", "BibleSpawner", "CrossSpawner",
            "CrossRevealed", "BibbleRevealed",
            "MansionKeySpawnIndex_Normal", "MansionKeySpawnIndex_Hard",
            "WaitingRoomKeySpawnIndex_Normal", "WaitingRoomKeySpawnIndex_Hard",
            "FlashlightSpawnIndex_Normal", "FlashlightSpawnIndex_Hard",
            "WrenchSpawnIndex_Normal", "WrenchSpawnIndex_Hard",
            "VaultKeySpawnIndex_Normal", "VaultKeySpawnIndex_Hard",
            "OfficeKeySpawnIndex_Normal", "OfficeKeySpawnIndex_Hard"
        })
            keys.Add(key);

        foreach (string key in keys)
            PlayerPrefs.DeleteKey(ProfileKey(id, key));
        PlayerPrefs.DeleteKey(registry);
    }

    private static bool IsKnownProfile(string id)
    {
        if (id == OriginalProfileId)
            return true;
        if (string.IsNullOrEmpty(id) || !Guid.TryParseExact(id, "N", out _))
            return false;

        string metadataPath = Path.Combine(Application.persistentDataPath, ProfilesFolder, id, ProfileFile);
        return File.Exists(metadataPath);
    }
}
