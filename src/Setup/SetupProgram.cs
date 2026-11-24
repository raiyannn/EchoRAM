using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EchoRAMSetup
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }

            if (!IsRunningAsAdmin())
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = args != null && args.Length > 0 ? string.Join(" ", args) : "",
                        Verb = "runas",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch { }
                return;
            }

            if (args != null && args.Length > 0)
            {
                string arg = args[0].ToLowerInvariant().Trim('-', '/');
                if (arg == "install" || arg == "setup" || arg == "autostart")
                {
                    bool ok = TaskManager.InstallAutoStart();
                    MessageBox.Show(
                        ok ? "EchoRAM has been installed to start automatically on Windows logon."
                           : "Failed to configure Task Scheduler auto-start.",
                        "EchoRAM Setup", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    return;
                }
                else if (arg == "uninstall" || arg == "remove")
                {
                    bool ok = TaskManager.UninstallAutoStart();
                    MessageBox.Show(
                        ok ? "EchoRAM auto-start has been removed from Windows."
                           : "Failed to remove Task Scheduler entry.",
                        "EchoRAM Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }

        public static bool IsRunningAsAdmin()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }

    public static class TaskManager
    {
        public const string TaskName = "EchoRAM";

        public static bool IsAutoStartInstalled()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/Query /TN \"" + TaskName + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        public static bool InstallAutoStart()
        {
            try
            {
                string dir = Path.GetDirectoryName(Application.ExecutablePath);
                string exePath = Path.Combine(dir, "EchoRAM.exe");
                if (!File.Exists(exePath))
                    return false;

                UninstallAutoStart();

                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = string.Format("/Create /TN \"{0}\" /TR \"\\\"{1}\\\"\" /SC ONLOGON /RL HIGHEST /F /DELAY 0000:30",
                        TaskName, exePath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        public static bool UninstallAutoStart()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = string.Format("/Delete /TN \"{0}\" /F", TaskName),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }
    }

    class SetupForm : Form
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private Label lblStatusBadge;
        private Label lblExePath;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnLaunch;

        public SetupForm()
        {
            this.Text = "EchoRAM - Setup";
            this.Size = new Size(580, 440);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(14, 18, 25);
            this.ForeColor = Color.FromArgb(245, 248, 255);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            bool iconLoaded = false;
            string icoPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ram_icon.ico");
            if (File.Exists(icoPath))
            {
                try { this.Icon = new Icon(icoPath); iconLoaded = true; } catch { }
            }
            if (!iconLoaded)
            {
                try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            }

            InitUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
                DwmSetWindowAttribute(this.Handle, 19, ref darkMode, sizeof(int));
            }
            catch { }
        }

        private void InitUI()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(18, 23, 32)
            };
            header.Paint += (s, e) =>
            {
                using (var p = new Pen(Color.FromArgb(40, 50, 70), 1f))
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "EchoRAM - Setup",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 248, 255),
                Location = new Point(20, 14),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Windows Logon Auto-Start Configuration",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 165, 190),
                Location = new Point(22, 42),
                AutoSize = true
            };
            header.Controls.Add(lblSub);
            this.Controls.Add(header);

            var card = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(524, 140),
                BackColor = Color.FromArgb(24, 30, 42)
            };
            card.Paint += (s, e) =>
            {
                using (GraphicsPath path = CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10))
                {
                    using (var b = new SolidBrush(Color.FromArgb(24, 30, 42)))
                        e.Graphics.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(40, 50, 70), 1f))
                        e.Graphics.DrawPath(p, path);
                }
            };

            var lblCardH = new Label
            {
                Text = "AUTO-START STATUS",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(95, 110, 135),
                Location = new Point(16, 12),
                AutoSize = true
            };
            card.Controls.Add(lblCardH);

            lblStatusBadge = new Label
            {
                Location = new Point(16, 38),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                AutoSize = true
            };
            card.Controls.Add(lblStatusBadge);

            string exePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "EchoRAM.exe");
            lblExePath = new Label
            {
                Text = "Target: " + exePath,
                Location = new Point(16, 75),
                Size = new Size(490, 36),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 165, 190)
            };
            card.Controls.Add(lblExePath);

            this.Controls.Add(card);

            btnInstall = new Button
            {
                Text = "Enable Auto-Start on Windows Boot",
                Location = new Point(20, 246),
                Size = new Size(524, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 210, 255),
                ForeColor = Color.FromArgb(10, 20, 35),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += (s, e) =>
            {
                bool ok = TaskManager.InstallAutoStart();
                MessageBox.Show(
                    ok ? "EchoRAM will now start automatically when you log on to Windows."
                       : "Failed to configure Task Scheduler auto-start.",
                    "Setup", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                RefreshStatus();
            };
            this.Controls.Add(btnInstall);

            btnUninstall = new Button
            {
                Text = "Remove Auto-Start",
                Location = new Point(20, 298),
                Size = new Size(256, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(32, 40, 56),
                ForeColor = Color.FromArgb(245, 248, 255),
                Cursor = Cursors.Hand
            };
            btnUninstall.FlatAppearance.BorderColor = Color.FromArgb(40, 50, 70);
            btnUninstall.Click += (s, e) =>
            {
                bool ok = TaskManager.UninstallAutoStart();
                MessageBox.Show(
                    ok ? "Auto-start has been removed from Windows."
                       : "Could not remove auto-start.",
                    "Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshStatus();
            };
            this.Controls.Add(btnUninstall);

            btnLaunch = new Button
            {
                Text = "Launch EchoRAM",
                Location = new Point(288, 298),
                Size = new Size(256, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 213, 115),
                ForeColor = Color.FromArgb(10, 20, 35),
                Cursor = Cursors.Hand
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += (s, e) =>
            {
                string target = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "EchoRAM.exe");
                if (File.Exists(target))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = target,
                            UseShellExecute = true
                        });
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not launch application: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("EchoRAM.exe was not found in the same folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnLaunch);

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            bool isInstalled = TaskManager.IsAutoStartInstalled();
            if (isInstalled)
            {
                lblStatusBadge.Text = "Active (Starts 30s after logon with admin privileges)";
                lblStatusBadge.ForeColor = Color.FromArgb(46, 213, 115);
                btnInstall.Text = "Auto-Start Active (Click to Re-apply)";
            }
            else
            {
                lblStatusBadge.Text = "Not Installed (Manual launch only)";
                lblStatusBadge.ForeColor = Color.FromArgb(255, 171, 0);
                btnInstall.Text = "Enable Auto-Start on Windows Boot (Admin)";
            }
        }

        private static GraphicsPath CreateRoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
