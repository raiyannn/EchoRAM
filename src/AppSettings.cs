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
                // Gacha & Anime Action RPGs
                "Client-Win64-Shipping",     // Wuthering Waves
                "Wuthering Waves",
                "KRWebClient",
                "PGR",                       // Punishing: Gray Raven
                "Punishing Gray Raven",
                "GenshinImpact",             // Genshin Impact
                "YuanShen",
                "StarRail",                  // Honkai: Star Rail
                "ZenlessZoneZero",           // Zenless Zone Zero
                "BH3",                       // Honkai Impact 3rd
                "Endfield",                  // Arknights: Endfield
                "nikke",                     // GODDESS OF VICTORY: NIKKE

                // Competitive FPS & Shooters (Steam / Riot / Battle.net)
                "cs2",                       // Counter-Strike 2
                "VALORANT-Win64-Shipping",   // Valorant
                "VALORANT",
                "r5apex",                    // Apex Legends
                "FortniteClient-Win64-Shipping", // Fortnite
                "TslGame",                   // PUBG: BATTLEGROUNDS
                "Overwatch",                 // Overwatch 2
                "RainbowSix",                // Rainbow Six Siege
                "RainbowSix_Vulkan",
                "cod",                       // Call of Duty
                "bf2042",                    // Battlefield 2042
                "helldivers2",               // Helldivers 2
                "Destiny2",                  // Destiny 2
                "EscapeFromTarkov",          // Escape from Tarkov
                "Discovery",                 // THE FINALS
                "RustClient",                // Rust

                // MOBA & Competitive
                "League of Legends",         // League of Legends
                "dota2",                     // Dota 2
                "RocketLeague",              // Rocket League

                // Popular Steam AAA, Open-World & Co-op
                "eldenring",                 // Elden Ring
                "Cyberpunk2077",             // Cyberpunk 2077
                "witcher3",                  // The Witcher 3
                "GTA5",                      // Grand Theft Auto V
                "RDR2",                      // Red Dead Redemption 2
                "bg3",                       // Baldur's Gate 3
                "bg3_dx11",
                "b1-Win64-Shipping",         // Black Myth: Wukong
                "HogwartsLegacy",            // Hogwarts Legacy
                "Starfield",                 // Starfield
                "SkyrimSE",                  // Skyrim Special Edition
                "Fallout4",                  // Fallout 4
                "MonsterHunterWorld",        // Monster Hunter: World
                "MonsterHunterWilds",        // Monster Hunter Wilds
                "MonsterHunterRise",         // Monster Hunter Rise
                "Palworld-Win64-Shipping",   // Palworld
                "PathOfExileSteam",          // Path of Exile
                "PathOfExile",
                "PathOfExile2Steam",         // Path of Exile 2
                "Diablo IV",                 // Diablo IV
                "Warframe.x64",              // Warframe
                "FFXIV_dx11",                // Final Fantasy XIV
                "GhostOfTsushima",           // Ghost of Tsushima
                "GoWR",                      // God of War Ragnarok
                "ForzaHorizon5",             // Forza Horizon 5
                "SoTGame",                   // Sea of Thieves
                "Hades",                     // Hades
                "Hades2",                    // Hades II
                "Terraria",                  // Terraria
                "Stardew Valley",            // Stardew Valley
                "javaw",                     // Minecraft Java Edition
                "Minecraft.Windows",         // Minecraft Bedrock
                "RobloxPlayerBeta"           // Roblox
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
