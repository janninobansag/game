# VAREN Data Dictionary

## Scope

This is the field-level reference for VAREN data saved between game sessions. It covers the Normal and Hard SQLite save databases, global settings, and PlayerPrefs trigger/cache values. It does not include temporary runtime values such as animation state or a NavMeshAgent path.

## Storage

| Store | Location | Purpose |
| --- | --- | --- |
| Normal save | `gameSave_v2.db` in `Application.persistentDataPath` | Chapter 1 / Normal progress. |
| Hard save | `gameSave_Hard_v2.db` in `Application.persistentDataPath` | Chapter 2 / Hard progress. |
| Settings | `settings.db` in `Application.persistentDataPath` | Global player/menu settings. |
| PlayerPrefs | Unity PlayerPrefs | Compatibility cache, one-time triggers, and random-spawn choices. |

SQLite booleans are stored as `0` (false) or `1` (true). All player-save tables belong to `SaveProfileId = 1`. Position fields are `PosX`, `PosY`, `PosZ`; rotation fields are the Unity quaternion `RotX`, `RotY`, `RotZ`, `RotW`.

## Save profile

### `SaveProfileData`

| Field | Type | Meaning |
| --- | --- | --- |
| `Id` | integer, primary key | Local profile id; always `1`. |
| `SaveName` | text | Display name; currently `Local Save`. |
| `Difficulty` | text | `Normal` or `Hard`. |
| `SchemaVersion` | integer | Save-schema compatibility version. |

## Shared Normal and Hard save tables

| Table | Fields | Meaning |
| --- | --- | --- |
| `PlayerData` | `Id`, `SaveProfileId`, `PosX/Y/Z`, `RotX/Y/Z/W`, `Health`, `MaxHealth`, `Sensitivity`, `CurrentScene` | Player transform, health, sensitivity, and scene. |
| `InventoryData` | `Id`, `SaveProfileId`, `ItemName`, `Quantity`, `IsEquipped` | Inventory entry, stack count, and equipped state. |
| `DoorData` | `Id`, `SaveProfileId`, `DoorId`, `DoorName`, `IsUnlocked`, `IsOpen`, `RotX/Y/Z/W` | Stable door id, state, and rotation. |
| `DrawerData` | `Id`, `SaveProfileId`, `DrawerId`, `DrawerName`, `IsOpen`, `LocalPosX/Y/Z` | Stable drawer id, state, and local position. |
| `RitualData` | `Id`, `SaveProfileId`, `IsComplete` | Whether the Varen ritual is complete. |
| `NoteData` | `Id`, `SaveProfileId`, `NoteTitle`, `IsRead` | Note identifier and read state. |
| `GameStateData` | `Id`, `SaveProfileId`, `Key`, `Value` | General named gameplay state; `Value` is text. |
| `DroppedItemData` | `Id`, `SaveProfileId`, `ItemName`, `IsHeld`, `IsDropped`, `PosX/Y/Z`, `RotX/Y/Z/W` | Generic dropped item state and transform. |
| `FlashlightData` | `Id`, `SaveProfileId`, `FlashlightName`, `BatteryLife`, `CurrentBattery`, `IsOn`, `IsHeld`, `WasDropped`, `PosX/Y/Z` | Flashlight charge, power, ownership, and position. |
| `KeyData` | `Id`, `SaveProfileId`, `KeyName`, `WasUsed` | Key identifier and consumed/unlocked state. |
| `BatteryData` | `Id`, `SaveProfileId`, `BatteryName`, `RechargeAmount`, `IsHeld`, `IsDropped`, `IsUsed`, `PosX/Y/Z`, `RotX/Y/Z/W` | Battery charge value, state, and transform. |
| `RitualItemData` | `Id`, `SaveProfileId`, `ItemName`, `IsRevealed`, `IsPlaced`, `IsDropped`, `PosX/Y/Z`, `RotX/Y/Z/W` | Candle, Cross, Bible, and other ritual-item state. |
| `ProgressionData` | `Id`, `SaveProfileId`, `ProgressValue`, `TotalPoints` | Current and total points for progress percentage. |
| `SubtitleData` | `SubtitleId` (primary key), `SaveProfileId`, `IsTriggered` | One-time subtitle identifier and shown state. |
| `StaminaData` | `Id`, `SaveProfileId`, `CurrentStamina` | Player stamina, used by Hard mode. |
| `IntroData` | `Id` (primary key), `SaveProfileId`, `SectionIndex`, `LineIndex`, `IsComplete` | Story intro position and completion state. |
| `AIPositionData` | `Id`, `SaveProfileId`, `AIId`, `SceneName`, `PosX/Y/Z`, `RotX/Y/Z/W` | Monster/AI identifier, scene, and transform. |

`Id` is an auto-increment primary key except where the table says otherwise. `SaveProfileId` is a foreign key to `SaveProfileData.Id`. `IsHeld`, `IsDropped`, `IsOpen`, `IsRead`, `IsComplete`, and similarly named fields are booleans.

## Hard-mode-only tables

| Table | Fields | Meaning |
| --- | --- | --- |
| `WrenchData` | `WrenchId` (primary key), `SaveProfileId`, `IsHeld`, `IsDropped`, `PosX/Y/Z`, `RotX/Y/Z/W` | Wrench state and transform. |
| `GasData` | `GasId` (primary key), `SaveProfileId`, `IsHeld`, `IsDropped`, `PosX/Y/Z`, `RotX/Y/Z/W` | Gas-can state and transform. |
| `GeneratorCoverData` | `CoverId` (primary key), `SaveProfileId`, `IsRemoved` | Generator-cover identity and removed state. |

## Global settings

### `SettingsData` in `settings.db`

| Field | Type | Meaning |
| --- | --- | --- |
| `Id` | integer, primary key | Single settings row; always `1`. |
| `Volume` | float | Master volume. |
| `Sensitivity` | float | Player look sensitivity. |
| `Brightness` | float | Display brightness. |
| `QualityLevel` | integer | Unity Quality level index. |
| `Language` | integer | Selected language index. |

## PlayerPrefs keys

`<id>` and `<saveKey>` are dynamic identifiers configured in the Unity Inspector or generated from stable scene objects.

| Key / pattern | Type | Meaning |
| --- | --- | --- |
| `GameDifficulty` | string | Active difficulty: `Normal` or `Hard`. |
| `ShouldLoadSave` | integer | Menu request to load a save. |
| `SkipIntro` | integer | Story intro should be skipped. |
| `SaveTime` | string | Last save timestamp used by menus. |
| `CheckpointPosX/Y/Z` | float | Legacy checkpoint coordinates. |
| `MasterVolume`, `MouseSensitivity`, `Brightness` | float | Compatibility cache for global settings. |
| `QualityLevel`, `GameLanguage`, `SettingsRepairVersion` | integer | Cached graphics/language settings and one-time repair version. |
| `ObjectiveTriggerKeys` | string | Registry of one-time objective-trigger ids. |
| `ObjectiveTrigger_Normal_<id>` / `ObjectiveTrigger_Hard_<id>` | integer | Objective trigger fired for that difficulty. |
| `ProgressionTriggerKeys` | string | Registry of one-time progression-trigger ids. |
| `ProgressionTrigger_Normal_<id>` / `ProgressionTrigger_Hard_<id>` | integer | Progression trigger fired for that difficulty. |
| `<saveKey>_Normal` / `<saveKey>_Hard` | integer | Difficulty-specific random pickup/spawn state. |
| Progression-system save key | integer | PlayerPrefs-backed progression value, when configured. |

## Maintenance

When adding a persistent field, table, or PlayerPrefs key, update this file. For SQLite schema changes, also update `DATABASE_SCHEMA.md` and its ERD file. Keep Unity `.meta` files with new or moved assets so scene and prefab references remain stable.

## Related documents

- [Database guide](DATABASE_README.md)
- [Database schema and ERD links](DATABASE_SCHEMA.md)
- [Database design](DATABASE_DESIGN.md)
- [Program workflow](PROGRAM_WORKFLOW.md)