# PeglinQuickRestart

Hold **X** for 1.5 seconds during a run to abandon it and start a new one with the same setup:

- same class and Cruciball level
- same custom loadout (starting orbs and relics), if you used one
- same seed if you entered a custom seed; otherwise a new random seed, exactly like pressing Play

A countdown shows at the top of the screen while you hold the key. Let go to cancel.

## How it works

The restart goes through the game's own steps: Abandon Run, main menu, Play, pick the class/loadout/seed, Play.
That way the game does all its own cleanup and new-run setup (and saves class unlock progress) as if you'd clicked through.

The setup is recorded when you press Play, and saved to `BepInEx/config/PeglinQuickRestart.lastrun.txt`, so a run you
continue after closing the game can still be restarted. A standard (non-custom) run can always be restarted, even if it was
started before the mod was installed; a custom run started before the mod was installed can't, since its loadout isn't known.

## Config (`BepInEx/config/PeglinQuickRestart.cfg`)

| Setting | Default | |
|---|---|---|
| RestartKey | X | Key to hold. Modifiers work, e.g. `X + LeftControl`. |
| HoldSeconds | 1.5 | Real seconds, unaffected by game speed or pause. |
| ShowHoldProgress | true | Show the countdown while holding. |

## Building

```
dotnet build -c Release
dotnet test tests
```

The build copies the DLL into `BepInEx/plugins/PeglinQuickRestart` of the Steam install
(override with `-p:GamePath="D:\Games\Peglin"`).
