using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EchoRAM
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
                if (arg == "install" || arg == "autostart")
                {
                    bool ok = AutoStartManager.EnableAutoStart();
                    MessageBox.Show(
                        ok ? "EchoRAM has been configured to start automatically on Windows logon."
                           : "Failed to configure Windows Task Scheduler auto-start.",
                        "EchoRAM", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
                else if (arg == "uninstall" || arg == "remove")
                {
                    bool ok = AutoStartManager.DisableAutoStart();
                    MessageBox.Show(
                        ok ? "EchoRAM auto-start has been removed from Windows logon."
                           : "Failed to remove auto-start from Task Scheduler.",
                        "EchoRAM", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            bool createdNew;
            using (var mutex = new Mutex(true, "Global\\EchoRAMSingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    IntPtr existingHwnd = NativeMethods.FindWindow(null, "EchoRAM");
                    if (existingHwnd != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindow(existingHwnd, 9); // SW_RESTORE
                        NativeMethods.SetForegroundWindow(existingHwnd);
                    }
                    else
                    {
                        MessageBox.Show("EchoRAM is already running in your system tray.",
                            "EchoRAM", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayAppContext());
            }
        }

        private static bool IsRunningAsAdmin()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }

    public static class AutoStartManager
    {
        private const string TaskName = "EchoRAM";

        public static bool IsAutoStartEnabled()
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

        public static bool EnableAutoStart()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                DisableAutoStart();

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
                    if (p.ExitCode == 0)
                    {
                        CreateStartMenuShortcut();
                        return true;
                    }
                    return false;
                }
            }
            catch { return false; }
        }

        public static bool DisableAutoStart()
        {
            RemoveStartMenuShortcut();
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

        private static string GetShortcutPath()
        {
            string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            return Path.Combine(programs, "EchoRAM.lnk");
        }

        public static void CreateStartMenuShortcut()
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                object shell = Activator.CreateInstance(shellType);
                string lnkPath = GetShortcutPath();
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                if (shortcut != null)
                {
                    Type scType = shortcut.GetType();
                    string exePath = Application.ExecutablePath;
                    scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { exePath });
                    scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(exePath) });
                    scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { exePath + ",0" });
                    scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "EchoRAM - Windows Memory Optimizer" });
                    scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                }
            }
            catch { }
        }

        public static void RemoveStartMenuShortcut()
        {
            try
            {
                string lnkPath = GetShortcutPath();
                if (File.Exists(lnkPath))
                    File.Delete(lnkPath);
            }
            catch { }
        }
    }
}
