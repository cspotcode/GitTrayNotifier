namespace GitTrayNotifier;

/// <summary>
/// Hosts the system tray icon and drives the application lifecycle.
/// </summary>
class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _scanMenuItem;
    private readonly ToolStripMenuItem _nextScanMenuItem;

    // Fires every minute to update the countdown label.
    private readonly System.Windows.Forms.Timer _countdownTimer;
    // Fires when it's time to perform the next automatic scan.
    private readonly System.Windows.Forms.Timer _pollTimer;

    private DateTime _nextScanAt;
    private bool _scanning;

    public TrayApplicationContext()
    {
        _scanMenuItem = new ToolStripMenuItem("Scan now", null, OnScanNow);
        _nextScanMenuItem = new ToolStripMenuItem { Enabled = false };

        _countdownTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _countdownTimer.Tick += (_, _) => UpdateCountdownLabel();

        _pollTimer = new System.Windows.Forms.Timer();
        _pollTimer.Tick += OnPollTimerTick;

        _trayIcon = new NotifyIcon
        {
            Text = "Git Tray Notifier",
            Icon = LoadAppIcon(),
            Visible = true,
            ContextMenuStrip = BuildContextMenu(),
        };

        ScheduleNextScan();
        _countdownTimer.Start();
    }

    private static Icon LoadAppIcon()
    {
        var asm = typeof(TrayApplicationContext).Assembly;
        using var stream = asm.GetManifestResourceStream("GitTrayNotifier.app.ico")!;
        return new Icon(stream);
    }

    private AppConfig LoadConfig()
    {
        try
        {
            return ConfigLoader.Load(ConfigLoader.GetConfigFilePath());
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(8000, "Git Tray Notifier — config error",
                $"Could not read config file:\n{ex.Message}", ToolTipIcon.Error);
            return AppConfig.Default;
        }
    }

    // Arms the poll timer based on the current config's polling interval.
    private void ScheduleNextScan()
    {
        var interval = LoadConfig().ParsedPollingInterval;
        _nextScanAt = DateTime.Now + interval;

        _pollTimer.Stop();
        _pollTimer.Interval = (int)Math.Min(interval.TotalMilliseconds, int.MaxValue);
        _pollTimer.Start();

        UpdateCountdownLabel();
    }

    private void UpdateCountdownLabel()
    {
        var remaining = _nextScanAt - DateTime.Now;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        var totalMinutes = (int)remaining.TotalMinutes;
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        _nextScanMenuItem.Text = hours > 0
            ? $"Next scan in {hours}h{minutes:D2}m"
            : $"Next scan in {minutes}m";
    }

    private async void OnPollTimerTick(object? sender, EventArgs e)
    {
        _pollTimer.Stop();
        await RunScanAsync();
        ScheduleNextScan();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(_scanMenuItem);
        menu.Items.Add(_nextScanMenuItem);
        menu.Items.Add("Settings", null, OnSettings);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon.Visible = false;
            Application.Exit();
        });

        return menu;
    }

    private async void OnScanNow(object? sender, EventArgs e)
    {
        await RunScanAsync();
        ScheduleNextScan();
    }

    private async Task RunScanAsync()
    {
        if (_scanning) return;
        _scanning = true;
        _scanMenuItem.Enabled = false;
        _scanMenuItem.Text = "Scanning…";
        try
        {
            var config = LoadConfig();
            var results = await Scanner.ScanAllAsync(config);
            var updates = results.Where(r => r.HasUpdates).ToList();
            ShowUpdateNotification(_trayIcon, updates);
        }
        finally
        {
            _scanMenuItem.Text = "Scan now";
            _scanMenuItem.Enabled = true;
            _scanning = false;
        }
    }

    public static void ShowUpdateNotification(NotifyIcon trayIcon, List<ScanResult> updates)
    {
        if (updates.Count == 0) return;
        var message = string.Join("\n", updates.Select(r => $"{r.Repo.Path} ({r.Repo.Branch})"));
        trayIcon.ShowBalloonTip(5000, "Git updates available", message, ToolTipIcon.Info);
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        using var form = new SettingsForm();
        form.ShowDialog();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Dispose();
            _countdownTimer.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
