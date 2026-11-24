using System;
using System.Drawing;
using System.Windows.Forms;

namespace EchoRAM
{
    partial class DashboardForm
    {
        private void InitRamMapView()
        {
            viewRamMap = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.BgDark, Padding = new Padding(20) };

            var lblRmHeader = new Label
            {
                Text = "KERNEL MEMORY LIST TOOLS",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true
            };
            viewRamMap.Controls.Add(lblRmHeader);

            int startY = 56;
            CreateToolCard(viewRamMap, startY, "Empty Working Sets",
                "Trims the memory working set of all processes and kernel memory.",
                () => context.RunFlush(TrayAppContext.FlushLevel.WorkingSets, "ToolCard"));

            CreateToolCard(viewRamMap, startY + 76, "Empty System Working Set",
                "Purges the system-wide file cache (SetSystemFileCacheSize).",
                () => context.RunFlush(TrayAppContext.FlushLevel.SystemWorkingSet, "ToolCard"));

            CreateToolCard(viewRamMap, startY + 152, "Empty Modified Page List",
                "Flushes modified memory pages out to pagefile/disk storage.",
                () => context.RunFlush(TrayAppContext.FlushLevel.ModifiedPages, "ToolCard"));

            CreateToolCard(viewRamMap, startY + 228, "Empty Standby List",
                "Discards cached inactive pages across priorities 0-7.",
                () => context.RunFlush(TrayAppContext.FlushLevel.StandbyOnly, "ToolCard"));

            CreateToolCard(viewRamMap, startY + 304, "Empty Priority 0 Standby List",
                "Purges only the lowest-priority background standby pages.",
                () => context.RunFlush(TrayAppContext.FlushLevel.LowPriorityStandby, "ToolCard"));

            panelContentArea.Controls.Add(viewRamMap);
        }

        private void CreateToolCard(Panel parent, int top, string title, string desc, Action onExec)
        {
            var card = new Panel
            {
                Location = new Point(20, top),
                Height = 64,
                BackColor = UITheme.BgCard
            };
            card.Paint += (s, e) =>
            {
                UITheme.DrawCard(e.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10, UITheme.BgCard, UITheme.Border);
            };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Location = new Point(16, 12),
                AutoSize = true
            };
            card.Controls.Add(lblT);

            var lblD = new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextSecondary,
                Location = new Point(18, 34),
                AutoSize = true
            };
            card.Controls.Add(lblD);

            var btnExec = new Button
            {
                Text = "Flush",
                Size = new Size(100, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.AccentCyan,
                Cursor = Cursors.Hand
            };
            btnExec.FlatAppearance.BorderColor = UITheme.Border;
            btnExec.Click += (s, e) => onExec();
            card.Controls.Add(btnExec);

            card.Resize += (s, e) =>
            {
                btnExec.Location = new Point(card.Width - 116, 15);
            };

            parent.Resize += (s, e) =>
            {
                card.Width = parent.ClientSize.Width - 40;
            };

            parent.Controls.Add(card);
        }
    }
}
