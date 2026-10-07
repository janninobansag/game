using SQLite4Unity3d;

// PURPOSE: SQLite data records used by SaveSystem. Each class maps to one save table.

// ── SQLITE DATA MODELS ──
[Table("WrenchData")]
public class WrenchData
{
    [PrimaryKey]
    public string WrenchId { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public bool IsHeld { get; set; }
    public bool IsDropped { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}
[Table("GasData")]
public class GasData
{
    [PrimaryKey]
    public string GasId { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public bool IsHeld { get; set; }
    public bool IsDropped { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}
[Table("GeneratorCoverData")]
public class GeneratorCoverData
{
    [PrimaryKey]
    public string CoverId { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public bool IsRemoved { get; set; }
}
[Table("AIPositionData")]
public class AIPositionData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string AIId { get; set; }
    public string SceneName { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}
[Table("PlayerData")]
public class PlayerData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Sensitivity { get; set; }
    public string CurrentScene { get; set; }
}

[Table("InventoryData")]
public class InventoryData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string ItemName { get; set; }
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
}

[Table("DoorData")]
public class DoorData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string DoorId { get; set; }
    public string DoorName { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsOpen { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}

[Table("DrawerData")]
public class DrawerData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
    public string DrawerId { get; set; }
    public string DrawerName { get; set; }
    public bool IsOpen { get; set; }
    public float LocalPosX { get; set; }
    public float LocalPosY { get; set; }
    public float LocalPosZ { get; set; }
}
[Table("RitualData")]
public class RitualData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public bool IsComplete { get; set; }
}

[Table("NoteData")]
public class NoteData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string NoteTitle { get; set; }
    public bool IsRead { get; set; }
}

[Table("GameStateData")]
public class GameStateData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string Key { get; set; }
    public string Value { get; set; }
}

[Table("DroppedItemData")]
public class DroppedItemData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string ItemName { get; set; }
    public bool IsHeld { get; set; }
    public bool IsDropped { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }
}

[Table("FlashlightData")]
public class FlashlightData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string FlashlightName { get; set; }
    public float BatteryLife { get; set; }
    public float CurrentBattery { get; set; }
    public bool IsOn { get; set; }
    public bool IsHeld { get; set; }
}

[Table("KeyData")]
public class KeyData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string KeyName { get; set; }
    public bool WasUsed { get; set; }
}

[Table("BatteryData")]
public class BatteryData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string BatteryName { get; set; }
    public float RechargeAmount { get; set; }
    public bool IsHeld { get; set; }
    public bool IsUsed { get; set; }
}

[Table("RitualItemData")]
public class RitualItemData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public string ItemName { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsPlaced { get; set; }
}

// ── NEW: ProgressionData table ──
[Table("ProgressionData")]
public class ProgressionData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public int ProgressValue { get; set; }
    public int TotalPoints { get; set; }
}

[Table("SubtitleData")]
public class SubtitleData
{
    [PrimaryKey]
    public string SubtitleId { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public bool IsTriggered { get; set; }
}
[Table("StaminaData")]
public class StaminaData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public float CurrentStamina { get; set; }
}

[Table("IntroData")]
public class IntroData
{
    [PrimaryKey]
    public int Id { get; set; }
        public int SaveProfileId { get; set; } = RelationalSaveSchema.LocalProfileId;
public int SectionIndex { get; set; }
    public int LineIndex { get; set; }
    public bool IsComplete { get; set; }
}


/// <summary>
/// Creates the Version 3 local save schema. Each database file has one save
/// profile (Id = 1); every game-state table is a child of that profile.
