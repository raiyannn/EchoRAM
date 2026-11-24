using System;
using System.Diagnostics;
using System.IO;

namespace EchoRAMBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "EchoRAM - Build Tool";
            Console.WriteLine("EchoRAM Build Tool");
            Console.WriteLine("------------------");

            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string csc = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe";
            if (!File.Exists(csc))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] .NET Framework csc.exe not found at " + csc);
                Console.ResetColor();
                PauseIfInteractive(args);
                return;
            }

            string ico = Path.Combine(dir, "ram_icon.ico");
            string icoArg = File.Exists(ico) ? " /win32icon:\"" + ico + "\"" : "";

            string srcDir = Path.Combine(dir, "src");
            string[] appSources = new string[]
            {
                Path.Combine(srcDir, "Program.cs"),
                Path.Combine(srcDir, "NativeMethods.cs"),
                Path.Combine(srcDir, "AppSettings.cs"),
                Path.Combine(srcDir, "Monitors.cs"),
                Path.Combine(srcDir, "UITheme.cs"),
                Path.Combine(srcDir, "Updater.cs"),
                Path.Combine(srcDir, "TrayAppContext.cs"),
                Path.Combine(srcDir, "DashboardForm.cs"),
                Path.Combine(srcDir, "DashboardForm.Overview.cs"),
                Path.Combine(srcDir, "DashboardForm.Processes.cs"),
                Path.Combine(srcDir, "DashboardForm.Gaming.cs"),
                Path.Combine(srcDir, "DashboardForm.RamMap.cs"),
                Path.Combine(srcDir, "DashboardForm.Settings.cs")
            };

            Console.WriteLine("[1/2] Compiling EchoRAM.exe...");
            string outApp = Path.Combine(dir, "EchoRAM.exe");

            bool wasRunning = StopRunningInstances();

            string quotedSources = "";
            foreach (string s in appSources)
                quotedSources += " \"" + s + "\"";

            string argsApp = string.Format(
                "/target:winexe /out:\"{0}\" /optimize+ /platform:x64{1} /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll{2}",
                outApp, icoArg, quotedSources);

            int exit1 = RunCompiler(csc, argsApp);
            if (exit1 == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] EchoRAM.exe compiled");
                Console.ResetColor();

                if (wasRunning)
                    RelaunchApp(outApp);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Failed to compile EchoRAM.exe");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("[2/2] Compiling Setup.exe...");
            string srcSetup = Path.Combine(srcDir, Path.Combine("Setup", "SetupProgram.cs"));
            string outSetup = Path.Combine(dir, "Setup.exe");
            string argsSetup = string.Format(
                "/target:winexe /out:\"{0}\" /optimize+ /platform:x64{1} /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll \"{2}\"",
                outSetup, icoArg, srcSetup);

            int exit2 = RunCompiler(csc, argsSetup);
            if (exit2 == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] Setup.exe compiled");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Failed to compile Setup.exe");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("Build finished.");
            PauseIfInteractive(args);
        }

        private static bool StopRunningInstances()
        {
            bool wasRunning = false;
            try
            {
                var procs = Process.GetProcessesByName("EchoRAM");
                if (procs != null && procs.Length > 0)
                {
                    wasRunning = true;
                    foreach (var p in procs)
                    {
                        try { p.Kill(); p.WaitForExit(3000); } catch { }
                    }
                    System.Threading.Thread.Sleep(400);
                }
            }
            catch { }
            return wasRunning;
        }

        private static void RelaunchApp(string exePath)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/Run /TN \"\\EchoRAM\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                p.WaitForExit(2000);
                if (p.ExitCode != 0)
                    Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
            }
            catch
            {
                try { Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true }); } catch { }
            }
        }

        private static void PauseIfInteractive(string[] args)
        {
            bool noPause = (args != null && args.Length > 0 && Array.Exists(args, a => a.Equals("/nopause", StringComparison.OrdinalIgnoreCase))) || Console.IsInputRedirected;
            if (!noPause)
            {
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }

        static int RunCompiler(string csc, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = csc,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (Process p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Console.WriteLine(output);
                    Console.WriteLine(error);
                }
                return p.ExitCode;
            }
        }
    }
}
