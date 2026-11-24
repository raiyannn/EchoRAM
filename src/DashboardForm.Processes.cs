using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace EchoRAM
{
    partial class DashboardForm
    {
        private void InitProcessesView()
        {
            viewProcesses = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.BgDark, Padding = new Padding(20) };

            var lblProcHeader = new Label
            {
                Text = "PROCESS MEMORY MANAGER",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true
            };
            viewProcesses.Controls.Add(lblProcHeader);

            txtSearchProc = new TextBox
            {
                Location = new Point(270, 15),
                Size = new Size(220, 28),
                BackColor = UITheme.BgCard,
                ForeColor = UITheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtSearchProc.TextChanged += (s, e) => RefreshProcessList();
            viewProcesses.Controls.Add(txtSearchProc);

            var btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(500, 14),
                Size = new Size(95, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.TextPrimary
            };
            btnRefresh.Click += (s, e) => RefreshProcessList();
            viewProcesses.Controls.Add(btnRefresh);

            lvProcesses = new ListView
            {
                Location = new Point(20, 56),
                View = View.Details,
                FullRowSelect = true,
                BackColor = UITheme.BgCard,
                ForeColor = UITheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f)
            };
            lvProcesses.Columns.Add("Process Name", 280);
            lvProcesses.Columns.Add("PID", 85);
            lvProcesses.Columns.Add("Working Set", 140);
            lvProcesses.Columns.Add("Private Bytes", 140);
            viewProcesses.Controls.Add(lvProcesses);

            var btnTrim = new Button
            {
                Text = "Trim Selected App",
                Size = new Size(185, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = UITheme.AccentCyan,
                ForeColor = Color.FromArgb(10, 20, 35)
            };
            btnTrim.Click += (s, e) =>
            {
                if (lvProcesses.SelectedItems.Count > 0)
                {
                    int pid = int.Parse(lvProcesses.SelectedItems[0].SubItems[1].Text);
                    string name = lvProcesses.SelectedItems[0].Text;
                    bool ok = NativeMethods.TrimSingleProcess(pid);
                    MessageBox.Show(ok ? ("Trimmed working set for " + name) : "Could not trim process.",
                        "Process Trim", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshProcessList();
                }
            };
            viewProcesses.Controls.Add(btnTrim);

            var btnTrimAll = new Button
            {
                Text = "Trim All Background Apps",
                Size = new Size(220, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = UITheme.BgCardHover,
                ForeColor = UITheme.TextPrimary
            };
            btnTrimAll.Click += (s, e) =>
            {
                int count = NativeMethods.TrimProcessWorkingSets(context.IsGameActive ? context.ActiveGameName : null);
                MessageBox.Show(string.Format("Trimmed working sets across {0} processes.", count),
                    "Bulk Trim", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshProcessList();
            };
            viewProcesses.Controls.Add(btnTrimAll);

            viewProcesses.Resize += (s, e) =>
            {
                int w = viewProcesses.ClientSize.Width - 40;
                int h = viewProcesses.ClientSize.Height - 120;
                lvProcesses.Size = new Size(w, Math.Max(200, h));

                btnTrim.Location = new Point(20, lvProcesses.Bottom + 12);
                btnTrimAll.Location = new Point(215, lvProcesses.Bottom + 12);
            };

            panelContentArea.Controls.Add(viewProcesses);
        }

        private void RefreshProcessList()
        {
            if (!viewProcesses.Visible) return;
            try
            {
                lvProcesses.BeginUpdate();
                lvProcesses.Items.Clear();

                string filter = (txtSearchProc != null ? txtSearchProc.Text.Trim().ToLowerInvariant() : "");

                Process[] procs = Process.GetProcesses();
                var list = new List<Process>();
                foreach (var p in procs)
                {
                    if (string.IsNullOrEmpty(filter) || p.ProcessName.ToLowerInvariant().Contains(filter))
                        list.Add(p);
                }

                list.Sort((a, b) =>
                {
                    try { return b.WorkingSet64.CompareTo(a.WorkingSet64); }
                    catch { return 0; }
                });

                int count = Math.Min(60, list.Count);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var p = list[i];
                        double wsMB = p.WorkingSet64 / (1024.0 * 1024.0);
                        double privMB = p.PrivateMemorySize64 / (1024.0 * 1024.0);

                        var lvi = new ListViewItem(p.ProcessName);
                        lvi.SubItems.Add(p.Id.ToString());
                        lvi.SubItems.Add(string.Format("{0:N0} MB", wsMB));
                        lvi.SubItems.Add(string.Format("{0:N0} MB", privMB));

                        if (context.IsGameActive && string.Equals(p.ProcessName, context.ActiveGameName.Replace(".exe", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            lvi.ForeColor = UITheme.AccentPurple;
                            lvi.Font = new Font(lvProcesses.Font, FontStyle.Bold);
                        }

                        lvProcesses.Items.Add(lvi);
                    }
                    catch { }
                }
            }
            finally
            {
                lvProcesses.EndUpdate();
            }
        }
    }
}
