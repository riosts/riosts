#nullable disable
using System.Windows.Forms;

namespace PixelWorldsInjector.Forms;

partial class SettingsDialog
{
    private System.ComponentModel.IContainer components;

    private Label lblGamePath;
    private TextBox txtGamePath;
    private Button btnBrowse;
    private Label lblSteamAppId;
    private NumericUpDown numSteamAppId;
    private Label lblMutexHint;
    private TextBox txtMutexHint;
    private CheckBox chkBypassSteam;
    private GroupBox grpSteamEmu;
    private Label lblGoldbergPath;
    private TextBox txtGoldbergPath;
    private Button btnBrowseGoldberg;
    private Label lblGoldbergStatus;
    private Button btnInstallGoldberg;
    private Button btnRestoreGoldberg;
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

        lblGamePath = new Label();
        txtGamePath = new TextBox();
        btnBrowse = new Button();
        lblSteamAppId = new Label();
        numSteamAppId = new NumericUpDown();
        lblMutexHint = new Label();
        txtMutexHint = new TextBox();
        chkBypassSteam = new CheckBox();
        grpSteamEmu = new GroupBox();
        lblGoldbergPath = new Label();
        txtGoldbergPath = new TextBox();
        btnBrowseGoldberg = new Button();
        lblGoldbergStatus = new Label();
        btnInstallGoldberg = new Button();
        btnRestoreGoldberg = new Button();
        btnOk = new Button();
        btnCancel = new Button();

        SuspendLayout();
        grpSteamEmu.SuspendLayout();

        lblGamePath.AutoSize = true;
        lblGamePath.Location = new System.Drawing.Point(12, 15);
        lblGamePath.Text = "PixelWorlds.exe path:";

        txtGamePath.Location = new System.Drawing.Point(160, 12);
        txtGamePath.Size = new System.Drawing.Size(320, 23);

        btnBrowse.Location = new System.Drawing.Point(485, 11);
        btnBrowse.Size = new System.Drawing.Size(80, 25);
        btnBrowse.Text = "Browse...";
        btnBrowse.Click += BtnBrowse_Click;

        lblSteamAppId.AutoSize = true;
        lblSteamAppId.Location = new System.Drawing.Point(12, 50);
        lblSteamAppId.Text = "Steam AppID:";

        numSteamAppId.Location = new System.Drawing.Point(160, 47);
        numSteamAppId.Size = new System.Drawing.Size(120, 23);
        numSteamAppId.Maximum = int.MaxValue;
        numSteamAppId.Minimum = 0;

        chkBypassSteam.AutoSize = true;
        chkBypassSteam.Location = new System.Drawing.Point(160, 80);
        chkBypassSteam.Text = "Drop steam_appid.txt (bypass Steam client)";

        lblMutexHint.AutoSize = true;
        lblMutexHint.Location = new System.Drawing.Point(12, 115);
        lblMutexHint.Text = "Mutex name hint (optional):";

        txtMutexHint.Location = new System.Drawing.Point(160, 112);
        txtMutexHint.Size = new System.Drawing.Size(405, 23);

        // ---- GoldBerg Steam Emulator group ----
        grpSteamEmu.Location = new System.Drawing.Point(12, 150);
        grpSteamEmu.Size = new System.Drawing.Size(553, 165);
        grpSteamEmu.Text = "Steam Emulator (GoldBerg) — required when Steam ticket auth is needed";

        lblGoldbergPath.AutoSize = true;
        lblGoldbergPath.Location = new System.Drawing.Point(10, 25);
        lblGoldbergPath.Text = "GoldBerg steam_api64.dll:";

        txtGoldbergPath.Location = new System.Drawing.Point(160, 22);
        txtGoldbergPath.Size = new System.Drawing.Size(295, 23);

        btnBrowseGoldberg.Location = new System.Drawing.Point(460, 21);
        btnBrowseGoldberg.Size = new System.Drawing.Size(80, 25);
        btnBrowseGoldberg.Text = "Browse...";
        btnBrowseGoldberg.Click += BtnBrowseGoldberg_Click;

        lblGoldbergStatus.Location = new System.Drawing.Point(10, 55);
        lblGoldbergStatus.Size = new System.Drawing.Size(530, 40);
        lblGoldbergStatus.ForeColor = System.Drawing.SystemColors.GrayText;
        lblGoldbergStatus.Text = "Status: unknown";

        btnInstallGoldberg.Location = new System.Drawing.Point(10, 105);
        btnInstallGoldberg.Size = new System.Drawing.Size(220, 30);
        btnInstallGoldberg.Text = "Install GoldBerg into game";
        btnInstallGoldberg.Click += BtnInstallGoldberg_Click;

        btnRestoreGoldberg.Location = new System.Drawing.Point(240, 105);
        btnRestoreGoldberg.Size = new System.Drawing.Size(220, 30);
        btnRestoreGoldberg.Text = "Restore original Steam DLL";
        btnRestoreGoldberg.Click += BtnRestoreGoldberg_Click;

        grpSteamEmu.Controls.Add(lblGoldbergPath);
        grpSteamEmu.Controls.Add(txtGoldbergPath);
        grpSteamEmu.Controls.Add(btnBrowseGoldberg);
        grpSteamEmu.Controls.Add(lblGoldbergStatus);
        grpSteamEmu.Controls.Add(btnInstallGoldberg);
        grpSteamEmu.Controls.Add(btnRestoreGoldberg);

        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new System.Drawing.Point(395, 330);
        btnOk.Size = new System.Drawing.Size(85, 28);
        btnOk.Text = "OK";
        btnOk.Click += BtnOk_Click;

        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new System.Drawing.Point(485, 330);
        btnCancel.Size = new System.Drawing.Size(85, 28);
        btnCancel.Text = "Cancel";

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ClientSize = new System.Drawing.Size(585, 375);
        Controls.Add(lblGamePath);
        Controls.Add(txtGamePath);
        Controls.Add(btnBrowse);
        Controls.Add(lblSteamAppId);
        Controls.Add(numSteamAppId);
        Controls.Add(chkBypassSteam);
        Controls.Add(lblMutexHint);
        Controls.Add(txtMutexHint);
        Controls.Add(grpSteamEmu);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";

        grpSteamEmu.ResumeLayout(false);
        grpSteamEmu.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
