using SQLite4Unity3d;

// PURPOSE: Creates the SQLite tables used by SaveSystem and records the schema version.

public static class RelationalSaveSchema
{
    public const int LocalProfileId = 1;
    public const int SchemaVersion = 6;

    public static void Create(SQLiteConnection connection, bool isHardMode)
    {
        connection.Execute("PRAGMA foreign_keys = ON");
        connection.Execute("CREATE TABLE IF NOT EXISTS SaveProfileData (" +
                           "Id INTEGER PRIMARY KEY, " +
                           "SaveName TEXT NOT NULL, " +
                           "Difficulty TEXT NOT NULL, " +
                           "SchemaVersion INTEGER NOT NULL)");

        string difficulty = isHardMode ? "Hard" : "Normal";
        connection.Execute("INSERT OR IGNORE INTO SaveProfileData " +
                           "(Id, SaveName, Difficulty, SchemaVersion) VALUES " +
                           "(1, 'Local Save', '" + difficulty + "', " + SchemaVersion + ")");

        // Versions 4 through 6 move dropped transforms to DroppedItemData. Existing
        // battery, flashlight, and ritual records are reset because old saves are unused.
        int storedSchemaVersion = connection.ExecuteScalar<int>(
            "SELECT SchemaVersion FROM SaveProfileData WHERE Id = " + LocalProfileId);
        if (storedSchemaVersion < SchemaVersion)
        {
            if (storedSchemaVersion < 4)
            {
                connection.Execute("DROP TABLE IF EXISTS BatteryData");
            }

            if (storedSchemaVersion < 5)
            {
                connection.Execute("DROP TABLE IF EXISTS FlashlightData");
            }

            if (storedSchemaVersion < 6)
            {
                connection.Execute("DROP TABLE IF EXISTS RitualItemData");
            }
        }

        connection.Execute("UPDATE SaveProfileData SET Difficulty = '" + difficulty + "', SchemaVersion = " + SchemaVersion + " WHERE Id = " + LocalProfileId);

        CreateChildTable(connection, "PlayerData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "PosX REAL, PosY REAL, PosZ REAL, RotX REAL, RotY REAL, RotZ REAL, RotW REAL, Health REAL, MaxHealth REAL, Sensitivity REAL, CurrentScene TEXT");
        CreateChildTable(connection, "InventoryData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "ItemName TEXT, Quantity INTEGER, IsEquipped INTEGER");
        CreateChildTable(connection, "DoorData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "DoorId TEXT, DoorName TEXT, IsUnlocked INTEGER, IsOpen INTEGER, RotX REAL, RotY REAL, RotZ REAL, RotW REAL");
        CreateChildTable(connection, "DrawerData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "DrawerId TEXT, DrawerName TEXT, IsOpen INTEGER, LocalPosX REAL, LocalPosY REAL, LocalPosZ REAL");
        CreateChildTable(connection, "RitualData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "IsComplete INTEGER");
        CreateChildTable(connection, "NoteData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "NoteTitle TEXT, IsRead INTEGER");
        CreateChildTable(connection, "GameStateData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "Key TEXT, Value TEXT");
        CreateChildTable(connection, "DroppedItemData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "ItemName TEXT, IsHeld INTEGER NOT NULL DEFAULT 0, IsDropped INTEGER NOT NULL DEFAULT 1, PosX REAL, PosY REAL, PosZ REAL, RotX REAL, RotY REAL, RotZ REAL, RotW REAL");
        CreateChildTable(connection, "FlashlightData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "FlashlightName TEXT, BatteryLife REAL, CurrentBattery REAL, IsOn INTEGER NOT NULL DEFAULT 0, IsHeld INTEGER");
        CreateChildTable(connection, "KeyData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "KeyName TEXT, WasUsed INTEGER");
        CreateChildTable(connection, "BatteryData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "BatteryName TEXT, RechargeAmount REAL, IsHeld INTEGER, IsUsed INTEGER");
        CreateChildTable(connection, "RitualItemData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "ItemName TEXT, IsRevealed INTEGER, IsPlaced INTEGER");
        CreateChildTable(connection, "ProgressionData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "ProgressValue INTEGER, TotalPoints INTEGER");
        CreateChildTable(connection, "SubtitleData", "SubtitleId TEXT PRIMARY KEY", "IsTriggered INTEGER NOT NULL DEFAULT 0");
        CreateChildTable(connection, "StaminaData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "CurrentStamina REAL");
        CreateChildTable(connection, "IntroData", "Id INTEGER PRIMARY KEY", "SectionIndex INTEGER, LineIndex INTEGER, IsComplete INTEGER");
        CreateChildTable(connection, "AIPositionData", "Id INTEGER PRIMARY KEY AUTOINCREMENT", "AIId TEXT, SceneName TEXT, PosX REAL, PosY REAL, PosZ REAL, RotX REAL, RotY REAL, RotZ REAL, RotW REAL");

        if (!isHardMode)
            return;

        CreateChildTable(connection, "WrenchData", "WrenchId TEXT PRIMARY KEY", "IsHeld INTEGER, IsDropped INTEGER, PosX REAL, PosY REAL, PosZ REAL, RotX REAL, RotY REAL, RotZ REAL, RotW REAL");
        CreateChildTable(connection, "GasData", "GasId TEXT PRIMARY KEY", "IsHeld INTEGER, IsDropped INTEGER, PosX REAL, PosY REAL, PosZ REAL, RotX REAL, RotY REAL, RotZ REAL, RotW REAL");
        CreateChildTable(connection, "GeneratorCoverData", "CoverId TEXT PRIMARY KEY", "IsRemoved INTEGER");
    }

    private static void CreateChildTable(SQLiteConnection connection, string tableName, string primaryKey, string columns)
    {
        connection.Execute("CREATE TABLE IF NOT EXISTS " + tableName + " (" +
                           primaryKey + ", " +
                           "SaveProfileId INTEGER NOT NULL DEFAULT 1, " +
                           columns + ", " +
                           "FOREIGN KEY (SaveProfileId) REFERENCES SaveProfileData(Id) ON DELETE CASCADE)");
    }
}
