using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using PixelWorldsInjector.Models;
using PixelWorldsInjector.Services;

namespace PixelWorldsInjector.Forms;

[SupportedOSPlatform("windows")]
public partial class SettingsDialog : Form
{
    private readonly AppSettings _settings;

    public SettingsDialog(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        txtGamePath.Text = settings.GameExePath;
        numSteamAppId.Value = settings.SteamAppId;
        chkBypassSteam.Checked = settings.BypassSteam;
        txtMutexHint.Text = settings.MutexNameHint;
        txtGoldbergPath.Text = settings.GoldbergDllPath;
        RefreshGoldbergStatus();
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select PixelWorlds.exe",
            Filter = "PixelWorlds.exe|PixelWorlds.exe|All executables|*.exe",
            CheckFileExists = true,
        };
        if (!string.IsNullOrEmpty(txtGamePath.Text) && File.Exists(txtGamePath.Text))
        {
            ofd.InitialDirectory = Path.GetDirectoryName(txtGamePath.Text);
        }

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            txtGamePath.Text = ofd.FileName;
            RefreshGoldbergStatus();
        }
    }

    private void BtnBrowseGoldberg_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select GoldBerg steam_api64.dll",
            Filter = "Steam API DLL|steam_api64.dll;steam_api.dll|DLL files|*.dll|All files|*.*",
            CheckFileExists = true,
        };
        if (!string.IsNullOrEmpty(txtGoldbergPath.Text) && File.Exists(txtGoldbergPath.Text))
        {
            ofd.InitialDirectory = Path.GetDirectoryName(txtGoldbergPath.Text);
        }

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            txtGoldbergPath.Text = ofd.FileName;
        }
    }

    private void BtnInstallGoldberg_Click(object? sender, EventArgs e)
    {
        var gamePath = txtGamePath.Text.Trim();
        var dllPath = txtGoldbergPath.Text.Trim();

        if (!File.Exists(gamePath))
        {
            MessageBox.Show(this, "Set a valid PixelWorlds.exe path first.", "Install GoldBerg", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!File.Exists(dllPath))
        {
            MessageBox.Show(this, "Select the GoldBerg steam_api64.dll file first.", "Install GoldBerg", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "This will rename the game's steam_api64.dll to steam_api64.original.dll and replace it with the GoldBerg DLL.\n\n" +
            "Close the game first if it is running. Continue?",
            "Install GoldBerg",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.OK)
        {
            return;
        }

        try
        {
            SteamEmu.Install(gamePath, dllPath);
            MessageBox.Show(this, "GoldBerg installed.", "Install GoldBerg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("GoldBerg install failed", ex);
            MessageBox.Show(this, "Install failed:\n\n" + ex.Message, "Install GoldBerg", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            RefreshGoldbergStatus();
        }
    }

    private void BtnRestoreGoldberg_Click(object? sender, EventArgs e)
    {
        var gamePath = txtGamePath.Text.Trim();
        if (!File.Exists(gamePath))
        {
            MessageBox.Show(this, "Set a valid PixelWorlds.exe path first.", "Restore Steam DLL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "This will restore the original Steam API DLL from the steam_api64.original.dll backup.\n\n" +
            "Close the game first if it is running. Continue?",
            "Restore Steam DLL",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.OK)
        {
            return;
        }

        try
        {
            SteamEmu.Restore(gamePath);
            MessageBox.Show(this, "Original Steam API DLL restored.", "Restore Steam DLL", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("GoldBerg restore failed", ex);
            MessageBox.Show(this, "Restore failed:\n\n" + ex.Message, "Restore Steam DLL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            RefreshGoldbergStatus();
        }
    }

    private void RefreshGoldbergStatus()
    {
        var gamePath = txtGamePath.Text.Trim();
        if (string.IsNullOrEmpty(gamePath) || !File.Exists(gamePath))
        {
            lblGoldbergStatus.Text = "Status: PixelWorlds.exe path not set.";
            return;
        }

        var status = SteamEmu.GetStatus(gamePath);
        lblGoldbergStatus.Text = status.Installed
            ? $"Status: GoldBerg INSTALLED ({status.Detail})"
            : $"Status: GoldBerg NOT installed ({status.Detail})";
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        _settings.GameExePath = txtGamePath.Text.Trim();
        _settings.SteamAppId = (int)numSteamAppId.Value;
        _settings.BypassSteam = chkBypassSteam.Checked;
        _settings.MutexNameHint = txtMutexHint.Text.Trim();
        _settings.GoldbergDllPath = txtGoldbergPath.Text.Trim();
    }
}
