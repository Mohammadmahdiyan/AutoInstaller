# Configuration Keys

A manifest is a JSON object. The `type` key is read without regard to letter case. The `InstallFile(s)`, `InstallFolder(s)`, `IgnoreFile(s)`, and `IgnoreFolder(s)` key names are also case-insensitive. For the existing `require`, `requires`, `conflictCleanup`, `deleteThis`, and `replacements` fields, use the camel-case spellings shown below; their current parser uses those exact names.

## Package type

The normalized type values accepted by the validator are:

- `PutInModLoader` (`PIM`)
- `Replacing` (`RIP`)
- `PutInCleo` (`PIC`)
- `PutInGameFolder` (`PGF`)
- `PutAndReplace` (`PAR`)
- `PutAndReplaces` (`PRS`)
- `VehicleAndSkinAndWeapon` (`VSW` / `VSS`)
- `VehiclesAndSkinsAndWeapons`
- `SavesAndMissions` (`SAW`)
- `MissionDsl` (`DSL`)

Type matching removes spaces, `_`, and `-`, then compares case-insensitively. Note: the current `NormalizedType` implementation maps both vehicle/skin/weapon spellings to the single-asset normalized type; do not assume they activate distinct flows.

## Requirements

`require` accepts one object; `requires` accepts one object or an array of objects. Requirement fields include `checkFile`, `checkFolder`, `checkFiles`, `checkFolders`, and `reqAddress`. `reqPath` is also accepted as an alias for `reqAddress`. File/folder paths are checked relative to the game folder unless rooted.

```json
{
  "type": "PutInModLoader",
  "require": { "checkFile": "cleo.asi", "reqAddress": "Scripts/CLEO" },
  "requires": [{ "checkFolders": ["modloader"] }]
}
```

## Cleanup and replacement

`deleteThis` accepts a string or array of game-relative files/folders to remove before installation. `conflictCleanup` is parsed with `file`, `folder`, `files`, `folders`, `replaceWith`, and `replacesWith` fields; the current install paths do not apply that object automatically. For `PutAndReplace`, `replacements` is an array of source/target pairs (or source-path strings).

## File and folder selection

The four selection keys accept a string or an array of relative paths. Singular and plural forms are merged. `/` is normalized to `\\`; paths escaping the package root are rejected.

- `InstallFile` / `InstallFiles`: whitelist files. Listed files in subfolders are installed at the target root using their file name.
- `InstallFolder` / `InstallFolders`: whitelist folders recursively and keep their folder structure.
- `IgnoreFile` / `IgnoreFiles`: exclude a matching file name at any depth, or an exact relative path when a path is supplied.
- `IgnoreFolder` / `IgnoreFolders`: exclude a folder and its descendants.

These selection lists are currently consumed by the CLEO package installer. Without an install-file list, root-level files are selected. Without an install-folder list, subfolders are not selected unless ignore-folder mode requests all top-level folders.

```json
{
  "type": "PutInCleo",
  "InstallFiles": ["logs/memory2026.cs", "RZL Trainer.cs"],
  "IgnoreFile": "crash.log"
}
```
