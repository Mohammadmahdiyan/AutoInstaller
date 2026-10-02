# Configuration Keys

This reference covers keys read by the current parser and installers. Put package-manifest keys in a top-level JSON object. A manifest is searched for at the package root using this priority: `mod.json`, `config.json`, then `<package folder name>.json` (filename matching is case-insensitive).

The `type` property and `InstallFile(s)`, `InstallFolder(s)`, `IgnoreFile(s)`, and `IgnoreFolder(s)` are matched case-insensitively. Other package-manifest keys below are currently read with the exact camel-case spelling shown. The four Install/Ignore key groups accept either one string or an array of strings; singular and plural forms are merged.

## `type`

Required as a string for a non-empty manifest. It chooses the behavior described in `06-ConfigTypes.md`. Matching ignores letter case, spaces, hyphens, and underscores.

```json
{ "Type": "PutInCleo" }
```

## `require` and `requires`

`require` accepts one requirement object. `requires` accepts one object or an array of objects. If both are present, both are checked. Supported fields:

- `checkFile`: one file path. A string or array is accepted; for an array, the first non-empty string is used.
- `checkFolder`: one folder path. A string or array is accepted; for an array, the first non-empty string is used.
- `checkFiles`: a string or array of file paths. All listed files must exist for the file condition to pass.
- `checkFolders`: a string or array of folder paths. All listed folders must exist for the folder condition to pass.
- `reqAddress`: address of the package used to satisfy a missing requirement.
- `reqPath`: alias for `reqAddress`; it is used only when `reqAddress` was not set.
- `require`: a nested requirement object. Nested fields fill values that are not already set on the outer object.

Relative check paths are resolved from the game folder; rooted paths are checked directly. A requirement passes when its complete file condition **or** its complete folder condition passes. If it does not pass, the installer uses `reqAddress` to find and install a required package. Without an address, installation stops with a warning.

```json
{
  "type": "PutInModLoader",
  "require": {
    "checkFile": "cleo.asi",
    "reqAddress": "Scripts/CLEO"
  },
  "requires": [
    { "checkFolders": ["cleo", "modloader"] },
    { "checkFiles": ["modloader.asi", "cleo.asi"] }
  ]
}
```

These property names are case-sensitive in the current parser; use the exact spellings shown.

## `conflictCleanup`

The parser reads and stores a `conflictCleanup` object with these fields:

- `file`, `folder`: one path; a string or array is accepted and the first non-empty array item is used.
- `files`, `folders`: a string or array of paths.
- `replaceWith`: one path; a string or array is accepted and the first non-empty array item is used.
- `replacesWith`: a string or array of replacement paths.

The current installation paths do not execute this cleanup object automatically. Including it does not currently remove files or perform replacements.

```json
{
  "type": "PutInModLoader",
  "conflictCleanup": {
    "files": ["old_mod.cs", "old_settings.ini"],
    "folders": "old_data",
    "replacesWith": ["new_mod.cs"]
  }
}
```

The key names inside this object are case-sensitive in the current parser.

## `deleteThis`

Accepts one string or an array of file/folder paths. Before supported install flows, each path is resolved relative to the game folder and removed if it exists; directories are removed recursively. Rooted paths and paths that escape the game folder are rejected.

```json
{
  "type": "PutInModLoader",
  "deleteThis": ["data/old_handling.cfg", "old_mod_folder"]
}
```

The exact key spelling `deleteThis` is required.

## `replacements`

Used by `PutAndReplace`. The validator requires an array. Each item may be:

- A string path, used as both source and destination.
- An object with string `source` and `target` paths.

The source is relative to the install payload; the target is relative to the game folder. Existing target files use the replacement backup flow.

```json
{
  "type": "PutAndReplace",
  "replacements": [
    "data/handling.cfg",
    { "source": "custom/weapon.dat", "target": "data/weapon.dat" }
  ]
}
```

The property and its `source` / `target` members are case-sensitive in the current parser.

## `InstallFile` and `InstallFiles`

Each key accepts a string or array of strings. If both are present, values are merged, trimmed, normalized to `\\` separators, and de-duplicated case-insensitively. Paths must be relative to the package root and must not escape it. In `PutInCleo`, either key enables file whitelist mode: every listed file must exist, and a file listed from a subfolder is flattened to its filename in `<game>\cleo`.

```json
{
  "type": "PutInCleo",
  "InstallFile": "RZL Trainer.cs",
  "INSTALLFILES": ["logs/memory2026.cs", "RZL Trainer.cs"]
}
```

## `InstallFolder` and `InstallFolders`

Each key accepts a string or array of strings; singular and plural values are merged and de-duplicated. Listed folders must exist and stay inside the package root. For `PutInCleo`, their contents are selected recursively and their folder names and inner structure are kept under `<game>\cleo`.

```json
{
  "type": "PutInCleo",
  "InstallFolders": ["RZL Trainer", "scripts/helpers"]
}
```

## `IgnoreFile` and `IgnoreFiles`

Each key accepts a string or array of strings and both forms are merged. A value without a separator matches that filename at any depth. A value with a separator matches the exact package-relative path. Matching is case-insensitive, and ignores are applied after Install selection.

```json
{
  "type": "PutInCleo",
  "IgnoreFile": "crash.log",
  "IgnoreFiles": ["logs/debug.txt", "temporary.ini"]
}
```

## `IgnoreFolder` and `IgnoreFolders`

Each key accepts a string or array of strings and both forms are merged. A listed folder and all descendants are excluded case-insensitively.

For `PutInCleo`, if `InstallFolders` is absent but `IgnoreFolders` is present, all top-level folders are selected recursively except ignored folders. If neither InstallFolders nor IgnoreFolders is present, subfolders are not selected automatically. `.git`, `.vscode`, and `.vs` are excluded from automatic folder selection; explicitly listing one in `InstallFolders` allows it.

```json
{
  "type": "PutInCleo",
  "IgnoreFolders": ["logs", "RZL Trainer/cache"]
}
```

## CLEO selection defaults and exclusions

For `PutInCleo`, when `InstallFiles` is absent, root-level package files are selected. Subfolders are not selected unless `InstallFolders` is supplied or `IgnoreFolders` activates automatic top-level folder selection. Ignore rules are applied after selection. The resolved manifest, README files, and media files are excluded from install entries. Other `.json` files are not automatically treated as manifests.

```json
{
  "type": "PutInCleo",
  "InstallFiles": ["RZL Trainer.cs", "memory2026.cs"],
  "IgnoreFiles": ["memory2026.cs"]
}
```

## Not currently supported

The manifest model/parser does not define backup-selection keys such as `backup`, `backupFile(s)`, `backupFolder(s)`, or `dontBackupFile(s)` / `dontBackupFolder(s)`. Do not rely on those keys to change backup behavior.
