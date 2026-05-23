using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using PixelWorldsInjector.Models;

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
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        _settings.GameExePath = txtGamePath.Text.Trim();
        _settings.SteamAppId = (int)numSteamAppId.Value;
        _settings.BypassSteam = chkBypassSteam.Checked;
        _settings.MutexNameHint = txtMutexHint.Text.Trim();
    }
}
