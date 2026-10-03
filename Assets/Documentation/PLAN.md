# Chapter 2 Mansion Escape Plan

## Summary

Chapter 2 begins in the first-floor bedroom. The player spends roughly 15–20 minutes searching the mansion, finds the key to the upstairs Tikbalang bedroom, releases the Tikbalang, retrieves the mansion master key, and escapes through the main front door. After escaping, Chapter 2 continues through the guard house, Houses 1–3, church, ritual, and Varen sealing sequence.

## Gameplay Route

1. The player wakes in the first-floor bedroom with the mansion’s front door locked.
2. A note explains that the front-door master key was taken to the upstairs bedroom.
3. The `Bedroom key` randomly appears at one of four first-floor locations:
   - Office desk or drawer
   - Waiting-area furniture
   - Kitchen cupboard or drawer
   - Storage-room cabinet
4. Each possible location receives a nearby written or environmental clue. Only the clue matching the chosen spawn is enabled for that playthrough.
5. The player crosses the living room and kids’ play area, then uses the stairs.
6. The second-floor kids’ bedroom remains optional and contains lore, batteries, or healing supplies.
7. The player crosses the long second-floor hallway and unlocks the far-left bedroom with the `Bedroom key`.
8. Entering the doorway activates the existing `TikbalangJumpscareTrigger`. The first jumpscare plays, then the Tikbalang begins chasing.
9. The fixed `Mansion key` is inside that bedroom. The player collects it and returns across the second floor, down the stairs, and through the living room.
10. The Mansion key unlocks the main front door. The player exits without loading another scene and continues toward the guard house.

## Implementation Changes

- Reconfigure the `Bedroom key` to use the existing random spawn system and target the Tikbalang bedroom door.
- Add matching clue references to `RandomKeySpawn`, enable only the clue associated with the saved spawn index, and keep that index stable across saves.
- Parent spawned drawer keys to their selected spawn marker and keep their Rigidbody kinematic until pickup so they stay flat and move with the drawer.
- Place the `Mansion key` permanently inside the Tikbalang bedroom and remove random spawning from it.
- Configure the Mansion key to unlock only the main front door.
- Position the Tikbalang trigger across the bedroom threshold so the key cannot be collected before the first encounter.
- Keep the Tikbalang dormant until that trigger succeeds. Persist the released state so loading a save does not replay the first jumpscare.
- On a loaded post-encounter save, disable the trigger and restore the Tikbalang directly to its active chase state.
- Keep the wrench dedicated to the generator tank cover.
- Keep the White Lady outside near the church and leave the existing post-mansion Chapter 2 route unchanged.
- Rebuild and verify the mansion NavMesh so the Tikbalang can travel through the bedroom doorway, second-floor corridor, stairs, living room, and first-floor rooms.

## Test Plan

- Start a new Chapter 2 game and confirm the player appears in the first-floor bedroom.
- Test every randomized Bedroom key location and confirm its matching clue, rotation, drawer parenting, pickup, and saved spawn index.
- Confirm the Bedroom key cannot unlock the front door and the Mansion key cannot unlock the Tikbalang bedroom.
- Confirm opening and entering the Tikbalang bedroom starts the first jumpscare once.
- Confirm the Tikbalang can chase across both floors and open usable doors without becoming stuck on the stairs.
- Save and reload before and after releasing the Tikbalang; verify keys, doors, clues, and encounter state restore correctly.
- Unlock the front door and confirm the player can continue outside toward the guard house without loading the outro.
- Confirm the White Lady remains near the church and the existing guard house, Houses 1–3, church, ritual, and Varen sealing progression still works.

## Assumptions

- The far-left second-floor bedroom is the Tikbalang bedroom.
- The central entrance below the first-floor storage rooms is the main mansion exit.
- Mansion guidance uses notes and environmental clues without HUD objectives or location markers.
- The second-floor kids’ bedroom provides optional rewards and lore rather than another required key.
