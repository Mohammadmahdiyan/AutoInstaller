namespace GtaSaModManager.Controls.DeleteMods;

/// <summary>
/// One row in the delete window. Everything here is read from the MOD folder
/// (SourcePackagePath), never from the game folder.
/// </summary>
public sealed class DeleteModEntry
{
    public string Name { get; set; } = string.Empty;

    /// <summary>README text of the mod (may be empty).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Images, GIFs and videos found in the mod folder.</summary>
    public List<string> MediaFiles { get; set; } = new();

    /// <summary>Every installed file that will be removed. Its count drives the number next to the progress ring.</summary>
    public List<string> Files { get; set; } = new();

    /// <summary>Caller specific data (manifest entry, mixed part, ...).</summary>
    public object? Tag { get; set; }
}

/// <summary>
/// Performs the real deletion. Report the number of files removed so far through <paramref name="progress"/>.
/// Return true when the entry was removed.
/// </summary>
public delegate Task<bool> DeleteModHandler(DeleteModEntry entry, IProgress<int> progress);
