# FFVI Pixel Remaster Save Editor

A save editor for Final Fantasy VI Pixel Remaster (Steam), written in C# with an Avalonia UI. Runs on Windows and Linux (including Steam Deck).

## Why this exists

The main community save editor for FFVI Pixel Remaster was [KiameV/final-fantasy-vi-save-editor](https://github.com/KiameV/final-fantasy-vi-save-editor), which the author archived in July 2025. As far as I could find, there is no actively maintained replacement. This project fills that gap by reimplementing the save format pipeline in C# and providing a cross-platform GUI for editing every commonly modified field.

## Features

Working:

- Reads and writes the encrypted Pixel Remaster save format (Rijndael-256, custom padding, DEFLATE, base64, BOM).
- Party fields: gil, total gil, step count. Total gil is auto-incremented when gil goes up, matching the game's behavior.
- Per character stats with a Base + Total view. Edit the Total and the editor computes the bonus that goes into the save. Covers Strength, Stamina, Speed, Magic, Attack, Defense, Magic Defense, Evasion, Magic Evasion. Hit Rate, Critical Rate, Luck, Intelligence, Spirit are exposed as bonus-only (no documented base in PR).
- Per character HP / MP / Level / Max HP+MP bonus fields.
- Spell learning: 54-spell checkbox list per character with Learn All / Forget All. Writes both `abilityList` and `abilityDictionary` so spells appear in the in-game magic menu.
- Inventory: 273-item lookup with categories. New Entry / Remove Selected / Max all to 99. Empty placeholder slots are rendered with descriptive labels.
- Equipment: 6 slots per character (Weapon, Shield, Helmet, Armor, Relic 1, Relic 2) with category-filtered dropdowns. Equipping an item auto-adds it to the inventory if absent, so the game's validation pass doesn't unequip it on load. The equipment slot's `count` field is set from the actual inventory count, which the game requires for the save to be considered valid.
- Espers: 27-esper checkbox list for ownership, plus a per-character equipped esper dropdown.

Not yet supported:

- Party composition / corps slots
- Story progression flags (`scenarioFlags`, `treasure`)
- Esper learning progress per character per spell
- Keywords, warehouse items

## Install

Windows 10 or 11, or Linux x64 (including Steam Deck desktop mode). No .NET install required, the executable is self-contained.

1. Download the latest zip for your platform from the [Releases](../../releases) page: `Ffvi.SaveTool-win-x64-YYYYMMDD.zip` or `Ffvi.SaveTool-linux-x64-YYYYMMDD.zip`.
2. Unzip anywhere (Desktop, Documents, wherever).
3. Run `Ffvi.SaveTool.App.exe` (Windows) or `Ffvi.SaveTool.App` (Linux; you may need `chmod +x Ffvi.SaveTool.App` first).

On first launch Windows SmartScreen may warn about an unrecognized app. Click "More info", then "Run anyway". The binary is unsigned, which is normal for hobby projects.

## Usage

1. Locate your save folder. On Windows it is `%USERPROFILE%\Documents\My Games\FINAL FANTASY VI PR\Steam\<steam-id>\`. On Linux (Proton) it is `~/.steam/steam/steamapps/compatdata/1173820/pfx/drive_c/users/steamuser/Documents/My Games/FINAL FANTASY VI PR/Steam/<steam-id>/` (also checked under `~/.local/share/Steam/`). The editor opens its file dialog there by default. The editor backs up each file automatically before saving (see Safety notes), but copying the folder somewhere safe first is still a good habit.
2. Close the game. Running the game while editing risks file locks, autosaves overwriting your edits, and Steam Cloud syncing stale data.
3. Recommended: turn off Steam Cloud for FFVI PR during editing. In Steam, right click the game, Properties, untick "Keep game saves in the Steam Cloud". Steam Cloud can revert your local edits at any time. Re-enable when you're done.
4. Launch `Ffvi.SaveTool.App`.
5. File, Open. The dialog defaults to your save folder. Pick a slot file (the larger ones, around 60 KB or more).
6. Pick a character from the left panel. Use the tabs to edit Stats, Spells, Equipment, Items, Espers.
7. File, Save. The window title shows `*` while there are unsaved changes, and the editor asks before closing or opening another file if you have any.
8. Launch the game and load the slot to verify.

### Identifying save files

Save filenames are base64-hashed and not human readable. The status bar shows `slot id=N` after opening a file. The id maps to:

| `id` value | In-game slot |
|---|---|
| 1 to 20 | File 1 to File 20 (manual saves) |
| 21 | Quick Save |
| 22 | Autosave |

You can also identify files by content. Larger files (50 KB and up) with a `pictureData` field are slot saves. Small files in the same folder hold slot occupancy flags, story flags, and game configuration.

## Safety notes

- Before overwriting a save, the editor copies the existing file to a backups folder, named `<file>.<yyyyMMdd-HHmmss>`. On Windows this is `%LOCALAPPDATA%\Ffvi.SaveTool\backups`, on Linux `~/.local/share/Ffvi.SaveTool/backups`. If saving fails because the backup could not be made, the original is left untouched.
- If your edited slot shows as Empty in the game's load menu, the save was rejected. Restore from backup. This usually means a field the editor doesn't yet handle was left in an inconsistent state. Open an issue with the steps you took.

## How the save format works

From disk to readable JSON:

```
file bytes
  -> strip UTF-8 BOM if present
  -> append '=' to the base64 string until length % 4 == 0
  -> base64 decode
  -> Rijndael-256-CBC decrypt (hardcoded 32-byte key and 32-byte IV)
  -> custom zero-byte unpadding (not PKCS7)
  -> DEFLATE decompress
  -> UTF-8 JSON
```

The JSON is deeply nested. Many fields hold escaped JSON strings rather than nested objects, so reaching a character stat traverses several string-unwrap steps:

```
top.userData (escaped string)
  .ownedCharacterList (escaped string)
    .target[N] (escaped string for each character)
      .parameter (escaped string)
        .currentHP, currentMP, addtionalMaxHp, ...
```

Field names in the save are preserved from the game's data model, including typos like `addtional` (instead of `additional`) and `owendGil` (instead of `ownedGil`).

### Two .NET-specific gotchas

1. `RijndaelManaged` is AES-only on .NET Core and later. Setting `BlockSize = 256` compiles but throws `PlatformNotSupportedException` at runtime. The project uses [BouncyCastle](https://www.bouncycastle.org/csharp/) for the actual 256-bit block Rijndael.
2. The default `System.Text.Json` encoder writes `"` for double quotes inside nested string values. The game's Unity-based parser rejects this and considers the save corrupted. Use `JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }` instead, which writes `\"`.

## Building from source

You only need this section if you want to modify the code or build your own release. End users should use the prebuilt zip from the Releases page.

Requirements:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Clone, run and test:

```bash
git clone <repo-url>
cd ffvi-editor
dotnet run --project src/Ffvi.SaveTool.App
dotnet test src/Ffvi.SaveTool.Tests
```

To produce release zips for Windows and Linux (self-contained single-file executable plus README and LICENSE):

```powershell
.\build-release.ps1
```

Output is at `publish\Ffvi.SaveTool-win-x64-YYYYMMDD.zip` and `publish\Ffvi.SaveTool-linux-x64-YYYYMMDD.zip`.

### Project layout

```
ffvi-editor/
  src/
    Ffvi.SaveTool.Lib/   Class library (crypto pipeline, JSON model, edit API).
    Ffvi.SaveTool.App/   Avalonia editor (Windows and Linux).
    Ffvi.SaveTool.Tests/ xUnit tests.
    Ffvi.SaveTool.Diag/  Small console runner used for inspecting save contents.
    Ffvi.SaveTool.slnx
  build-release.ps1      One-shot release build script.
```

## Credits

The data tables and the Rijndael key, IV, and padding scheme were all derived from [KiameV/final-fantasy-vi-save-editor](https://github.com/KiameV/final-fantasy-vi-save-editor). Everything was reimplemented in C#, but without that prior reverse engineering this project would not exist.

Item, spell, esper, character, and stat metadata cross-referenced against the [Final Fantasy Wiki](https://finalfantasy.fandom.com/).

Cryptography by [BouncyCastle.NET](https://www.bouncycastle.org/csharp/).

## License

MIT. See `LICENSE`.

## Disclaimer

Not affiliated with or endorsed by Square Enix. Final Fantasy VI Pixel Remaster is the property of Square Enix Holdings Co., Ltd. This editor only modifies save files on your own local disk. It does not patch, decompile, or otherwise interact with the game binary.
