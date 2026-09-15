# VAREN Save Database

This document describes the SQLite save database used by `Assets/SaveSystem.cs`.
It is a save-game database, not an online account database.

## ERD files

Copy one of these files into [dbdiagram.io](https://dbdiagram.io), depending on
which save database you want to view:

| Database file | Game mode / purpose | ERD source |
| --- | --- | --- |
| `gameSave_v2.db` | Chapter 1 Normal | [CHAPTER_1_DATABASE_ERD.dbml](CHAPTER_1_DATABASE_ERD.dbml) |
| `gameSave_Hard_v2.db` | Chapter 2 Hard | [DATABASE_ERD.dbml](DATABASE_ERD.dbml) |
| `settings.db` | Menu and player settings | [SETTINGS_DATABASE_ERD.dbml](SETTINGS_DATABASE_ERD.dbml) |

## Scope

The normal Chapter 1 (`gameSave_v2.db`) and Hard Chapter 2 (`gameSave_Hard_v2.db`) saves use separate database files. The
following tables exist in both modes:

| System | Table |
| --- | --- |
| Player transform, health, sensitivity, scene | `PlayerData` |
| Inventory entries | `InventoryData` |
| Doors | `DoorData` |
| Ritual completion | `RitualData` |
| Notes read by the player | `NoteData` |
| General key/value state | `GameStateData` |
| Generic dropped items | `DroppedItemData` |
| Flashlight state | `FlashlightData` |
| Used keys | `KeyData` |
| Batteries | `BatteryData` |
| Ritual objects | `RitualItemData` |
| Progress values | `ProgressionData` |
| Triggered subtitles | `SubtitleData` |
| Player stamina | `StaminaData` |
| Intro/cutscene progress | `IntroData` |
| AI transforms per scene | `AIPositionData` |

These tables are **Hard Chapter 2 only**. They are never created in the normal
Chapter 1 database:

| System | Table |
| --- | --- |
| Wrench position and state | `WrenchData` |
| Gas-can position and state | `GasData` |
| Generator cover, fuel, and inserted-key markers | `GeneratorCoverData` |

## Relationships

Version 2 is a formal relational SQLite save schema. Each player-save database
contains one `SaveProfileData` parent row (`Id = 1`). Every game-state table has a
`SaveProfileId` foreign key that references this parent with `ON DELETE CASCADE`.

This creates a one-to-many relationship from `SaveProfileData` to player state,
inventory, doors, items, AI data, and Chapter 2 generator data. A profile is the
single-player boundary of one database file.

Item names and scene names are still logical identifiers used by Unity gameplay
scripts; they are not foreign keys between item-type tables.

## Data types

- `integer`: whole number; an `Id` marked as auto-increment is the table primary key.
- `varchar`: text, usually an object name or save identifier.
- `boolean`: saved by SQLite as an integer (`0` false, `1` true).
- `float`: decimal number. Position uses `PosX`, `PosY`, `PosZ`; rotation stores a
  quaternion as `RotX`, `RotY`, `RotZ`, `RotW`.

## Source of truth

The schema is defined in `Assets/SaveSystem.cs`. If a data model class changes,
update both documentation files so the ERD remains accurate.
