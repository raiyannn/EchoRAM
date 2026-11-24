using System.IO;
using System.Web.Script.Serialization;

namespace EchoRAM
{
    public class AppSettings
    {
        public double HighUsagePercent { get; set; }
        public double MediumUsagePercent { get; set; }
        public int CheckIntervalSeconds { get; set; }
        public int PeriodicFlushMinutes { get; set; }
        public int CooldownSeconds { get; set; }

        public bool EnableGameMode { get; set; }
        public string[] GameProcessNames { get; set; }
        public double GameHighUsagePercent { get; set; }
        public int GameTrimIntervalMinutes { get; set; }
        public bool GamePriorityBoost { get; set; }
        public bool AggressiveBackgroundTrimInGame { get; set; }

        public bool EnableIdleFlush { get; set; }
        public int IdleMinutesThreshold { get; set; }

        public bool EnableGlobalHotkey { get; set; }
        public string Hotkey { get; set; }

        public bool EnableCpuSafety { get; set; }
        public double CpuThresholdPercent { get; set; }

        public bool EnableNotifications { get; set; }

        public AppSettings()
        {
            HighUsagePercent = 80.0;
            MediumUsagePercent = 65.0;
            CheckIntervalSeconds = 15;
            PeriodicFlushMinutes = 10;
            CooldownSeconds = 45;

            EnableGameMode = true;
            GameProcessNames = new string[]
            {
                "Client-Win64-Shipping",     // Wuthering Waves
                "Wuthering Waves",
                "KRWebClient",
                "PGR",                       // Punishing: Gray Raven
                "Punishing Gray Raven",
                "GenshinImpact",             // Genshin Impact
                "YuanShen",
                "StarRail",                  // Honkai: Star Rail
                "ZenlessZoneZero"            // Zenless Zone Zero
            };
            GameHighUsagePercent = 75.0;
            GameTrimIntervalMinutes = 2;
            GamePriorityBoost = true;
            AggressiveBackgroundTrimInGame = true;

            EnableIdleFlush = true;
            IdleMinutesThreshold = 5;

            EnableGlobalHotkey = true;
            Hotkey = "Ctrl+Alt+F";

            EnableCpuSafety = true;
            CpuThresholdPercent = 85.0;

            EnableNotifications = true;
        }

        public static AppSettings Load(string path)
        {
            var serializer = new JavaScriptSerializer();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var settings = serializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        if (settings.GameHighUsagePercent <= 0) settings.GameHighUsagePercent = 75.0;
                        if (settings.GameTrimIntervalMinutes <= 0) settings.GameTrimIntervalMinutes = 2;
                        return settings;
                    }
                }
                catch { }
            }

            var def = new AppSettings();
            Save(path, def);
            return def;
        }

        public static void Save(string path, AppSettings settings)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(settings);
                json = json.Replace("{\"", "{\r\n  \"")
                           .Replace(",\"", ",\r\n  \"")
                           .Replace("}", "\r\n}");
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}
