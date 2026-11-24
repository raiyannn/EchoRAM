using System;
using System.Windows.Forms;

namespace EchoRAM
{
    class CpuMonitor
    {
        private ulong prevSysTime = 0;
        private ulong prevIdleTime = 0;

        public double GetCpuUsage()
        {
            System.Runtime.InteropServices.ComTypes.FILETIME idleTime, kernelTime, userTime;
            if (!NativeMethods.GetSystemTimes(out idleTime, out kernelTime, out userTime))
                return 0.0;

            ulong sys = FileTimeToUlong(kernelTime) + FileTimeToUlong(userTime);
            ulong idle = FileTimeToUlong(idleTime);

            if (prevSysTime == 0)
            {
                prevSysTime = sys;
                prevIdleTime = idle;
                return 0.0;
            }

            ulong sysDiff = sys - prevSysTime;
            ulong idleDiff = idle - prevIdleTime;

            prevSysTime = sys;
            prevIdleTime = idle;

            if (sysDiff == 0) return 0.0;
            double usage = (1.0 - (double)idleDiff / sysDiff) * 100.0;
            if (usage < 0.0) usage = 0.0;
            if (usage > 100.0) usage = 100.0;
            return usage;
        }

        private static ulong FileTimeToUlong(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        }
    }

    class HotKeyMessageWindow : Form
    {
        public event Action HotKeyPressed;
        private const int HOTKEY_ID = 9001;

        public HotKeyMessageWindow()
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0;
            this.Width = 0;
            this.Height = 0;
        }

        public bool Register(uint modifiers, Keys key)
        {
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID);
            return NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID, modifiers | NativeMethods.MOD_NOREPEAT, (uint)key);
        }

        public void Unregister()
        {
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                if (HotKeyPressed != null) HotKeyPressed();
            }
            base.WndProc(ref m);
        }
    }
}
