using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace EchoRAM
{
    partial class DashboardForm
    {
        private void InitGamingView()
        {
            viewGaming = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.BgDark, Padding = new Padding(20) };

            var lblGmHeader = new Label
            {
                Text = "GAME SHIELD OPTIMIZER",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true
            };
            viewGaming.Controls.Add(lblGmHeader);

            var heroGame = new Panel
            {
                Location = new Point(20, 48),
                BackColor = UITheme.BgCard,
                Padding = new Padding(16)
            };
            heroGame.Paint += (s, e) =>
            {
                Color borderC = context.IsGameActive ? UITheme.AccentPurple : UITheme.Border;
                UITheme.DrawCard(e.Graphics, new Rectangle(0, 0, heroGame.Width - 1, heroGame.Height - 1), 12, UITheme.BgCard, borderC);
            };

            lblGameHeroStatus = new Label
            {
                Text = "Status: Monitoring in Background",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = UITheme.AccentGreen,
                Location = new Point(16, 14),
                AutoSize = true
            };
            heroGame.Controls.Add(lblGameHeroStatus);

            lblGameExplain = new Label
            {
                Text = "Game Shield excludes watched games (e.g. Client-Win64-Shipping) and anti-cheat services from trimming while clearing standby cache and background process working sets.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UITheme.TextSecondary,
                Location = new Point(16, 42),
                Size = new Size(650, 40)
            };
            heroGame.Controls.Add(lblGameExplain);

            btnBoostWuWa = new Button
            {
                Text = "BOOST GAME MEMORY NOW",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = UITheme.AccentPurple,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnBoostWuWa.FlatAppearance.BorderSize = 0;
            btnBoostWuWa.Click += (s, e) =>
            {
                btnBoostWuWa.Text = "BOOSTING...";
                btnBoostWuWa.Enabled = false;
                context.RunFlush(TrayAppContext.FlushLevel.GameModeBoost, "Manual Game Boost");
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Thread.Sleep(1500);
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            btnBoostWuWa.Text = "BOOST GAME MEMORY NOW";
                            btnBoostWuWa.Enabled = true;
                        });
                    }
                });
            };
            heroGame.Controls.Add(btnBoostWuWa);
            viewGaming.Controls.Add(heroGame);

            var cardConfig = new Panel
            {
                BackColor = UITheme.BgCard,
                Padding = new Padding(16)
            };
            cardConfig.Paint += (s, e) =>
            {
                UITheme.DrawCard(e.Graphics, new Rectangle(0, 0, cardConfig.Width - 1, cardConfig.Height - 1), 12, UITheme.BgCard, UITheme.Border);
            };

            var lblCfgTitle = new Label
            {
                Text = "IN-GAME TUNING",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.AccentCyan,
                Location = new Point(16, 12),
                AutoSize = true
            };
            cardConfig.Controls.Add(lblCfgTitle);

            chkAggressiveGameTrim = CreateCheckBox("Aggressively trim background applications while gaming", 38);
            chkAggressiveGameTrim.ForeColor = UITheme.TextPrimary;
            cardConfig.Controls.Add(chkAggressiveGameTrim);

            chkGamePriority = CreateCheckBox("Elevate detected game process priority to High", 68);
            chkGamePriority.ForeColor = UITheme.TextPrimary;
            cardConfig.Controls.Add(chkGamePriority);

            var lblThresh = new Label
            {
                Text = "In-Game RAM Flush Trigger (%):",
                Location = new Point(16, 102),
                AutoSize = true,
                ForeColor = UITheme.TextSecondary,
                Font = new Font("Segoe UI", 9f)
            };
            numGameHigh = new NumericUpDown
            {
                Location = new Point(230, 99),
                Width = 70,
                Minimum = 50,
                Maximum = 95,
                BackColor = UITheme.BgDark,
                ForeColor = UITheme.TextPrimary
            };
            cardConfig.Controls.Add(lblThresh);
            cardConfig.Controls.Add(numGameHigh);

            var lblInt = new Label
            {
                Text = "Trim Interval in Game (Minutes):",
                Location = new Point(330, 102),
                AutoSize = true,
                ForeColor = UITheme.TextSecondary,
                Font = new Font("Segoe UI", 9f)
            };
            numGameInterval = new NumericUpDown
            {
                Location = new Point(560, 99),
                Width = 70,
                Minimum = 1,
                Maximum = 30,
                BackColor = UITheme.BgDark,
                ForeColor = UITheme.TextPrimary
            };
            cardConfig.Controls.Add(lblInt);
            cardConfig.Controls.Add(numGameInterval);

            var btnSaveGameCfg = new Button
            {
                Text = "Save Gaming Profile",
                Size = new Size(170, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.AccentCyan,
                Cursor = Cursors.Hand
            };
            btnSaveGameCfg.FlatAppearance.BorderColor = UITheme.Border;
            btnSaveGameCfg.Click += (s, e) =>
            {
                context.Settings.AggressiveBackgroundTrimInGame = chkAggressiveGameTrim.Checked;
                context.Settings.GamePriorityBoost = chkGamePriority.Checked;
                context.Settings.GameHighUsagePercent = (double)numGameHigh.Value;
                context.Settings.GameTrimIntervalMinutes = (int)numGameInterval.Value;
                AppSettings.Save(context.ConfigPath, context.Settings);
                MessageBox.Show("Gaming profile saved.", "Game Shield", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            cardConfig.Controls.Add(btnSaveGameCfg);

            viewGaming.Controls.Add(cardConfig);

            var lblListH = new Label
            {
                Text = "Watched Game Executables (Protected):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                AutoSize = true
            };
            viewGaming.Controls.Add(lblListH);

            lbGameList = new ListBox
            {
                BackColor = UITheme.BgCard,
                ForeColor = UITheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f)
            };
            viewGaming.Controls.Add(lbGameList);

            txtNewGame = new TextBox
            {
                BackColor = UITheme.BgCard,
                ForeColor = UITheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            viewGaming.Controls.Add(txtNewGame);

            var btnAdd = new Button
            {
                Text = "Add Game",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.AccentCyan,
                ForeColor = Color.FromArgb(10, 20, 35),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.Click += (s, e) =>
            {
                string g = txtNewGame.Text.Trim();
                if (!string.IsNullOrEmpty(g))
                {
                    var current = new List<string>(context.Settings.GameProcessNames ?? new string[0]);
                    if (!current.Contains(g))
                    {
                        current.Add(g);
                        context.Settings.GameProcessNames = current.ToArray();
                        AppSettings.Save(context.ConfigPath, context.Settings);
                        txtNewGame.Clear();
                        RefreshGameList();
                    }
                }
            };
            viewGaming.Controls.Add(btnAdd);

            var btnRemove = new Button
            {
                Text = "Remove Selected",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnRemove.Click += (s, e) =>
            {
                if (lbGameList.SelectedItem != null)
                {
                    string sel = lbGameList.SelectedItem.ToString();
                    var current = new List<string>(context.Settings.GameProcessNames ?? new string[0]);
                    current.Remove(sel);
                    context.Settings.GameProcessNames = current.ToArray();
                    AppSettings.Save(context.ConfigPath, context.Settings);
                    RefreshGameList();
                }
            };
            viewGaming.Controls.Add(btnRemove);

            viewGaming.Resize += (s, e) =>
            {
                int w = viewGaming.ClientSize.Width - 40;
                heroGame.Location = new Point(20, 48);
                heroGame.Size = new Size(w, 140);
                lblGameExplain.Size = new Size(w - 32, 42);
                btnBoostWuWa.Location = new Point(16, 92);
                btnBoostWuWa.Size = new Size(Math.Min(450, w - 32), 36);

                cardConfig.Location = new Point(20, heroGame.Bottom + 12);
                cardConfig.Size = new Size(w, 145);
                btnSaveGameCfg.Location = new Point(w - 190, 98);

                lblListH.Location = new Point(20, cardConfig.Bottom + 12);

                int listTop = lblListH.Bottom + 8;
                int listH = viewGaming.ClientSize.Height - listTop - 60;
                lbGameList.Location = new Point(20, listTop);
                lbGameList.Size = new Size(w, Math.Max(100, listH));

                txtNewGame.Location = new Point(20, lbGameList.Bottom + 10);
                txtNewGame.Size = new Size(w - 280, 30);

                btnAdd.Location = new Point(txtNewGame.Right + 10, lbGameList.Bottom + 9);
                btnAdd.Size = new Size(120, 32);

                btnRemove.Location = new Point(btnAdd.Right + 10, lbGameList.Bottom + 9);
                btnRemove.Size = new Size(140, 32);
            };

            panelContentArea.Controls.Add(viewGaming);
        }

        private void RefreshGameList()
        {
            if (lbGameList == null) return;
            lbGameList.Items.Clear();
            if (context.Settings.GameProcessNames != null)
            {
                foreach (var g in context.Settings.GameProcessNames)
                    lbGameList.Items.Add(g);
            }

            if (chkAggressiveGameTrim != null)
                chkAggressiveGameTrim.Checked = context.Settings.AggressiveBackgroundTrimInGame;
            if (chkGamePriority != null)
                chkGamePriority.Checked = context.Settings.GamePriorityBoost;
            if (numGameHigh != null)
                numGameHigh.Value = (decimal)context.Settings.GameHighUsagePercent;
            if (numGameInterval != null)
                numGameInterval.Value = context.Settings.GameTrimIntervalMinutes;

            if (context.IsGameActive)
            {
                lblGameHeroStatus.Text = string.Format("Status: ACTIVE [{0}] (Shield & High Priority Engaged)", context.ActiveGameName);
                lblGameHeroStatus.ForeColor = UITheme.AccentPurple;
            }
            else
            {
                lblGameHeroStatus.Text = "Status: Monitoring in Background";
                lblGameHeroStatus.ForeColor = UITheme.AccentGreen;
            }
        }
    }
}
