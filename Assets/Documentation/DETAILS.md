# VAREN - Complete Game Details

## Project Identity

**VAREN** is a first-person Philippine-forest horror game made in Unity.

- Unity version: `2022.3.62f3`
- Main game modes: Normal and Hard
- Normal mode starts in `chapter 1`
- Hard mode starts in `chapter 2`
- Main story locations: a cursed forest, an abandoned village, an abandoned church, and the ritual area

## Story Premise

### Canonical Story Draft: The Fall Of Malawak Forest

In **1989**, Malawak Forest contains a small but lively forest village. It has three houses, a guard house, and a church. Father Mateo is the village priest and its respected head.

The villagers are:

- House 1: Laica and Shyna
- House 2: Ann
- House 3: Mil
- Guard house: Jude, the village guard

Father Mateo becomes possessed by a mysterious shadow named **Varen**. Varen had observed the village and chose Mateo because he held the highest position of trust and authority. Varen controls Mateo's body but hides its identity. Mil and Jude notice that Mateo's behavior and movements are becoming strange; they suspect possession but are not certain. Laica and Shyna, who are closest to Father Mateo, do not know he is possessed.

Varen, speaking through Mateo, tells Shyna and Laica to prepare candles, a cross, and a Bible in the church. The following day, Mateo announces a prayer gathering. The villagers believe it is a normal prayer, but Varen is secretly preparing a ritual that would let it permanently keep Father Mateo's body without revealing its true form.

At the church, the prayer begins normally. When Mateo orders everyone to form a circle, Shyna questions the unusual instruction. Mateo tells everyone to obey, and the ritual begins. Minutes later, the real Father Mateo briefly breaks through Varen's control. He shouts that he is possessed and begs the villagers to stop the ritual. They stop, which prevents Varen from permanently taking Mateo's body.

Varen kills Father Mateo and reveals its true identity. It kills Ann, Laica, and Shyna during the attack. Mil and Jude are the only survivors of the church attack and barely escape. Father Mateo had taught Mil how to seal an evil spirit, so Mil and Jude try to purify the candles, Bible, and cross. These items are sacred and can drive away or seal an evil spirit once they have been purified. Jude guards outside House 3 while Mil returns to the church, collects the items, and brings them back. Mil successfully makes the items holy and sacred, but Varen is still hunting them. Jude screams outside; Mil investigates too late and finds Jude dead. When Mil returns inside House 3, Varen is already there and kills Mil.

Laica becomes the **White Lady** after death because she cannot accept that she died. Jude becomes the **Tikbalang** for the same reason. They remain trapped as spirits in Malawak Forest.

In the present day, the player is a normal adventurer on vacation who unexpectedly finds an isolated town in the middle of Malawak Forest. Curiosity leads the player to explore the three houses, guard house, and church and uncover what happened to the village.

## Supernatural AI Characters

The intended game roster contains exactly three supernatural AI characters:

1. **Varen**
2. **The White Lady**
3. **The Tikbalang**

### Varen

Varen is a shadow spirit that observed Malawak Forest and possessed Father Mateo, the village's highest authority, to gain trust and perform a ritual. It is the character currently called the mutant in some code and Unity object names. The old implementation name is `MutantAI`; it does not mean Varen is a separate creature.

The project contains two Varen behavior systems:

- `MutantAI.cs`: the older Varen AI.
- `MonsterAI_New.cs`: the newer Varen AI with the Q&A encounter.

### The White Lady

The White Lady is Laica's spirit. Varen killed Laica during the church attack, and she became the White Lady because she could not accept her own death. She appears at the edge of the forest, standing still and watching. Legend says that anyone who looks into her eyes will see their own death.

A separate `WhiteLady.cs` implementation was not found among the current top-level scripts.

### The Tikbalang

The Tikbalang is Jude's spirit. Varen killed Jude outside House 3, and he became trapped in the forest because he could not accept his death. The Tikbalang is a half-man, half-horse creature that misleads travelers, confuses them, and leads them into traps. If its footsteps are heard behind the player, the player should not turn around.

A separate `Tikbalang.cs` implementation was not found among the current top-level scripts.

### Shadows And Voices

The forest contains shadows and voices that may sound like the player's mother, friends, or the player. The warning is: **Do not answer.**

`ShadowEntity.cs` implements a separate shadow apparition that moves between waypoints, flickers, fades when watched, and may disappear after completing its route. It is a supernatural effect/entity, but it is not listed as one of the three primary AI characters in the story roster.

## Story And Scene Flow

The enabled build scenes are:

1. `IntroScene`
2. `menu`
3. `chapter 1`
4. `chapter 2`
5. `outro`

### Intro

`IntroVideo.cs` plays the intro video, allows the player to skip it with any key, fades the screen, and loads the menu. The video and narration should be updated to tell the 1989 Malawak Forest story.

`StoryIntro.cs` and `IntroNarration.cs` still contain older 1969 lore. They must be rewritten before release so the in-game introduction matches the canonical 1989 Malawak Forest story above.

`IntroNarration.cs` contains an older narration about 1969, the three missing people, and the abandoned church. When loading a saved game, the intro can be skipped using the `SkipIntro` PlayerPrefs flag.

### Menu

`MainMenu.cs` provides the styled main menu, animated buttons, settings, and about panel.

`MenuManager.cs` controls starting and loading games. It also displays separate Normal and Hard save progress.

### Outro

`OutroManager.cs` plays the outro video, allows it to be skipped, fades out, and returns the player to the menu.

## Player Controls

`PlayerController.cs` controls first-person movement:

- `WASD` or Unity `Horizontal`/`Vertical` axes: move
- Mouse: look around
- `Left Shift`: sprint forward
- `Space`: jump
- `Left Control`: crouch
- `Escape` or `P`: pause or resume
- `M`: open or close the map
- `E`: interact, pick up items, open doors, or continue the ritual
- `F`: flashlight toggle, or close books/picture frames
- `G`: drop the selected inventory item
- Mouse wheel: switch selected inventory item

The cursor is locked during normal gameplay and released for menus, readable documents, picture frames, and Q&A.

### Reading And Viewing Lock

Books, notes, and picture frames use a shared `PlayerController.IsReadingDocument` lock. While one is open, player movement and mouse look are disabled, and the cursor stays unlocked and visible. Closing the current document/viewer restores normal controls. This shared lock also prevents other interaction scripts from repeatedly hiding the cursor while a document is open.

### Hard-Mode Stamina

Hard mode uses `StaminaController.cs` on the Chapter 2 player. Holding `Left Shift` drains a 100-point stamina bar over eight seconds. After a 0.5-second delay without sprinting, stamina recovers over about five seconds. At zero stamina, the player walks until stamina recovers. Normal mode has unlimited sprinting and does not show the stamina bar.

The current stamina value is saved in the mode's SQLite database and restored when a Hard-mode save is loaded.

`CameraHeadBob.cs` checks whether sprinting is actually permitted. If stamina reaches zero while Shift is still held, the camera and held item use the normal walking bob instead of sprint bob.

## Exploration And Interactions

Most interactions use a raycast from the center of the player's screen.

- `DoorInteraction.cs`: opens and closes doors, plays audio, supports locks, and can control a linked door.
- `DrawerInteraction.cs`: slides drawers open and closed, supports locks, and avoids toggling when the player is aiming at a pickup.
- `PictureFrame.cs`: opens a picture viewer with a title, image, description, fade animation, and close controls. Viewing a frame locks player movement and keeps the cursor visible.
- `Book.cs`: opens a multi-page Canvas UI book. `E` moves forward, `Q` moves backward, `Escape`/`F` closes it, and the Back/Next/Close UI buttons are clickable.
- `note.cs`: opens a Canvas UI note and can trigger objectives, audio, and subtitles when closed.
- `ItemHighlight.cs`: handles item highlighting.
- `MapSystem.cs`: opens a map and disables player movement while it is open.

### Canvas HUD And Reader UI

The gameplay Canvas in both `chapter 1` and `chapter 2` contains the UI controllers for the flashlight battery, book, and note. Reopening a scene in Unity creates their editable Canvas children; save the scene afterward to keep those children in the Hierarchy.

- `StaminaBar`: Hard-mode stamina Slider. It is visible only in Hard mode.
- `FlashlightBatteryBar`: flashlight battery Slider. It is visible only while a flashlight is held.
- `Book Prompt` and `Book Reader UI`: the book interaction prompt and reader panel. The book reader uses an old-paper background with Back, Next, and Close buttons.
- `Note Prompt` and `Note Reader UI`: the note interaction prompt and reader panel. The note reader uses the same old-paper background and Close button.

The parchment texture is `Assets/Resources/UI/OldPaperBookBackground.png` and is loaded by both reader interfaces.

`PauseMenu.cs` brings the Pause and Settings panels to the last Canvas sibling when opened, so they render above the book and note UI. `PictureFrame.cs` does not draw its legacy GUI while the game is paused, so it cannot cover the pause panel.

## Inventory And Items

`Inventory.cs` stores the player's collected objects. The default capacity is three items.

When an item is selected, it is attached to the camera and shown in the player's hand. When deselected, it is detached and hidden. Items can be dropped in front of the player.

Item systems include:

- `PickupItem.cs`: generic pickup interaction.
- `Key.cs` and `KeyUse.cs`: key collection and door unlocking.
- `FlashlightPickup.cs`: flashlight state, battery drain, and `F` toggle.
- `BatteryPickup.cs` and `BatteryUse.cs`: battery collection and flashlight recharging.
- `CandleItem.cs`: candle behavior and light while held.
- `CandleHolder.cs`: places candles at the ritual location.
- `TableHolder.cs`: places the Bible and cross at the ritual location.
- `DrawerItemParent.cs`: parents loose items placed inside a drawer.
- `PrefabManager.cs`: spawns dropped item prefabs.

## Ritual Progression

The player must collect and place:

- Two candles
- A Bible
- A cross

`CandleHolder.cs` and `TableHolder.cs` remove placed items from normal pickup behavior and save their placement state.

`RitualManager.cs` checks whether all four required items are placed. Once they are present:

1. Whisper audio plays.
2. The game waits before turning off the lights.
3. Ritual audio plays.
4. Candle lights fade out.
5. An objective is triggered.
6. The Varen prefab is spawned at the configured spawn point.

`RitualTree.cs` controls the final sealing stage. Once the ritual is complete, the player looks at the ritual tree and holds `E` for the configured duration, normally 15 seconds. The ritual fades the lights, disables/fades Varen, triggers the final objective, fades to black, and loads the outro scene.

Successfully completing the ritual tree awards 5 progression points (5% of the 100-point progression total) once and immediately saves that reward before the outro loads, so it appears in the load panel.

## Objectives And Reveals

- `ObjectiveManager.cs`: displays persistent objectives with fade and flash effects.
- `ObjectiveTrigger.cs`: displays temporary objective notifications.
- `ObjectiveTriggerActivator.cs`: activates objectives from events.
- `PickupTriggerSpawner.cs`: reveals objects after another pickup and can save its activation state.
- `ItemSubtitleTrigger.cs` and `PlayerSubtitleTrigger.cs`: display subtitle events.

## Varen AI And Q&A

`MonsterAI_New.cs` is the newer Varen behavior system. It uses a `NavMeshAgent` and supports:

- Patrol movement
- Player detection by distance
- Optional field-of-view checking
- Obstacle and bush detection
- Chasing
- Jumpscare animation and audio
- Q&A encounters
- Teleporting away after an answer

When Varen catches the player:

1. Varen triggers the jumpscare.
2. Player movement is disabled.
3. Other `MutantAI` instances and their NavMesh agents are frozen.
4. The Q&A panel appears.
5. One question is selected randomly.
6. The player has 30 seconds by default.

Answer results:

- Correct answer: shows feedback, teleports Varen away, and ends the encounter.
- Wrong answer: damages the player by 15 by default, teleports Varen away, and ends the encounter.
- Timeout: follows the wrong-answer behavior.

The Q&A panel is hidden while the game is paused and appears again when the game resumes. Its buttons are automatically connected to the active `MonsterAI_New` instance.

## Health, Death, And Checkpoints

`PlayerHealth.cs` manages health, damage effects, temporary invulnerability, and death.

`CheckpointTrigger.cs` saves the first checkpoint reached in the current scene. Respawning:

1. Fades the screen to black.
2. Moves the player to the checkpoint.
3. Restores player health.
4. Re-enables player control.
5. Resets the legacy jumpscare system.
6. Resets the ritual tree.
7. Resets the older `MutantAI` to roaming.
8. Fades back into the game.

## Save System

`SaveSystem.cs` uses SQLite and persists across scenes. It stores:

- Player position, rotation, health, and sensitivity
- Inventory items
- Door states and rotations
- Ritual completion
- Read notes
- Checkpoint information
- Dropped items
- Flashlight battery/state
- Hard-mode stamina (`StaminaData` table)
- Used keys
- Batteries and their state
- Ritual item reveal, placement, and drop state
- Progression data

Save files are stored using:

- Normal: `gameSave.db`
- Hard: `gameSave_Hard.db`

The save path is inside Unity's `Application.persistentDataPath`, so it changes appropriately for Windows, macOS, and Linux.

`PauseMenu.cs` saves before returning to the menu. Automatic saving also occurs when the application quits, if enabled.

Completed saves (100% progression) are retained but cannot be loaded from the load panel. The panel labels them `Completed` and keeps their load button visible but disabled.

`ProgressionSystem.cs` tracks progression points up to 100. `ProgressionTrigger.cs` awards points once per trigger and stores each trigger's completed state separately for Normal and Hard mode. Starting a new game clears the progression value and trigger states.

## Audio And Visual Effects

Audio systems include:

- `AudioManager.cs`: global volume and settings.
- `AudioTrigger.cs`: scene-triggered audio.
- `FootstepSound.cs`: player footsteps.
- `ButtonSound.cs`: UI hover/click sounds.
- Local audio on doors, drawers, ritual objects, books, notes, and jumpscares.

Visual and atmosphere systems include:

- `CameraHeadBob.cs`: head bob and external camera shake.
- `ScreenEffects.cs`: scanlines and vignette effects.
- `LightFlickerTrigger.cs`: flickering lights.
- `LightPulser.cs`: pulsing lights.
- `ScaryEffectTrigger.cs`: heartbeat, blood, static, whispers, color drain, vignette, and shake.
- `ShadowTrigger.cs`: shadow-related scene events.
- `SceneFader.cs`: scene transition fades.

## Known Implementation Notes

- The story has three primary AI characters, but only Varen currently has named AI code in the top-level scripts. White Lady and Tikbalang need their own AI scripts if they are meant to actively chase, attack, or interact with the player.
- The Varen script is still called `MonsterAI_New.cs`, and the older script is `MutantAI.cs`. Renaming them requires updating Unity component and Inspector references.
- Both old and new jumpscare systems exist: `JumpscareSystem.cs` and `JumpscareSystemNEW.cs`.
- The checkpoint code explicitly resets the older `MutantAI`; verify that the active Varen prefab uses the intended AI system.
- Inventory capacity is three, while the ritual requires four objects. The player must place or drop at least one item during the ritual setup.
- Picking up any inventory item now selects it immediately and keeps its model visible in the player?s hand. This does not change the existing save/load data format.
- The ritual UI still contains the word `Munduan` in one location, while the story name is Varen. Change it if `Munduan` is not intended as an alternate name.
- An active flashlight now stays attached to the camera and continues lighting and draining its battery when the player selects another bag item; dropping it and battery depletion still turn it off.
- Some save operations use empty exception handlers, so certain database failures may fail silently.
- Health is saved with the player data and now restores through `PlayerHealth.RestoreHealth`, which refreshes the HealthBar immediately. The bar uses a green-to-red status color and the label displays `HP current / max`; older saves with no HP values start at the Inspector maximum.
- `SQLite4Unity3d` uses native `sqlite3` calls. Windows, macOS, and Linux builds must each be tested with saving and loading.
- The old circular HealthBar image is hidden when the game starts and replaced by a rectangular Canvas `HealthBarSlider`, matching the stamina-bar style. Its size and position can be adjusted on `PlayerHealth` with `Health Bar Size` and `Health Bar Position`.
- Video playback depends on the codecs supported by the target operating system.

## Story Details Still To Decide

- Where Varen originally came from and why it entered Malawak Forest.
- What clues Mil leaves behind before Varen kills him.
- What the player can do to help, release, or avoid Laica and Jude's trapped spirits.
- Why the Q&A challenge exists in the story.
- What the final sealing does to Varen.

## Bag UI Canvas Layout

- The bag uses three bottom-center Canvas slots. Bag UI and its Bag Slot children are created under the Canvas in Edit mode, so their Rect Transforms can be manually repositioned. Use **Rebuild Bag UI in Canvas** on BagUI if the scene needs a fresh layout.

- Bag UI was restyled to match the three-frame reference: a transparent layout container with three 94x94 bottom-center slot frames, no BAG title, no hint text, and no large panel background.

## 3D Bag Item Previews

- BagUI now requests a 3D preview for each stored item instead of displaying its flat material texture.
- InventoryItemPreview creates a hidden camera and a safe visual-only copy of each pickup prefab, rendering it to the slot without changing the real item in the player's hand.

## Transparent Item Icons

- Added Assets/Resources/UI/InventoryItemIcons.png, a transparent six-icon sheet made from the supplied references.
- Bag slots now use the supplied transparent icon for recognized batteries, candles, keys, flashlights, crosses, and Bibles. Other inventory items keep the 3D preview fallback.

- Removed the selected-slot yellow tint from Bag UI. Every frame, slot icons are reset to white and frames to their normal brown color.

## Bag Selection Clarity

- The currently equipped bag slot now has a separate gold outline outside its frame; icons themselves remain their original colors.
- Mouse-wheel selection now cycles through all three bag slots, including empty slots. Selecting an empty slot hides the previously held item but does not drop it.

- Split the transparent item sheet into individual Battery, Candle, Key, Flashlight, Cross, and Bible PNG icons. Bag UI now loads each icon separately, so images cannot spill into neighboring slots.

- Fixed Bag slot frame darkening: the frame image is now kept at Color.white during play so Unity displays its original wood colors instead of tinting it nearly black.

## Canvas Subtitle UI

- Replaced SubtitleManager OnGUI subtitles with a Canvas-based Subtitle UI hierarchy: background, accent line, shadow text, and subtitle text.
- The root is created under the Canvas in Edit mode. Move or resize Subtitle UI with Rect Transform to choose where subtitles appear; use **Rebuild Subtitle UI in Canvas** on the manager if needed.

- Subtitle fading now uses a CanvasGroup on Subtitle UI instead of resetting the background, accent, shadow, and text colors every frame. Manual UI colors and transparency are preserved during play.

## Editable HealthBar UI

- PlayerHealth now creates HealthBar UI under the Canvas in Edit mode, including Health Fill and Health Text children.
- Select HealthBar UI in the Canvas Hierarchy to move or resize it manually. **Rebuild HealthBar UI in Canvas** on PlayerHealth recreates the default layout when needed.

- Removed the legacy HealthBar UI during editor/runtime setup. If its DamageVignette was nested inside it, the script first reparents that vignette to the Canvas so damage feedback remains functional. The new HealthBar UI remains the only health bar.

- Prevented duplicate HealthBar UI objects: PlayerHealth now searches the Canvas for an existing health slider and reconnects to it before creating a new one.


## AI Script Ownership

- **Varen** uses `MutantAI.cs`.
- **White Lady** and **Tikbalang** share `MonsterAI_New.cs`.
- Future Varen behaviour changes must be made in `MutantAI.cs`; changes to `MonsterAI_New.cs` affect White Lady and Tikbalang.


## Varen Catch Damage and Respawn

- Varen (`MutantAI.cs`) deals 30 HP damage when he catches the player.
- If the player survives, the jumpscare returns them to their latest checkpoint while preserving their remaining HP; checkpoint respawns from other causes still restore full HP.
- If Varen's hit reduces HP to zero, the active chapter reloads from its beginning with full health. This restart does not delete the player's manually saved database data.


## Persistent One-Time Subtitles

- Pickup subtitles (`ItemSubtitleTrigger`) and area subtitles (`PlayerSubtitleTrigger`) now receive stable hidden IDs and can display only once.
- The SQLite `SubtitleData` table saves which subtitles have already played. Loading a game restores those states, including for inactive subtitle trigger objects.
- Starting a new game or deleting a save clears the subtitle records together with the other saved game data.

- Subtitle triggers now also write their `SubtitleData` record immediately when activated and check SQLite before displaying, so they remain one-time even before the next full Save Game.

- `SubtitleData` now has a visible `IsTriggered` boolean column. Every activated subtitle row is stored with `IsTriggered = true`; older subtitle tables are upgraded automatically when the database initializes.



## Reliable Blood Damage Shader

- Added ReliableBloodDamage.shader, a simple UI blood overlay shader with a material stored in Resources.
- PlayerHealth reparents the existing DamageVignette directly to the Canvas, stretches it full screen, and gives it a high Canvas sorting order so every damage source including wrong Q&A answers shows the effect.
- A red UI fallback remains visible if the shader material cannot load; use **Blood Effect Intensity** on PlayerHealth to tune the shader strength.

- DamageVignette is now moved directly under the Canvas in Edit mode. Select it in the Canvas Hierarchy to manually adjust its Rect Transform; PlayerHealth preserves those manual values during play.

- Moved the saved DamageVignette object directly under the Canvas in Chapter 1 and Chapter 2. It is no longer nested inside HealthBarBackground, so it is visible in the Canvas Hierarchy and can be manually adjusted.


## Pushing the Backup Branch to GitHub

Run these commands in the Unity project folder:

```powershell
git switch backup
git status
git add.
git commit -m "Describe your changes"
git push -u origin backup
```

- Use `git status` first to review files before committing.
- Replace `Describe your changes` with a short note, such as `Fix health UI`.
- `git push -u origin backup` uploads the branch to GitHub. Future pushes from the same branch only need `git push`.

- Varen now explicitly requests the Canvas blood overlay for 1.2 seconds after each successful hit. The old JumpscareSystem GUI no longer draws its dark/red background layer, so it cannot hide the Canvas blood effect; its warning text remains.

## Unity Editor MCP Attempt

The first Unity MCP package tested (`com.emeryporter.unitymcp` 2.2.0) was removed. Although it declared Unity 2022.3 support, it used newer Unity API members such as `Rigidbody.linearDamping`, so it cannot compile in this project s Unity 2022.3.62f3 Editor. Its local Codex endpoint was removed as well.

The project package manifest is restored without that MCP package. Reopen Unity so Package Manager clears the old Safe Mode errors before considering a different, verified Unity 2022.3-compatible bridge.

## Flashlight Forward Beam

The flashlight now forces its child light to use a Spot light and exposes **Spot Angle**, **Inner Spot Angle**, and **Shadows** in `FlashlightPickup` s Inspector. The flashlight prefab uses a focused 42 outer cone, 28 inner cone, and its existing 80.8 range. Its active fog settings were corrected from a 12.2-unit end distance and density `23.68` to a clearer 20 100 range with density `0.01`, allowing the forward beam to reveal objects farther away.

You can tune the beam later by selecting `Assets/ANDRIE/DROPPED ITEMS/Flashlight.prefab` and changing the **Beam Shape** values on `FlashlightPickup`. Smaller angles make a narrower, longer-looking beam; larger angles light more of the surrounding area.

- Flashlight fog control is now disabled on the Flashlight prefab. Turning the flashlight on no longer changes global map fog or brightens the whole scene; only its focused beam is used.

- Flashlight beam balance was increased: intensity 120, range 120, outer cone 52, and inner cone 34. This keeps a long focused beam while softly lighting more of the nearby surroundings without changing global fog.

- Fixed the held flashlight beam direction: FlashlightPickup now aims its child Spot Light at Camera.main in LateUpdate while held. The model can remain rotated naturally in the player's hand, but the beam now points straight ahead. The prefab enables **Aim Beam With Camera** by default.

- Flashlight long-distance strength was raised after testing showed the 120-intensity beam only lit close ground: intensity is now 800, range 220, outer cone 58, and inner cone 42. This compensates for Unity spotlight distance falloff while retaining a forward beam.

- Flashlight fog reduction is enabled again with a gentle density of `0.04` while on. Chapter 1's normal exponential-squared fog density is `0.06`, so this improves long-range visibility without the overly bright result caused by the earlier `0.01` setting. The original fog is restored immediately when the flashlight turns off.

- Removed all flashlight fog controls and `RenderSettings` access. The flashlight now affects only its own Spot Light; scene fog is entirely controlled manually in Unity's Lighting/Render Settings. This also prevents the flashlight cleanup from accessing RenderSettings during Unity shutdown.

## Language Selection Settings

The menu scene's existing language buttons now save a selected language in `PlayerPrefs` under `GameLanguage`:

- `ENGLISH` selects English (`0`).
- `KOREA` selects Korean (`1`).
- `TAGALOG` selects Tagalog (`2`).

`SettingsPanel` highlights the selected language and keeps the choice after restarting the game. The buttons no longer call `SetQuality`, so choosing a language does not change graphics quality. This is the language-selection foundation; localized text and Korean/Tagalog font support are the next step.

### Korean menu text

- Added `MenuLocalization.cs`, which changes supported settings labels when Korean is selected and restores their original English text when English is selected.
- `SettingsPanel` now has a **Korean Font** field. It must reference `NotoSansKR TMP Font Asset` in `Assets/OTHERS/Noto_Sans_KR` for Korean characters to display.
- Volume, sensitivity, and brightness labels are translated dynamically. Tagalog remains English until Tagalog translations are added.
- The stylized main-menu title and buttons (`VAREN`, Load Game, Play, Settings, About, Quit) are deliberately kept in English and retain their original horror font.

- Main-menu protection now ignores the decorative spaces in labels such as `V A R E N` and `P L A Y`, so Korean selection does not replace their original horror font.

- Opening **Controls** in the settings panel now hides the Language heading and all language buttons. Returning to Settings shows them again.

- Korean localization now covers the Controls title, both control instruction lists, Graphics Quality, Save, and Back. Translation matching ignores decorative spaces and line breaks.
- `RefreshUI()` now preserves the active volume, sensitivity, and brightness values instead of reloading older PlayerPrefs values when the settings UI or language is changed.

## Story Intro and Gameplay Korean Translation

- Replaced the StoryIntro narrative with the Malawak Forest story: Father Mateo, Varen, the broken church ritual, Mil and Jude, the sacred items, the White Lady, the Tikbalang, and the present-day adventurer.
- Added Korean versions of the story intro and common gameplay subtitles. SubtitleManager now translates text at display time and switches to Noto Sans KR in Chapter 1, Chapter 2, and IntroTimeline.

- Selecting a language now captures the live slider values before refreshing labels, preventing volume, sensitivity, and brightness from displaying stale zero values.

- Added a one-time settings repair for the previous language-switching bug: if all three saved values are exactly zero, the menu restores 100% volume, 5.0 sensitivity, and 50% brightness. Individual intentional zero values are preserved.

- AudioManager defaults now match SettingsPanel (100% volume, 5.0 sensitivity, 50% brightness). The one-time zero-settings repair was advanced so existing affected PlayerPrefs values are repaired on the next launch.

- Settings labels now display the actual Slider values directly, preventing Volume, Sensitivity, and Brightness text from showing stale `0` values after a correct save/load.

- SaveSettings now captures the visible Volume, Sensitivity, and Brightness Slider values immediately before writing PlayerPrefs, so restarting the game loads the settings the player actually saved.


## Tagalog Localization

- The existing PH menu button now selects Tagalog.
- Settings labels, graphics options, controls instructions, story intro, and supported gameplay subtitles have Tagalog translations.
- The stylized VAREN title and left-side main menu artwork text intentionally remain English.

## Settings Panel Startup Visibility

- The visual SettingsPanel now hides automatically when the menu starts.
- The separate Setting pannel cs manager remains active, so saved slider values and language still load correctly.

## Settings Database

- Menu settings now use a separate SQLite database named settings.db in the game's persistent-data folder.
- Its SettingsData table stores volume, sensitivity, brightness, graphics quality, and selected language in one global row.
- Existing PlayerPrefs values migrate automatically the first time the updated menu runs. PlayerPrefs remains only as a compatibility cache for older scripts.
- The database is separate from Normal and Hard save files, so opening Settings cannot create a false game save or affect player progress.

For the complete table-by-table database reference, see DATABASE_README.md.

## Documentation Organization

- Project documentation is organized in Assets/Documentation.
- README.md is the entry point; it links to the project details, database guide, and game story.

## Story Intro Controls and Save State

- Left mouse click advances the story intro one sentence at a time.
- Enter skips the remaining story intro.
- IntroData saves the current section and sentence in the active game save. Quitting before completion resumes from that sentence; completing or skipping the intro prevents it from replaying.

## Pause During Story Intro

- The intro overlay now hides while the pause menu is open, keeping the pause panel visible on top.
- Resuming during an active intro keeps player movement disabled until the intro is finished or skipped.

## New Game Intro and Subtitle Reset

- Starting a New Game now clears IntroData and SubtitleData for the selected difficulty after its database is reinitialized.
- The story intro appears again for a New Game, while loading an existing save still resumes or skips it based on IntroData.
- Gameplay subtitle localization now reads the selected language directly from SettingsData, with PlayerPrefs only as a fallback.
