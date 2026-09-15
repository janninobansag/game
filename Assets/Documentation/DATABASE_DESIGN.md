# VAREN Database Design

## 1. Purpose

VAREN uses SQLite to persist offline game progress and global player settings. The database design separates game saves by difficulty and keeps global settings independent of gameplay progress.

The design supports a single local player profile per save database. It is an offline, single-player design: no online accounts, multiplayer records, or server synchronization are required.

## 2. Database Files

| Database file | Scope | Purpose |
| --- | --- | --- |
| `gameSave_v2.db` | Normal mode | Stores the player and world state for Chapter 1 / Normal mode. |
| `gameSave_Hard_v2.db` | Hard mode | Stores the player and world state for Chapter 2 / Hard mode, including generator systems. |
| `settings.db` | Global | Stores one shared row for graphics, sound, sensitivity, brightness, and language settings. |

All database files are created under Unity `Application.persistentDataPath`. They are not stored inside the Unity `Assets` folder.

## 3. Design Approach

### Player save databases

Both player-save databases use the same relational pattern:

```text
SaveProfileData (parent)
        |
        +-- PlayerData
        +-- InventoryData
        +-- DoorData
        +-- RitualData
        +-- NoteData
        +-- GameStateData
        +-- DroppedItemData
        +-- FlashlightData
        +-- KeyData
        +-- BatteryData
        +-- RitualItemData
        +-- ProgressionData
        +-- SubtitleData
        +-- StaminaData
        +-- IntroData
        +-- AIPositionData
        +-- WrenchData, GasData, GeneratorCoverData (Hard mode only)
```

`SaveProfileData.Id` is the parent primary key. Each game-state table includes `SaveProfileId`, which is a foreign key to that parent row.

### Global settings database

`settings.db` contains one `SettingsData` row. It is intentionally separate because menu settings apply to both difficulties and should not create or overwrite a player save.

## 4. Core Entities

| Entity | Primary key | Main purpose |
| --- | --- | --- |
| `SaveProfileData` | `Id` | Parent profile for one local game save. Stores save name, difficulty, and schema version. |
| `PlayerData` | `Id` | Player position, rotation, health, sensitivity, and current scene. |
| `InventoryData` | `Id` | Items in the three-slot inventory, their quantities, and equipped state. |
| `DoorData` | `Id` | Door identity, lock/open state, and rotation. |
| `DroppedItemData` | `Id` | Generic dropped item state, location, rotation, and held/dropped flags. |
| `FlashlightData` | `Id` | Flashlight battery, held/dropped state, location, and whether the light is on. |
| `BatteryData` | `Id` | Battery state, charge amount, use state, and world transform. |
| `KeyData` | `Id` | Whether a named key has been used. |
| `RitualItemData` | `Id` | Bible, cross, candles, and other ritual item reveal/place/drop state. |
| `RitualData` | `Id` | Overall ritual completion state. |
| `ProgressionData` | `Id` | Progress points and total points displayed by the game. |
| `SubtitleData` | `SubtitleId` | One-time subtitle completion state. |
| `IntroData` | `Id` | Current intro section, line, and completion state. |
| `AIPositionData` | `Id` | AI identity, scene, and saved transform. |
| `StaminaData` | `Id` | Current player stamina for Hard mode. |
| `GameStateData` | `Id` | Named state values for gameplay that does not need its own fixed table. |

## 5. Hard-mode Entities

The following tables exist only in `gameSave_Hard_v2.db` because they belong to the Chapter 2 generator sequence.

| Entity | Primary key | Purpose |
| --- | --- | --- |
| `WrenchData` | `WrenchId` | Tracks the wrench held/dropped state and transform. |
| `GasData` | `GasId` | Tracks gas held/dropped state and transform. |
| `GeneratorCoverData` | `CoverId` | Stores whether the generator cover is removed/open and other named generator states. |

## 6. Relationships and Cardinality

| Parent | Child | Relationship | Meaning |
| --- | --- | --- | --- |
| `SaveProfileData` | `PlayerData` | One-to-many | A profile can have player-state records; the game normally saves one current player row. |
| `SaveProfileData` | `InventoryData` | One-to-many | A profile owns multiple inventory item rows. |
| `SaveProfileData` | `DoorData` | One-to-many | A profile owns multiple saved door states. |
| `SaveProfileData` | item, ritual, AI, subtitle, and progression tables | One-to-many | Each row belongs to one selected save profile. |
| `SettingsData` | none | Standalone single row | Settings are global, not owned by a Normal or Hard save profile. |

Foreign keys use `ON DELETE CASCADE`. If a save profile is removed, its dependent game-state rows are removed with it. This prevents orphaned records.

## 7. Integrity Rules

- Every player-save child record must include a valid `SaveProfileId`.
- The save schema enables SQLite foreign-key enforcement with `PRAGMA foreign_keys = ON`.
- A new player-save database creates a default `SaveProfileData` row with `Id = 1`.
- Normal and Hard mode never share a game-save database file.
- Settings do not reference a save profile because they are shared globally.
- Stable names and IDs such as `DoorId`, `AIId`, `GasId`, and `SubtitleId` allow Unity scene objects to locate their matching saved state.

## 8. Save and Load Data Flow

```text
Game action changes state
        -> SaveSystem gathers player, inventory, item, ritual, AI, subtitle, and progression values
        -> SaveSystem selects Normal or Hard database
        -> SaveSystem writes rows using SaveProfileId = 1
        -> SQLite save file persists in Application.persistentDataPath

Player selects Load Game
        -> MenuManager selects the saved difficulty and scene
        -> SaveSystem reads rows for SaveProfileId = 1
        -> Unity restores the player, world objects, UI state, and progression
```

## 9. Design Rationale

- **Separate Normal and Hard saves:** prevents Chapter 2 generator data from mixing with Chapter 1 progress.
- **Parent SaveProfileData table:** provides a real relational parent for all gameplay state and makes the foreign-key relationship visible in the ERD.
- **Dedicated state tables:** keep frequently used game features clear and easy to restore, such as inventory, doors, flashlight, ritual items, and AI positions.
- **GameStateData flexibility:** supports small named values without changing the schema for every minor state.
- **Separate settings database:** keeps player preferences independent from gameplay saves.
- **SQLite:** works offline, is lightweight, and is supported across Windows, macOS, and Linux with the project's SQLite plugins.

## 10. Related Documentation

| Document | What it provides |
| --- | --- |
| [Database Guide](DATABASE_README.md) | Database files, save/load flow, and table responsibilities. |
| [Database Schema](DATABASE_SCHEMA.md) | Detailed field-level schema reference. |
| [Logical Relationships](DATABASE_LOGICAL_RELATIONSHIPS.md) | Explanation of how Unity scene object IDs match saved rows. |
| [Normal Mode ERD](CHAPTER_1_DATABASE_ERD.dbml) | DBML source for the Normal-mode relational diagram. |
| [Hard Mode ERD](DATABASE_ERD.dbml) | DBML source for the Hard-mode relational diagram. |
| [Settings ERD](SETTINGS_DATABASE_ERD.dbml) | DBML source for the settings database diagram. |

## 11. Scope and Future Expansion

The current design is appropriate for one offline player per save database. If VAREN later supports multiple named save slots, cloud saves, or user accounts, create additional `SaveProfileData` rows and let the player select a profile instead of always using profile `Id = 1`.