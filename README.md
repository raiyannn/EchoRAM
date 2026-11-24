# EchoRAM

EchoRAM is a lightweight, portable Windows RAM optimizer and background process manager written in C# (.NET Framework 4.5, WinForms). It uses native Windows NT kernel memory APIs to clear standby caches and trim idle background working sets without touching active games or anti-cheat services.

## Download

Grab the latest standalone `EchoRAM.exe` from the **[Releases](../../releases/latest)** page.

- Single portable executable — no installer or extra DLLs required.
- Generates a default `config.json` and `EchoRAM.log` in its folder on first run.
- Place `EchoRAM.exe` in a permanent folder before enabling **Start with Windows**, since the scheduled task points to that path.

## What It Does

- **Kernel Memory List Flushing**: Clears Working Sets, System Working Set (file cache), Modified Page List, Standby List, and Priority 0 Standby List using `NtSetSystemInformation` and `SetSystemFileCacheSize`.
- **Automatic Threshold & Idle Cleanup**: Monitors physical memory usage in the background and clears cached memory when usage crosses configurable thresholds or when your PC is idle.
- **Game Shield Mode**: Watches for configured game processes (including Wuthering Waves, Punishing: Gray Raven, Genshin Impact, Honkai: Star Rail, and Zenless Zone Zero), protects the game and anti-cheat processes (`ACE`, `EasyAntiCheat`, `SGuard`, `dwm`, `audiodg`) from working-set trims, elevates game process priority, and trims background bloat.
- **CPU Load Guard**: Defers non-critical flushes when CPU usage is high (default 85%) to avoid frame drops during heavy workloads.
- **Process Memory Manager**: Inspect running processes sorted by Working Set / Private Bytes and trim individual apps or all background apps on demand.
- **System Tray & Global Hotkey**: Runs quietly in the system tray with a live memory indicator and a global hotkey (`Ctrl+Alt+F` by default) for instant cleanup.
- **Anti-Cheat Safe**: Uses standard Windows memory management APIs only — no DLL injection, memory hooking, or driver loading.

## Requirements

- **OS**: Windows 10 or Windows 11 (64-bit)
- **Runtime**: .NET Framework 4.5 or newer (pre-installed on Windows 10/11)
- **Permissions**: Administrator privileges (required by Windows to flush NT kernel memory lists). Enabling **Start with Windows** in Settings or the tray menu registers a Task Scheduler entry (`\EchoRAM`) so it starts at logon without UAC prompts.

## Usage

1. Run `EchoRAM.exe` and accept the Administrator UAC prompt.
2. EchoRAM starts in your **System Tray**. Double-click the tray icon to open the Dashboard, or right-click for quick flush options, top RAM consumers, and auto-start controls.
3. Press `Ctrl+Alt+F` anytime to trigger an immediate flush.
4. *(Optional)* Open **Settings** in the Dashboard to enable **Start EchoRAM on Windows logon** (which also adds a Start Menu shortcut), customize thresholds, or change the hotkey.

> **Note on Windows SmartScreen:** Because `EchoRAM.exe` is an unsigned open-source binary, Windows SmartScreen may show *"Windows protected your PC"* on first launch when downloaded from the internet. Click **More info → Run anyway** (or build from source below).

## Building from Source

### Using Visual Studio / MSBuild

Open `EchoRAM.sln` in Visual Studio and build `Release | x64`, or run:

```powershell
msbuild EchoRAM.sln /p:Configuration=Release /p:Platform=x64
```

### Using `csc.exe` (No Visual Studio required)

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe `
  /target:winexe /out:EchoRAM.exe /optimize+ /platform:x64 /win32icon:ram_icon.ico `
  /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll `
  src\*.cs
```

## Project Structure

```text
├── EchoRAM.sln                  # Visual Studio solution
├── EchoRAM.csproj               # .NET Framework 4.5 project file
├── ram_icon.ico                 # Application icon
├── src/
│   ├── Program.cs               # Entry point, elevation, CLI flags, AutoStartManager
│   ├── NativeMethods.cs         # NT kernel & Win32 P/Invoke wrappers
│   ├── AppSettings.cs           # JSON configuration model & persistence
│   ├── Monitors.cs              # CPU usage monitor & global hotkey window
│   ├── UITheme.cs               # Dark theme colors & GDI+ helpers
│   ├── TrayAppContext.cs        # Tray icon, menu, background monitor, flush logic
│   ├── DashboardForm*.cs        # WinForms dashboard & views
│   ├── Setup/SetupProgram.cs    # Optional standalone auto-start setup utility
│   └── Build/BuildProgram.cs    # Optional standalone csc.exe build helper
├── LICENSE
└── README.md
```

## License

This project is licensed under the [MIT License](LICENSE).
