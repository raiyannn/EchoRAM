using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EchoRAM
{
    partial class DashboardForm
    {
        private void InitOverviewView()
        {
            viewOverview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.BgDark
            };

            cardGauge = new Panel { BackColor = UITheme.BgCard };
            cardGauge.Paint += CardGauge_Paint;
            viewOverview.Controls.Add(cardGauge);

            cardActions = new Panel { BackColor = UITheme.BgCard };
            cardActions.Paint += (s, e) =>
            {
                UITheme.DrawCard(e.Graphics, new Rectangle(0, 0, cardActions.Width - 1, cardActions.Height - 1), 12, UITheme.BgCard, UITheme.Border);
            };

            var lblActHeader = new Label
            {
                Text = "MEMORY CLEANUP",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Location = new Point(18, 14),
                AutoSize = true
            };
            cardActions.Controls.Add(lblActHeader);

            btnHeroClean = new Button
            {
                Text = "OPTIMIZE RAM NOW",
                Location = new Point(18, 40),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                BackColor = UITheme.AccentCyan,
                ForeColor = Color.FromArgb(10, 20, 35),
                Cursor = Cursors.Hand
            };
            btnHeroClean.FlatAppearance.BorderSize = 0;
            btnHeroClean.Click += (s, e) =>
            {
                btnHeroClean.Text = "OPTIMIZING...";
                btnHeroClean.Enabled = false;
                context.RunFlush(context.IsGameActive ? TrayAppContext.FlushLevel.GameModeBoost : TrayAppContext.FlushLevel.Full, "UI Button");
            };
            cardActions.Controls.Add(btnHeroClean);

            btnStandbyClean = new Button
            {
                Text = "Purge Standby Cache",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnStandbyClean.FlatAppearance.BorderColor = UITheme.Border;
            btnStandbyClean.Click += (s, e) => context.RunFlush(TrayAppContext.FlushLevel.StandbyOnly, "UI Standby");
            cardActions.Controls.Add(btnStandbyClean);

            btnWorkingSetClean = new Button
            {
                Text = "Empty Working Sets",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnWorkingSetClean.FlatAppearance.BorderColor = UITheme.Border;
            btnWorkingSetClean.Click += (s, e) => context.RunFlush(TrayAppContext.FlushLevel.WorkingSets, "UI Working Sets");
            cardActions.Controls.Add(btnWorkingSetClean);

            lblStatusMessage = new Label
            {
                Text = "Ready - all 5 kernel memory list commands available",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = UITheme.TextSecondary,
                AutoSize = true
            };
            cardActions.Controls.Add(lblStatusMessage);

            var hotkeyPill = new Panel
            {
                BackColor = Color.FromArgb(16, 20, 30)
            };
            var lblHk = new Label
            {
                Text = "Global Hotkey: " + context.Settings.Hotkey + "  (Press anytime to flush)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = UITheme.AccentCyan,
                Location = new Point(12, 10),
                AutoSize = true
            };
            hotkeyPill.Controls.Add(lblHk);
            cardActions.Controls.Add(hotkeyPill);

            cardActions.Resize += (s, e) =>
            {
                int w = cardActions.Width - 36;
                btnHeroClean.Size = new Size(w, 48);

                int subW = (w - 10) / 2;
                btnStandbyClean.Location = new Point(18, 98);
                btnStandbyClean.Size = new Size(subW, 36);

                btnWorkingSetClean.Location = new Point(18 + subW + 10, 98);
                btnWorkingSetClean.Size = new Size(subW, 36);

                lblStatusMessage.Location = new Point(18, 148);

                hotkeyPill.Location = new Point(18, 185);
                hotkeyPill.Size = new Size(w, 36);
            };

            viewOverview.Controls.Add(cardActions);

            cardBreakdown = new Panel { BackColor = UITheme.BgCard };
            cardBreakdown.Paint += CardBreakdown_Paint;
            viewOverview.Controls.Add(cardBreakdown);

            panelContentArea.Controls.Add(viewOverview);
            LayoutOverviewCards();
        }

        private void LayoutOverviewCards()
        {
            if (viewOverview == null || cardGauge == null || cardActions == null || cardBreakdown == null) return;

            int pad = 16;
            int totalW = viewOverview.ClientSize.Width;
            int totalH = viewOverview.ClientSize.Height;
            if (totalW < 300 || totalH < 300) return;

            int topH = 265;
            int gaugeW = (int)((totalW - (pad * 3)) * 0.40);
            if (gaugeW < 260) gaugeW = 260;
            int actionsW = totalW - gaugeW - (pad * 3);

            cardGauge.Location = new Point(pad, pad);
            cardGauge.Size = new Size(gaugeW, topH);

            cardActions.Location = new Point(cardGauge.Right + pad, pad);
            cardActions.Size = new Size(actionsW, topH);

            int breakdownY = cardGauge.Bottom + pad;
            int breakdownH = Math.Max(240, totalH - breakdownY - pad);

            cardBreakdown.Location = new Point(pad, breakdownY);
            cardBreakdown.Size = new Size(totalW - (pad * 2), breakdownH);

            cardGauge.Invalidate();
            cardActions.Invalidate();
            cardBreakdown.Invalidate();
        }

        private void CardGauge_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var cardRect = new Rectangle(0, 0, cardGauge.Width - 1, cardGauge.Height - 1);
            UITheme.DrawCard(g, cardRect, 14, UITheme.BgCard, UITheme.Border);

            var mem = NativeMethods.GetMemoryInfo();
            float pct = (float)mem.dwMemoryLoad;
            double usedGB = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024 * 1024);
            double totalGB = mem.ullTotalPhys / (1024.0 * 1024 * 1024);

            int cx = cardGauge.Width / 2;
            int cy = (cardGauge.Height / 2) - 12;
            int radius = Math.Min(cardGauge.Width, cardGauge.Height) / 2 - 32;
            if (radius < 35) radius = 35;

            var arcRect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);

            using (var penTrack = new Pen(Color.FromArgb(32, 40, 56), 15f))
            {
                penTrack.StartCap = LineCap.Round;
                penTrack.EndCap = LineCap.Round;
                g.DrawArc(penTrack, arcRect, 135, 270);
            }

            Color arcColor = (pct >= 80) ? UITheme.AccentRed : ((pct >= 65) ? UITheme.AccentAmber : UITheme.AccentCyan);
            if (context.IsGameActive) arcColor = UITheme.AccentPurple;

            using (var penArc = new Pen(arcColor, 15f))
            {
                penArc.StartCap = LineCap.Round;
                penArc.EndCap = LineCap.Round;
                float sweep = (pct / 100.0f) * 270.0f;
                if (sweep > 1.0f)
                    g.DrawArc(penArc, arcRect, 135, sweep);
            }

            string pctText = string.Format("{0}%", (int)pct);
            using (var fBig = new Font("Segoe UI", 30f, FontStyle.Bold))
            using (var bWhite = new SolidBrush(UITheme.TextPrimary))
            {
                SizeF sz = g.MeasureString(pctText, fBig);
                g.DrawString(pctText, fBig, bWhite, cx - (sz.Width / 2), cy - (sz.Height / 2) - 12);
            }

            string subText = string.Format("{0:F1} / {1:F1} GB", usedGB, totalGB);
            using (var fSub = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var bDim = new SolidBrush(UITheme.TextSecondary))
            {
                SizeF sz = g.MeasureString(subText, fSub);
                g.DrawString(subText, fSub, bDim, cx - (sz.Width / 2), cy + 26);
            }

            string statusStr;
            Color statusC;
            if (context.IsGameActive)
            {
                statusStr = string.Format("Game Shield Active [{0}]", context.ActiveGameName);
                statusC = UITheme.AccentPurple;
            }
            else if (pct >= 80)
            {
                statusStr = "High Memory Pressure";
                statusC = UITheme.AccentRed;
            }
            else
            {
                statusStr = "Memory Healthy";
                statusC = UITheme.AccentGreen;
            }
            using (var fSt = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var bSt = new SolidBrush(statusC))
            {
                SizeF sz = g.MeasureString(statusStr, fSt);
                g.DrawString(statusStr, fSt, bSt, cx - (sz.Width / 2), cardGauge.Height - 30);
            }
        }

        private void CardBreakdown_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var cardRect = new Rectangle(0, 0, cardBreakdown.Width - 1, cardBreakdown.Height - 1);
            UITheme.DrawCard(g, cardRect, 14, UITheme.BgCard, UITheme.Border);

            var mc = NativeMethods.GetMemoryComposition();

            using (var fBold = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var bText = new SolidBrush(UITheme.TextPrimary))
                g.DrawString("PHYSICAL RAM COMPOSITION", fBold, bText, 18, 16);

            int barX = 18;
            int barY = 46;
            int barW = cardBreakdown.Width - 36;
            int barH = 28;
            if (barW < 100) return;

            double tot = mc.TotalGB > 0 ? mc.TotalGB : 16.0;
            int wInUse = (int)((mc.InUseGB / tot) * barW);
            int wStandby = (int)((mc.StandbyGB / tot) * barW);
            int wFree = Math.Max(0, barW - (wInUse + wStandby));

            int curX = barX;
            using (var bInUse = new SolidBrush(UITheme.MemInUse))
                g.FillRectangle(bInUse, curX, barY, wInUse, barH);
            curX += wInUse;

            using (var bStandby = new SolidBrush(UITheme.MemStandby))
                g.FillRectangle(bStandby, curX, barY, wStandby, barH);
            curX += wStandby;

            using (var bFree = new SolidBrush(UITheme.MemFree))
                g.FillRectangle(bFree, curX, barY, wFree, barH);

            using (var pBar = new Pen(UITheme.Border, 1.5f))
                g.DrawRectangle(pBar, barX, barY, barW, barH);

            int legY = 88;
            int colW = barW / 3;
            DrawLegendItem(g, barX + 5, legY, UITheme.MemInUse, string.Format("Active (In-Use): {0:F1} GB", mc.InUseGB));
            DrawLegendItem(g, barX + colW + 5, legY, UITheme.MemStandby, string.Format("Standby Cache: {0:F1} GB", mc.StandbyGB));
            DrawLegendItem(g, barX + (colW * 2) + 5, legY, UITheme.MemFree, string.Format("Free Available: {0:F1} GB", mc.FreeGB));

            int tileY = 126;
            int tileH = cardBreakdown.Height - tileY - 18;
            if (tileH < 50) tileH = 50;

            int tileW = (barW - 20) / 3;

            var tile1 = new Rectangle(barX, tileY, tileW, tileH);
            var tile2 = new Rectangle(barX + tileW + 10, tileY, tileW, tileH);
            var tile3 = new Rectangle(barX + (tileW * 2) + 20, tileY, tileW, tileH);

            UITheme.DrawCard(g, tile1, 10, Color.FromArgb(18, 22, 32), UITheme.BorderSubtle);
            UITheme.DrawCard(g, tile2, 10, Color.FromArgb(18, 22, 32), UITheme.BorderSubtle);
            UITheme.DrawCard(g, tile3, 10, Color.FromArgb(18, 22, 32), UITheme.BorderSubtle);

            double cpu = cpuMon.GetCpuUsage();
            TimeSpan idle = NativeMethods.GetIdleTime();

            using (var fSm = new Font("Segoe UI", 8.5f))
            using (var fV = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var bDim = new SolidBrush(UITheme.TextSecondary))
            using (var bW = new SolidBrush(UITheme.TextPrimary))
            {
                g.DrawString("CPU LOAD", fSm, bDim, tile1.X + 14, tile1.Y + 10);
                Color cpuC = (cpu >= 85) ? UITheme.AccentRed : UITheme.AccentGreen;
                using (var bC = new SolidBrush(cpuC))
                    g.DrawString(string.Format("{0:F0}% ({1})", cpu, cpu >= 85 ? "Heavy" : "Normal"), fV, bC, tile1.X + 14, tile1.Y + 30);

                g.DrawString("USER ACTIVITY", fSm, bDim, tile2.X + 14, tile2.Y + 10);
                string idleStr = idle.TotalMinutes < 1 ? "Active" : string.Format("Idle ({0:F0}m)", idle.TotalMinutes);
                g.DrawString(idleStr, fV, bW, tile2.X + 14, tile2.Y + 30);

                g.DrawString("SYSTEM LOAD", fSm, bDim, tile3.X + 14, tile3.Y + 10);
                g.DrawString(string.Format("{0} procs / {1} threads", mc.ProcessCount, mc.ThreadCount), fV, bW, tile3.X + 14, tile3.Y + 30);
            }
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color c, string label)
        {
            using (var bDot = new SolidBrush(c))
                g.FillEllipse(bDot, x, y + 2, 12, 12);
            using (var f = new Font("Segoe UI", 9.5f))
            using (var bText = new SolidBrush(UITheme.TextSecondary))
                g.DrawString(label, f, bText, x + 18, y);
        }
    }
}
