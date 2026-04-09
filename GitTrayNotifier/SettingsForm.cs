using System.Diagnostics;

namespace GitTrayNotifier;

class SettingsForm : Form
{
    private readonly TextBox _configPathBox;
    private readonly CheckBox _launchOnStartupBox;

    public SettingsForm()
    {
        Text = "Git Tray Notifier Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(12);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var layout = new TableLayoutPanel
        {
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Config file path row
        layout.Controls.Add(new Label { Text = "Config file path:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);

        _configPathBox = new TextBox
        {
            Text = ConfigLoader.GetConfigFilePath(),
            Dock = DockStyle.Fill,
            // Margin = new Padding(6, 3, 6, 3),
        };
        layout.Controls.Add(_configPathBox, 1, 0);

        var browseButton = new Button { Text = "Browse…", AutoSize = true };
        browseButton.Click += OnBrowse;
        layout.Controls.Add(browseButton, 2, 0);

        // Launch on startup row
        _launchOnStartupBox = new CheckBox
        {
            Text = "Launch on Windows startup",
            Checked = StartupManager.IsEnabled(),
            AutoSize = true,
            // Margin = new Padding(0, 6, 0, 0),
        };
        layout.Controls.Add(_launchOnStartupBox, 0, 1);
        layout.SetColumnSpan(_launchOnStartupBox, 3);

        // Buttons row
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right,
        };

        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var openVsCodeButton = new Button { Text = "Open Config in VS Code", AutoSize = true };
        openVsCodeButton.Click += OnOpenInVsCode;
        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += OnSave;

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(openVsCodeButton);
        buttonPanel.Controls.Add(saveButton);

        layout.Controls.Add(buttonPanel, 0, 2);
        layout.SetColumnSpan(buttonPanel, 3);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select config file",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = _configPathBox.Text,
            CheckFileExists = false,
            CheckPathExists = true,
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            _configPathBox.Text = dialog.FileName;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        ConfigLoader.SetConfigFilePath(_configPathBox.Text);
        if (_launchOnStartupBox.Checked)
            StartupManager.Enable();
        else
            StartupManager.Disable();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnOpenInVsCode(object? sender, EventArgs e)
    {
        var path = _configPathBox.Text;

        if (!File.Exists(path))
        {
            var result = MessageBox.Show(
                $"Config file does not exist:\n{path}\n\nCreate it now?",
                "Git Tray Notifier",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var schemaPath = Path.Combine(AppContext.BaseDirectory, "config.schema.json");
                var schemaRef = File.Exists(schemaPath)
                    ? new Uri(schemaPath).AbsoluteUri
                    : "./config.schema.json";
                File.WriteAllText(path, $$"""
                    {
                      "$schema": "{{schemaRef}}"
                    }
                    """);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create config file:\n{ex.Message}", "Git Tray Notifier",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo("code", $"\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open VS Code:\n{ex.Message}", "Git Tray Notifier",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
