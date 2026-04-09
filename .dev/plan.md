Write in C#, using `dotnet` CLI
Target .NET 11

`justfile` for development workflows.

App that runs in the background.
System tray icon.

Windows subsystem (not a console app), but it will be able to launch a console window when we're running development / testing subcommands, to show logged outputs. (using AllocConsole)

App has subcommand CLI interface (argv) which we'll use for testing and development.

On a timer, it checks a configurable list of git repositories for upstream updates, showing a notification if they have updates.

- Do we have any configured WSL2 repos? If so, use `wsl.exe list -v` to check if the default WSL distro is running. Skip WSL2 repo checks if the distro is not running, to avoid triggering the VM to launch.

For each repo:
- Uses `git ls-remote` to check commit of the remote branch
- Uses `git rev-parse` to check commit of the local branch # TODO better way to check state of the local working tree, since ultimately that's what we care about: the working tree, not the branch?
- If remote branch commit is different from local commit, show tray notification

Configurable list of git repositories:
- Type: WSL2 or Windows
- Path
- Branch

Other configurable options:
- path to config file
  - this path is stored in windows registry
  - default to a dotfile in home dir or appdata, so chezmoi can manage it
  - allowing it to be reconfigured means you can put it in OneDrive, for example
- all other configurable options stored in config file
  - polling interval: 1hr, 1day, etc

Configuration is available from tray icon menu "Settings" button
Rather than an in-app editor, have a button to open the config file in VSCode
