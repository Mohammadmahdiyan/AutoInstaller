# Configuration Types

This guide describes the package `type` values currently recognized by GTA San Andreas Mod Manager. Put the type in a JSON manifest at the top level of the mod package. The manifest may be named `mod.json`, `config.json`, or `<package folder name>.json`; lookup checks those names in that order.

The `type` property name and its value are case-insensitive. The parser trims the value, removes spaces, hyphens, and underscores, then normalizes supported names and aliases. A non-empty manifest must contain a string `type` to pass package validation.

## PutInModLoader

Installs the selected payload under:

```text
<game folder>\modloader\<mod name>\
```

For ordinary packages, the payload is the first eligible top-level subfolder (alphabetical order). If there is no eligible subfolder, root-level package files form the payload. Manifest, README, and supported media files are not copied as ordinary install files.

Accepted values: `PutInModLoader`, `ModLoader`, `PIM`.

```json
{
  "type": "PutInModLoader"
}
```

Example package:

```text
RZL Trainer/
	config.json
	RZL Trainer.cs
	preview.png
```

The installed script is placed in `<game>\modloader\RZL Trainer\`.

## Replacing

Copies payload files to matching relative paths in the game folder, replacing existing files. Existing originals are backed up when a backup plan is available and accepted. This dedicated replacing path is dispatched by the legacy **Install Mod** entry point; the Step 3 wizard does not currently have a separate `Replacing` branch.

Accepted values: `Replacing`, `RIP`.

```json
{
  "type": "Replacing"
}
```

With `data\handling.cfg` in the payload, its game destination is `<game>\data\handling.cfg`.

## PutInCleo

Installs selected files into `<game>\cleo\`. It uses the `InstallFiles`, `InstallFolders`, `IgnoreFiles`, and `IgnoreFolders` selection rules described in `07-ConfigKeys.md`. Without install lists, root-level files are selected; subfolders are not installed unless folder selection is enabled.

Accepted values: `PutInCleo`, `PIC`.

```json
{
  "type": "PutInCleo",
  "InstallFiles": ["ragdoll.asi", "Ragdoll.json"]
}
```

## PutInGameFolder

Copies payload contents into the game folder while preserving relative paths. Existing destination files are backed up through the replacement backup flow when applicable.

Accepted values: `PutInGameFolder`, `PGF`.

```json
{
  "type": "PutInGameFolder"
}
```

For a payload containing `ragdoll.asi` and `ragdoll.bmp`, both files are placed directly in `<game>\`.

## PutAndReplace

Uses a required `replacements` array to map each payload source file to a game-relative destination. The original destination file is backed up through the replacement backup flow when applicable. See `07-ConfigKeys.md` for the supported array forms.

Accepted values: `PutAndReplace`, `PAR`.

```json
{
  "type": "PutAndReplace",
  "replacements": [{ "source": "handling.cfg", "target": "data/handling.cfg" }]
}
```

## PutAndReplaces

Copies payload files to the same relative paths under the game folder and backs up existing destination files through the replacement backup flow when applicable. Unlike `PutAndReplace`, it does not define an individual source-to-target mapping for each file.

Accepted values: `PutAndReplaces`, `PRS`.

```json
{
  "type": "PutAndReplaces"
}
```

For example, a payload `data\handling.cfg` targets `<game>\data\handling.cfg`.

## VehicleAndSkinAndWeapon (single asset)

Uses the Step 3 / Step 5 asset-selection flow for a single source model. The source `.dff` / `.txd` basename is matched to the asset catalog to determine the vehicle, skin, or weapon list. The selected target's `NameFile` is used for the installed model filename in the ModLoader package.

Accepted values: `VehicleAndSkinAndWeapon`, `VSW`, `VSS`. The legacy spelling `VehicleAndSkinsAndWeapons` (with an `s` after `Vehicle`) is also normalized to this single-asset type.

```json
{
  "type": "VehicleAndSkinAndWeapon"
}
```

If `xaa.dff` is not in any catalog, Step 5 asks whether it is a vehicle, skin, or weapon before showing that type's assets.

## VehiclesAndSkinsAndWeapons (multiple assets)

Scans the package for `.dff` and `.txd` files, groups files by identical basename, and shows Step 5 once per model. Each known model starts with its detected asset type; unknown model names ask the user to choose a type. Mapped and kept models are installed under the ModLoader package folder using their selected target name or original basename.

Accepted value: `VehiclesAndSkinsAndWeapons` (plural `Vehicles`). Do not confuse it with the single-asset `VehicleAndSkinsAndWeapons` alias above.

```json
{
  "type": "VehiclesAndSkinsAndWeapons"
}
```

For example, `infernus.dff` and `infernus.txd` are one source model. A differently named TXD is not paired with that DFF.

## SavesAndMissions

Recognizes save-slot files named `GTASAsf<number>.b`. It installs them into the next available slot in `Documents\GTA San Andreas User Files`; when a destination slot must be reused, the existing file is moved into that folder's hidden `.trash` directory. A package containing `DYOM<number>.dat` is treated as a DYOM package and also requires the DYOM dependency in the Base Mods folder.

Accepted values: `SavesAndMissions`, `SAM`.

```json
{
  "type": "SavesAndMissions"
}
```

## MissionDsl

If `Documents\GTA San Andreas User Files\DSL` does not exist, installs the DYOM v8.1 dependency from `Base Mods\Scripts\DYOM\DYOM v8.1` into `Documents\GTA San Andreas User Files\DYOM v8.1`. If that dependency source is missing, installation stops with a warning. It then replaces the entire `DSL` folder with the package contents, excluding metadata, README, and media files.

Accepted values: `MissionDsl`, `DSL`.

```json
{
  "type": "MissionDsl"
}
```

## Important behavior notes

- Type validation currently accepts `putinmodloader`, `replacing`, `putincleo`, `putingamefolder`, `putandreplace`, `putandreplaces`, `vehicleandskinandweapon`, `vehiclesandskinsandweapons`, `savesandmissions`, and `missiondsl` after normalization.
- `VehicleAndSkinsAndWeapons` normalizes to the single-asset value because it lacks the plural `Vehicles`; `VehiclesAndSkinsAndWeapons` remains the distinct multi-asset value.
- A missing or blank type defaults to `putinmodloader` when a `ModManifest` is constructed, but validation requires a string type in a non-empty manifest.
- `conflictCleanup` is parsed and stored, but current install flows do not execute its cleanup instructions automatically.

```

```
