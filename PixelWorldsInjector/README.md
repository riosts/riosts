# Pixel Worlds Injector

A Sandboxie-style **multi-instance launcher** for the PC version of Pixel Worlds.

It is intentionally minimal and **non-invasive**:

- ✗ No memory editing of the game
- ✗ No cheats, hacks, automation, or anti-cheat bypass
- ✓ A named-mutex bypass that lets multiple copies of `PixelWorlds.exe` run side by side
- ✓ Per-instance data isolation via filesystem junctions (each instance gets its own `LocalLow\Kukouri\Pixel Worlds` folder)
- ✓ A `steam_appid.txt` drop so the game can start without the Steam client running (still requires a legally installed copy of the game)

## How it works

1. **Steam locator** finds your Pixel Worlds installation by parsing `libraryfolders.vdf` and Steam's registry keys.
2. **`steam_appid.txt`** is written next to `PixelWorlds.exe` so the Steamworks SDK starts the game without Steam needing to be running. This is the same trick that Steam ships in its own SDK for development.
3. **Junction swap** — before launching an isolated instance, the launcher swaps `%USERPROFILE%\AppData\LocalLow\Kukouri\Pixel Worlds` for a junction pointing at the instance's private folder. The original folder is moved aside and restored when the game exits.
4. **Mutex bypass** — once the game is running, the launcher uses `NtQuerySystemInformation` + `NtDuplicateObject(DUPLICATE_CLOSE_SOURCE)` to close the single-instance mutex inside the game's process, so a second launch will succeed. No code is injected into the game.

All persistent state lives under `%AppData%\PixelWorldsInjector\`:

```
%AppData%\PixelWorldsInjector\
├── settings.json
├── injector.log
└── instances\
    └── <instance-id>\LocalLow\Kukouri\Pixel Worlds\...
```

## Building

Requirements:

- Windows 10/11
- .NET 8 SDK (`dotnet --version` ≥ 8.0)

```powershell
dotnet build PixelWorldsInjector.sln -c Release
```

For a single-file build:

```powershell
dotnet publish src/PixelWorldsInjector/PixelWorldsInjector.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The output `.exe` will be in `src\PixelWorldsInjector\bin\Release\net8.0-windows\win-x64\publish\`.

You can also grab the prebuilt artifact from the latest successful **CI run** (`.github/workflows/build.yml`).

## Usage

1. Launch `PixelWorldsInjector.exe` as **Administrator** (required for the handle manipulation and junction creation).
2. The launcher will auto-detect `PixelWorlds.exe` from your Steam libraries on first run. If it can't, click **Browse...** and point it at the file manually.
3. Click **Create Instance** to add an isolated profile. Give it a name like `Main` or `Alt`.
4. Select the instance and click **Launch**. The game window will open; log in using the Pixel Worlds account form (same flow as the Android client).
5. Create more instances and launch them in parallel. Each one will have its own save folder when **Isolate save data** is on.

## Limitations

- Junction-based data isolation is global: only one instance has its private folder mounted at any time. For true parallel multi-account play with separate local data, rely on the **in-game login form** and accept that Unity `PlayerPrefs` (HKCU registry) are shared across instances.
- The mutex bypass requires admin privileges. The app manifest already requests this.
- Anti-virus may flag the binary the first time you run it because it uses `NtDuplicateObject` and writes `steam_appid.txt`. This is a documented technique; the source is fully open in this repository.

## ToS disclaimer

This tool is provided as-is for users who want to run multiple Pixel Worlds clients on the same PC (e.g. to manage personal alt accounts the same way the Android client lets you). It does not provide any in-game advantage on its own. **You are responsible for complying with the Pixel Worlds Terms of Service**, including any rules against multi-accounting, automation, or use of unofficial clients.

The maintainers do not endorse or support using this tool for botting, scripting, RMT, anti-cheat circumvention, or any other activity that violates the game's ToS.

## License

MIT (see [LICENSE](LICENSE) once added).
