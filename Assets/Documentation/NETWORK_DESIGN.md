# VAREN Network Design

## Current architecture: offline single player

VAREN currently has no client-server, multiplayer, online-account, cloud-save, or live-service architecture. The game runs entirely on the player's device.

There are no networking packages or gameplay scripts using Netcode for GameObjects, Mirror, Photon, sockets, web requests, or a custom server protocol.

## Local data flow

```text
Player input
    -> Unity gameplay scripts
    -> SaveSystem / SettingsDatabase
    -> SQLite files and PlayerPrefs on the local device
    -> Game loads the same local state next session
```

| Component | Role | Network requirement |
| --- | --- | --- |
| `PlayerController`, AI, ritual, inventory, Q&A | Local gameplay simulation | None. |
| `SaveSystem` | Saves Normal and Hard progress | Local SQLite only. |
| `SettingsDatabase` | Saves volume, brightness, language, and quality | Local SQLite only. |
| PlayerPrefs | Stores small compatibility, trigger, and spawn flags | Local device only. |
| GitHub repository | Source-code backup/version control during development | Not used by the shipped game at runtime. |

## Saved files

| File / store | Purpose |
| --- | --- |
| `gameSave_v2.db` | Normal / Chapter 1 player save. |
| `gameSave_Hard_v2.db` | Hard / Chapter 2 player save. |
| `settings.db` | Global game settings. |
| PlayerPrefs | Difficulty, one-time trigger state, random spawn choices, and compatibility cache. |

These files are stored under Unity's `Application.persistentDataPath` on each device. They are not automatically uploaded or shared with another player.

## Security and privacy

- The current game does not collect player accounts, email addresses, passwords, IP addresses, analytics, or gameplay telemetry.
- Save files are local and are not encrypted or synchronized to a server.
- Deleting a local save file or PlayerPrefs can reset that device's progress.

## Future online or multiplayer design

Adding multiplayer later would require a separate design; it cannot be enabled only by changing a Unity Inspector setting. At minimum, the game would need:

1. A networking framework, such as Unity Netcode for GameObjects, Mirror, or Photon.
2. A host/server authority model for player movement, monster AI, damage, ritual progress, inventory, and Q&A results.
3. Networked versions of players, Tikbalang, White Lady, items, doors, and ritual holders.
4. Synchronization rules for position, animation, sound, health, pickups, and scene state.
5. A service/backend only if accounts, cloud saves, matchmaking, leaderboards, or cross-device progress are needed.

Until then, the correct network design for VAREN is local-only/offline.

## Related documentation

- [System data dictionary](DATA_DICTIONARY.md)
- [Database guide](DATABASE_README.md)
- [Database design](DATABASE_DESIGN.md)
- [Program workflow](PROGRAM_WORKFLOW.md)