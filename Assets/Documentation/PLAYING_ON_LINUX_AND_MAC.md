# Playing VAREN on Linux and macOS

This guide is for players who downloaded a VAREN **Linux** or **macOS** ZIP release. You do not need Unity or the source code to play.

> Extract the ZIP file first. Do not try to run the game from inside the ZIP archive.

## Linux

The Linux release folder contains files similar to this:

```text
VAREN.x86_64
UnityPlayer.so
VAREN_Data/
MUNDUAN_BurstDebugInformation_DoNotShip/   (optional debug information)
```

Keep `VAREN.x86_64`, `UnityPlayer.so`, and `VAREN_Data` together in the same folder. Moving or deleting `VAREN_Data` or `UnityPlayer.so` prevents the game from starting.

### Start the game with the file manager

1. Extract the Linux ZIP to a normal folder, for example `Downloads/VAREN-Linux`.
2. Right-click `VAREN.x86_64` and open **Properties** or **Permissions**.
3. Enable **Allow executing file as program** (the exact wording differs by desktop environment).
4. Double-click `VAREN.x86_64` and choose **Run** when asked.

### Start the game with Terminal

1. Open Terminal.
2. Change to the folder where you extracted the game. Example:

   ```bash
   cd ~/Downloads/VAREN-Linux
   ```

3. Run these as **two separate commands**:

   ```bash
   chmod +x VAREN.x86_64
   ./VAREN.x86_64
   ```

The first command gives the game permission to run. You normally only need to do it once. The second command starts VAREN.

### Linux compatibility and troubleshooting

- The build is for **64-bit Linux (`x86_64`)**. It can run on Kali Linux, Ubuntu, Mint, Fedora, and other compatible 64-bit desktop distributions.
- There is no required `.sh` file. `VAREN.x86_64` is the game launcher.
- If the game does not open, run `./VAREN.x86_64` from Terminal and keep the Terminal open. Any error shown there helps identify the missing dependency.
- If a desktop environment blocks the file, use the Terminal steps above instead.

## macOS

The macOS release contains an application bundle such as `VAREN.app` or `Mac.app`.

### Start the game

1. Extract the macOS ZIP in Finder.
2. Keep the entire `.app` bundle together. Do not open or move files inside `Contents`.
3. Move the app to **Applications** or leave it in a normal folder such as Downloads.
4. Control-click (or right-click) the app and choose **Open**.
5. In the security prompt, choose **Open** again.

The first Control-click, then Open action is important because an independently shared game build may not be notarized by Apple. After accepting it once, future launches can use a normal double-click.

### If macOS blocks the app

If macOS says the developer cannot be verified:

1. Try Control-clicking the app and selecting **Open** first.
2. If it is still blocked, open **System Settings > Privacy & Security**.
3. Find the VAREN message near the bottom and choose **Open Anyway**.
4. Confirm **Open**.

### Apple Silicon MacBooks (M1, M2, M3, M4)

The current macOS release is an **Intel 64-bit** Unity build. It runs on Intel Macs directly. Apple Silicon Macs can run it through Rosetta 2.

If Rosetta is not already installed, open the macOS **Terminal** app and run:

```bash
softwareupdate --install-rosetta --agree-to-license
```

Enter an administrator password if macOS asks. When installation finishes, open the VAREN app again from Finder.

## Save files

VAREN creates save files automatically on the player's computer. They are separate from the downloaded game folder.

- Linux: Unity stores saves under the user's local application-data folder.
- macOS: Unity stores saves under the user's Library/Application Support folder.

Do not delete the save files unless the player intentionally wants to start over.

## Before reporting a problem

Include these details when reporting an issue:

- Operating system and version
- Whether the computer is Intel/AMD or Apple Silicon
- Whether the game was extracted before opening
- A screenshot of the error, or the Terminal output on Linux
- Whether the problem happens on a New Game or after loading a save