# Changelog

## 1.2.0

- New **Restart** button to the left of End Run (End Run stays where the game puts it) on the run-over screen after dying, copied from the game's own button so it works with mouse, keyboard and controller. Holding the restart key on that screen now does the same. The run is recorded in the Encirclepedia and stats exactly as with End Run; the run summary screen is skipped. Hide the button with the new `ShowRestartButton` setting.
- The restart key does nothing once the run is won (the win screen, the Core choice and the credits), so it can't abandon a won run before the game records it.
- Fix: keeping the restart key held through a restart no longer restarts the new run again; let go and hold again to restart.
- A starting orb or relic the game no longer knows (for example from a removed mod) is skipped instead of stopping the restart. An unknown class restarts with the class selected on the character screen.
- `QuickRestartApi` for other mods: restart handlers that take over the restart, blockers that turn it off, restart listeners, `CanRestart`, `IsRestarting` and `RequestRestart`. See "For mod authors" in the README.

## 1.1.1

- Fix: holding the controller button (L3 by default) now restarts the run. In 1.1.0 it never did.
- The restart countdown is larger and its box grows to fit the message, so it stays readable with custom fonts.

## 1.1.0

- Controller support: hold L3 (click the left stick) to restart. Change it with the new `ControllerRestartButton` setting, or set it to `None` to turn it off.

## 1.0.0

- First release: hold X for 1.5 seconds to restart the run with the same class, Cruciball level and custom loadout, on the same custom seed or a new random one, with an on-screen countdown and a configurable key and hold time.
