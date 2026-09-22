# Development Handoff — 2026-09-22

## Latest Git status

- Branch: `update/tikbalang-white-lady`
- Latest pushed commit: `f5a7b53` — `Add Tikbalang animation, audio, terrain and graphics updates`
- Previous related commit: `5297d4d` — `Update Tikbalang chase and White Lady audio and graphics optimization`

## Gameplay updates completed

### Tikbalang

- Added the `Tikbalang.controller` Animator Controller. Its movement parameters are `walking` and `run`.
- Added `Horse Chase.mp3` as the Tikbalang chase sound. Assign it to **Chase Sound** on the `TikbalangAI` component if it is not already assigned.
- After the initial Tikbalang encounter/Q&A, the player seeing Tikbalang with a clear camera view starts his running chase.
- Once seen, Tikbalang keeps running and the chase sound keeps playing until he teleports.
- Teleporting resets the running chase state and stops the chase sound. The player must see Tikbalang again to restart running.
- Added Inspector settings on `TikbalangAI`:
  - **Run Speed**
  - **Chase Sound** and **Chase Volume**
  - **Player Sight Distance** (`0` means unlimited range)
  - **Running Catch Range** (default `2.3`) for a farther running jumpscare
  - **Catch Range** (default `1.4`) remains the walking jumpscare distance
- Wrong or timed-out Tikbalang Q&A answers now use the shared `PlayerHealth` blood/vignette effect after the feedback, so it stays visible when the Q&A panel closes.

### White Lady

- White Lady teleports to assigned `Whitelady spawn` empty objects rather than random locations.
- White Lady stays idle while her Q&A is active and holds the camera on her until the answer/timeout finishes.
- Chase audio stops during Q&A and when the jumpscare begins; the jumpscare sound does not overlap the chase loop.
- Added protection against repeated Q&A panels and NavMesh `Stop` warnings.

### Objectives and ritual

- One-time objective triggers are saved and do not replay after loading a game. Starting a new game clears those saved trigger states.
- Fixed the ritual holder references so Large Candle, Large Candle (1), Cross, and Bible can complete the Varen spawn ritual.

### Graphics, terrain, and performance

- Updated terrain, tree, URP Quality assets, NavMesh, and occlusion-culling bake data for Chapters 1 and 2.
- Occlusion Culling guidance: solid houses/walls/large rocks should be **Occluder Static** and **Occludee Static**. Trees should normally be **Occludee Static** only.
- Standard Unity Terrain renders as a large renderer; it cannot be occlusion-culled one wall-sized section at a time without splitting it into terrain tiles or meshes.

## Before the next development session

1. Open both Chapter 1 and Chapter 2 and test the Tikbalang first encounter, sight-triggered run, teleport reset, Q&A, chase sound, and running catch distance.
2. Test White Lady Q&A: chase sound must stop at jumpscare, camera remains locked during Q&A, and she teleports only after the result.
3. Verify the `Horse Chase.mp3` reference remains assigned in every scene/prefab that contains Tikbalang.
4. Keep Unity `.meta` files whenever adding, moving, or renaming assets so references remain intact.

