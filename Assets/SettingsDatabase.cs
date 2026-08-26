using System.IO;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;

/// <summary>
/// Stores menu options separately from normal and hard-mode player saves.
/// </summary>
public static class SettingsDatabase
{
    private const string DatabaseFileName = "settings.db";

    private static string DatabasePath
    {
        get { return Path.Combine(Application.persistentDataPath, DatabaseFileName); }
    }

    public static bool TryLoad(out SettingsData settings)
    {
        settings = null;
        if (!File.Exists(DatabasePath))
            return false;

        SQLiteConnection connection = null;
        try
        {
            connection = new SQLiteConnection(DatabasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            connection.CreateTable<SettingsData>();
            settings = connection.Table<SettingsData>().FirstOrDefault();
            return settings != null;
        }
        catch (System.Exception)
        {
            return false;
        }
        finally
        {
            if (connection != null)
                connection.Close();
        }
    }

    public static bool Save(SettingsData settings)
    {
        if (settings == null)
            return false;

        SQLiteConnection connection = null;
        try
        {
            connection = new SQLiteConnection(DatabasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            connection.CreateTable<SettingsData>();
            settings.Id = 1;
            connection.InsertOrReplace(settings);
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
        finally
        {
            if (connection != null)
                connection.Close();
        }
    }
}

[Table("SettingsData")]
public class SettingsData
{
    [PrimaryKey]
    public int Id { get; set; }
    public float Volume { get; set; }
    public float Sensitivity { get; set; }
    public float Brightness { get; set; }
    public int QualityLevel { get; set; }
    public int Language { get; set; }
}