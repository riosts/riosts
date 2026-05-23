using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using PixelWorldsInjector.Models;

namespace PixelWorldsInjector.Forms;

[SupportedOSPlatform("windows")]
public partial class InstanceEditDialog : Form
{
    private readonly Instance _instance;

    public InstanceEditDialog(Instance instance)
    {
        _instance = instance;
        InitializeComponent();
        txtName.Text = instance.Name;
        txtAccount.Text = instance.AccountLabel;
        txtExtraArgs.Text = instance.ExtraArgs;
        chkIsolate.Checked = instance.IsolateData;
        chkUseSteamEmu.Checked = instance.UseSteamEmu;
        txtSteamName.Text = instance.SteamAccountName;
        txtSteamId.Text = instance.SteamId;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var steamId = txtSteamId.Text.Trim();
        if (steamId.Length > 0 && (steamId.Length != 17 || !ulong.TryParse(steamId, out _)))
        {
            MessageBox.Show(this, "SteamID must be a 17-digit number, or empty to auto-derive one.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        _instance.Name = name;
        _instance.AccountLabel = txtAccount.Text.Trim();
        _instance.ExtraArgs = txtExtraArgs.Text.Trim();
        _instance.IsolateData = chkIsolate.Checked;
        _instance.UseSteamEmu = chkUseSteamEmu.Checked;
        _instance.SteamAccountName = txtSteamName.Text.Trim();
        _instance.SteamId = steamId;
    }
}
