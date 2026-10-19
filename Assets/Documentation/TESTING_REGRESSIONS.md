# Automated PlayMode integration tests

These tests complement the isolated EditMode unit tests. They create temporary runtime fixtures, exercise multiple gameplay components together, and use disposable databases under Unity's temporary cache directory. The tests restore the `PlayerPrefs` keys and static singleton state they touch and remove their temporary database files during teardown.

## Test Runner instructions

1. Open **Window > General > Test Runner**.
2. Select **PlayMode** and choose **Run All** to run IT-001 through IT-011.
3. Select **EditMode** and choose **Run All** to run the existing unit-test suite.
4. Copy the observed result into the testing-plan document only after each corresponding test has run. Do not treat an expected result or an unrun test as a pass.

## Latest automated run

On 2026-10-20, Unity Test Runner reported **11/11 PlayMode tests passed** and **45/45 EditMode tests passed**, with no failures. This verifies the automated fixture/database scenarios only; the scene-driven and sensory checks below remain manual.

The Windows 64-bit Player build also completed successfully to `Temp/VarenIntegrationBuild_20261020`. The output contains `Varen.Game.dll` and `SQLite4Unity3d.dll`; `Varen.PlayModeTests.dll` is absent, confirming the PlayMode test assembly is excluded from the Player.

The runtime scripts are compiled into `Varen.Game`; `Varen.PlayModeTests` is a test-only assembly and is excluded from normal Player builds. The SQLite source has its own `SQLite4Unity3d` assembly so the game assembly can reference it.

| Test ID | Automated PlayMode test | Automated scope | Actual result |
| --- | --- | --- | --- |
| IT-001 | `IT001_InventoryKeyUnlocksMatchingDoorAndConsumesKey` | Adds a matching key to Inventory, invokes the key interaction against a locked Door, then checks unlock/open and key consumption. | PASSED |
| IT-002 | `IT002_InventoryItemPlacement_PersistsRitualPlacement` | Places a Bible from Inventory into a TableHolder and checks ownership, placement, and pickup state. | PASSED |
| IT-003 | `IT003_SaveLoad_RestoresPlayerInventoryAndDoorState` | Saves to a disposable database, changes player/inventory/door state, and checks restoration. | PASSED |
| IT-004 | `IT004_TikbalangQnA_CorrectAnswerKeepsHealthAndReleasesEncounter` | Runs the Tikbalang catch/Q&A coroutine with a runtime answer-button fixture and checks health and encounter release. | PASSED |
| IT-005 | `IT005_GeneratorReload_RestoresInsertedKeyFuelAndLights` | Loads saved generator fuel/key state into runtime components, then checks the saved light state through Save/Load. | PASSED |
| IT-006 | `IT006_SettingsDatabase_AppliesAudioAndMenuLocalization` | Loads isolated settings data, applies audio values, and checks Tagalog menu localization. | PASSED |
| IT-007 | `IT007_SequentialEncounters_PausesAndResumesOtherAI` | Checks that a second AI's animation pauses while another owns the encounter and resumes after release. | PASSED |
| IT-008 | `IT008_WhiteLadyOneShot_LoadRestoresAndNewGameClearsTrigger` | Saves/restores a completed White Lady trigger and checks that clearing one-shot state allows it again. | PASSED |
| IT-009 | `IT009_NoteCloseEvents_LoadDoesNotReplayAndNewGameResets` | Closes a note, checks its audio/objective one-shot state across Save/Load, then checks the New Game reset. | PASSED |
| IT-010 | `IT010_UnpouredGas_LoadKeepsOriginalGasWhileLightsAreOn` | Saves an unpoured Gas can with generator lights on and checks the can remains at its saved position after loading. | PASSED |
| IT-011 | `IT011_RespawnedGas_LoadRestoresSpawnPointWorldPosition` | Runs the generator from a seeded post-pour state, checks Gas appears at one configured spawn point, then checks Save/Load restores that exact world position. | PASSED |

## Manual checks still required

The runtime fixtures do not replace testing the configured gameplay scenes. In particular, manually verify actual collision/physics entry, NavMesh chasing and timing, rapid Tikbalang-to-White-Lady handoff with mouse clicks, real scene reload or application quit/relaunch, visual presentation, and audible audio output. IT-011 seeds the already-poured state because the legacy input system does not provide deterministic simulated key-hold input in this suite; the complete pour interaction remains a manual check.

## Related EditMode unit coverage

- `WhiteLadyQnA_RestoresSharedChoicesAfterTikbalangAnswer` checks shared choices and answer callbacks in isolation.
- `WhiteLadyOneShot_LoadRestoresAndNewGameClearsTrigger` checks the one-shot database policy.
- `OneShotEvents_RestoreNoteAndWhiteLadyWithoutReplaying` checks note/audio and White Lady restoration.
- `RespawnedGas_MarksItsWorldPositionForSaving` checks that a replacement Gas is marked as a dropped world item.
- Generator Gas restoration policy cases cover the combinations of prior pouring, generator lights, active run, and fuel state.
