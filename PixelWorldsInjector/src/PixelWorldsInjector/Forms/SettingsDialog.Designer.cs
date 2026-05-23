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
        btnOk = new Button();
        btnCancel = new Button();

        SuspendLayout();

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

        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new System.Drawing.Point(395, 155);
        btnOk.Size = new System.Drawing.Size(85, 28);
        btnOk.Text = "OK";
        btnOk.Click += BtnOk_Click;

        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new System.Drawing.Point(485, 155);
        btnCancel.Size = new System.Drawing.Size(85, 28);
        btnCancel.Text = "Cancel";

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ClientSize = new System.Drawing.Size(585, 200);
        Controls.Add(lblGamePath);
        Controls.Add(txtGamePath);
        Controls.Add(btnBrowse);
        Controls.Add(lblSteamAppId);
        Controls.Add(numSteamAppId);
        Controls.Add(chkBypassSteam);
        Controls.Add(lblMutexHint);
        Controls.Add(txtMutexHint);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";

        ResumeLayout(false);
        PerformLayout();
    }
}
