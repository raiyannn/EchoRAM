using System.Drawing;
using System.Drawing.Drawing2D;

namespace EchoRAM
{
    public static class UITheme
    {
        public static readonly Color BgDark = Color.FromArgb(14, 18, 25);
        public static readonly Color BgSidebar = Color.FromArgb(18, 23, 32);
        public static readonly Color BgCard = Color.FromArgb(24, 30, 42);
        public static readonly Color BgCardHover = Color.FromArgb(32, 40, 56);
        public static readonly Color Border = Color.FromArgb(40, 50, 70);
        public static readonly Color BorderSubtle = Color.FromArgb(30, 38, 54);

        public static readonly Color TextPrimary = Color.FromArgb(245, 248, 255);
        public static readonly Color TextSecondary = Color.FromArgb(150, 165, 190);
        public static readonly Color TextMuted = Color.FromArgb(95, 110, 135);

        public static readonly Color AccentCyan = Color.FromArgb(0, 210, 255);
        public static readonly Color AccentTeal = Color.FromArgb(0, 245, 212);
        public static readonly Color AccentGreen = Color.FromArgb(46, 213, 115);
        public static readonly Color AccentAmber = Color.FromArgb(255, 171, 0);
        public static readonly Color AccentRed = Color.FromArgb(255, 71, 87);
        public static readonly Color AccentPurple = Color.FromArgb(165, 94, 234);

        public static readonly Color MemInUse = Color.FromArgb(41, 128, 185);
        public static readonly Color MemModified = Color.FromArgb(241, 196, 15);
        public static readonly Color MemStandby = Color.FromArgb(142, 68, 173);
        public static readonly Color MemFree = Color.FromArgb(39, 174, 96);

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
