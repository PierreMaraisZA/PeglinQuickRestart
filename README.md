# PeglinQuickRestart

Bad start? Hold one button to try again.

## Features

- **Hold X (or L3 on a controller) for 1.5 seconds to restart.** The run is abandoned and a new one starts straight away, without clicking through Abandon Run, the main menu, character select and Play.
- **Same setup every time.** The new run uses the same class and Cruciball level as the one you left.
- **Custom loadouts carry over.** If you edited your starting orbs or relics, the new run starts with the same ones.
- **Seeds work like the game's.** A run started on a custom seed restarts on that same seed, so you can practise a seed over and over. Any other run gets a new random seed, just like pressing Play.
- **Controller support.** Hold **L3** (click the left stick). It's the one button Peglin leaves unbound, so it doesn't clash with anything.
- **No accidental restarts.** A countdown shows at the top of the screen while you hold the key or button. Let go before it ends to cancel.
- **Works anywhere in a run:** on the map, mid-battle, in shops and events, with the pause menu open, and on the game over screen. The hold time is real time, so game speed and pausing don't change it.
- **Remembers your setup.** You can restart a run you continued after closing the game, not just one you started this session.
- **Plays by the game's rules.** The restart goes through the game's own steps, so the new run is set up exactly like one you start by hand. Class unlock progress is saved, and achievements are on or off just as they would be (off for custom runs, as in the base game).

## How it works

A restart does what you would do by hand:

1. **Abandon Run**, exactly like the pause menu option.
2. Go to the **main menu** and press **Play**.
3. Pick the **same class and Cruciball level**, and set the **custom loadout and seed** if the run had them.
4. Press **Play** on the character select screen.

Going through the menus means the game does all its own cleanup and new-run setup, rather than the mod copying it. A restart takes a few seconds while the screens fade.

## Installation

### With a mod manager (recommended)

1. Install r2modman or the Thunderstore Mod Manager.
2. Search for **PeglinQuickRestart** and install it.
3. Launch Peglin.

### Manually

1. Install [BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/).
2. Download `PeglinQuickRestart.dll` from the release.
3. Put it in `Peglin/BepInEx/plugins/`.
4. Start the game once to create the config file.

## Settings

Change these in `BepInEx/config/PeglinQuickRestart.cfg` (start the game once to create it). Changes apply immediately; changing `HoldSeconds` restarts a hold that's in progress.

| Setting | Default | What it does |
|---|---|---|
| `RestartKey` | X | Key to hold. Add modifiers to make it harder to hit by accident, e.g. `X + LeftControl`. [Key names](https://docs.unity3d.com/ScriptReference/KeyCode.html) |
| `ControllerRestartButton` | JoystickButton8 (L3) | Controller button to hold. `None` turns it off |
| `HoldSeconds` | 1.5 | How long to hold the key or button, in real seconds |
| `ShowHoldProgress` | true | Show the countdown while the key or button is held |

The mod also keeps the last run's setup in `BepInEx/config/PeglinQuickRestart.lastrun.txt`. It's safe to delete; you just won't be able to restart a custom run you continue later.

## Compatibility

- Built for **Peglin v2.0.12**.
- Requires [BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/).

## Known limitations

- **A restart counts as abandoning the run.** Like Abandon Run, it resets your "reached the boss" streak if you hadn't reached the boss yet.
- **Custom runs started before installing the mod** can't be restarted, because the mod doesn't know their loadout. Standard runs always can.
- **If a restart fails partway** (for example after a game update), you're left on the main menu and the old run is already abandoned. The reason is shown on screen and written to `BepInEx/LogOutput.log`.
- The controller button assumes an Xbox-style layout, which is what Steam Input uses. Without Steam Input, a PlayStation controller may need a different `ControllerRestartButton`.
- Clicking the left stick can nudge your aim. If you let go early to cancel, check your aim before firing.
- The key and button do nothing on the main menu or while a screen is loading.
- English only.

## Some of my other mods

- [PeglinBalatroScore](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinBalatroScore/) — a Balatro-style pegs × damage score, flames and a bomb chest.
- [PeglinFonts](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinFonts/) — change the font of every text in the game, with five weights.
- [PeglinMaxSpeed](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinMaxSpeed/) — play at up to 6× speed without physics bugs.
- [PeglinRelicRerolls](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinRelicRerolls/) — reroll relic rewards and shop relics.

[Check out all my Peglin mods](https://old.thunderstore.io/c/peglin/p/PierreMarais/)

### [Please consider buying me a coffee ☕](https://ko-fi.com/pierremarais)
