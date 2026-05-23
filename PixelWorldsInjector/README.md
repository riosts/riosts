# Pixel Worlds Injector

A Sandboxie-style **multi-instance launcher** for the PC version of Pixel Worlds.

It is intentionally minimal and **non-invasive**:

- ✗ No memory editing of the game
- ✗ No cheats, hacks, automation, or anti-cheat bypass
- ✓ A named-mutex bypass that lets multiple copies of `PixelWorlds.exe` run side by side
- ✓ Per-instance data isolation via filesystem junctions (each instance gets its own `LocalLow\Kukouri\Pixel Worlds` folder)
- ✓ A `steam_appid.txt` drop so the game can start without the Steam client running (still requires a legally installed copy of the game)
- ✓ Optional GoldBerg Steam Emulator integration for games whose servers require a real-looking Steam ticket (e.g. Pixel Worlds itself — see [Steam Emulator](#steam-emulator-goldberg) below)

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

## Steam Emulator (GoldBerg)

The `steam_appid.txt` trick is enough for many games but **not** for Pixel Worlds: the game's world server rejects clients whose Steamworks SDK returns an empty auth ticket. To run Pixel Worlds without the real Steam client you need a Steam Emulator that produces a fake-but-format-valid ticket. The injector integrates with [GoldBerg Steam Emulator](https://gitlab.com/Mr_Goldberg/goldberg_emulator) (and its forks, e.g. [`gbe_fork`](https://github.com/Detanup01/gbe_fork)).

**GoldBerg is not bundled with this project** for licensing and AV-distribution reasons. The user must download it from upstream and point the launcher at the resulting `steam_api64.dll`.

### One-time setup

1. Download the latest GoldBerg release from upstream and unzip it somewhere persistent (e.g. `C:\Tools\Goldberg\`).
2. In the injector, open **File → Settings**.
3. Set **GoldBerg steam_api64.dll** to the unzipped `steam_api64.dll` (the 64-bit one — Pixel Worlds is a 64-bit game).
4. Click **Install GoldBerg into game**. This will:
   - Rename the game's existing `steam_api64.dll` to `steam_api64.original.dll` (backup).
   - Copy the GoldBerg DLL into the game directory.
5. The status label in Settings should now read `GoldBerg INSTALLED ...`.

### Per-instance Steam identity

In **Edit Instance**, tick **Use Steam Emulator for this instance** and optionally set the **Steam display name** and **SteamID64** (17 digits). Both fields are optional — leave them blank and the launcher will auto-derive a deterministic SteamID from the instance id so each instance keeps a stable identity across launches.

These values have **no link to a real Steam account**; they only control what the emulator reports to the game.

### Restoring the original DLL

If you ever want to go back to launching the game via Steam normally, open **Settings** and click **Restore original Steam DLL**. The backup will be moved back into place.

### Caveats

- Windows Defender and other AV products often flag GoldBerg as a generic threat. It is open source and widely used; you may need to whitelist the DLL.
- Some online games detect Steam emulators server-side and refuse the connection. The injector cannot work around that. Pixel Worlds historically does not detect GoldBerg, but this can change.
- Using GoldBerg on a Steam-installed copy of a game does **not** require Steam to be running, but you still need the game to be properly installed via Steam at least once.

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

You can also grab the prebuilt artifact from the latest successful **CI run** (`.github/workflows/pixelworlds-injector-build.yml`).

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

MIT — see [LICENSE](LICENSE).
