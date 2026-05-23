using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PixelWorldsInjector.Models;
using PixelWorldsInjector.Services;

namespace PixelWorldsInjector.Forms;

[SupportedOSPlatform("windows")]
public partial class MainForm : Form
{
    private readonly ConfigStore _configStore = new();
    private AppSettings _settings;
    private readonly ProcessMonitor _processMonitor;
    private InstanceLauncher _launcher;

    public MainForm()
    {
        InitializeComponent();
        _settings = _configStore.Load();
        _launcher = new InstanceLauncher(_settings, _configStore);

        _processMonitor = new ProcessMonitor(() => _settings.Instances);
        _processMonitor.StatusChanged += OnProcessStatusChanged;

        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_settings.GameExePath))
        {
            var detected = SteamLocator.FindPixelWorldsExe();
            if (!string.IsNullOrEmpty(detected))
            {
                _settings.GameExePath = detected!;
                _configStore.Save(_settings);
                Logger.Info($"Auto-detected PixelWorlds.exe at {detected}");
            }
        }

        tbGamePath.Text = _settings.GameExePath;
        RefreshList();
        UpdateStatus("Ready.");
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
        try
        {
            _configStore.Save(_settings);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save settings on close", ex);
        }
    }

    private void RefreshList()
    {
        listInstances.BeginUpdate();
        try
        {
            listInstances.Items.Clear();
            foreach (var instance in _settings.Instances)
            {
                var alive = instance.RunningPid is int pid && _processMonitor.IsAlive(pid);
                if (!alive)
                {
                    instance.RunningPid = null;
                }

                var item = new ListViewItem(instance.Name)
                {
                    Tag = instance,
                };
                item.SubItems.Add(instance.AccountLabel);
                item.SubItems.Add(instance.IsolateData ? "Yes" : "No");
                item.SubItems.Add(alive ? $"Running (pid {instance.RunningPid})" : "Stopped");
                item.SubItems.Add(instance.LastLaunchedUtc?.LocalDateTime.ToString("yyyy-MM-dd HH:mm") ?? "-");
                listInstances.Items.Add(item);
            }
        }
        finally
        {
            listInstances.EndUpdate();
        }
    }

    private void UpdateStatus(string text)
    {
        if (statusStrip.InvokeRequired)
        {
            statusStrip.BeginInvoke(() => statusLabel.Text = text);
        }
        else
        {
            statusLabel.Text = text;
        }
    }

    private Instance? GetSelected()
    {
        if (listInstances.SelectedItems.Count == 0)
        {
            return null;
        }

        return listInstances.SelectedItems[0].Tag as Instance;
    }

    private void OnProcessStatusChanged(Instance instance, bool alive)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            RefreshList();
            UpdateStatus(alive
                ? $"'{instance.Name}' started (pid {instance.RunningPid})."
                : $"'{instance.Name}' stopped.");
        });
    }

    // ------- Menu handlers -------

    private void MenuFileSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new SettingsDialog(_settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            SaveSettings();
            tbGamePath.Text = _settings.GameExePath;
            UpdateStatus("Settings saved.");
        }
    }

    private void MenuFileOpenDataFolder_Click(object? sender, EventArgs e)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", _configStore.ConfigDirectory)
        {
            UseShellExecute = true,
        });
    }

    private void MenuHelpAbout_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this,
            "Pixel Worlds Injector\n\n" +
            "Open-source multi-instance launcher for Pixel Worlds (PC).\n" +
            "Uses Sandboxie-style data isolation (filesystem junctions) and a\n" +
            "non-invasive named-mutex bypass so several copies of the game can\n" +
            "run side by side without Steam blocking them.\n\n" +
            "This tool does NOT modify the game's memory or behavior. Use at your\n" +
            "own risk and in compliance with the Pixel Worlds Terms of Service.",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ------- Toolbar handlers -------

    private void TbCreate_Click(object? sender, EventArgs e)
    {
        var instance = new Instance
        {
            Name = $"Instance {_settings.Instances.Count + 1}",
            IsolateData = true,
        };

        using var dlg = new InstanceEditDialog(instance);
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings.Instances.Add(instance);
        SaveSettings();
        RefreshList();
        UpdateStatus($"Created '{instance.Name}'.");
    }

    private void TbEdit_Click(object? sender, EventArgs e)
    {
        var instance = GetSelected();
        if (instance is null)
        {
            return;
        }

        using var dlg = new InstanceEditDialog(instance);
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SaveSettings();
        RefreshList();
        UpdateStatus($"Updated '{instance.Name}'.");
    }

    private void TbDelete_Click(object? sender, EventArgs e)
    {
        var instance = GetSelected();
        if (instance is null)
        {
            return;
        }

        var result = MessageBox.Show(this,
            $"Delete instance '{instance.Name}'?\nIts saved data folder under %AppData% will be removed.",
            "Confirm delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var dir = Path.Combine(_configStore.InstancesDirectory, instance.Id);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to delete instance data folder for {instance.Id}", ex);
        }

        _settings.Instances.Remove(instance);
        SaveSettings();
        RefreshList();
        UpdateStatus($"Deleted '{instance.Name}'.");
    }

    private void TbLaunch_Click(object? sender, EventArgs e)
    {
        var instance = GetSelected();
        if (instance is null)
        {
            MessageBox.Show(this, "Select an instance first.", "Launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _ = LaunchInstanceAsync(instance);
    }

    private async Task LaunchInstanceAsync(Instance instance)
    {
        if (string.IsNullOrWhiteSpace(_settings.GameExePath) || !File.Exists(_settings.GameExePath))
        {
            MessageBox.Show(this,
                "PixelWorlds.exe is not set. Use Browse... or Auto-detect first.",
                "Game path missing",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        UpdateStatus($"Launching '{instance.Name}'...");
        try
        {
            // Reconstruct launcher in case settings (e.g. game path) changed.
            _launcher = new InstanceLauncher(_settings, _configStore);
            var result = await _launcher.LaunchAsync(instance).ConfigureAwait(true);
            SaveSettings();
            RefreshList();
            UpdateStatus($"'{instance.Name}' launched. PID {result.Pid}, {result.MutexesClosed} mutex(es) closed.");
        }
        catch (Exception ex)
        {
            Logger.Error("Launch failed", ex);
            MessageBox.Show(this, ex.Message, "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus($"Launch failed: {ex.Message}");
        }
    }

    private void ListInstances_DoubleClick(object? sender, EventArgs e) => TbLaunch_Click(sender, e);

    private void TbGamePath_LostFocus(object? sender, EventArgs e)
    {
        var path = tbGamePath.Text?.Trim() ?? string.Empty;
        if (path != _settings.GameExePath)
        {
            _settings.GameExePath = path;
            SaveSettings();
        }
    }

    private void TbBrowse_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select PixelWorlds.exe",
            Filter = "PixelWorlds.exe|PixelWorlds.exe|All executables|*.exe",
            CheckFileExists = true,
        };
        if (!string.IsNullOrEmpty(_settings.GameExePath) && File.Exists(_settings.GameExePath))
        {
            ofd.InitialDirectory = Path.GetDirectoryName(_settings.GameExePath);
        }

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _settings.GameExePath = ofd.FileName;
            tbGamePath.Text = ofd.FileName;
            SaveSettings();
            UpdateStatus($"Game path set to {ofd.FileName}");
        }
    }

    private void TbAutoDetect_Click(object? sender, EventArgs e)
    {
        var detected = SteamLocator.FindPixelWorldsExe();
        if (detected is null)
        {
            MessageBox.Show(this,
                "Could not auto-detect PixelWorlds.exe from Steam libraries.\n" +
                "Use Browse... to set it manually.",
                "Auto-detect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _settings.GameExePath = detected;
        tbGamePath.Text = detected;
        SaveSettings();
        UpdateStatus($"Auto-detected: {detected}");
    }
}
