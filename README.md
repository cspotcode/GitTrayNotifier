# GitTrayNotifier

Windows system tray app that polls git repositories and notifies you when upstream branches have updates.

This is useful when you keep personal tooling - scripts, dotfiles - in git across multiple machines. After pushing changes from one machine, this app will remind you to pull on your other machines.

Supports both Windows and WSL2 repos, skipping WSL2 checks if your default distro isn't running to avoid waking the VM.

*Code was written by AI with minimal review; couldn't justify spending more time on this*

## Installation

This tool runs on Windows and requires the .NET 10.0 runtime, which can be installed from here: https://dotnet.microsoft.com/en-us/download/dotnet/10.0/runtime

Download and extract the latest [Release](https://github.com/cspotcode/GitTrayNotifier/releases).

Run the `.exe`.

In the system tray, you should see a Git logo. Right-click and choose "Settings" to configure.

## Configuration

Right-click the tray icon and choose "Settings."

Config file path is stored in the Windows registry and defaults to `%AppData%/.GitTrayNotifier/config.json`. You can point it at a different location, for example, to store it in OneDrive and synchronize your configuration across machines.

### `config.json`

- `pollingInterval`: how often to check (e.g. `1h`, `1d`)
- `repositories`:
    - `type`: `windows` or `wsl2`
    - `path`: absolute path to the repo
    - `branch`: branch to monitor

## How it works

It runs `git ls-remote` to get the remote branch commit.

It runs `git rev-parse` to get the local branch commit.

If they differ, it fires a notification.
