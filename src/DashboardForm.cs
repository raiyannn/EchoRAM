using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EchoRAM
{
    partial class DashboardForm : Form
    {
        private TrayAppContext context;
        private System.Windows.Forms.Timer uiTimer;
        private CpuMonitor cpuMon;

        private Panel panelHeader;
        private Panel panelSidebar;
        private Panel panelContentArea;

        private Panel viewOverview;
        private Panel viewProcesses;
        private Panel viewGaming;
        private Panel viewRamMap;
        private Panel viewSettings;

        private Panel cardGauge;
        private Panel cardActions;
        private Panel cardBreakdown;

        private Button btnNavOverview;
        private Button btnNavProcesses;
        private Button btnNavGaming;
        private Button btnNavRamMap;
        private Button btnNavSettings;

        private Label lblModeBadge;
        private Label lblSessionStats;
        private Label lblStatusMessage;
        private Button btnHeroClean;
        private Button btnStandbyClean;
        private Button btnWorkingSetClean;

        private ListView lvProcesses;
        private TextBox txtSearchProc;

        private Label lblGameHeroStatus;
        private Label lblGameExplain;
        private ListBox lbGameList;
        private TextBox txtNewGame;
        private Button btnBoostWuWa;
        private CheckBox chkAggressiveGameTrim;
        private CheckBox chkGamePriority;
        private NumericUpDown numGameHigh;
        private NumericUpDown numGameInterval;

        private CheckBox chkAutoStart;
        private CheckBox chkGameMode;
        private CheckBox chkIdleClean;
        private CheckBox chkCpuSafety;
        private CheckBox chkNotifications;
        private CheckBox chkAutoUpdate;
        private NumericUpDown numHighUsage;
        private NumericUpDown numMediumUsage;
        private NumericUpDown numIdleMinutes;
        private TextBox txtHotkey;
        private ComboBox cmbTheme;
        private TextBox txtExcludedProcs;
        private Label lblTitle;
        private Label lblSub;

        public DashboardForm(TrayAppContext ctx)
        {
            this.context = ctx;
            this.cpuMon = new CpuMonitor();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            InitializeComponent();

            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 1000;
            uiTimer.Tick += (s, e) => RefreshDashboardData();
        }

        private void ApplyDwmTheme()
        {
            if (!this.IsHandleCreated) return;
            try
            {
                int darkMode = UITheme.IsLightMode ? 0 : 1;
                NativeMethods.DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
                NativeMethods.DwmSetWindowAttribute(this.Handle, 19, ref darkMode, sizeof(int));
            }
            catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDwmTheme();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                LayoutOverviewCards();
                RefreshDashboardData();
                uiTimer.Start();
            }
            else
            {
                uiTimer.Stop();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                context.ShowNotification("Running in Background", "EchoRAM continues monitoring RAM from the tray.");
                return;
            }
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.Text = "EchoRAM";
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(920, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = UITheme.BgDark;
            this.ForeColor = UITheme.TextPrimary;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.AutoScaleMode = AutoScaleMode.None;

            bool iconLoaded = false;
            if (File.Exists(context.IconPath))
            {
                try { this.Icon = new Icon(context.IconPath); iconLoaded = true; }
                catch { }
            }
            if (!iconLoaded)
            {
                try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                catch { }
            }

            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = UITheme.BgSidebar
            };
            panelHeader.Paint += (s, e) =>
            {
                using (var p = new Pen(UITheme.Border, 1f))
                    e.Graphics.DrawLine(p, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };

            lblTitle = new Label
            {
                Text = "EchoRAM",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Location = new Point(22, 14),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblTitle);

            lblSub = new Label
            {
                Text = "Windows Memory Optimizer & Game Shield",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, 40),
                AutoSize = true,
                UseMnemonic = false
            };
            panelHeader.Controls.Add(lblSub);

            lblModeBadge = new Label
            {
                Text = "STANDARD PROTECTION",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.AccentGreen,
                Padding = new Padding(10, 5, 10, 5),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblModeBadge.Location = new Point(this.ClientSize.Width - 250, 14);
            panelHeader.Controls.Add(lblModeBadge);

            lblSessionStats = new Label
            {
                Text = "Session: 0 MB freed (0 flushes)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = UITheme.AccentCyan,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblSessionStats.Location = new Point(this.ClientSize.Width - 250, 42);
            panelHeader.Controls.Add(lblSessionStats);

            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 195,
                BackColor = UITheme.BgSidebar
            };
            panelSidebar.Paint += (s, e) =>
            {
                using (var p = new Pen(UITheme.Border, 1f))
                    e.Graphics.DrawLine(p, panelSidebar.Width - 1, 0, panelSidebar.Width - 1, panelSidebar.Height);
            };

            btnNavOverview = CreateNavButton("Dashboard", 12, () => SwitchView(viewOverview));
            btnNavProcesses = CreateNavButton("Processes", 64, () => { SwitchView(viewProcesses); RefreshProcessList(); });
            btnNavGaming = CreateNavButton("Game Mode", 116, () => { SwitchView(viewGaming); RefreshGameList(); });
            btnNavRamMap = CreateNavButton("Memory Lists", 168, () => SwitchView(viewRamMap));
            btnNavSettings = CreateNavButton("Settings", 220, () => { SwitchView(viewSettings); LoadSettingsToUI(); });

            panelSidebar.Controls.Add(btnNavOverview);
            panelSidebar.Controls.Add(btnNavProcesses);
            panelSidebar.Controls.Add(btnNavGaming);
            panelSidebar.Controls.Add(btnNavRamMap);
            panelSidebar.Controls.Add(btnNavSettings);

            panelContentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.BgDark
            };

            // Add Fill panel before Left/Top docked panels so Z-order prevents clipping behind sidebar
            this.Controls.Add(panelContentArea);
            this.Controls.Add(panelSidebar);
            this.Controls.Add(panelHeader);

            panelHeader.SendToBack();
            panelSidebar.SendToBack();
            panelContentArea.BringToFront();

            InitOverviewView();
            InitProcessesView();
            InitGamingView();
            InitRamMapView();
            InitSettingsView();

            SwitchView(viewOverview);

            this.Resize += (s, e) =>
            {
                LayoutOverviewCards();
                if (lblModeBadge != null)
                    lblModeBadge.Location = new Point(panelHeader.Width - lblModeBadge.Width - 25, 14);
                if (lblSessionStats != null)
                    lblSessionStats.Location = new Point(panelHeader.Width - lblSessionStats.Width - 25, 42);
            };
        }

        private Button CreateNavButton(string text, int top, Action onClick)
        {
            var btn = new Button
            {
                Text = "  " + text,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(10, top),
                Size = new Size(175, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextSecondary,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = UITheme.BgCardHover;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SwitchView(Panel targetView)
        {
            viewOverview.Visible = (targetView == viewOverview);
            viewProcesses.Visible = (targetView == viewProcesses);
            viewGaming.Visible = (targetView == viewGaming);
            viewRamMap.Visible = (targetView == viewRamMap);
            viewSettings.Visible = (targetView == viewSettings);

            btnNavOverview.ForeColor = (targetView == viewOverview) ? UITheme.AccentCyan : UITheme.TextSecondary;
            btnNavProcesses.ForeColor = (targetView == viewProcesses) ? UITheme.AccentCyan : UITheme.TextSecondary;
            btnNavGaming.ForeColor = (targetView == viewGaming) ? UITheme.AccentCyan : UITheme.TextSecondary;
            btnNavRamMap.ForeColor = (targetView == viewRamMap) ? UITheme.AccentCyan : UITheme.TextSecondary;
            btnNavSettings.ForeColor = (targetView == viewSettings) ? UITheme.AccentCyan : UITheme.TextSecondary;

            btnNavOverview.BackColor = (targetView == viewOverview) ? UITheme.BgCardHover : Color.Transparent;
            btnNavProcesses.BackColor = (targetView == viewProcesses) ? UITheme.BgCardHover : Color.Transparent;
            btnNavGaming.BackColor = (targetView == viewGaming) ? UITheme.BgCardHover : Color.Transparent;
            btnNavRamMap.BackColor = (targetView == viewRamMap) ? UITheme.BgCardHover : Color.Transparent;
            btnNavSettings.BackColor = (targetView == viewSettings) ? UITheme.BgCardHover : Color.Transparent;

            if (targetView == viewOverview)
                LayoutOverviewCards();
        }

        private CheckBox CreateCheckBox(string text, int top)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(18, top),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UITheme.TextPrimary
            };
        }

        private void RefreshDashboardData()
        {
            if (!this.Visible) return;

            if (context.IsGameActive)
            {
                lblModeBadge.Text = string.Format("GAME SHIELD [{0}]", context.ActiveGameName);
                lblModeBadge.BackColor = UITheme.AccentPurple;
                if (btnHeroClean != null && btnHeroClean.Enabled)
                {
                    btnHeroClean.Text = "BOOST GAME RAM";
                    btnHeroClean.BackColor = UITheme.AccentPurple;
                    btnHeroClean.ForeColor = Color.White;
                }
            }
            else
            {
                lblModeBadge.Text = "STANDARD PROTECTION";
                lblModeBadge.BackColor = UITheme.AccentGreen;
                if (btnHeroClean != null && btnHeroClean.Enabled)
                {
                    btnHeroClean.Text = "OPTIMIZE RAM NOW";
                    btnHeroClean.BackColor = UITheme.AccentCyan;
                    btnHeroClean.ForeColor = Color.FromArgb(10, 20, 35);
                }
            }

            if (context.TotalReclaimedGB >= 1.0)
                lblSessionStats.Text = string.Format("Session: {0:F2} GB freed ({1} flushes)", context.TotalReclaimedGB, context.TotalFlushes);
            else
                lblSessionStats.Text = string.Format("Session: {0:F0} MB freed ({1} flushes)", context.TotalReclaimedGB * 1024.0, context.TotalFlushes);

            cardGauge.Invalidate();
            cardBreakdown.Invalidate();
        }

        public void OnFlushCompleted(double freedGB, uint beforeLoad, uint afterLoad)
        {
            if (context.IsGameActive)
            {
                btnHeroClean.Text = "BOOST GAME RAM";
                btnHeroClean.BackColor = UITheme.AccentPurple;
                btnHeroClean.ForeColor = Color.White;
            }
            else
            {
                btnHeroClean.Text = "OPTIMIZE RAM NOW";
                btnHeroClean.BackColor = UITheme.AccentCyan;
                btnHeroClean.ForeColor = Color.FromArgb(10, 20, 35);
            }
            btnHeroClean.Enabled = true;

            lblStatusMessage.Text = string.Format("Completed: RAM dropped from {0}% to {1}% (Freed {2:F2} GB)",
                beforeLoad, afterLoad, freedGB);
            lblStatusMessage.ForeColor = UITheme.AccentGreen;
        }

        private void ApplyThemeToUI()
        {
            ApplyDwmTheme();
            this.BackColor = UITheme.BgDark;
            this.ForeColor = UITheme.TextPrimary;
            panelHeader.BackColor = UITheme.BgSidebar;
            panelSidebar.BackColor = UITheme.BgSidebar;
            panelContentArea.BackColor = UITheme.BgDark;
            if (lblTitle != null) lblTitle.ForeColor = UITheme.TextPrimary;
            if (lblSub != null) lblSub.ForeColor = UITheme.TextSecondary;

            ApplyThemeRecursive(panelContentArea);
            SwitchView(viewOverview.Visible ? viewOverview :
                       viewProcesses.Visible ? viewProcesses :
                       viewGaming.Visible ? viewGaming :
                       viewRamMap.Visible ? viewRamMap : viewSettings);
            this.Invalidate(true);
        }

        private void ApplyThemeRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Panel)
                {
                    if (c == viewOverview || c == viewProcesses || c == viewGaming || c == viewRamMap || c == viewSettings)
                        c.BackColor = UITheme.BgDark;
                    else
                        c.BackColor = UITheme.BgCard;
                }
                else if (c is TextBox || c is NumericUpDown || c is ComboBox || c is ListBox || c is ListView)
                {
                    c.BackColor = UITheme.BgCard;
                    c.ForeColor = UITheme.TextPrimary;
                }
                else if (c is CheckBox)
                {
                    c.ForeColor = UITheme.TextPrimary;
                }
                else if (c is Label && c != lblModeBadge && c != lblSessionStats)
                {
                    if (c.ForeColor != UITheme.AccentCyan && c.ForeColor != UITheme.AccentGreen && c.ForeColor != UITheme.AccentPurple)
                        c.ForeColor = UITheme.TextPrimary;
                }

                if (c.HasChildren)
                    ApplyThemeRecursive(c);
            }
        }
    }
}
