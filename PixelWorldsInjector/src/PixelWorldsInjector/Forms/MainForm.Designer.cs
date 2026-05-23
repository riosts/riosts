#nullable disable
using System.Windows.Forms;

namespace PixelWorldsInjector.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuStrip;
    private ToolStripMenuItem menuFile;
    private ToolStripMenuItem menuFileSettings;
    private ToolStripMenuItem menuFileOpenDataFolder;
    private ToolStripMenuItem menuFileExit;
    private ToolStripMenuItem menuHelp;
    private ToolStripMenuItem menuHelpAbout;

    private ToolStrip toolStrip;
    private ToolStripButton tbCreate;
    private ToolStripButton tbLaunch;
    private ToolStripButton tbEdit;
    private ToolStripButton tbDelete;
    private ToolStripSeparator tbSeparator1;
    private ToolStripLabel tbGamePathLabel;
    private ToolStripTextBox tbGamePath;
    private ToolStripButton tbBrowse;
    private ToolStripButton tbAutoDetect;

    private ListView listInstances;
    private ColumnHeader colName;
    private ColumnHeader colAccount;
    private ColumnHeader colIsolate;
    private ColumnHeader colStatus;
    private ColumnHeader colLastLaunch;

    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _processMonitor?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        menuStrip = new MenuStrip();
        menuFile = new ToolStripMenuItem();
        menuFileSettings = new ToolStripMenuItem();
        menuFileOpenDataFolder = new ToolStripMenuItem();
        menuFileExit = new ToolStripMenuItem();
        menuHelp = new ToolStripMenuItem();
        menuHelpAbout = new ToolStripMenuItem();

        toolStrip = new ToolStrip();
        tbCreate = new ToolStripButton();
        tbLaunch = new ToolStripButton();
        tbEdit = new ToolStripButton();
        tbDelete = new ToolStripButton();
        tbSeparator1 = new ToolStripSeparator();
        tbGamePathLabel = new ToolStripLabel();
        tbGamePath = new ToolStripTextBox();
        tbBrowse = new ToolStripButton();
        tbAutoDetect = new ToolStripButton();

        listInstances = new ListView();
        colName = new ColumnHeader();
        colAccount = new ColumnHeader();
        colIsolate = new ColumnHeader();
        colStatus = new ColumnHeader();
        colLastLaunch = new ColumnHeader();

        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();

        // menuStrip
        menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuHelp });
        menuStrip.Location = new System.Drawing.Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.TabIndex = 0;

        // menuFile
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuFileSettings, menuFileOpenDataFolder, new ToolStripSeparator(), menuFileExit });
        menuFile.Text = "&File";

        menuFileSettings.Text = "&Settings...";
        menuFileSettings.Click += MenuFileSettings_Click;

        menuFileOpenDataFolder.Text = "Open &Data Folder";
        menuFileOpenDataFolder.Click += MenuFileOpenDataFolder_Click;

        menuFileExit.Text = "E&xit";
        menuFileExit.Click += (_, _) => Close();

        menuHelp.DropDownItems.Add(menuHelpAbout);
        menuHelp.Text = "&Help";
        menuHelpAbout.Text = "&About";
        menuHelpAbout.Click += MenuHelpAbout_Click;

        // toolStrip
        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            tbCreate, tbLaunch, tbEdit, tbDelete,
            tbSeparator1,
            tbGamePathLabel, tbGamePath, tbBrowse, tbAutoDetect,
        });
        toolStrip.Location = new System.Drawing.Point(0, 24);

        tbCreate.Text = "Create Instance";
        tbCreate.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tbCreate.Click += TbCreate_Click;

        tbLaunch.Text = "Launch";
        tbLaunch.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tbLaunch.Click += TbLaunch_Click;

        tbEdit.Text = "Edit";
        tbEdit.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tbEdit.Click += TbEdit_Click;

        tbDelete.Text = "Delete";
        tbDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tbDelete.Click += TbDelete_Click;

        tbGamePathLabel.Text = "PixelWorlds.exe:";

        tbGamePath.AutoSize = false;
        tbGamePath.Width = 360;
        tbGamePath.LostFocus += TbGamePath_LostFocus;

        tbBrowse.Text = "Browse...";
        tbBrowse.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tbBrowse.Click += TbBrowse_Click;

        tbAutoDetect.Text = "Auto-detect";
        tbAutoDetect.DisplayStyle = ToolStripItemDisplayStyle.Text;
        tbAutoDetect.Click += TbAutoDetect_Click;

        // listInstances
        listInstances.Columns.AddRange(new[] { colName, colAccount, colIsolate, colStatus, colLastLaunch });
        listInstances.Dock = DockStyle.Fill;
        listInstances.FullRowSelect = true;
        listInstances.GridLines = true;
        listInstances.HideSelection = false;
        listInstances.MultiSelect = false;
        listInstances.View = View.Details;
        listInstances.DoubleClick += ListInstances_DoubleClick;

        colName.Text = "Name";
        colName.Width = 180;
        colAccount.Text = "Account label";
        colAccount.Width = 180;
        colIsolate.Text = "Isolate data";
        colIsolate.Width = 90;
        colStatus.Text = "Status";
        colStatus.Width = 100;
        colLastLaunch.Text = "Last launched";
        colLastLaunch.Width = 160;

        // statusStrip
        statusStrip.Items.Add(statusLabel);
        statusLabel.Text = "Ready";

        // MainForm
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(900, 500);
        Controls.Add(listInstances);
        Controls.Add(toolStrip);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "MainForm";
        Text = "Pixel Worlds Injector";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new System.Drawing.Size(720, 380);
    }
}
