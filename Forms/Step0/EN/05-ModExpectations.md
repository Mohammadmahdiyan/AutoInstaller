# Mod Package Expectations

Select a package folder that contains the files to install and, when used, one top-level manifest. Manifest lookup checks these names in order:

1. `mod.json`
2. `config.json`
3. `<package folder name>.json`

For vehicle, skin, and weapon model packages, `.dff` and `.txd` files are identified by their base names. A matching pair has the same base name, such as `infernus.dff` and `infernus.txd`. Multi-model packages group files by matching base name; files with different names are not paired.

A `README.txt` or `README.md` can describe the mod. Image and video files may be used as previews; the package installer filters supported media and manifest/README files from installable entries as appropriate. CLEO or other scripted packages may use manifest file/folder lists to select exactly what is installed.
