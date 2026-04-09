# Implementation notes

Non-obvious technical decisions, conventions, and gotchas that aren't derivable from reading the code alone. Update this file when adding new subsystems or making decisions that future contributors would otherwise have to re-derive.

## Project structure

Solution file: `GitTrayNotifier.sln` (two projects).
Main app: `GitTrayNotifier/` — `net10.0-windows`, `UseWindowsForms`, `OutputType=WinExe`.
Tests: `GitTrayNotifier.Tests/` — `net10.0`, xUnit.
`build`, `test`, and `format` in the justfile target the `.sln`; `run`, `publish`, `add-package` target the main `.csproj`.

## Key files

- `Config.cs` — `AppConfig` record, `ConfigLoader` static class
- `StartupManager.cs` — `IsEnabled`, `Enable`, `Disable` for the `HKCU\...\Run` startup entry
- `Scanner.cs` — `Scanner.ScanAllAsync`, `RunProcessAsync`
- `TrayApplicationContext.cs` — tray icon, polling timer, countdown label
- `SettingsForm.cs` — settings dialog (config path + open in VS Code)
- `NativeMethods.cs` — `AllocConsole` P/Invoke

## Config file path

Stored in registry at `HKCU\Software\GitTrayNotifier\ConfigFilePath`.
Written on first run if missing (defaults to `%APPDATA%\GitTrayNotifier\config.json`).
`justfile` has a `regedit` recipe that opens pwsh navigated to that key.

## Polling interval format

Config is re-read from disk on every scan (not cached), so changes take effect at next scan.

## WSL2 scanning

Uses `wsl.exe --list --verbose` (UTF-16 output) to check if the default distro is running before scanning any WSL2 repos — avoids waking the VM.
Git commands in WSL2 run via `wsl.exe --cd <path> -- git <args>`.

## Spawning processes

All child processes (git, wsl.exe) go through `Scanner.RunProcessAsync`.
`CreateNoWindow = true` is set to suppress console flicker in tray mode.
In subcommand mode, `AllocConsole()` is called first so output is visible.

## Notifications

`TrayApplicationContext.ShowUpdateNotification(NotifyIcon, List<ScanResult>)` is the single place that formats and fires balloon tips. Both the polling path and `show-two-notifications` subcommand use it.

## Subcommands

Dispatched in `Program.Main` when `args.Length > 0`.
