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

        _instance.Name = name;
        _instance.AccountLabel = txtAccount.Text.Trim();
        _instance.ExtraArgs = txtExtraArgs.Text.Trim();
        _instance.IsolateData = chkIsolate.Checked;
    }
}
