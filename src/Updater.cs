using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace EchoRAM
{
    public class UpdateInfo
    {
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleasePageUrl { get; set; }
        public string Error { get; set; }
    }

    public static class Updater
    {
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/raiyannn/EchoRAM/releases/latest";

        public static void CheckForUpdatesAsync(Action<UpdateInfo> callback)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateInfo info = CheckForUpdates();
                if (callback != null)
                    callback(info);
            });
        }

        public static UpdateInfo CheckForUpdates()
        {
            var result = new UpdateInfo
            {
                UpdateAvailable = false,
                LatestVersion = Program.AppVersion,
                ReleasePageUrl = "https://github.com/raiyannn/EchoRAM/releases/latest"
            };

            try
            {
                // Enable TLS 1.2 for GitHub API on .NET Framework 4.5
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;

                var req = (HttpWebRequest)WebRequest.Create(LatestReleaseApiUrl);
                req.Method = "GET";
                req.UserAgent = "EchoRAM-Updater/" + Program.AppVersion;
                req.Accept = "application/vnd.github.v3+json";
                req.Timeout = 10000;

                string json;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    json = reader.ReadToEnd();
                }

                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<Dictionary<string, object>>(json);
                if (data == null) return result;

                string tag = data.ContainsKey("tag_name") ? Convert.ToString(data["tag_name"]) : "";
                string htmlUrl = data.ContainsKey("html_url") ? Convert.ToString(data["html_url"]) : result.ReleasePageUrl;

                string cleanRemote = tag.Trim().TrimStart('v', 'V');
                result.LatestVersion = cleanRemote;
                result.ReleasePageUrl = htmlUrl;

                if (data.ContainsKey("assets") && data["assets"] is ArrayList)
                {
                    foreach (object item in (ArrayList)data["assets"])
                    {
                        var asset = item as Dictionary<string, object>;
                        if (asset == null) continue;
                        string name = asset.ContainsKey("name") ? Convert.ToString(asset["name"]) : "";
                        if (string.Equals(name, "EchoRAM.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            result.DownloadUrl = asset.ContainsKey("browser_download_url")
                                ? Convert.ToString(asset["browser_download_url"])
                                : null;
                            break;
                        }
                    }
                }

                Version currentVer;
                Version remoteVer;
                if (Version.TryParse(Program.AppVersion, out currentVer) &&
                    Version.TryParse(cleanRemote, out remoteVer))
                {
                    result.UpdateAvailable = remoteVer > currentVer;
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        public static bool DownloadAndApplyUpdate(UpdateInfo info, out string error)
        {
            error = null;
            if (info == null || string.IsNullOrEmpty(info.DownloadUrl))
            {
                error = "No EchoRAM.exe asset found in the latest release.";
                return false;
            }

            string currentExe = Application.ExecutablePath;
            string tempExe = currentExe + ".update";

            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;

                if (File.Exists(tempExe))
                    File.Delete(tempExe);

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "EchoRAM-Updater/" + Program.AppVersion);
                    wc.DownloadFile(info.DownloadUrl, tempExe);
                }

                var fi = new FileInfo(tempExe);
                if (!fi.Exists || fi.Length < 20480)
                {
                    error = "Downloaded update file appears invalid.";
                    try { if (File.Exists(tempExe)) File.Delete(tempExe); } catch { }
                    return false;
                }

                // Replace locked EXE after current process exits, then relaunch
                string cmdArgs = string.Format(
                    "/c timeout /t 2 /nobreak >nul & move /y \"{0}\" \"{1}\" >nul & start \"\" \"{1}\"",
                    tempExe, currentExe);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try { if (File.Exists(tempExe)) File.Delete(tempExe); } catch { }
                return false;
            }
        }
    }
}
