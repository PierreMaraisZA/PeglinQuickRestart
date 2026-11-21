# PeglinQuickRestart

Bad start? Hold one button to try again.

## Features

- **Hold X (or L3 on a controller) for 1.5 seconds to restart.** The run is abandoned and a new one starts straight away, without clicking through Abandon Run, the main menu, character select and Play.
- **Restart button after dying.** The run-over screen has a **Restart** button to the left of End Run. It works with mouse, keyboard and controller, and the run is recorded in the Encirclepedia and stats exactly as with End Run.
- **Same setup every time.** The new run uses the same class and Cruciball level as the one you left.
- **Custom loadouts carry over.** If you edited your starting orbs or relics, the new run starts with the same ones.
- **Seeds work like the game's.** A run started on a custom seed restarts on that same seed, so you can practise a seed over and over. Any other run gets a new random seed, just like pressing Play.
- **No accidental restarts.** A countdown shows at the top of the screen while you hold the key or button. Let go before it ends to cancel.
- **Works anywhere in a run.** On the map, mid-battle, in shops and events, with the pause menu open, and on the run-over screen, where holding the key does the same as the Restart button. The hold time is real time, so game speed and pausing don't change it.
- **Wins are safe.** Once you beat the final boss, the key does nothing on the win screen, the Core choice or the credits, so a won run is always recorded.
- **Copes with missing items.** A starting orb or relic that no longer exists (for example from a mod you removed) is skipped and the rest are kept. An unknown class restarts as the class selected on the character screen.
- **Remembers your setup.** You can restart a run you continued after closing the game, not just one you started this session.
- **Plays by the game's rules.** The restart goes through the game's own steps, so the new run is set up exactly like one you start by hand. Class unlock progress is saved, and achievements are on or off just as they would be (off for custom runs, as in the base game).

## How it works

A restart does what you would do by hand:

1. **Abandon Run**, exactly like the pause menu option. On the run-over screen it presses **End Run** instead, so the game records the run, then skips the run summary.
2. Go to the **main menu** and press **Play**.
3. Pick the **same class and Cruciball level**, and set the **custom loadout and seed** if the run had them.
4. Press **Play** on the character select screen.

Going through the menus means the game does all its own cleanup and new-run setup, rather than the mod copying it. A restart takes a few seconds while the screens fade.

## For mod authors

Other mods can take over the restart (for example a game mode that restarts into its own kind of run), turn it off, hear about it, or ask for one. `QuickRestartApi` is a public static class in this mod's assembly. Find it by its simple name, check `ApiVersion`, and register with your own plugin GUID:

```csharp
// In your plugin: [BepInDependency("PeglinQuickRestart", BepInDependency.DependencyFlags.SoftDependency)]
if (Chainloader.PluginInfos.TryGetValue("PeglinQuickRestart", out PluginInfo info))
{
    Type api = info.Instance.GetType().Assembly.GetTypes().FirstOrDefault(t => t.Name == "QuickRestartApi");
    if (api != null && (int)api.GetProperty("ApiVersion").GetValue(null) >= 1)
    {
        api.GetMethod("RegisterRestartHandler").Invoke(null, new object[]
        {
            MyPlugin.Guid,
            (Func<bool>)(() => MyMode.IsRunActive),   // true: this run is mine
            (Action)MyMode.Restart,                    // abandon and start again, your own way
        });
    }
}
```

| Member | What it does |
|---|---|
| `int ApiVersion` | `1` |
| `RegisterRestartHandler(guid, Func<bool> canHandle, Action restart)` | Asked before the normal restart, by the hold key and the Restart button. The first handler whose `canHandle()` is true gets `restart()` and does its own abandon and cleanup; QuickRestart does nothing else. While `canHandle()` is true when Play is pressed, QuickRestart keeps its own last-run record unchanged |
| `SetRestartBlocker(guid, Func<bool> block)` | While any blocker returns true, the hold key does nothing and the Restart button is hidden |
| `SetRestartListener(guid, Action<string> listener)` | Called when a restart is started (QuickRestart's own) or handed to a handler, with what asked for it: `"key"`, `"button"` or `"api"`. New names may be added; ignore unknown ones |
| `bool CanRestart` | True if a restart asked for now would go ahead (false while one is already on its way, including after End Run is pressed) |
| `bool IsRestarting` | True while QuickRestart's own restart is going through the menus |
| `bool RequestRestart(guid)` | Restarts exactly as holding the key would (blockers, handlers, and End Run's recording on the run-over screen). True if it went ahead |
| `RemoveAll(guid)` | Removes everything registered with that GUID |

Registering again with the same GUID replaces your earlier registration; passing `null` removes it. A callback that throws is logged once and treated as "no opinion" (a listener that throws is skipped), except a `restart()` that throws, which still counts as handled so the normal restart never runs on top of a half-finished one.

Blockers are checked first, then handlers, then QuickRestart's own rules. After a won run the normal restart is off, but a handler whose `canHandle()` is true still gets the restart, so your mode decides for itself. If a handler takes a restart from the run-over screen but the screen is still up 3 seconds later with nothing loading, the Restart button works again. Call everything from Unity's main thread. A restart requested from inside a handler or listener does nothing, and a blocker or `canHandle()` that asks the API gets "no opinion" from blockers and handlers instead of looping.

## Installation

**With a mod manager (recommended):**
1. Install r2modman or the Thunderstore Mod Manager.
2. Search for PeglinQuickRestart and install it. BepInEx is installed for you.
3. Launch Peglin.

**Manually:**
1. Install [BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/).
2. Put `PeglinQuickRestart.dll` in `Peglin/BepInEx/plugins/`.
3. Start the game once to create the config file.

## Settings

Change these in `BepInEx/config/PeglinQuickRestart.cfg` (start the game once to create it). Restart the game after changing them.

| Setting | Default | What it does |
|---|---|---|
| `RestartKey` | `X` | Key to hold. Add modifiers to make it harder to hit by accident, e.g. `X + LeftControl`. `None` turns it off. [Key names](https://docs.unity3d.com/ScriptReference/KeyCode.html) |
| `ControllerRestartButton` | `JoystickButton8` | Controller button to hold (L3, clicking the left stick). `None` turns it off |
| `HoldSeconds` | `1.5` | How long to hold the key or button, in real seconds |
| `ShowHoldProgress` | `true` | Show the countdown while the key or button is held |
| `ShowRestartButton` | `true` | Show the Restart button on the run-over screen. The key works there either way |

## Compatibility

- Built for **Peglin v2.0.12**.
- Requires [BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/).

## Good to know

- **A restart mid-run counts as abandoning the run.** Like Abandon Run, it resets your "reached the boss" streak if you hadn't reached the boss yet. A restart after dying counts the same as End Run.
- **Restarting after dying skips the run summary screen**, including its list of newly unlocked orbs, relics and classes. The unlocks themselves are kept; use End Run to see the summary.
- **Custom runs started before installing the mod** can't be restarted, because the mod doesn't know their loadout. Standard runs always can.
- **If a restart fails partway** (for example after a game update), the old run is already abandoned and you're left on the main menu, character select or run summary screen, wherever it stopped. The reason is shown on screen and written to `BepInEx/LogOutput.log`.
- **Your save is the game's own.** The mod only writes the last run's setup to `BepInEx/config/PeglinQuickRestart.lastrun.txt`. It's safe to delete; you just won't be able to restart a custom run you continue later.
- **L3 is the one button Peglin leaves unbound**, so it doesn't clash with anything. It assumes an Xbox-style layout, which is what Steam Input uses; without Steam Input, a PlayStation controller may need a different `ControllerRestartButton`.
- Clicking the left stick can nudge your aim. If you let go early to cancel, check your aim before firing.
- The key and button do nothing on the main menu or while a screen is loading.

## Some of my other mods

- [PeglinBalatroScore](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinBalatroScore/) — a Balatro-style pegs × damage score, with flames and a bomb chest.
- [PeglinFonts](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinFonts/) — change the font of every text in the game.
- [PeglinKeybinds](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinKeybinds/) — rebind keyboard and controller controls from inside the game.
- [PeglinMaxSpeed](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinMaxSpeed/) — play at up to 15× speed without physics bugs.
- [PeglinRelicRerolls](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinRelicRerolls/) — reroll the relics you're offered.

[Check out all my Peglin mods](https://old.thunderstore.io/c/peglin/p/PierreMarais/)

### [Please consider buying me a coffee ☕](https://ko-fi.com/pierremarais)
