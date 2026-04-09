namespace GitTrayNotifier;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var configPath = ConfigLoader.GetConfigFilePath();

        if (args.Length > 0)
        {
            // Dev/test subcommands: allocate a console so output is visible.
            NativeMethods.AllocConsole();
            var config = ConfigLoader.Load(configPath);
            RunSubcommand(args, config);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Normal tray-app mode.
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }

    static void RunSubcommand(string[] args, AppConfig config)
    {
        switch (args[0])
        {
            case "scan-and-log":
                var results = Task.Run(() => Scanner.ScanAllAsync(config)).GetAwaiter().GetResult();
                foreach (var r in results)
                {
                    Console.WriteLine($"[{r.Repo.Type}] {r.Repo.Path} @ {r.Repo.Branch}");
                    if (r.Error is not null)
                    {
                        Console.WriteLine($"  ERROR: {r.Error}");
                    }
                    else
                    {
                        Console.WriteLine($"  local:  {r.LocalCommit}");
                        Console.WriteLine($"  remote: {r.RemoteCommit}");
                        Console.WriteLine(r.HasUpdates ? "  >> HAS UPDATES" : "  up to date");
                    }
                }
                break;
            case "show-two-notifications":
                var fakeRepos = new[]
                {
                    new RepositoryConfig(RepositoryType.Windows, @"C:\dev\my-project", "main"),
                    new RepositoryConfig(RepositoryType.Wsl2, "/home/user/another-repo", "main"),
                };
                var fakeResults = fakeRepos.Select(r => new ScanResult(r,
                    LocalCommit: "aaaaaaaabbbbbbbbccccccccdddddddd00000001",
                    RemoteCommit: "aaaaaaaabbbbbbbbccccccccdddddddd00000002",
                    HasUpdates: true,
                    Error: null)).ToList();
                using (var icon = new NotifyIcon { Icon = SystemIcons.Information, Visible = true })
                {
                    TrayApplicationContext.ShowUpdateNotification(icon, fakeResults);
                    Console.WriteLine("Notification shown. Press any key to exit...");
                    Console.ReadKey();
                }
                return; // skip the generic "Press any key" at the end
            case "show-settings":
                ApplicationConfiguration.Initialize();
                Application.Run(new SettingsForm());
                return; // skip the generic "Press any key" at the end
            case "noop":
                break;
            case "show-config":
                Console.WriteLine($"Config file: {ConfigLoader.GetConfigFilePath()}");
                Console.WriteLine($"Polling interval: {config.PollingInterval}");
                Console.WriteLine($"Repositories ({config.Repositories.Count}):");
                foreach (var repo in config.Repositories)
                    Console.WriteLine($"  [{repo.Type}] {repo.Path} @ {repo.Branch}");
                break;
            default:
                Console.WriteLine($"Unknown subcommand: {args[0]}");
                Console.WriteLine("Available subcommands: scan-and-log, show-config, show-settings, noop");

                break;
        }
    }
}
