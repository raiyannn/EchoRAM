using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace EchoRAM
{
    class TrayAppContext : ApplicationContext
    {
        public AppSettings Settings { get; private set; }
        public string ConfigPath { get; private set; }
        public string LogPath { get; private set; }
        public string IconPath { get; private set; }

        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer monitorTimer;
        private HotKeyMessageWindow hotKeyWindow;
        private CpuMonitor cpuMonitor;

        private DateTime lastFlushTime = DateTime.MinValue;
        private int periodicCounter = 0;
        private bool isFlushRunning = false;
        private bool idleCleanTriggered = false;

        public bool IsGameActive { get; private set; }
        public string ActiveGameName { get; private set; }

        public int TotalFlushes { get; private set; }
        public double TotalReclaimedGB { get; private set; }

        private Bitmap baseIconBitmap = null;
        private IntPtr currentHIcon = IntPtr.Zero;

        private DashboardForm dashboardForm = null;
        private UpdateInfo pendingUpdate = null;
        private DateTime lastUpdateCheck = DateTime.MinValue;
        private System.Windows.Forms.Timer updateDelayTimer;

        public TrayAppContext()
        {
            string appDir = Path.GetDirectoryName(Application.ExecutablePath);
            ConfigPath = Path.Combine(appDir, "config.json");
            LogPath = Path.Combine(appDir, "EchoRAM.log");
            IconPath = Path.Combine(appDir, "ram_icon.ico");

            Settings = AppSettings.Load(ConfigPath);
            cpuMonitor = new CpuMonitor();

            if (File.Exists(IconPath))
            {
                try
                {
                    using (var temp = new Icon(IconPath, 32, 32))
                        baseIconBitmap = temp.ToBitmap();
                }
                catch { }
            }
            if (baseIconBitmap == null)
            {
                try
                {
                    using (var assoc = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
                    {
                        if (assoc != null)
                            baseIconBitmap = new Bitmap(assoc.ToBitmap(), 32, 32);
                    }
                }
                catch { }
            }

            NativeMethods.EnablePrivileges();

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "EchoRAM",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };

            trayIcon.DoubleClick += (s, e) => ShowDashboard();
            trayIcon.BalloonTipClicked += (s, e) =>
            {
                if (pendingUpdate != null && pendingUpdate.UpdateAvailable)
                    PromptAndInstallUpdate(pendingUpdate);
            };

            SetupHotkey();

            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = Math.Max(5000, Settings.CheckIntervalSeconds * 1000);
            monitorTimer.Tick += OnMonitorTick;
            monitorTimer.Start();

            if (Settings.CheckForUpdates)
            {
                updateDelayTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                updateDelayTimer.Tick += (s, e) =>
                {
                    updateDelayTimer.Stop();
                    updateDelayTimer.Dispose();
                    updateDelayTimer = null;
                    CheckForUpdates(false);
                };
                updateDelayTimer.Start();
            }

            UpdateTrayIconAndTooltip();

            Log("-------------------------------------------------");
            Log(string.Format("EchoRAM initialized (Polling: {0}s, GameMode: {1})",
                Settings.CheckIntervalSeconds, Settings.EnableGameMode));
            ShowNotification("EchoRAM Active",
                string.Format("Double-click tray icon to open Dashboard. Hotkey: {0}", Settings.Hotkey));
        }

        public void ShowDashboard()
        {
            if (dashboardForm == null || dashboardForm.IsDisposed)
                dashboardForm = new DashboardForm(this);

            dashboardForm.Show();
            dashboardForm.WindowState = FormWindowState.Normal;
            dashboardForm.BringToFront();
            dashboardForm.Activate();
        }

        public void SetupHotkey()
        {
            if (!Settings.EnableGlobalHotkey) return;

            try
            {
                if (hotKeyWindow == null)
                {
                    hotKeyWindow = new HotKeyMessageWindow();
                    hotKeyWindow.HotKeyPressed += () =>
                    {
                        Log("Hotkey pressed (" + Settings.Hotkey + ") - triggering flush");
                        RunFlush(IsGameActive ? FlushLevel.GameModeBoost : FlushLevel.Full, "Hotkey");
                    };
                    hotKeyWindow.Show();
                    hotKeyWindow.Hide();
                }

                uint mod = 0;
                Keys key = Keys.F;

                string hk = (Settings.Hotkey ?? "").ToUpperInvariant();
                if (hk.Contains("CTRL")) mod |= NativeMethods.MOD_CONTROL;
                if (hk.Contains("ALT")) mod |= NativeMethods.MOD_ALT;
                if (hk.Contains("SHIFT")) mod |= NativeMethods.MOD_SHIFT;
                if (hk.Contains("WIN")) mod |= NativeMethods.MOD_WIN;

                string[] parts = hk.Split('+');
                if (parts.Length > 0)
                {
                    string keyName = parts[parts.Length - 1].Trim();
                    try { key = (Keys)Enum.Parse(typeof(Keys), keyName, true); }
                    catch { key = Keys.F; }
                }

                hotKeyWindow.Register(mod, key);
            }
            catch { }
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var openDash = new ToolStripMenuItem("Open Dashboard", null, (s, e) => ShowDashboard());
            openDash.Font = new Font(openDash.Font, FontStyle.Bold);
            menu.Items.Add(openDash);

            menu.Items.Add(new ToolStripSeparator());

            var statusItem = new ToolStripMenuItem("RAM: Loading...");
            statusItem.Name = "menu_status";
            menu.Items.Add(statusItem);

            var modeItem = new ToolStripMenuItem("Mode: Standard");
            modeItem.Name = "menu_mode";
            menu.Items.Add(modeItem);

            var statsItem = new ToolStripMenuItem("Session: 0 MB freed (0 flushes)");
            statsItem.Name = "menu_stats";
            statsItem.Enabled = false;
            menu.Items.Add(statsItem);

            menu.Items.Add(new ToolStripSeparator());

            var quickFlush = new ToolStripMenuItem(string.Format("Quick Flush (Standby) [{0}]", Settings.Hotkey));
            quickFlush.Click += (s, e) => RunFlush(IsGameActive ? FlushLevel.GameModeBoost : FlushLevel.StandbyOnly, "Manual Quick");
            menu.Items.Add(quickFlush);

            var fullFlush = new ToolStripMenuItem("Full Deep Clean (All 5 Lists)");
            fullFlush.Click += (s, e) => RunFlush(IsGameActive ? FlushLevel.GameModeBoost : FlushLevel.Full, "Manual Deep Clean");
            menu.Items.Add(fullFlush);

            menu.Items.Add(new ToolStripSeparator());

            var hogsMenu = new ToolStripMenuItem("Top 5 RAM Processes");
            hogsMenu.Name = "menu_hogs";
            hogsMenu.DropDownItems.Add("Scanning...");
            menu.Items.Add(hogsMenu);

            var manualMenu = new ToolStripMenuItem("Memory List Operations");
            manualMenu.DropDownItems.Add("Empty Working Sets", null, (s, e) => RunFlush(FlushLevel.WorkingSets, "Manual"));
            manualMenu.DropDownItems.Add("Empty System Working Set", null, (s, e) => RunFlush(FlushLevel.SystemWorkingSet, "Manual"));
            manualMenu.DropDownItems.Add("Empty Modified Page List", null, (s, e) => RunFlush(FlushLevel.ModifiedPages, "Manual"));
            manualMenu.DropDownItems.Add("Empty Standby List", null, (s, e) => RunFlush(FlushLevel.StandbyOnly, "Manual"));
            manualMenu.DropDownItems.Add("Empty Priority 0 Standby List", null, (s, e) => RunFlush(FlushLevel.LowPriorityStandby, "Manual"));
            menu.Items.Add(manualMenu);

            menu.Items.Add(new ToolStripSeparator());

            var autostartItem = new ToolStripMenuItem("Start with Windows");
            autostartItem.Name = "menu_autostart";
            autostartItem.Click += (s, e) =>
            {
                bool isEnabled = AutoStartManager.IsAutoStartEnabled();
                bool ok = isEnabled ? AutoStartManager.DisableAutoStart() : AutoStartManager.EnableAutoStart();
                if (ok)
                {
                    bool newState = !isEnabled;
                    autostartItem.Checked = newState;
                    ShowNotification("Auto-Start " + (newState ? "Enabled" : "Disabled"),
                        newState ? "EchoRAM will now start automatically at Windows logon."
                                 : "Auto-start has been removed from Windows.");
                }
            };
            menu.Items.Add(autostartItem);

            menu.Items.Add("Edit config.json", null, (s, e) =>
            {
                if (File.Exists(ConfigPath)) Process.Start("notepad.exe", ConfigPath);
            });

            menu.Items.Add("Open Log File", null, (s, e) =>
            {
                if (File.Exists(LogPath)) Process.Start("notepad.exe", LogPath);
            });

            var updateItem = new ToolStripMenuItem("Check for Updates");
            updateItem.Name = "menu_update";
            updateItem.Click += (s, e) => CheckForUpdates(true);
            menu.Items.Add(updateItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, OnExit);

            menu.Opening += (s, e) => UpdateMenuDynamicData(menu);

            return menu;
        }

        private void UpdateMenuDynamicData(ContextMenuStrip menu)
        {
            var mem = NativeMethods.GetMemoryInfo();
            double usedGB = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024 * 1024);
            double totalGB = mem.ullTotalPhys / (1024.0 * 1024 * 1024);

            var asItem = menu.Items["menu_autostart"] as ToolStripMenuItem;
            if (asItem != null)
                asItem.Checked = AutoStartManager.IsAutoStartEnabled();

            var updItem = menu.Items["menu_update"] as ToolStripMenuItem;
            if (updItem != null)
            {
                if (pendingUpdate != null && pendingUpdate.UpdateAvailable)
                    updItem.Text = string.Format("Install Update (v{0})", pendingUpdate.LatestVersion);
                else
                    updItem.Text = string.Format("Check for Updates (v{0})", Program.AppVersion);
            }

            var st = menu.Items["menu_status"];
            if (st != null)
                st.Text = string.Format("RAM: {0}% ({1:F1} GB / {2:F1} GB)", mem.dwMemoryLoad, usedGB, totalGB);

            var md = menu.Items["menu_mode"];
            if (md != null)
            {
                if (IsGameActive)
                {
                    md.Text = string.Format("Mode: Game Shield [{0}]", ActiveGameName);
                    md.ForeColor = Color.DarkMagenta;
                }
                else
                {
                    md.Text = "Mode: Standard Protection";
                    md.ForeColor = Color.DarkGreen;
                }
            }

            var stats = menu.Items["menu_stats"];
            if (stats != null)
            {
                if (TotalReclaimedGB >= 1.0)
                    stats.Text = string.Format("Session: {0:F2} GB freed ({1} flushes)", TotalReclaimedGB, TotalFlushes);
                else
                    stats.Text = string.Format("Session: {0:F0} MB freed ({1} flushes)", TotalReclaimedGB * 1024.0, TotalFlushes);
            }

            var hogsMenu = menu.Items["menu_hogs"] as ToolStripMenuItem;
            if (hogsMenu != null)
            {
                hogsMenu.DropDownItems.Clear();
                try
                {
                    var procs = Process.GetProcesses();
                    var list = new List<KeyValuePair<string, long>>();

                    foreach (var p in procs)
                    {
                        try { list.Add(new KeyValuePair<string, long>(p.ProcessName, p.WorkingSet64)); }
                        catch { }
                    }

                    list.Sort((a, b) => b.Value.CompareTo(a.Value));
                    int count = Math.Min(5, list.Count);
                    for (int i = 0; i < count; i++)
                    {
                        double mb = list[i].Value / (1024.0 * 1024.0);
                        hogsMenu.DropDownItems.Add(string.Format("{0}. {1} ({2:F0} MB)", i + 1, list[i].Key, mb));
                    }
                }
                catch
                {
                    hogsMenu.DropDownItems.Add("Unable to inspect processes.");
                }
            }
        }

        private void OnMonitorTick(object sender, EventArgs e)
        {
            if (isFlushRunning) return;

            if (Settings.CheckForUpdates && (DateTime.Now - lastUpdateCheck).TotalHours >= 24)
                CheckForUpdates(false);

            var mem = NativeMethods.GetMemoryInfo();
            double ramPercent = mem.dwMemoryLoad;
            double cpuUsage = cpuMonitor.GetCpuUsage();
            TimeSpan idleTime = NativeMethods.GetIdleTime();

            CheckGameDetection();
            UpdateTrayIconAndTooltip();

            periodicCounter++;

            if (!IsGameActive && Settings.EnableIdleFlush && idleTime.TotalMinutes >= Settings.IdleMinutesThreshold)
            {
                if (!idleCleanTriggered && ramPercent >= Settings.MediumUsagePercent && CanFlush())
                {
                    idleCleanTriggered = true;
                    Log(string.Format("System Idle for {0:F0}m (RAM: {1}%) - running idle cleanup",
                        idleTime.TotalMinutes, ramPercent));
                    RunFlush(FlushLevel.Full, "Idle Clean");
                    return;
                }
            }
            else
            {
                idleCleanTriggered = false;
            }

            // Defer non-critical flushes during heavy CPU load to avoid frame drops
            if (Settings.EnableCpuSafety && cpuUsage >= Settings.CpuThresholdPercent)
                return;

            if (IsGameActive)
            {
                double gameHighThresh = Settings.GameHighUsagePercent > 0 ? Settings.GameHighUsagePercent : Settings.HighUsagePercent;
                int gameTicksPeriodic = (Settings.GameTrimIntervalMinutes * 60) / Math.Max(1, Settings.CheckIntervalSeconds);

                if (ramPercent >= gameHighThresh && CanFlush())
                {
                    Log(string.Format("In-Game RAM Pressure ({0}% >= {1}%) - executing Game Shield boost for [{2}]",
                        ramPercent, gameHighThresh, ActiveGameName));
                    RunFlush(FlushLevel.GameModeBoost, "High RAM in Game");
                }
                else if (gameTicksPeriodic > 0 && periodicCounter >= gameTicksPeriodic && CanFlush())
                {
                    periodicCounter = 0;
                    if (ramPercent >= 60.0)
                    {
                        Log(string.Format("Periodic Game Sweep (RAM: {0}%) - trimming background apps for [{1}]",
                            ramPercent, ActiveGameName));
                        RunFlush(FlushLevel.GameModeBoost, "Game Periodic Boost");
                    }
                    else
                    {
                        RunFlush(FlushLevel.StandbyOnly, "Game Standby Flush");
                    }
                }
                return;
            }

            int ticksPerPeriodic = (Settings.PeriodicFlushMinutes * 60) / Math.Max(1, Settings.CheckIntervalSeconds);

            if (ramPercent >= Settings.HighUsagePercent && CanFlush())
            {
                RunFlush(FlushLevel.Full, "High RAM");
            }
            else if (ramPercent >= Settings.MediumUsagePercent && CanFlush())
            {
                RunFlush(FlushLevel.StandbyOnly, "Medium RAM");
            }
            else if (periodicCounter >= ticksPerPeriodic && CanFlush())
            {
                periodicCounter = 0;
                RunFlush(FlushLevel.StandbyOnly, "Periodic");
            }
        }

        private bool CanFlush()
        {
            int cooldown = IsGameActive ? Math.Min(30, Settings.CooldownSeconds) : Settings.CooldownSeconds;
            return (DateTime.Now - lastFlushTime).TotalSeconds >= cooldown;
        }

        private void CheckGameDetection()
        {
            if (!Settings.EnableGameMode) return;

            string detected = "";
            try
            {
                if (Settings.GameProcessNames != null)
                {
                    foreach (string game in Settings.GameProcessNames)
                    {
                        if (string.IsNullOrEmpty(game)) continue;
                        string clean = game.Replace(".exe", "").Trim();
                        Process[] procs = Process.GetProcessesByName(clean);
                        if (procs != null && procs.Length > 0)
                        {
                            detected = game;
                            BoostGamePriority(procs);
                            break;
                        }
                    }
                }

                // Fallback: auto-detect Unreal Engine games (*-Win64-Shipping)
                if (string.IsNullOrEmpty(detected))
                {
                    foreach (Process p in Process.GetProcesses())
                    {
                        try
                        {
                            if (p.ProcessName.EndsWith("-Win64-Shipping", StringComparison.OrdinalIgnoreCase))
                            {
                                detected = p.ProcessName;
                                BoostGamePriority(new Process[] { p });
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (!IsGameActive && !string.IsNullOrEmpty(detected))
            {
                IsGameActive = true;
                ActiveGameName = detected;
                Log(string.Format("[Game Detected] {0} - engaging Game Shield", ActiveGameName));
                ShowNotification("Game Shield Engaged",
                    string.Format("Detected [{0}]. Background processes trimmed; game memory protected with High Priority.", ActiveGameName));
                RunFlush(FlushLevel.PreGameFlush, "Pre-Game Boost");
            }
            else if (IsGameActive && string.IsNullOrEmpty(detected))
            {
                string exitedGame = ActiveGameName;
                IsGameActive = false;
                ActiveGameName = "";
                Log(string.Format("[Game Exited] {0} - running post-game reclamation flush", exitedGame));
                RunFlush(FlushLevel.Full, "Post-Game Reclamation");
                ShowNotification("Game Session Ended", string.Format("Cleaned up remaining memory from [{0}].", exitedGame));
            }
        }

        private void BoostGamePriority(Process[] procs)
        {
            if (!Settings.GamePriorityBoost || procs == null) return;
            foreach (var gp in procs)
            {
                try
                {
                    if (gp.PriorityClass != ProcessPriorityClass.High)
                        gp.PriorityClass = ProcessPriorityClass.High;
                }
                catch { }
            }
        }

        private void UpdateTrayIconAndTooltip()
        {
            try
            {
                var mem = NativeMethods.GetMemoryInfo();
                double ramPct = mem.dwMemoryLoad;

                Color statusColor;
                if (IsGameActive)
                    statusColor = Color.FromArgb(180, 0, 255);
                else if (ramPct >= Settings.HighUsagePercent)
                    statusColor = Color.FromArgb(235, 45, 45);
                else if (ramPct >= Settings.MediumUsagePercent)
                    statusColor = Color.FromArgb(240, 160, 20);
                else
                    statusColor = Color.FromArgb(30, 200, 100);

                using (var bmp = new Bitmap(32, 32))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    if (baseIconBitmap != null)
                        g.DrawImage(baseIconBitmap, 0, 0, 32, 32);
                    else
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(20, 30, 50)))
                            g.FillRectangle(brush, 4, 4, 24, 24);
                    }

                    using (var glowBrush = new SolidBrush(Color.FromArgb(80, statusColor)))
                        g.FillEllipse(glowBrush, 18, 18, 14, 14);

                    using (var dotBrush = new SolidBrush(statusColor))
                        g.FillEllipse(dotBrush, 20, 20, 10, 10);

                    IntPtr hIcon = bmp.GetHicon();
                    Icon newIcon = Icon.FromHandle(hIcon);
                    trayIcon.Icon = newIcon;

                    if (currentHIcon != IntPtr.Zero)
                        NativeMethods.DestroyIcon(currentHIcon);
                    currentHIcon = hIcon;
                }

                string modeText = IsGameActive ? ("[Game: " + ActiveGameName + "]") : "Standard";
                trayIcon.Text = string.Format("EchoRAM ({0}% used)\nMode: {1} | Double-click for UI",
                    ramPct, modeText);
            }
            catch { }
        }

        public enum FlushLevel
        {
            StandbyOnly,
            WorkingSets,
            SystemWorkingSet,
            ModifiedPages,
            LowPriorityStandby,
            Full,
            GameModeGentle,
            GameModeBoost,
            PreGameFlush
        }

        public void RunFlush(FlushLevel level, string reason)
        {
            if (isFlushRunning) return;
            isFlushRunning = true;

            var before = NativeMethods.GetMemoryInfo();

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    int trimmedCount = 0;
                    switch (level)
                    {
                        case FlushLevel.Full:
                            NativeMethods.PurgeWorkingSets();
                            NativeMethods.PurgeSystemWorkingSet();
                            NativeMethods.PurgeModifiedPages();
                            NativeMethods.PurgeStandbyList();
                            NativeMethods.PurgeLowPriorityStandbyList();
                            trimmedCount = NativeMethods.TrimBackgroundProcesses(IsGameActive ? Settings.GameProcessNames : null);
                            break;

                        case FlushLevel.PreGameFlush:
                        case FlushLevel.GameModeBoost:
                        case FlushLevel.GameModeGentle:
                            NativeMethods.PurgeStandbyList();
                            NativeMethods.PurgeLowPriorityStandbyList();
                            NativeMethods.PurgeSystemWorkingSet();
                            NativeMethods.PurgeModifiedPages();
                            if (Settings.AggressiveBackgroundTrimInGame)
                            {
                                trimmedCount = NativeMethods.TrimBackgroundProcesses(Settings.GameProcessNames);
                            }
                            break;

                        case FlushLevel.StandbyOnly:
                            NativeMethods.PurgeStandbyList();
                            NativeMethods.PurgeLowPriorityStandbyList();
                            break;

                        case FlushLevel.WorkingSets:
                            NativeMethods.PurgeWorkingSets();
                            trimmedCount = NativeMethods.TrimBackgroundProcesses(IsGameActive ? Settings.GameProcessNames : null);
                            break;

                        case FlushLevel.SystemWorkingSet:
                            NativeMethods.PurgeSystemWorkingSet();
                            break;

                        case FlushLevel.ModifiedPages:
                            NativeMethods.PurgeModifiedPages();
                            break;

                        case FlushLevel.LowPriorityStandby:
                            NativeMethods.PurgeLowPriorityStandbyList();
                            break;
                    }

                    lastFlushTime = DateTime.Now;
                    periodicCounter = 0;
                    TotalFlushes++;

                    var after = NativeMethods.GetMemoryInfo();
                    double freedGB = 0.0;
                    if (after.ullAvailPhys > before.ullAvailPhys)
                    {
                        freedGB = (double)(after.ullAvailPhys - before.ullAvailPhys) / (1024.0 * 1024 * 1024);
                        TotalReclaimedGB += freedGB;
                    }

                    string logEntry = string.Format("[{0}] Flush ({1} / {2}) - RAM: {3}% -> {4}% (Freed: {5:F2} GB, Trimmed: {6} procs)",
                        DateTime.Now.ToString("HH:mm:ss"), level, reason,
                        before.dwMemoryLoad, after.dwMemoryLoad, freedGB, trimmedCount);
                    Log(logEntry);

                    UpdateTrayIconAndTooltip();

                    if (dashboardForm != null && !dashboardForm.IsDisposed && dashboardForm.Visible)
                    {
                        dashboardForm.Invoke((MethodInvoker)delegate
                        {
                            dashboardForm.OnFlushCompleted(freedGB, before.dwMemoryLoad, after.dwMemoryLoad);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log("Flush error: " + ex.Message);
                }
                finally
                {
                    isFlushRunning = false;
                }
            });
        }

        public void CheckForUpdates(bool userInitiated)
        {
            lastUpdateCheck = DateTime.Now;
            Updater.CheckForUpdatesAsync(info =>
            {
                Action handleResult = () =>
                {
                    if (!string.IsNullOrEmpty(info.Error))
                    {
                        if (userInitiated)
                        {
                            MessageBox.Show("Could not check for updates:\n" + info.Error,
                                "EchoRAM Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        return;
                    }

                    if (info.UpdateAvailable)
                    {
                        pendingUpdate = info;
                        Log(string.Format("[Update] EchoRAM v{0} available (current: v{1})", info.LatestVersion, Program.AppVersion));
                        if (userInitiated)
                        {
                            PromptAndInstallUpdate(info);
                        }
                        else
                        {
                            ShowNotification(
                                string.Format("EchoRAM Update Available (v{0})", info.LatestVersion),
                                "Click here or open Settings / Tray Menu to download and install.");
                        }
                    }
                    else if (userInitiated)
                    {
                        MessageBox.Show(
                            string.Format("You are already running the latest version (v{0}).", Program.AppVersion),
                            "EchoRAM Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                try
                {
                    if (dashboardForm != null && !dashboardForm.IsDisposed && dashboardForm.IsHandleCreated)
                        dashboardForm.BeginInvoke((MethodInvoker)delegate { handleResult(); });
                    else if (hotKeyWindow != null && !hotKeyWindow.IsDisposed && hotKeyWindow.IsHandleCreated)
                        hotKeyWindow.BeginInvoke((MethodInvoker)delegate { handleResult(); });
                    else
                        handleResult();
                }
                catch { }
            });
        }

        public void PromptAndInstallUpdate(UpdateInfo info)
        {
            if (info == null || !info.UpdateAvailable) return;

            DialogResult dr = MessageBox.Show(
                string.Format("EchoRAM v{0} is available on GitHub (current: v{1}).\n\nDownload and install the update now?",
                    info.LatestVersion, Program.AppVersion),
                "EchoRAM Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (dr != DialogResult.Yes) return;

            string err;
            if (Updater.DownloadAndApplyUpdate(info, out err))
            {
                Log(string.Format("[Update] Installing v{0} and restarting...", info.LatestVersion));
                OnExit(null, EventArgs.Empty);
            }
            else
            {
                MessageBox.Show(
                    "Auto-update failed: " + err + "\n\nOpening GitHub Releases page instead.",
                    "EchoRAM Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                try { Process.Start(new ProcessStartInfo { FileName = info.ReleasePageUrl, UseShellExecute = true }); } catch { }
            }
        }

        public void ShowNotification(string title, string message)
        {
            if (!Settings.EnableNotifications) return;
            try
            {
                trayIcon.BalloonTipTitle = title;
                trayIcon.BalloonTipText = message;
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(3000);
            }
            catch { }
        }

        public void Log(string msg)
        {
            try
            {
                string line = string.Format("[{0}] {1}{2}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg, Environment.NewLine);
                File.AppendAllText(LogPath, line);
            }
            catch { }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Log("EchoRAM shutting down.");
            monitorTimer.Stop();
            monitorTimer.Dispose();

            if (hotKeyWindow != null)
            {
                hotKeyWindow.Unregister();
                hotKeyWindow.Close();
                hotKeyWindow.Dispose();
            }

            if (dashboardForm != null && !dashboardForm.IsDisposed)
                dashboardForm.Dispose();

            if (currentHIcon != IntPtr.Zero)
                NativeMethods.DestroyIcon(currentHIcon);

            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }
    }
}
