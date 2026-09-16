# VAREN Activity Diagram

This activity diagram shows the player's main game path, including Normal mode, Hard mode, loading, the ritual, the final sealing, and quitting.

```mermaid
flowchart TD
    start([Start Game]) --> intro[Play intro scene]
    intro --> menu[Open main menu]
    menu --> choice{Choose action}

    choice -->|Settings| settings[Load or save settings]
    settings --> menu

    choice -->|New Game| difficulty{Choose difficulty}
    difficulty -->|Normal| normal[Clear Normal state]
    normal --> chapterOne[Load chapter 1]
    difficulty -->|Hard| hard[Clear Hard state]
    hard --> chapterTwo[Load chapter 2]

    choice -->|Load Game| savedMode{Choose saved mode}
    savedMode -->|Normal| loadNormal[Load Normal save]
    loadNormal --> chapterOne
    savedMode -->|Hard| loadHard[Load Hard save]
    loadHard --> chapterTwo

    choice -->|Quit| saveQuit[Save active game]
    saveQuit --> quit([Quit Game])

    chapterOne --> explore[Explore and interact]
    chapterTwo --> explore
    explore --> encounter{Enemy encounter?}
    encounter -->|Tikbalang catches player| tikCatch[Lock movement, focus camera, play jumpscare]
    tikCatch --> tikQuestion[Open Q&A and unlock cursor]
    tikQuestion --> tikResult{Correct answer?}
    tikResult -->|Yes| tikEscape[Teleport Tikbalang away]
    tikResult -->|Wrong or timeout| tikDamage[Deal 40 damage by default]
    tikDamage --> alive{Player survives?}
    tikEscape --> explore
    encounter -->|Other enemy| survive[Chase, damage, or Q and A]
    survive --> alive
    alive -->|No| respawn[Checkpoint respawn or restart]
    respawn --> explore
    alive -->|Yes| explore
    encounter -->|No| objective{Current objective}

    objective -->|Hard generator task| generator[Open cover, pour gas, insert key, run generator]
    generator --> explore
    objective -->|Find ritual items| collect[Find candles, Bible, and cross]
    collect --> place[Place item in matching holder]
    place --> allItems{All 4 items placed?}
    allItems -->|No| explore
    allItems -->|Yes| ritualDelay[Wait final ritual delay]
    ritualDelay --> spawn[VAREN spawns and candle lights fade]
    spawn --> tree[Reach Ritual Tree]

    objective -->|Reach Ritual Tree| tree
    tree --> hold[Hold E to seal ritual]
    hold --> complete{Hold duration complete?}
    complete -->|No| explore
    complete -->|Yes| stopVaren[Stop VAREN movement and fade effects]
    stopVaren --> saveProgress[Save progression]
    saveProgress --> outro[Load outro scene]
    outro --> menu

    style start fill:#CDF4D3,stroke:#66D575
    style quit fill:#D9D9D9,stroke:#B3B3B3
    style allItems fill:#FFECBD,stroke:#FFC943
    style complete fill:#FFECBD,stroke:#FFC943
    style stopVaren fill:#DCCCFF,stroke:#874FFF
    style saveProgress fill:#C2E5FF,stroke:#3DADFF
```

## Activity Notes

- **Settings** is global and uses `settings.db`.
- **Normal** uses `gameSave_v2.db`; **Hard** uses `gameSave_Hard_v2.db`.
- The generator route is a Chapter 2 Hard-mode activity. It is only available after the gas tank cover is opened.
- The ritual route requires two candles, a Bible, and a cross.
- Completing the Ritual Tree sequence stops VAREN, saves progression, loads the outro, and returns the player to the main menu.
- Releasing `E`, looking away, or leaving interaction range before the final ritual duration is complete stops the current hold and allows the player to try again.
### Tikbalang Encounter Notes

- On first discovery, Tikbalang teleports in front of the player and plays its discovery sound; this is not a Q&A damage event.
- When it later catches the player, movement locks, the camera focuses on Tikbalang, and the Q&A panel opens.
- Wrong answers and timeouts deal 40 damage by default. After each result, Tikbalang teleports away and may catch the player again after its cooldown.