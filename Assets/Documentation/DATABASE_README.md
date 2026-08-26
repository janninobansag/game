# VAREN Database Guide

## Database files

The game uses SQLite database files in Unity's persistent-data folder. These files are created while the game runs, not inside `Assets`.

| File | Purpose | Used by |
| --- | --- | --- |
| `gameSave.db` | Normal-mode player save | `SaveSystem.cs` |
| `gameSave_Hard.db` | Hard-mode player save | `SaveSystem.cs` |
| `settings.db` | Global menu settings for every difficulty | `SettingsDatabase.cs`, `SettingsPanel.cs` |

`settings.db` is separate so changing menu settings cannot create, overwrite, or damage a Normal or Hard player save.

## Settings database

### `SettingsData`

One row is stored in this table. It is loaded whenever the menu settings manager starts and updated when the player presses SAVE or selects a language.

| Field | Purpose |
| --- | --- |
| `Id` | Always `1`; identifies the single settings row. |
| `Volume` | Master volume, from 0 to 100. |
| `Sensitivity` | Mouse sensitivity. |
| `Brightness` | Ambient/reflection brightness value. |
| `QualityLevel` | Graphics quality: 0 Low, 1 Medium, 2 High. |
| `Language` | 0 English, 1 Korean, 2 Tagalog. |

Connections:

- `SettingsPanel.cs` reads the row on menu startup and applies the sliders, labels, graphics, volume, and language.
- `SettingsPanel.cs` writes the row when SAVE is pressed or a language is selected.
- `MenuLocalization.cs` reads the selected language through the settings manager's compatibility cache.
- `GameplayLocalization.cs` uses the selected language for story intro and supported subtitles.

Older `PlayerPrefs` values are automatically copied into `SettingsData` the first time the updated menu runs. PlayerPrefs is retained only as a small compatibility cache for older scripts.

## Player-save database tables

Normal and Hard modes have the same table structure, but each mode has its own database file.

| Table | What it stores | Main connection |
| --- | --- | --- |
| `PlayerData` | Player position, rotation, scene, health, max health, sensitivity. | `SaveSystem.cs`, `PlayerHealth.cs`, `PlayerController.cs` |
| `InventoryData` | Items currently in the three-slot inventory. | `SaveSystem.cs`, `Inventory.cs` |
| `DoorData` | Door unlock/open state and rotation. | `SaveSystem.cs`, door scripts |
| `RitualData` | Ritual completion state. | `SaveSystem.cs`, ritual scripts |
| `NoteData` | Notes that have been read. | `SaveSystem.cs`, note scripts |
| `GameStateData` | General named game-state values. | `SaveSystem.cs` and gameplay scripts |
| `DroppedItemData` | Dropped item name, position, and rotation. | `SaveSystem.cs`, inventory/pickup scripts |
| `FlashlightData` | Flashlight battery, held state, and dropped position. | `SaveSystem.cs`, `FlashlightPickup.cs` |
| `KeyData` | Used keys. | `SaveSystem.cs`, `Key.cs` |
| `BatteryData` | Battery amount, used/held/dropped state, position, rotation. | `SaveSystem.cs`, `BatteryPickup.cs` |
| `RitualItemData` | Candle, cross, Bible, and other ritual-item reveal/place/drop state. | `SaveSystem.cs`, ritual item scripts |
| `StaminaData` | Current Hard-mode stamina. | `SaveSystem.cs`, `StaminaController.cs` |
| `SubtitleData` | Whether one-time subtitles have already triggered. | `SaveSystem.cs`, subtitle trigger scripts |`r`n| `IntroData` | Story intro section, sentence, and completion state. | `SaveSystem.cs`, `StoryIntro.cs` |
| `ProgressionData` | Current progress points and total points used for the load-panel percentage. | `SaveSystem.cs`, `ProgressionSystem.cs`, `MenuManager.cs` |

## Save and load flow

```text
Player presses Save / game auto-saves
        -> SaveSystem.SaveGame()
        -> Writes player, inventory, item, ritual, stamina, subtitle, and progression data
        -> gameSave.db or gameSave_Hard.db

Player chooses Load Game
        -> MenuManager checks the correct database file and its ProgressionData
        -> SaveSystem.LoadGame()
        -> Restores the player, items, UI-related state, stamina, subtitles, and progress
```

## Important rules

- Do not manually delete individual tables or rows unless you have made a backup first.
- `gameSave.db` and `gameSave_Hard.db` are different saves. Editing one does not edit the other.`r`n- Starting a New Game clears IntroData and SubtitleData only for the selected difficulty, so the story and one-time subtitles can play again.
- `settings.db` is global: English, Korean, or Tagalog selection is shared by both difficulties.
- The visual `SettingsPanel` can be inactive. Keep the separate `Setting pannel cs` manager active so it can load settings.
