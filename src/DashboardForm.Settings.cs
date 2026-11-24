using System;
using System.Drawing;
using System.Windows.Forms;

namespace EchoRAM
{
    partial class DashboardForm
    {
        private void InitSettingsView()
        {
            viewSettings = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.BgDark, Padding = new Padding(20) };

            var lblSetHeader = new Label
            {
                Text = "AUTOMATION & PERFORMANCE SETTINGS",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true,
                UseMnemonic = false
            };
            viewSettings.Controls.Add(lblSetHeader);

            var cardOpts = new Panel
            {
                Location = new Point(20, 56),
                BackColor = UITheme.BgCard,
                Padding = new Padding(18)
            };
            cardOpts.Paint += (s, e) =>
            {
                UITheme.DrawCard(e.Graphics, new Rectangle(0, 0, cardOpts.Width - 1, cardOpts.Height - 1), 12, UITheme.BgCard, UITheme.Border);
            };

            chkAutoStart = CreateCheckBox("Start EchoRAM on Windows logon (Admin, no UAC prompt)", 18);
            chkGameMode = CreateCheckBox("Enable Automatic Game Detection & Shield", 50);
            chkIdleClean = CreateCheckBox("Enable Idle Deep Clean (Flushes when user is AFK)", 82);
            chkCpuSafety = CreateCheckBox("Enable CPU Load Guard (Defer flush under heavy CPU load)", 114);
            chkNotifications = CreateCheckBox("Show Windows Tray Notifications on Flush", 146);
            chkAutoUpdate = CreateCheckBox("Automatically check GitHub for updates", 178);

            cardOpts.Controls.Add(chkAutoStart);
            cardOpts.Controls.Add(chkGameMode);
            cardOpts.Controls.Add(chkIdleClean);
            cardOpts.Controls.Add(chkCpuSafety);
            cardOpts.Controls.Add(chkNotifications);
            cardOpts.Controls.Add(chkAutoUpdate);

            var lblH = new Label { Text = "High RAM Flush Trigger (%):", Location = new Point(18, 222), AutoSize = true, ForeColor = UITheme.TextSecondary };
            numHighUsage = new NumericUpDown { Location = new Point(250, 219), Width = 75, Minimum = 40, Maximum = 95, BackColor = UITheme.BgDark, ForeColor = UITheme.TextPrimary };
            cardOpts.Controls.Add(lblH); cardOpts.Controls.Add(numHighUsage);

            var lblM = new Label { Text = "Medium RAM Flush Trigger (%):", Location = new Point(18, 259), AutoSize = true, ForeColor = UITheme.TextSecondary };
            numMediumUsage = new NumericUpDown { Location = new Point(250, 256), Width = 75, Minimum = 30, Maximum = 90, BackColor = UITheme.BgDark, ForeColor = UITheme.TextPrimary };
            cardOpts.Controls.Add(lblM); cardOpts.Controls.Add(numMediumUsage);

            var lblI = new Label { Text = "Idle Trigger Time (Minutes):", Location = new Point(18, 296), AutoSize = true, ForeColor = UITheme.TextSecondary };
            numIdleMinutes = new NumericUpDown { Location = new Point(250, 293), Width = 75, Minimum = 1, Maximum = 60, BackColor = UITheme.BgDark, ForeColor = UITheme.TextPrimary };
            cardOpts.Controls.Add(lblI); cardOpts.Controls.Add(numIdleMinutes);

            var lblHk = new Label { Text = "Global Flush Hotkey:", Location = new Point(18, 333), AutoSize = true, ForeColor = UITheme.TextSecondary };
            txtHotkey = new TextBox { Location = new Point(250, 330), Width = 140, BackColor = UITheme.BgDark, ForeColor = UITheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            cardOpts.Controls.Add(lblHk); cardOpts.Controls.Add(txtHotkey);

            var lblTheme = new Label { Text = "Appearance Theme:", Location = new Point(420, 222), AutoSize = true, ForeColor = UITheme.TextSecondary };
            cmbTheme = new ComboBox
            {
                Location = new Point(560, 219),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.BgDark,
                ForeColor = UITheme.TextPrimary
            };
            cmbTheme.Items.AddRange(new object[] { "Dark", "Light", "System" });
            cardOpts.Controls.Add(lblTheme); cardOpts.Controls.Add(cmbTheme);

            var lblExcl = new Label { Text = "Excluded Apps (comma-separated):", Location = new Point(420, 259), AutoSize = true, ForeColor = UITheme.TextSecondary };
            txtExcludedProcs = new TextBox { Location = new Point(420, 283), Width = 280, BackColor = UITheme.BgDark, ForeColor = UITheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            cardOpts.Controls.Add(lblExcl); cardOpts.Controls.Add(txtExcludedProcs);

            var btnSave = new Button
            {
                Text = "Save & Apply Settings",
                Location = new Point(18, 382),
                Size = new Size(210, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = UITheme.AccentCyan,
                ForeColor = Color.FromArgb(10, 20, 35)
            };
            btnSave.Click += (s, e) => SaveSettingsFromUI();
            cardOpts.Controls.Add(btnSave);

            var btnCheckUpdate = new Button
            {
                Text = string.Format("Check for Updates (v{0})", Program.AppVersion),
                Location = new Point(242, 382),
                Size = new Size(220, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnCheckUpdate.FlatAppearance.BorderColor = UITheme.Border;
            btnCheckUpdate.Click += (s, e) => context.CheckForUpdates(true);
            cardOpts.Controls.Add(btnCheckUpdate);

            viewSettings.Controls.Add(cardOpts);

            viewSettings.Resize += (s, e) =>
            {
                cardOpts.Size = new Size(viewSettings.ClientSize.Width - 40, Math.Max(430, viewSettings.ClientSize.Height - 80));
            };

            panelContentArea.Controls.Add(viewSettings);
        }

        private void LoadSettingsToUI()
        {
            chkAutoStart.Checked = AutoStartManager.IsAutoStartEnabled();
            chkGameMode.Checked = context.Settings.EnableGameMode;
            chkIdleClean.Checked = context.Settings.EnableIdleFlush;
            chkCpuSafety.Checked = context.Settings.EnableCpuSafety;
            chkNotifications.Checked = context.Settings.EnableNotifications;
            chkAutoUpdate.Checked = context.Settings.CheckForUpdates;

            numHighUsage.Value = (decimal)context.Settings.HighUsagePercent;
            numMediumUsage.Value = (decimal)context.Settings.MediumUsagePercent;
            numIdleMinutes.Value = context.Settings.IdleMinutesThreshold;
            txtHotkey.Text = context.Settings.Hotkey ?? "Ctrl+Alt+F";

            string tMode = context.Settings.ThemeMode ?? "Dark";
            int idx = cmbTheme.FindStringExact(tMode);
            cmbTheme.SelectedIndex = idx >= 0 ? idx : 0;

            txtExcludedProcs.Text = context.Settings.ExcludedProcesses != null
                ? string.Join(", ", context.Settings.ExcludedProcesses)
                : "";
        }

        private void SaveSettingsFromUI()
        {
            bool currentAs = AutoStartManager.IsAutoStartEnabled();
            if (chkAutoStart.Checked != currentAs)
            {
                if (chkAutoStart.Checked) AutoStartManager.EnableAutoStart();
                else AutoStartManager.DisableAutoStart();
            }

            context.Settings.EnableGameMode = chkGameMode.Checked;
            context.Settings.EnableIdleFlush = chkIdleClean.Checked;
            context.Settings.EnableCpuSafety = chkCpuSafety.Checked;
            context.Settings.EnableNotifications = chkNotifications.Checked;
            context.Settings.CheckForUpdates = chkAutoUpdate.Checked;

            context.Settings.HighUsagePercent = (double)numHighUsage.Value;
            context.Settings.MediumUsagePercent = (double)numMediumUsage.Value;
            context.Settings.IdleMinutesThreshold = (int)numIdleMinutes.Value;
            context.Settings.Hotkey = txtHotkey.Text.Trim();

            context.Settings.ThemeMode = cmbTheme.SelectedItem != null ? cmbTheme.SelectedItem.ToString() : "Dark";
            var exclList = new System.Collections.Generic.List<string>();
            foreach (string part in (txtExcludedProcs.Text ?? "").Split(','))
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed)) exclList.Add(trimmed);
            }
            context.Settings.ExcludedProcesses = exclList.ToArray();

            AppSettings.Save(context.ConfigPath, context.Settings);
            context.SetupHotkey();

            UITheme.ApplyTheme(context.Settings.ThemeMode);
            ApplyThemeToUI();

            MessageBox.Show("Settings saved and applied.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
