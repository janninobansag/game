# Repository Guidelines

## Project Structure & Module Organization

VAREN is a Unity 2022.3.62f3 project. Gameplay scenes and prefabs live in `Assets/Scenes` and other asset folders. C# gameplay code is grouped under `Assets/Scripts` by system (`AI`, `Audio`, `Core`, `Events`, `Generator`, `Interaction`, `Items`, `Player`, `Save`, and `UI`). `Assets/Editor` contains editor-only utilities and NUnit tests. `Assets/Documentation` holds database references, diagrams, workflows, and player guides. Unity packages and project settings are in `Packages/` and `ProjectSettings/`. Keep each `.meta` file with its asset; scenes and prefabs depend on Unity GUIDs.

## Build and Run

Open the repository root in Unity Hub with **Unity 2022.3.62f3**. Open `Assets/Scenes/IntroScene.unity` and press **Play** to run the game. Build desktop releases through **File > Build Settings**; install the matching Windows, macOS, or Linux Mono module first, and build each target into a separate empty folder. The active build scene order is documented in `README.md`.

## Coding Style

Use C# with four-space indentation and Allman braces. Name types, methods, and public properties in `PascalCase`; use `camelCase` for private fields. Match a script filename to its main component class. Keep partial classes grouped by responsibility, following patterns such as `SaveSystem.Save.cs`. Preserve serialized field names and `.meta` files when editing Unity assets.

## Testing

Run editor tests from **Window > General > Test Runner > EditMode > Run All**. Tests use NUnit and currently live in `Assets/Editor/VarenSaveAndGameplayTests.cs`; name cases descriptively, for example `SaveSchema_UsesVersionSix`. No coverage threshold is defined. Tests may access settings in Unity's persistent-data folder, so use a disposable test profile when practical. Before release, verify New Game, Save, Quit, and Load on each target platform.

## Commits and Pull Requests

Recent commits use short imperative subjects such as “Add map objective activation” and “Refactor save and menu systems.” Branch names commonly use categories such as `feat/`, `chore/`, and `refactor/`. PR descriptions should explain the change, identify affected scenes or systems, state the Unity version and verification performed, and include screenshots for visible scene or UI changes. Update database documentation and ERDs with schema changes.

## Generated and Local Data

Do not commit Unity-generated folders (`Library/`, `Temp/`, `Build/`, `Builds/`) or local save databases. Player saves and settings belong under `Application.persistentDataPath`, outside the repository.
