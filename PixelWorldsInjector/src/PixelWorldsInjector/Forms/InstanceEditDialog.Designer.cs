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
        btnOk = new Button();
        btnCancel = new Button();

        SuspendLayout();

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
        lblIsolateHint.Size = new System.Drawing.Size(285, 50);
        lblIsolateHint.ForeColor = System.Drawing.SystemColors.GrayText;
        lblIsolateHint.Text = "When enabled, the game's LocalLow\\Kukouri\\Pixel Worlds folder is swapped for this instance's private copy.";

        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new System.Drawing.Point(245, 185);
        btnOk.Size = new System.Drawing.Size(85, 28);
        btnOk.Text = "OK";
        btnOk.Click += BtnOk_Click;

        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new System.Drawing.Point(335, 185);
        btnCancel.Size = new System.Drawing.Size(85, 28);
        btnCancel.Text = "Cancel";

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ClientSize = new System.Drawing.Size(440, 225);
        Controls.Add(lblName);
        Controls.Add(txtName);
        Controls.Add(lblAccount);
        Controls.Add(txtAccount);
        Controls.Add(lblExtraArgs);
        Controls.Add(txtExtraArgs);
        Controls.Add(chkIsolate);
        Controls.Add(lblIsolateHint);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Edit Instance";

        ResumeLayout(false);
        PerformLayout();
    }
}
