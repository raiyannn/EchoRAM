using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EchoRAM
{
    static class NativeMethods
    {
        public const int SystemMemoryListInformation = 80;
        public const int MemoryCaptureAccessedBits = 0;
        public const int MemoryCaptureAndResetAccessedBits = 1;
        public const int MemoryEmptyWorkingSets = 2;
        public const int MemoryFlushModifiedList = 3;
        public const int MemoryPurgeStandbyList = 4;
        public const int MemoryPurgeLowPriorityStandbyList = 5;

        [DllImport("ntdll.dll")]
        public static extern int NtSetSystemInformation(int infoClass, ref int info, int length);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetSystemFileCacheSize(IntPtr MinimumFileCacheSize, IntPtr MaximumFileCacheSize, int Flags);

        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        public static int PurgeWorkingSets()
        {
            int cmd = MemoryEmptyWorkingSets;
            return NtSetSystemInformation(SystemMemoryListInformation, ref cmd, sizeof(int));
        }

        public static bool PurgeSystemWorkingSet()
        {
            return SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0);
        }

        public static int PurgeModifiedPages()
        {
            int cmd = MemoryFlushModifiedList;
            return NtSetSystemInformation(SystemMemoryListInformation, ref cmd, sizeof(int));
        }

        public static int PurgeStandbyList()
        {
            int cmd = MemoryPurgeStandbyList;
            return NtSetSystemInformation(SystemMemoryListInformation, ref cmd, sizeof(int));
        }

        public static int PurgeLowPriorityStandbyList()
        {
            int cmd = MemoryPurgeLowPriorityStandbyList;
            return NtSetSystemInformation(SystemMemoryListInformation, ref cmd, sizeof(int));
        }

        public static bool IsProtectedProcess(string processName, IEnumerable<string> gameNames)
        {
            if (string.IsNullOrEmpty(processName)) return true;

            if (string.Equals(processName, "EchoRAM", StringComparison.OrdinalIgnoreCase)) return true;

            // Keep DWM and audio service untrimmed while gaming to prevent frame hitching/audio pops
            if (string.Equals(processName, "dwm", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(processName, "audiodg", StringComparison.OrdinalIgnoreCase)) return true;

            // Skip common anti-cheat processes
            if (processName.IndexOf("anticheat", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (processName.IndexOf("ace_", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (processName.IndexOf("easyanticheat", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (string.Equals(processName, "ACE-Base", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(processName, "AntiCheatExpert", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(processName, "SGuard64", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(processName, "SGuard32", StringComparison.OrdinalIgnoreCase)) return true;

            if (gameNames != null)
            {
                foreach (var g in gameNames)
                {
                    if (string.IsNullOrEmpty(g)) continue;
                    string clean = g.Replace(".exe", "").Trim();
                    if (string.Equals(processName, clean, StringComparison.OrdinalIgnoreCase)) return true;
                    if (clean.IndexOf(processName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        processName.IndexOf(clean, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }

            return false;
        }

        public static int TrimBackgroundProcesses(IEnumerable<string> gameNames)
        {
            int count = 0;
            Process[] procs = Process.GetProcesses();
            foreach (Process p in procs)
            {
                try
                {
                    if (p.Id <= 4) continue;
                    if (IsProtectedProcess(p.ProcessName, gameNames)) continue;

                    IntPtr hProc = OpenProcess(0x0100 | 0x0400, false, p.Id);
                    if (hProc != IntPtr.Zero)
                    {
                        if (EmptyWorkingSet(hProc) != 0) count++;
                        CloseHandle(hProc);
                    }
                }
                catch { }
            }
            return count;
        }

        public static int TrimProcessWorkingSets(string excludeProcessName)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(excludeProcessName)) list.Add(excludeProcessName);
            return TrimBackgroundProcesses(list);
        }

        public static bool TrimSingleProcess(int pid)
        {
            try
            {
                IntPtr hProc = OpenProcess(0x0100 | 0x0400, false, pid);
                if (hProc != IntPtr.Zero)
                {
                    bool ok = EmptyWorkingSet(hProc) != 0;
                    CloseHandle(hProc);
                    return ok;
                }
            }
            catch { }
            return false;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        const uint TOKEN_QUERY = 0x0008;
        const int SE_PRIVILEGE_ENABLED = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID Luid;
            public int Attributes;
        }

        public static bool EnablePrivileges()
        {
            bool ok = true;
            ok &= EnablePrivilege("SeProfileSingleProcessPrivilege");
            ok &= EnablePrivilege("SeIncreaseQuotaPrivilege");
            ok &= EnablePrivilege("SeDebugPrivilege");
            return ok;
        }

        private static bool EnablePrivilege(string privilegeName)
        {
            IntPtr token;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
                return false;

            try
            {
                var tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Attributes = SE_PRIVILEGE_ENABLED;
                if (!LookupPrivilegeValue(null, privilegeName, out tp.Luid))
                    return false;

                bool res = AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                return res && Marshal.GetLastWin32Error() == 0;
            }
            finally
            {
                CloseHandle(token);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public static MEMORYSTATUSEX GetMemoryInfo()
        {
            var mem = new MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref mem);
            return mem;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetPerformanceInfo(out PERFORMANCE_INFORMATION pPerformanceInformation, int cb);

        [StructLayout(LayoutKind.Sequential)]
        public struct PERFORMANCE_INFORMATION
        {
            public int cb;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonpaged;
            public IntPtr PageSize;
            public int HandleCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public struct MemoryComposition
        {
            public double TotalGB;
            public double InUseGB;
            public double StandbyGB;
            public double FreeGB;
            public double ModifiedGB;
            public double RamPercent;
            public int ProcessCount;
            public int ThreadCount;
        }

        public static MemoryComposition GetMemoryComposition()
        {
            var mc = new MemoryComposition();
            var pi = new PERFORMANCE_INFORMATION();
            pi.cb = Marshal.SizeOf(typeof(PERFORMANCE_INFORMATION));
            if (GetPerformanceInfo(out pi, pi.cb))
            {
                long pageSize = pi.PageSize.ToInt64();
                mc.TotalGB = (pi.PhysicalTotal.ToInt64() * pageSize) / (1024.0 * 1024 * 1024);
                double availGB = (pi.PhysicalAvailable.ToInt64() * pageSize) / (1024.0 * 1024 * 1024);
                mc.StandbyGB = (pi.SystemCache.ToInt64() * pageSize) / (1024.0 * 1024 * 1024);
                mc.InUseGB = Math.Max(0.0, mc.TotalGB - availGB);
                mc.FreeGB = Math.Max(0.0, availGB - mc.StandbyGB);
                mc.ModifiedGB = Math.Max(0.0, mc.InUseGB * 0.04);
                mc.RamPercent = mc.TotalGB > 0 ? (mc.InUseGB / mc.TotalGB) * 100.0 : 0.0;
                mc.ProcessCount = pi.ProcessCount;
                mc.ThreadCount = pi.ThreadCount;
            }
            else
            {
                var mem = GetMemoryInfo();
                mc.TotalGB = mem.ullTotalPhys / (1024.0 * 1024 * 1024);
                double avail = mem.ullAvailPhys / (1024.0 * 1024 * 1024);
                mc.InUseGB = Math.Max(0.0, mc.TotalGB - avail);
                mc.StandbyGB = avail * 0.4;
                mc.FreeGB = avail * 0.6;
                mc.RamPercent = mem.dwMemoryLoad;
            }
            return mc;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static TimeSpan GetIdleTime()
        {
            var lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (GetLastInputInfo(ref lii))
            {
                uint elapsed = (uint)Environment.TickCount - lii.dwTime;
                return TimeSpan.FromMilliseconds(elapsed);
            }
            return TimeSpan.Zero;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
                                                 out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
                                                 out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;
        public const int WM_HOTKEY = 0x0312;
    }
}
