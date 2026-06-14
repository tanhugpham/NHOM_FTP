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

        // Labels
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblIpValue;
        private Label lblPortValue;
        private Label lblStartedAt;
        private Label lblStoragePath;
        private Label lblDbStatus;
        private Label lblFooter;
        private Label lblUptime;
        private Label lblStatusDot;
        private Label lblStatusText;

        // Hidden inputs
        private TextBox txtIp;
        private TextBox txtPort;

        // Buttons
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

        // Logs
        private ListBox lstLogs;

        // Timers
        private DateTime? _startedAt;
        private TimeSpan _pausedTotal = TimeSpan.Zero;
        private Timer _timer;
        private ToolTip _toolTip;

        // Design tokens
        private static readonly Color C_BG = Color.FromArgb(10, 10, 12);
        private static readonly Color C_SURFACE = Color.FromArgb(16, 16, 20);
        private static readonly Color C_BORDER = Color.FromArgb(32, 32, 40);
        private static readonly Color C_MUTED = Color.FromArgb(90, 90, 105);
        private static readonly Color C_SUBTLE = Color.FromArgb(160, 160, 175);
        private static readonly Color C_TEXT = Color.FromArgb(225, 225, 230);
        private static readonly Color C_WHITE = Color.FromArgb(245, 245, 250);
        private static readonly Color C_GREEN = Color.FromArgb(100, 220, 140);
        private static readonly Color C_RED = Color.FromArgb(220, 90, 90);
        private static readonly Color C_ACCENT = Color.FromArgb(140, 140, 240);

        private static readonly Font F_TITLE = new Font("Segoe UI", 16F, FontStyle.Bold);
        private static readonly Font F_SUBTITLE = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static readonly Font F_SECTION = new Font("Segoe UI", 9F, FontStyle.Bold);
        private static readonly Font F_LABEL = new Font("Segoe UI", 9F);
        private static readonly Font F_VALUE = new Font("Segoe UI", 10F, FontStyle.Bold);
        private static readonly Font F_BTN = new Font("Segoe UI", 9F, FontStyle.Bold);
        private static readonly Font F_MONO = new Font("Consolas", 9.5F);
        private static readonly Font F_STATUS = new Font("Segoe UI", 11F, FontStyle.Bold);

        public Form1()
        {
            InitializeComponent();
            BuildUI();

            _toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };

            txtPort.Text = "9000";
            txtIp.Text = GetLocalIpAddress();

            btnStop.Enabled = false;

            SetStatus(false);

            lblIpValue.Text = txtIp.Text;
            lblPortValue.Text = txtPort.Text;
            lblStartedAt.Text = "—";

            _toolTip.SetToolTip(lblStoragePath, GetStorageFolder());
            lblStoragePath.Cursor = Cursors.Hand;
            lblStoragePath.ForeColor = C_ACCENT;
            lblStoragePath.Click += btnOpenStorage_Click;

            lblDbStatus.Text = "MySql";

            _server = new TcpServer();
            _server.OnLog += AddLog;

            _timer = new Timer { Interval = 1000 };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            AddLog("Server UI initialized.");
        }

        // ────────────────────────────────────────────────────
        //  UI CONSTRUCTION
        // ────────────────────────────────────────────────────

        private void BuildUI()
        {
            Text = "Secure File Transfer — Server";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_BG;
            Font = F_LABEL;

            Controls.Clear();

            // Hidden inputs
            txtIp = new TextBox { Visible = false };
            txtPort = new TextBox { Visible = false };
            Controls.Add(txtIp);
            Controls.Add(txtPort);

            // Root layout: header | toolbar | body | footer
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = C_BG,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // body
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // footer
            Controls.Add(root);

            root.Controls.Add(BuildHeader());
            root.Controls.Add(BuildToolbar());
            root.Controls.Add(BuildBody());
            root.Controls.Add(BuildFooter());
        }

        // ── Header ──────────────────────────────────────────

        private Panel BuildHeader()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_SURFACE,
                Padding = new Padding(0)
            };

            // Left border accent line
            var accent = new Panel
            {
                Width = 3,
                Height = 72,
                BackColor = C_ACCENT,
                Location = new Point(0, 0)
            };
            panel.Controls.Add(accent);

            lblTitle = new Label
            {
                Text = "Secure File Transfer",
                Font = F_TITLE,
                ForeColor = C_WHITE,
                AutoSize = true,
                Location = new Point(24, 14)
            };
            panel.Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                Text = "Encrypted · Real-time · Reliable",
                Font = F_SUBTITLE,
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(26, 44)
            };
            panel.Controls.Add(lblSubtitle);

            // Status badge — anchored right
            var statusBox = new Panel
            {
                Width = 180,
                Height = 40,
                BackColor = C_BG,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.Width - 210, 16)
            };
            panel.Controls.Add(statusBox);

            lblStatusDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 11F),
                ForeColor = C_RED,
                AutoSize = true,
                Location = new Point(0, 4)
            };
            statusBox.Controls.Add(lblStatusDot);

            lblStatusText = new Label
            {
                Text = "STOPPED",
                Font = F_STATUS,
                ForeColor = C_RED,
                AutoSize = true,
                Location = new Point(22, 4)
            };
            statusBox.Controls.Add(lblStatusText);

            // Keep status right-aligned on resize
            panel.Resize += (s, e) =>
            {
                statusBox.Left = panel.Width - statusBox.Width - 28;
            };

            // Also store ref as lblStatus equivalent (use lblStatusText)
            return panel;
        }

        // ── Toolbar ─────────────────────────────────────────

        private Panel BuildToolbar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                Padding = new Padding(0)
            };

            // Top hairline
            var line = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = C_BORDER };
            bar.Controls.Add(line);
            // Bottom hairline
            var line2 = new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = C_BORDER };
            bar.Controls.Add(line2);

            int x = 20;

            btnStart = MakeToolBtn("▶  Start", C_GREEN, ref x);
            btnStart.Click += btnStart_Click;
            bar.Controls.Add(btnStart);

            btnStop = MakeToolBtn("■  Stop", C_RED, ref x);
            btnStop.Click += btnStop_Click;
            bar.Controls.Add(btnStop);

            btnRestart = MakeToolBtn("↺  Restart", C_MUTED, ref x);
            btnRestart.Click += btnRestart_Click;
            bar.Controls.Add(btnRestart);

            // Separator
            x += 12;
            var sep = new Panel { Width = 1, Height = 28, BackColor = C_BORDER, Location = new Point(x, 14) };
            bar.Controls.Add(sep);
            x += 16;

            btnOpenStorage = MakeToolBtn("⊞  Storage", C_SUBTLE, ref x);
            btnOpenStorage.Click += btnOpenStorage_Click;
            bar.Controls.Add(btnOpenStorage);

            btnClearLogs = MakeToolBtn("⌫  Clear Logs", C_SUBTLE, ref x);
            btnClearLogs.Click += btnClearLogs_Click;
            bar.Controls.Add(btnClearLogs);

            btnClearDbLogs = MakeToolBtn("⌫  Clear DB", C_SUBTLE, ref x);
            btnClearDbLogs.Click += btnClearDbLogs_Click;
            bar.Controls.Add(btnClearDbLogs);

            return bar;
        }

        private Button MakeToolBtn(string text, Color fg, ref int x)
        {
            var btn = new Button
            {
                Text = text,
                Font = F_BTN,
                ForeColor = fg,
                BackColor = C_BG,
                FlatStyle = FlatStyle.Flat,
                Height = 32,
                AutoSize = true,
                Location = new Point(x, 12),
                Padding = new Padding(10, 0, 10, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = C_BORDER;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = C_SURFACE;
            btn.FlatAppearance.MouseDownBackColor = C_BORDER;

            // Measure approximate width
            x += TextRenderer.MeasureText(text, F_BTN).Width + 32;

            return btn;
        }

        // ── Body ─────────────────────────────────────────────

        private TableLayoutPanel BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = C_BG,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            body.Controls.Add(BuildLeftPanel());
            body.Controls.Add(BuildRightPanel());

            return body;
        }

        private Panel BuildLeftPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                Margin = new Padding(0, 0, 12, 0)
            };

            // Info card
            var infoCard = MakeCard(0, 0, 280, 230);
            panel.Controls.Add(infoCard);

            AddSectionHead(infoCard, "SERVER INFO", 16, 14);

            int row = 44;
            AddInfoPair(infoCard, "IP", ref row, out lblIpValue);
            AddInfoPair(infoCard, "Port", ref row, out lblPortValue);
            AddInfoPair(infoCard, "Started", ref row, out lblStartedAt);
            AddInfoPair(infoCard, "Uptime", ref row, out lblUptime);
            AddInfoPair(infoCard, "Encryption", ref row, out Label lblEnc); lblEnc.Text = "AES-256";
            AddInfoPair(infoCard, "Chunk Size", ref row, out Label lblChunk); lblChunk.Text = "64 KB";
            AddInfoPair(infoCard, "Storage", ref row, out lblStoragePath); lblStoragePath.Text = "Server Storage";
            AddInfoPair(infoCard, "Database", ref row, out lblDbStatus);

            // Online clients card
            var clientCard = MakeCard(0, 244, 280, 220);
            panel.Controls.Add(clientCard);

            AddSectionHead(clientCard, "ONLINE CLIENTS", 16, 14);

            lstOnlineClients = new ListBox
            {
                Location = new Point(14, 40),
                Size = new Size(252, 120),
                BackColor = C_BG,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = F_MONO,
                SelectionMode = SelectionMode.MultiExtended,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22
            };
            lstOnlineClients.DrawItem += LstOnlineClients_DrawItem;
            clientCard.Controls.Add(lstOnlineClients);

            // Hairline separator
            var sep = new Panel { Left = 14, Top = 166, Width = 252, Height = 1, BackColor = C_BORDER };
            clientCard.Controls.Add(sep);

            btnPushFile = new Button
            {
                Text = "Push File to Selected",
                Font = F_BTN,
                ForeColor = C_TEXT,
                BackColor = C_SURFACE,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(14, 174),
                Size = new Size(252, 32),
                Cursor = Cursors.Hand
            };
            btnPushFile.FlatAppearance.BorderColor = C_BORDER;
            btnPushFile.FlatAppearance.BorderSize = 1;
            btnPushFile.FlatAppearance.MouseOverBackColor = C_BORDER;
            btnPushFile.Click += btnPushFile_Click;
            clientCard.Controls.Add(btnPushFile);

            _pushTimer = new Timer { Interval = 3000 };
            _pushTimer.Tick += PushTimer_Tick;

            // Anchor cards on resize
            panel.Resize += (s, e) =>
            {
                infoCard.Width = panel.Width;
                clientCard.Width = panel.Width;
            };

            return panel;
        }

        private void LstOnlineClients_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = selected ? C_BORDER : C_BG;
            Color fg = selected ? C_WHITE : C_TEXT;

            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            string text = ((ListBox)sender).Items[e.Index].ToString();
            using (var brush = new SolidBrush(fg))
                e.Graphics.DrawString(text, F_MONO, brush, e.Bounds.X + 6, e.Bounds.Y + 3);

            e.DrawFocusRectangle();
        }

        private Panel BuildRightPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };

            var card = MakeCard(0, 0, 600, 400);
            card.Dock = DockStyle.Fill;
            panel.Controls.Add(card);

            AddSectionHead(card, "ACTIVITY LOG", 16, 14);

            lstLogs = new ListBox
            {
                Location = new Point(14, 42),
                Size = new Size(560, 350),
                BackColor = C_BG,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = F_MONO,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20
            };
            lstLogs.DrawItem += LstLogs_DrawItem;
            card.Controls.Add(lstLogs);

            card.Resize += (s, e) =>
            {
                lstLogs.Size = new Size(card.Width - 28, card.Height - 56);
            };

            return panel;
        }

        private void LstLogs_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool even = e.Index % 2 == 0;
            Color bg = even ? C_BG : Color.FromArgb(14, 14, 18);
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            string line = ((ListBox)sender).Items[e.Index].ToString();
            // Split timestamp from message
            int pipe = line.IndexOf('|');
            if (pipe > 0)
            {
                string ts = line.Substring(0, pipe + 1);
                string msg = line.Substring(pipe + 1);

                using (var tsBrush = new SolidBrush(C_MUTED))
                using (var msgBrush = new SolidBrush(C_TEXT))
                {
                    int tsW = TextRenderer.MeasureText(ts, F_MONO).Width;
                    e.Graphics.DrawString(ts, F_MONO, tsBrush, e.Bounds.X + 8, e.Bounds.Y + 2);
                    e.Graphics.DrawString(msg, F_MONO, msgBrush, e.Bounds.X + 8 + tsW, e.Bounds.Y + 2);
                }
            }
            else
            {
                using (var b = new SolidBrush(C_TEXT))
                    e.Graphics.DrawString(line, F_MONO, b, e.Bounds.X + 8, e.Bounds.Y + 2);
            }
        }

        // ── Footer ───────────────────────────────────────────

        private Panel BuildFooter()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_SURFACE,
                Padding = new Padding(0)
            };

            var topLine = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = C_BORDER };
            footer.Controls.Add(topLine);

            lblFooter = new Label
            {
                Text = "Server stopped — click Start to begin",
                Font = new Font("Segoe UI", 9F),
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(20, 10)
            };
            footer.Controls.Add(lblFooter);

            var version = new Label
            {
                Text = "v2.0",
                Font = new Font("Segoe UI", 9F),
                ForeColor = C_BORDER,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footer.Width - 50, 10)
            };
            footer.Controls.Add(version);
            footer.Resize += (s, e) => version.Left = footer.Width - version.Width - 20;

            return footer;
        }

        // ────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────

        private Panel MakeCard(int x, int y, int w, int h)
        {
            var p = new Panel
            {
                BackColor = C_SURFACE,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Padding = new Padding(0)
            };
            // Draw border via Paint
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(C_BORDER, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private void AddSectionHead(Panel parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(x, y)
            };
            parent.Controls.Add(lbl);
        }

        private void AddInfoPair(Panel parent, string key, ref int row, out Label valueLabel)
        {
            var keyLbl = new Label
            {
                Text = key,
                Font = F_LABEL,
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(16, row)
            };
            parent.Controls.Add(keyLbl);

            valueLabel = new Label
            {
                Text = "—",
                Font = F_VALUE,
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(115, row)
            };
            parent.Controls.Add(valueLabel);

            row += 22;
        }

        private void SetStatus(bool running)
        {
            if (lblStatusDot == null || lblStatusText == null) return;

            if (lblStatusDot.InvokeRequired)
            {
                lblStatusDot.Invoke(new Action(() => SetStatus(running)));
                return;
            }

            if (running)
            {
                lblStatusDot.ForeColor = C_GREEN;
                lblStatusText.Text = "RUNNING";
                lblStatusText.ForeColor = C_GREEN;
                lblFooter.Text = "Server is running normally";
            }
            else
            {
                lblStatusDot.ForeColor = C_RED;
                lblStatusText.Text = "STOPPED";
                lblStatusText.ForeColor = C_RED;
                lblFooter.Text = "Server stopped — click Start to begin";
            }
        }

        // ────────────────────────────────────────────────────
        //  EVENT HANDLERS
        // ────────────────────────────────────────────────────

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Invalid port number.");
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnStop.Text = "■  Stop";
            btnStop.ForeColor = C_RED;

            SetStatus(true);

            _startedAt = DateTime.Now;
            lblStartedAt.Text = _startedAt.Value.ToString("HH:mm:ss");
            lblIpValue.Text = txtIp.Text;
            lblPortValue.Text = txtPort.Text;

            _pushTimer.Start();
            AddLog("Starting server...");

            await Task.Run(async () => await _server.StartAsync(port));
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            if (_server.IsRunning)
            {
                _server.Stop();

                if (_startedAt.HasValue)
                    _pausedTotal += DateTime.Now - _startedAt.Value;
                _startedAt = null;

                btnStart.Enabled = true;
                btnStop.Text = "▶  Resume";
                btnStop.ForeColor = C_GREEN;

                SetStatus(false);
                lblFooter.Text = "Server paused — click Resume to continue";

                AddLog("Server stopped.");
            }
            else
            {
                _startedAt = DateTime.Now - _pausedTotal;
                _pausedTotal = TimeSpan.Zero;

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnStop.Text = "■  Stop";
                btnStop.ForeColor = C_RED;

                SetStatus(true);

                lblIpValue.Text = txtIp.Text;
                lblPortValue.Text = txtPort.Text;

                _pushTimer.Start();
                AddLog("Resuming server...");

                int.TryParse(txtPort.Text, out int port);
                await Task.Run(async () => await _server.StartAsync(port));
            }
        }

        private async void btnRestart_Click(object sender, EventArgs e)
        {
            AddLog("Restarting server...");
            _server.Stop();
            await Task.Delay(500);
            btnStart_Click(sender, e);
        }

        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            lstLogs.Items.Clear();
            AddLog("UI logs cleared.");
        }

        private void btnOpenStorage_Click(object sender, EventArgs e)
        {
            string path = GetStorageFolder();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start(path);
        }

        private async void btnClearDbLogs_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Delete all database logs?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            var svc = new AdminCleanupService();
            await svc.ClearLogsAsync();
            AddLog("Database logs cleared.");
        }

        private void AddLog(string message)
        {
            if (lstLogs == null) return;

            if (lstLogs.InvokeRequired)
            {
                lstLogs.Invoke(new Action(() => AddLog(message)));
                return;
            }

            string entry = DateTime.Now.ToString("HH:mm:ss") + " | " + message;
            lstLogs.Items.Add(entry);
            lstLogs.TopIndex = lstLogs.Items.Count - 1;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_startedAt.HasValue)
            {
                TimeSpan up = DateTime.Now - _startedAt.Value;
                lblUptime.Text = up.ToString(@"hh\:mm\:ss");
                lblUptime.ForeColor = C_GREEN;
            }
            else
            {
                lblUptime.Text = "—";
                lblUptime.ForeColor = C_TEXT;
            }
        }

        private void PushTimer_Tick(object sender, EventArgs e)
        {
            if (!_server.IsRunning) return;

            var users = _server.GetOnlineUsers();
            lstOnlineClients.Items.Clear();

            if (users.Count == 0)
                lstOnlineClients.Items.Add("No clients connected");
            else
                foreach (var u in users)
                    lstOnlineClients.Items.Add(u);
        }

        private async void btnPushFile_Click(object sender, EventArgs e)
        {
            if (lstOnlineClients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select at least one client.");
                return;
            }

            var selected = new List<string>();
            foreach (var item in lstOnlineClients.SelectedItems)
            {
                string name = item.ToString();
                if (!name.StartsWith("No ")) selected.Add(name);
            }

            if (selected.Count == 0) { MessageBox.Show("No valid clients selected."); return; }

            var dlg = new OpenFileDialog { Title = "Select files to push", Multiselect = true };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            int ok = 0, fail = 0;
            var failedList = new List<string>();

            foreach (var user in selected)
            {
                bool pushed = await _server.PushFilesToClientAsync(user, dlg.FileNames);
                if (pushed) { ok++; AddLog("Push to " + user + ": " + dlg.FileNames.Length + " file(s)"); }
                else { fail++; failedList.Add(user); AddLog("Push failed: " + user); }
            }

            string summary = $"Done.\n\nSuccess: {ok}\nFailed:  {fail}";
            if (fail > 0) summary += "\n\nFailed:\n  " + string.Join("\n  ", failedList);

            MessageBox.Show(summary, "Push Results", MessageBoxButtons.OK,
                fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // ────────────────────────────────────────────────────
        //  UTILITIES
        // ────────────────────────────────────────────────────

        private string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            return ip?.ToString() ?? "127.0.0.1";
        }

        private string GetStorageFolder()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
}