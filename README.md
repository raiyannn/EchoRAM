# EchoRAM

EchoRAM is a lightweight, portable Windows RAM optimizer and background process manager written in C# (.NET Framework 4.5, WinForms). It uses native Windows NT kernel memory APIs to clear standby caches and trim idle background working sets without touching active games or anti-cheat services.

## Download

Grab the latest standalone `EchoRAM.exe` from the **[Releases](../../releases/latest)** page.

- Single portable executable — no installer or extra DLLs required.
- Generates a default `config.json` and `EchoRAM.log` in its folder on first run.
- Place `EchoRAM.exe` in a permanent folder before enabling **Start with Windows**, since the scheduled task points to that path.

## What It Does

- **Kernel Memory Cleanup**: Flushes Standby List, Modified Page List, System File Cache, and Working Sets via native Windows APIs.
- **Auto & Idle Triggers**: Automatically clears cached RAM when memory usage is high or your PC goes idle.
- **Game Mode**: Detects active games, boosts their priority, trims background apps, and leaves game and anti-cheat processes untouched.
- **Process Manager**: View top RAM-using apps and trim individual or background processes manually.
- **Tray & Hotkey**: Lives in the system tray with `Ctrl+Alt+F` for instant cleanup.

## Requirements

- **OS**: Windows 10 or Windows 11 (64-bit)
- **Runtime**: .NET Framework 4.5 or newer (pre-installed on Windows 10/11)
- **Permissions**: Administrator privileges (required by Windows to flush NT kernel memory lists). Enabling **Start with Windows** in Settings or the tray menu registers a Task Scheduler entry (`\EchoRAM`) so it starts at logon without UAC prompts.

## Usage

1. Run `EchoRAM.exe` and accept the Administrator UAC prompt.
2. EchoRAM starts in your **System Tray**. Double-click the tray icon to open the Dashboard, or right-click for quick flush options, top RAM consumers, and auto-start controls.
3. Press `Ctrl+Alt+F` anytime to trigger an immediate flush.
4. *(Optional)* Open **Settings** in the Dashboard to enable **Start EchoRAM on Windows logon** (which also adds a Start Menu shortcut), customize thresholds, or change the hotkey.

> **Note on Windows SmartScreen:** Because `EchoRAM.exe` is an unsigned open-source binary, Windows SmartScreen may show *"Windows protected your PC"* on first launch when downloaded from the internet. Click **More info → Run anyway**.

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
