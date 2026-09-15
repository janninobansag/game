# VAREN Use Case Diagram

This UML-style use case diagram shows what the **Player** can do in the VAREN game system. The diagram is written in Mermaid so GitHub renders it and the source remains editable.

```mermaid
flowchart LR
    player((Player))

    subgraph varenSystem [VAREN Game System]
        direction TB

        subgraph gameAccess [Game Access]
            startNormal([Start Normal Game])
            startHard([Start Hard Game])
            loadGame([Load Game])
            changeSettings([Change Settings])
            pauseQuit([Pause or Quit Game])
        end

        subgraph coreGameplay [Core Gameplay]
            explore([Explore Locations])
            interact([Interact with Objects])
            manageItems([Manage Inventory])
            useFlashlight([Use Flashlight and Battery])
            readContent([Read Notes and Books])
            saveGame([Save Game Progress])
        end

        subgraph progression [Progression and Survival]
            unlockArea([Unlock Doors and Search Drawers])
            encounter([Survive Enemy Encounters])
            answerQuestions([Answer Q and A Challenge])
            generator([Operate Generator in Hard Mode])
            placeRitual([Place Ritual Items])
            sealVaren([Seal VAREN at Ritual Tree])
        end
    end

    player --> startNormal
    player --> startHard
    player --> loadGame
    player --> changeSettings
    player --> pauseQuit
    player --> explore
    player --> interact
    player --> manageItems
    player --> useFlashlight
    player --> readContent
    player --> unlockArea
    player --> encounter
    player --> generator
    player --> placeRitual
    player --> sealVaren

    interact -.->|includes| manageItems
    interact -.->|includes| unlockArea
    interact -.->|includes| readContent
    manageItems -.->|includes| useFlashlight
    encounter -.->|may include| answerQuestions
    placeRitual -.->|leads to| sealVaren
    pauseQuit -.->|includes| saveGame

    style gameAccess fill:#C2E5FF,stroke:#3DADFF
    style coreGameplay fill:#C6FAF6,stroke:#5AD8CC
    style progression fill:#DCCCFF,stroke:#874FFF
    style sealVaren fill:#CDF4D3,stroke:#66D575
    style encounter fill:#FFECBD,stroke:#FFC943
```

## Actors

| Actor | Description |
| --- | --- |
| Player | The person playing VAREN through the keyboard, mouse, menus, and in-game interaction prompts. |

## Use Case Summary

| Area | Player use cases |
| --- | --- |
| Game access | Start a Normal or Hard game, load progress, change settings, pause, or quit. |
| Exploration | Move through locations, inspect objects, read notes/books, unlock doors, and open drawers. |
| Inventory | Pick up, select, drop, and use items including keys, batteries, flashlight, gas, and ritual items. |
| Survival | Avoid enemies, receive damage, respawn at checkpoints, and complete the Q-and-A sequence when it is triggered. |
| Hard mode | Open the generator cover with the wrench, pour gas, insert the Generator key, and run the generator. |
| Ritual progression | Place two candles, a Bible, and a cross; then hold `E` at the Ritual Tree to seal VAREN. |
| Persistence | Save and load player progress, world state, inventory, rituals, settings, subtitles, and progression. |

## Relationship Notes

- **Includes** means a use case is a required supporting action. For example, interacting with objects includes inventory actions when the object is a pickup.
- **May include** means the Q-and-A challenge occurs only for relevant enemy encounters.
- **Leads to** represents the game progression rule: placing all ritual items unlocks the final Ritual Tree sequence.
- The Player is the only external human actor. SQLite save files are internal game persistence, not a separate player-facing actor.