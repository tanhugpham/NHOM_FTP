using FileTransfer.Server.Networking;
using FileTransfer.Server.Services;

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FileTransfer.Server
{
    public partial class Form1 : Form
    {
        private TcpServer _server;

        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblIpValue;
        private Label lblPortValue;
        private Label lblStartedAt;
        private Label lblStoragePath;
        private Label lblDbStatus;
        private Label lblFooter;
        private Label lblUptime;

        private Label lblStatus;

        private TextBox txtIp;
        private TextBox txtPort;

        private Button btnStart;
        private Button btnStop;
        private Button btnRestart;
        private Button btnClearLogs;
        private Button btnOpenStorage;
        private Button btnClearDbLogs;

        // Push file UI
        private ListBox lstOnlineClients;
        private Button btnPushFile;
        private Timer _pushTimer;

        private ListBox lstLogs;

        private DateTime? _startedAt;
        private TimeSpan _pausedTotal = TimeSpan.Zero;
        private Timer _timer;
        private ToolTip _toolTip;

        public Form1()

        {
            
            InitializeComponent();

            BuildModernUi();
            _toolTip = new ToolTip();

            _toolTip.AutoPopDelay = 10000;
            _toolTip.InitialDelay = 300;
            _toolTip.ReshowDelay = 100;
            _toolTip.ShowAlways = true;

            txtPort.Text = "9000";
            txtIp.Text = GetLocalIpAddress();

            btnStop.Enabled = false;

            lblStatus.Text = "STOPPED";
            lblStatus.ForeColor =
                Color.FromArgb(239, 68, 68);

            lblIpValue.Text = txtIp.Text;
            lblPortValue.Text = txtPort.Text;

            lblStartedAt.Text = "-";

            string storagePath = GetStorageFolder();

            string fullStoragePath = GetStorageFolder();

           

            lblStoragePath.Text =
                "Server Storage";

            _toolTip.SetToolTip(
                lblStoragePath,
                fullStoragePath);

            lblStoragePath.Cursor =
                Cursors.Hand;

            lblStoragePath.ForeColor =
                Color.FromArgb(96, 165, 250);

            lblStoragePath.Click +=
                btnOpenStorage_Click;

            lblDbStatus.Text =
                "PostgreSQL Render";

            lblFooter.Text =
                "Server is stopped";

            _server = new TcpServer();
            _server.OnLog += AddLog;

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            _timer.Start();

            AddLog("Server UI initialized");
        }

        private void BuildModernUi()
        {
            this.Text =
                "Secure File Transfer Server";

            this.WindowState =
                FormWindowState.Maximized;

            this.StartPosition =
                FormStartPosition.CenterScreen;

            this.BackColor =
                Color.FromArgb(11, 18, 32);

            this.Font =
                new Font("Segoe UI", 10F);

            this.Controls.Clear();

            TableLayoutPanel root =
                new TableLayoutPanel();

            root.Dock = DockStyle.Fill;

            root.Padding =
                new Padding(14);

            root.BackColor =
                Color.FromArgb(11, 18, 32);

            root.ColumnCount = 1;
            root.RowCount = 4;

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 90));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 120));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 52));

            this.Controls.Add(root);

            Panel header = CreateCard();

            header.Dock = DockStyle.Fill;

            root.Controls.Add(header);

            lblTitle = new Label();

            lblTitle.Text =
                "SECURE FILE TRANSFER SERVER";

            lblTitle.Font =
                new Font(
                    "Segoe UI",
                    20F,
                    FontStyle.Bold);

            lblTitle.ForeColor =
                Color.White;

            lblTitle.AutoSize = true;

            lblTitle.Location =
                new Point(28, 24);

            header.Controls.Add(lblTitle);

            lblSubtitle = new Label();

            lblSubtitle.Text =
                "Real-time | Encrypted | Reliable | Secure";

            lblSubtitle.Font =
                new Font("Segoe UI", 9F);

            lblSubtitle.ForeColor =
                Color.FromArgb(203, 213, 225);

            lblSubtitle.AutoSize = true;

            lblSubtitle.Location =
                new Point(31, 70);

            header.Controls.Add(lblSubtitle);

            Panel statusCard =
                CreateSmallCard(330, 80);

            statusCard.Anchor =
                AnchorStyles.Top | AnchorStyles.Right;

            statusCard.Location =
                new Point(700, 10);

            header.Controls.Add(statusCard);

            Label lblStatusTitle =
                CreateSmallLabel(
                    "SERVER STATUS",
                    14,
                    12);

            statusCard.Controls.Add(lblStatusTitle);

            lblStatus = new Label();

            lblStatus.Text = "STOPPED";

            lblStatus.Font =
                new Font(
                    "Segoe UI",
                    17F,
                    FontStyle.Bold);

            lblStatus.ForeColor =
                Color.FromArgb(239, 68, 68);

            lblStatus.AutoSize = true;

            lblStatus.Location =
                new Point(14, 34);

            statusCard.Controls.Add(lblStatus);

            Panel infoCard =
                CreateSmallCard(300, 65);

            infoCard.Anchor =
                AnchorStyles.Top | AnchorStyles.Right;

            infoCard.Location =
                new Point(1045, 10);

            header.Controls.Add(infoCard);

            Label lblIpTitle =
                CreateSmallLabel(
                    "IP / PORT",
                    14,
                    12);

            infoCard.Controls.Add(lblIpTitle);

            lblIpValue =
                CreateValueLabel("-", 14, 36);

            infoCard.Controls.Add(lblIpValue);

            lblPortValue =
                CreateValueLabel("-", 170, 36);

            infoCard.Controls.Add(lblPortValue);

            FlowLayoutPanel actionRow =
                new FlowLayoutPanel();

            actionRow.Dock = DockStyle.Fill;

            actionRow.BackColor =
                Color.Transparent;

            actionRow.Padding =
                new Padding(0, 10, 0, 0);

            root.Controls.Add(actionRow);

            Panel serverControl =
                CreateCard();

            serverControl.Width = 520;
            serverControl.Height = 105;

            serverControl.Margin =
                new Padding(0, 0, 14, 0);

            actionRow.Controls.Add(serverControl);

            Label lblControl =
                CreateSectionTitle(
                    "Server Control",
                    16,
                    12);

            serverControl.Controls.Add(lblControl);

            txtIp = new TextBox();
            txtIp.Visible = false;

            txtPort = new TextBox();
            txtPort.Visible = false;

            btnStart =
                CreateButton(
                    "Start Server",
                    Color.FromArgb(22, 128, 61),
                    150);

            btnStart.Location =
                new Point(16, 44);

            btnStart.Click += btnStart_Click;

            serverControl.Controls.Add(btnStart);

            btnStop =
                CreateButton(
                    "Stop Server",
                    Color.FromArgb(220, 38, 38),
                    150);

            btnStop.Location =
                new Point(180, 44);

            btnStop.Click += btnStop_Click;

            serverControl.Controls.Add(btnStop);

            btnRestart =
                CreateButton(
                    "Restart",
                    Color.FromArgb(51, 65, 85),
                    135);

            btnRestart.Location =
                new Point(344, 44);

            btnRestart.Click += btnRestart_Click;

            serverControl.Controls.Add(btnRestart);

            Panel actions =
                CreateCard();

            actions.Width = 620;
            actions.Height = 105;

            actionRow.Controls.Add(actions);

            Label lblActions =
                CreateSectionTitle(
                    "Actions",
                    16,
                    12);

            actions.Controls.Add(lblActions);

            btnOpenStorage =
                CreateButton(
                    "Open Storage",
                    Color.FromArgb(37, 99, 235),
                    150);

            btnOpenStorage.Location =
                new Point(16, 44);

            btnOpenStorage.Click +=
                btnOpenStorage_Click;

            actions.Controls.Add(btnOpenStorage);

            btnClearLogs =
                CreateButton(
                    "Clear UI Logs",
                    Color.FromArgb(124, 58, 237),
                    150);

            btnClearLogs.Location =
                new Point(210, 44);

            btnClearLogs.Click +=
                btnClearLogs_Click;

            actions.Controls.Add(btnClearLogs);

            btnClearDbLogs =
                CreateButton(
                    "Clear DB Logs",
                    Color.FromArgb(234, 88, 12),
                    150);

            btnClearDbLogs.Location =
                new Point(404, 44);

            btnClearDbLogs.Click +=
                btnClearDbLogs_Click;

            actions.Controls.Add(btnClearDbLogs);

            TableLayoutPanel body =
                new TableLayoutPanel();

            body.Dock = DockStyle.Fill;

            body.ColumnCount = 2;

            body.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 360));

            body.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            root.Controls.Add(body);

            Panel left =
                CreateCard();

            left.Dock = DockStyle.Fill;

            left.Margin =
                new Padding(0, 0, 14, 0);

            body.Controls.Add(left);

            Label lblInfo =
                CreateSectionTitle(
                    "Server Information",
                    18,
                    16);

            left.Controls.Add(lblInfo);

            AddInfoRow(
                left,
                "Storage Path:",
                out lblStoragePath,
                62);

            AddInfoRow(
                left,
                "Database:",
                out lblDbStatus,
                102);

            AddInfoRow(
                left,
                "Encryption:",
                out Label lblEnc,
                142);

            lblEnc.Text = "AES";

            AddInfoRow(
                left,
                "Max Chunk:",
                out Label lblChunk,
                182);

            lblChunk.Text = "64 KB";

            AddInfoRow(
                left,
                "Started At:",
                out lblStartedAt,
                222);

            AddInfoRow(
                left,
                "Uptime:",
                out lblUptime,
                262);

            // Online Clients section
            Label lblClients =
                CreateSectionTitle(
                    "Online Clients",
                    18,
                    292);

            left.Controls.Add(lblClients);

            lstOnlineClients = new ListBox();
            lstOnlineClients.Location = new Point(18, 322);
            lstOnlineClients.Size = new Size(320, 130);
            lstOnlineClients.BackColor = Color.FromArgb(15, 23, 42);
            lstOnlineClients.ForeColor = Color.White;
            lstOnlineClients.BorderStyle = BorderStyle.FixedSingle;
            lstOnlineClients.Font = new Font("Consolas", 10F);

            left.Controls.Add(lstOnlineClients);

            btnPushFile =
                CreateButton(
                    "Push File to Selected",
                    Color.FromArgb(220, 38, 38),
                    320);

            btnPushFile.Location = new Point(18, 458);
            btnPushFile.Click += btnPushFile_Click;

            left.Controls.Add(btnPushFile);

            // Push timer to refresh client list
            _pushTimer = new Timer();
            _pushTimer.Interval = 3000; // every 3 seconds
            _pushTimer.Tick += PushTimer_Tick;

            Panel right =
                CreateCard();

            right.Dock = DockStyle.Fill;

            body.Controls.Add(right);

            Label lblLogsTitle =
                CreateSectionTitle(
                    "Activity Logs",
                    18,
                    16);

            right.Controls.Add(lblLogsTitle);

            lstLogs = new ListBox();

            lstLogs.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            lstLogs.Location =
                new Point(18, 52);

            lstLogs.Size =
                new Size(900, 420);

            lstLogs.BackColor =
                Color.FromArgb(15, 23, 42);

            lstLogs.ForeColor =
                Color.White;

            lstLogs.BorderStyle =
                BorderStyle.FixedSingle;

            lstLogs.Font =
                new Font("Consolas", 10F);

            right.Controls.Add(lstLogs);

            right.Resize += (s, e) =>
            {
                lstLogs.Size =
                    new Size(
                        right.Width - 36,
                        right.Height - 70);
            };

            Panel footer =
                CreateCard();

            footer.Dock = DockStyle.Fill;

            footer.Margin = new Padding(0);

            root.Controls.Add(footer);

            lblFooter = new Label();

            lblFooter.Text =
                "Server is stopped";

            lblFooter.ForeColor =
                Color.FromArgb(203, 213, 225);

            lblFooter.Font =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold);

            lblFooter.AutoSize = true;

            lblFooter.Location =
                new Point(18, 15);

            footer.Controls.Add(lblFooter);
            this.Resize += (s, e) =>
            {
                lstLogs.Width =
                    this.Width - 470;

                lstLogs.Height =
                    this.Height - 360;
            };
        }

        private Panel CreateCard()
        {
            Panel panel = new Panel();

            panel.BackColor =
                Color.FromArgb(16, 27, 51);

            return panel;
        }

        private Panel CreateSmallCard(
            int width,
            int height)
        {
            Panel panel = new Panel();

            panel.Width = width;
            panel.Height = height;

            panel.BackColor =
                Color.FromArgb(20, 34, 58);

            return panel;
        }

        private Label CreateSectionTitle(
            string text,
            int x,
            int y)
        {
            Label label = new Label();

            label.Text = text;

            label.Font =
                new Font(
                    "Segoe UI",
                    13F,
                    FontStyle.Bold);

            label.ForeColor =
                Color.White;

            label.AutoSize = true;

            label.Location =
                new Point(x, y);

            return label;
        }

        private Label CreateSmallLabel(
            string text,
            int x,
            int y)
        {
            Label label = new Label();

            label.Text = text;

            label.Font =
                new Font(
                    "Segoe UI",
                    9F,
                    FontStyle.Bold);

            label.ForeColor =
                Color.FromArgb(203, 213, 225);

            label.AutoSize = true;

            label.Location =
                new Point(x, y);

            return label;
        }

        private Label CreateValueLabel(
            string text,
            int x,
            int y)
        {
            Label label = new Label();

            label.Text = text;

            label.Font =
                new Font(
                    "Segoe UI",
                    13F,
                    FontStyle.Bold);

            label.ForeColor =
                Color.White;

            label.AutoSize = true;

            label.Location =
                new Point(x, y);

            return label;
        }

        private Button CreateButton(
            string text,
            Color color,
            int width)
        {
            Button button = new Button();

            button.Text = text;

            button.Width = width;
            button.Height = 36;

            button.BackColor = color;

            button.ForeColor =
                Color.White;

            button.FlatStyle =
                FlatStyle.Flat;

            button.FlatAppearance.BorderSize = 0;

            button.Font =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold);

            return button;
        }

        private void AddInfoRow(
            Panel parent,
            string title,
            out Label valueLabel,
            int y)
        {
            Label titleLabel = new Label();

            titleLabel.Text = title;

            titleLabel.ForeColor =
                Color.FromArgb(203, 213, 225);

            titleLabel.Font =
                new Font("Segoe UI", 10F);

            titleLabel.Location =
                new Point(18, y);

            titleLabel.AutoSize = true;

            parent.Controls.Add(titleLabel);

            valueLabel = new Label();

            valueLabel.Text = "-";

            valueLabel.ForeColor =
                Color.White;

            valueLabel.Font =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold);

            valueLabel.Location =
                new Point(130, y);

            valueLabel.AutoSize = true;

            parent.Controls.Add(valueLabel);
        }

        private async void btnStart_Click(
            object sender,
            EventArgs e)
        {
            int port;

            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnStop.Text = "Stop Server";
            btnStop.BackColor = Color.FromArgb(220, 38, 38);

            lblStatus.Text = "RUNNING";

            lblStatus.ForeColor =
                Color.FromArgb(34, 197, 94);

            lblFooter.Text =
                "Server is running normally";

            _startedAt = DateTime.Now;

            lblStartedAt.Text =
                _startedAt.Value.ToString("HH:mm:ss");

            lblIpValue.Text = txtIp.Text;
            lblPortValue.Text = txtPort.Text;

            _pushTimer.Start();

            AddLog("Starting server...");

            await Task.Run(async () =>
            {
                await _server.StartAsync(port);
            });
        }

        private async void btnStop_Click(
            object sender,
            EventArgs e)
        {
            if (_server.IsRunning)
            {
                _server.Stop();

                if (_startedAt.HasValue)
                {
                    _pausedTotal += DateTime.Now - _startedAt.Value;
                }
                _startedAt = null;

                btnStart.Enabled = true;
                btnStop.Text = "Resume Server";
                btnStop.BackColor = Color.FromArgb(22, 128, 61);

                lblStatus.Text = "STOPPED";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                lblFooter.Text = "Server stopped - Click Resume to restart";

                AddLog("Server stopped");
            }
            else
            {
                _startedAt = DateTime.Now - _pausedTotal;
                _pausedTotal = TimeSpan.Zero;

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnStop.Text = "Stop Server";
                btnStop.BackColor = Color.FromArgb(220, 38, 38);

                lblStatus.Text = "RUNNING";
                lblStatus.ForeColor = Color.FromArgb(34, 197, 94);
                lblFooter.Text = "Server is running normally";

                lblIpValue.Text = txtIp.Text;
                lblPortValue.Text = txtPort.Text;

                _pushTimer.Start();

                AddLog("Resuming server...");

                int port;
                int.TryParse(txtPort.Text, out port);

                await Task.Run(async () =>
                {
                    await _server.StartAsync(port);
                });
            }
        }

        private async void btnRestart_Click(
            object sender,
            EventArgs e)
        {
            AddLog("Restarting server...");

            _server.Stop();

            await Task.Delay(500);

            btnStart_Click(sender, e);
        }

        private void btnClearLogs_Click(
            object sender,
            EventArgs e)
        {
            lstLogs.Items.Clear();

            AddLog("UI logs cleared");
        }

        private void btnOpenStorage_Click(
            object sender,
            EventArgs e)
        {
            string path =
                GetStorageFolder();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(path);
        }

        private async void btnClearDbLogs_Click(
            object sender,
            EventArgs e)
        {
            DialogResult result =
                MessageBox.Show(
                    "Bạn có chắc muốn xóa log database không?",
                    "Confirm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            AdminCleanupService cleanupService =
                new AdminCleanupService();

            await cleanupService.ClearLogsAsync();

            AddLog("Database logs cleared");
        }

        private void AddLog(string message)
        {
            if (lstLogs == null)
                return;

            if (lstLogs.InvokeRequired)
            {
                lstLogs.Invoke(
                    new Action(() =>
                        AddLog(message)));

                return;
            }

            string log =
                DateTime.Now.ToString("HH:mm:ss")
                + " | "
                + message;

            lstLogs.Items.Add(log);

            lstLogs.TopIndex =
                lstLogs.Items.Count - 1;
        }

        private string GetLocalIpAddress()
        {
            var host =
                Dns.GetHostEntry(
                    Dns.GetHostName());

            var ip =
                host.AddressList
                .FirstOrDefault(x =>
                    x.AddressFamily ==
                    AddressFamily.InterNetwork);

            return ip != null
                ? ip.ToString()
                : "127.0.0.1";
        }

        private string GetStorageFolder()
        {
            string appFolder =
                AppDomain.CurrentDomain.BaseDirectory;

            string storagePath =
                Path.Combine(
                    appFolder,
                    "Storage");

            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            return storagePath;
        }
        private string ShortPath(
            string path,
            int maxLength)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return "-";

                    if (path.Length <= maxLength)
                        return path;

                    string[] parts =
                        path.Split(
                            Path.DirectorySeparatorChar);

                    if (parts.Length < 2)
                        return path;

                    return parts[0]
                        + Path.DirectorySeparatorChar
                        + "..."
                        + Path.DirectorySeparatorChar
                        + parts[parts.Length - 1];
                }
        private void PushTimer_Tick(object sender, EventArgs e)
        {
            if (!_server.IsRunning)
                return;

            var users = _server.GetOnlineUsers();

            lstOnlineClients.Items.Clear();

            if (users.Count == 0)
            {
                lstOnlineClients.Items.Add("(No clients connected)");
            }
            else
            {
                foreach (var user in users)
                {
                    lstOnlineClients.Items.Add(user);
                }
            }
        }

        private async void btnPushFile_Click(object sender, EventArgs e)
        {
            if (lstOnlineClients.SelectedItem == null)
            {
                MessageBox.Show("Please select a client from the list");
                return;
            }

            string selectedUsername = lstOnlineClients.SelectedItem.ToString();

            if (selectedUsername.StartsWith("("))
            {
                MessageBox.Show("No clients connected");
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select file to push to " + selectedUsername;

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string filePath = dialog.FileName;
            AddLog("Pushing file to " + selectedUsername + ": " + filePath);

            bool success = await _server.PushFileToClientAsync(
                selectedUsername, filePath);

            if (success)
            {
                AddLog("File queued for push: " + Path.GetFileName(filePath));
                MessageBox.Show(
                    "File queued for push to "
                    + selectedUsername
                    + ".\nClient will receive it on next poll.");
            }
            else
            {
                AddLog("Push failed");
                MessageBox.Show("Push failed");
            }
        }

        private void Timer_Tick(
            object sender,
            EventArgs e)
        {
            if (_startedAt.HasValue)
            {
                TimeSpan uptime =
                    DateTime.Now -
                    _startedAt.Value;

                lblUptime.Text =
                    uptime.ToString(@"hh\:mm\:ss");

                lblUptime.ForeColor =
                    Color.FromArgb(34, 197, 94);
            }
            else
            {
                lblUptime.Text = "-";
                lblUptime.ForeColor =
                    Color.White;
            }
        }
    }
}