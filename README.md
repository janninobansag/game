# VAREN

**VAREN** is a single-player first-person horror game set in a cursed Philippine forest. Explore abandoned locations, gather sacred ritual items, survive supernatural encounters, and seal the spirit Varen at the Ritual Tree.

Built with **Unity 2022.3.62f3**.

## Features

- First-person exploration, sprinting, crouching, jumping, and item interaction
- Inventory with pickups, keys, batteries, candles, Bible, cross, flashlight, and quest items
- Save and load support for Normal and Hard mode
- SQLite-based persistent saves and global settings
- Ritual progression: place two candles, a Bible, and a cross before completing the final sealing ritual
- Supernatural encounters with Varen, the White Lady, and the Tikbalang
- Flashlight battery system, doors, drawers, notes, books, objectives, subtitles, and multiple languages

## Controls

| Key | Action |
| --- | --- |
| `WASD` | Move |
| Mouse | Look around |
| `Left Shift` | Sprint |
| `Left Ctrl` | Crouch |
| `Space` | Jump |
| `E` | Interact, pick up, or hold during rituals |
| `F` | Toggle flashlight |
| `G` | Drop held item |
| Mouse wheel | Switch inventory item |
| `M` | Open map |
| `Esc` | Pause |

## Play in Unity

1. Install **Unity 2022.3.62f3** through Unity Hub.
2. Clone or download this repository.
3. Open the project folder in Unity Hub.
4. Open `Assets/Scenes/IntroScene.unity`.
5. Press Play.

The active build scene order is:

1. IntroScene
2. menu
3. chapter 1
4. chapter 2
5. outro

## Save Data

The game creates its save files in Unity's persistent-data folder, not inside the repository:

- `gameSave_v2.db` - Normal-mode save
- `gameSave_Hard_v2.db` - Hard-mode save
- `settings.db` - global volume, graphics, sensitivity, brightness, and language settings

Normal and Hard mode use separate save databases. See the [database guide](Assets/Documentation/DATABASE_README.md) for the full schema and save/load flow.

## Desktop Builds

The project includes SQLite native plugins for Windows, macOS, and Linux. In Unity Hub, install the matching modules for Unity 2022.3.62f3 before building:

- Windows Build Support (Mono)
- Mac Build Support (Mono)
- Linux Build Support (Mono)

Then use **File -> Build Settings**, choose the platform, click **Switch Platform**, and build into a separate empty folder for each operating system. Test every platform's New Game, Save, Quit, and Load Game flow before release.

## Documentation

- [Game Story](Assets/Documentation/GAME%20STORY.md)
- [Database Guide](Assets/Documentation/DATABASE_README.md)
- [Normal Mode ERD](Assets/Documentation/CHAPTER_1_DATABASE_ERD.dbml)
- [Hard Mode ERD](Assets/Documentation/DATABASE_ERD.dbml)
- [Settings ERD](Assets/Documentation/SETTINGS_DATABASE_ERD.dbml)
- [Complete Program Workflow](Assets/Documentation/PROGRAM_WORKFLOW.md)
- [Game Activity Diagram](Assets/Documentation/ACTIVITY_DIAGRAM.md)

## Project Structure

```text
Assets/             Game scenes, scripts, art, audio, and documentation
Packages/           Unity package manifest and lock file
ProjectSettings/    Unity project configuration
```

## Notes

This repository contains the Unity project source. Generated folders such as `Library/`, `Temp/`, build output, and each player's local save databases should not be committed.