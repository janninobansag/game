# VAREN Save Database

This document describes the SQLite save database used by `Assets/SaveSystem.cs`.
It is a save-game database, not an online account database.

## ERD files

Copy one of these files into [dbdiagram.io](https://dbdiagram.io), depending on
which save database you want to view:

| Database file | Game mode / purpose | ERD source |
| --- | --- | --- |
| `gameSave.db` | Chapter 1 Normal | [CHAPTER_1_DATABASE_ERD.dbml](CHAPTER_1_DATABASE_ERD.dbml) |
| `gameSave_Hard.db` | Chapter 2 Hard | [DATABASE_ERD.dbml](DATABASE_ERD.dbml) |
| `settings.db` | Menu and player settings | [SETTINGS_DATABASE_ERD.dbml](SETTINGS_DATABASE_ERD.dbml) |

## Scope

The normal Chapter 1 (`gameSave.db`) and Hard Chapter 2 (`gameSave_Hard.db`) saves use separate database files. The
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

There are currently **no SQLite foreign keys**. Game scripts find the correct row
using a string identifier, so dbdiagram correctly shows independent tables.

- `PlayerData.CurrentScene` identifies the player scene.
- `AIPositionData.SceneName` matches the scene in which that AI transform is saved.
- `InventoryData.ItemName`, `DroppedItemData.ItemName`, `BatteryData.BatteryName`,
  `KeyData.KeyName`, and `RitualItemData.ItemName` are logical item identifiers.
- `WrenchData.WrenchId` and `GasData.GasId` identify Chapter 2 objects.
- `GeneratorCoverData.CoverId` is a general Chapter 2 generator-state marker. The
  current generator systems use IDs such as `GeneratorGasTankCover`, `GeneratorFuel`,
  and `GeneratorKeyInserted`.

## Data types

- `integer`: whole number; an `Id` marked as auto-increment is the table primary key.
- `varchar`: text, usually an object name or save identifier.
- `boolean`: saved by SQLite as an integer (`0` false, `1` true).
- `float`: decimal number. Position uses `PosX`, `PosY`, `PosZ`; rotation stores a
  quaternion as `RotX`, `RotY`, `RotZ`, `RotW`.

## Source of truth

The schema is defined in `Assets/SaveSystem.cs`. If a data model class changes,
update both documentation files so the ERD remains accurate.
