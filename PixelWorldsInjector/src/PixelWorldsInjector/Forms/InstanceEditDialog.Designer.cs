#nullable disable
using System.Windows.Forms;

namespace PixelWorldsInjector.Forms;

partial class InstanceEditDialog
{
    private System.ComponentModel.IContainer components;

    private Label lblName;
    private TextBox txtName;
    private Label lblAccount;
    private TextBox txtAccount;
    private Label lblExtraArgs;
    private TextBox txtExtraArgs;
    private CheckBox chkIsolate;
    private Label lblIsolateHint;
    private GroupBox grpSteamEmu;
    private CheckBox chkUseSteamEmu;
    private Label lblSteamName;
    private TextBox txtSteamName;
    private Label lblSteamId;
    private TextBox txtSteamId;
    private Label lblSteamEmuHint;
    private Button btnOk;
    private Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblName = new Label();
        txtName = new TextBox();
        lblAccount = new Label();
        txtAccount = new TextBox();
        lblExtraArgs = new Label();
        txtExtraArgs = new TextBox();
        chkIsolate = new CheckBox();
        lblIsolateHint = new Label();
        grpSteamEmu = new GroupBox();
        chkUseSteamEmu = new CheckBox();
        lblSteamName = new Label();
        txtSteamName = new TextBox();
        lblSteamId = new Label();
        txtSteamId = new TextBox();
        lblSteamEmuHint = new Label();
        btnOk = new Button();
        btnCancel = new Button();

        SuspendLayout();
        grpSteamEmu.SuspendLayout();

        lblName.AutoSize = true;
        lblName.Location = new System.Drawing.Point(12, 15);
        lblName.Text = "Name:";

        txtName.Location = new System.Drawing.Point(140, 12);
        txtName.Size = new System.Drawing.Size(280, 23);

        lblAccount.AutoSize = true;
        lblAccount.Location = new System.Drawing.Point(12, 45);
        lblAccount.Text = "Account label (optional):";

        txtAccount.Location = new System.Drawing.Point(140, 42);
        txtAccount.Size = new System.Drawing.Size(280, 23);

        lblExtraArgs.AutoSize = true;
        lblExtraArgs.Location = new System.Drawing.Point(12, 75);
        lblExtraArgs.Text = "Extra CLI args:";

        txtExtraArgs.Location = new System.Drawing.Point(140, 72);
        txtExtraArgs.Size = new System.Drawing.Size(280, 23);

        chkIsolate.AutoSize = true;
        chkIsolate.Location = new System.Drawing.Point(140, 102);
        chkIsolate.Text = "Isolate save data (junction swap)";

        lblIsolateHint.AutoSize = false;
        lblIsolateHint.Location = new System.Drawing.Point(140, 125);
        lblIsolateHint.Size = new System.Drawing.Size(285, 35);
        lblIsolateHint.ForeColor = System.Drawing.SystemColors.GrayText;
        lblIsolateHint.Text = "Swaps the game's LocalLow\\Kukouri\\Pixel Worlds folder for this instance's private copy.";

        // ---- Steam Emulator group ----
        grpSteamEmu.Location = new System.Drawing.Point(12, 170);
        grpSteamEmu.Size = new System.Drawing.Size(420, 165);
        grpSteamEmu.Text = "Steam Emulator (GoldBerg)";

        chkUseSteamEmu.AutoSize = true;
        chkUseSteamEmu.Location = new System.Drawing.Point(15, 22);
        chkUseSteamEmu.Text = "Use Steam Emulator for this instance";

        lblSteamName.AutoSize = true;
        lblSteamName.Location = new System.Drawing.Point(15, 52);
        lblSteamName.Text = "Steam display name:";

        txtSteamName.Location = new System.Drawing.Point(155, 49);
        txtSteamName.Size = new System.Drawing.Size(245, 23);

        lblSteamId.AutoSize = true;
        lblSteamId.Location = new System.Drawing.Point(15, 80);
        lblSteamId.Text = "SteamID64 (17 digits):";

        txtSteamId.Location = new System.Drawing.Point(155, 77);
        txtSteamId.Size = new System.Drawing.Size(245, 23);

        lblSteamEmuHint.Location = new System.Drawing.Point(15, 105);
        lblSteamEmuHint.Size = new System.Drawing.Size(390, 50);
        lblSteamEmuHint.ForeColor = System.Drawing.SystemColors.GrayText;
        lblSteamEmuHint.Text = "Leave blank to auto-derive both from the instance name + id. Configure GoldBerg DLL path and run 'Install GoldBerg' in Settings before launching.";

        grpSteamEmu.Controls.Add(chkUseSteamEmu);
        grpSteamEmu.Controls.Add(lblSteamName);
        grpSteamEmu.Controls.Add(txtSteamName);
        grpSteamEmu.Controls.Add(lblSteamId);
        grpSteamEmu.Controls.Add(txtSteamId);
        grpSteamEmu.Controls.Add(lblSteamEmuHint);

        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new System.Drawing.Point(245, 345);
        btnOk.Size = new System.Drawing.Size(85, 28);
        btnOk.Text = "OK";
        btnOk.Click += BtnOk_Click;

        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new System.Drawing.Point(335, 345);
        btnCancel.Size = new System.Drawing.Size(85, 28);
        btnCancel.Text = "Cancel";

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ClientSize = new System.Drawing.Size(440, 385);
        Controls.Add(lblName);
        Controls.Add(txtName);
        Controls.Add(lblAccount);
        Controls.Add(txtAccount);
        Controls.Add(lblExtraArgs);
        Controls.Add(txtExtraArgs);
        Controls.Add(chkIsolate);
        Controls.Add(lblIsolateHint);
        Controls.Add(grpSteamEmu);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Edit Instance";

        grpSteamEmu.ResumeLayout(false);
        grpSteamEmu.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
