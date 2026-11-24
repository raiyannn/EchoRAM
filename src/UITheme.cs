using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace EchoRAM
{
    public static class UITheme
    {
        public static bool IsLightMode { get; private set; }

        public static Color BgDark = Color.FromArgb(14, 18, 25);
        public static Color BgSidebar = Color.FromArgb(18, 23, 32);
        public static Color BgCard = Color.FromArgb(24, 30, 42);
        public static Color BgCardHover = Color.FromArgb(32, 40, 56);
        public static Color Border = Color.FromArgb(40, 50, 70);
        public static Color BorderSubtle = Color.FromArgb(30, 38, 54);

        public static Color TextPrimary = Color.FromArgb(245, 248, 255);
        public static Color TextSecondary = Color.FromArgb(150, 165, 190);
        public static Color TextMuted = Color.FromArgb(95, 110, 135);

        public static Color AccentCyan = Color.FromArgb(0, 210, 255);
        public static Color AccentTeal = Color.FromArgb(0, 245, 212);
        public static Color AccentGreen = Color.FromArgb(46, 213, 115);
        public static Color AccentAmber = Color.FromArgb(255, 171, 0);
        public static Color AccentRed = Color.FromArgb(255, 71, 87);
        public static Color AccentPurple = Color.FromArgb(165, 94, 234);

        public static Color MemInUse = Color.FromArgb(41, 128, 185);
        public static Color MemModified = Color.FromArgb(241, 196, 15);
        public static Color MemStandby = Color.FromArgb(142, 68, 173);
        public static Color MemFree = Color.FromArgb(39, 174, 96);

        public static void ApplyTheme(string mode)
        {
            bool light = false;
            if (string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase))
                light = true;
            else if (string.Equals(mode, "System", StringComparison.OrdinalIgnoreCase))
                light = IsWindowsLightTheme();

            IsLightMode = light;

            if (light)
            {
                BgDark = Color.FromArgb(243, 245, 249);
                BgSidebar = Color.FromArgb(234, 238, 245);
                BgCard = Color.FromArgb(255, 255, 255);
                BgCardHover = Color.FromArgb(224, 231, 242);
                Border = Color.FromArgb(210, 218, 230);
                BorderSubtle = Color.FromArgb(226, 232, 240);

                TextPrimary = Color.FromArgb(20, 28, 40);
                TextSecondary = Color.FromArgb(85, 98, 118);
                TextMuted = Color.FromArgb(125, 138, 158);

                AccentCyan = Color.FromArgb(0, 145, 215);
                AccentGreen = Color.FromArgb(30, 175, 90);
                AccentAmber = Color.FromArgb(215, 135, 0);
                AccentRed = Color.FromArgb(220, 50, 65);
                AccentPurple = Color.FromArgb(130, 65, 205);
            }
            else
            {
                BgDark = Color.FromArgb(14, 18, 25);
                BgSidebar = Color.FromArgb(18, 23, 32);
                BgCard = Color.FromArgb(24, 30, 42);
                BgCardHover = Color.FromArgb(32, 40, 56);
                Border = Color.FromArgb(40, 50, 70);
                BorderSubtle = Color.FromArgb(30, 38, 54);

                TextPrimary = Color.FromArgb(245, 248, 255);
                TextSecondary = Color.FromArgb(150, 165, 190);
                TextMuted = Color.FromArgb(95, 110, 135);

                AccentCyan = Color.FromArgb(0, 210, 255);
                AccentGreen = Color.FromArgb(46, 213, 115);
                AccentAmber = Color.FromArgb(255, 171, 0);
                AccentRed = Color.FromArgb(255, 71, 87);
                AccentPurple = Color.FromArgb(165, 94, 234);
            }
        }

        private static bool IsWindowsLightTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AppsUseLightTheme");
                        if (val != null)
                            return Convert.ToInt32(val) == 1;
                    }
                }
            }
            catch { }
            return false;
        }

        public static GraphicsPath CreateRoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { path.AddRectangle(r); return path; }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawCard(Graphics g, Rectangle r, int radius, Color fillColor, Color borderColor)
        {
            using (GraphicsPath path = CreateRoundedPath(r, radius))
            {
                using (var b = new SolidBrush(fillColor))
                    g.FillPath(b, path);
                if (borderColor != Color.Transparent)
                {
                    using (var p = new Pen(borderColor, 1f))
                        g.DrawPath(p, path);
                }
            }
        }
    }
}
