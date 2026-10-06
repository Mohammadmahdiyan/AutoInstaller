using GtaSaModManager.Controls;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm
{
    private readonly List<string> _lastStep4FileNames = new();
    private double[] _step4FileWeights = Array.Empty<double>();
    private double _step4TotalWeight;
    private double _step4CompletedWeight;
    private string _step4ProgressTitle = string.Empty;
    private int _step4ProgressGeneration;
    private int _step4CurrentCopyIndex = -1;

    private void BeginStep4Progress(string title, IReadOnlyList<(string SourcePath, string RelativeName)> files)
    {
        _step4ProgressTitle = title;
        _lastStep4FileNames.Clear();
        _step4ProgressGeneration++;
        _step4CurrentCopyIndex = -1;
        _step4CompletedWeight = 0;
        _step4FileWeights = files.Select(file => GetStep4FileWeight(file.SourcePath)).ToArray();
        _step4TotalWeight = _step4FileWeights.Sum();
        _step4ProgressView?.Reset(title);
    }

    private async Task CopyStep4FileAsync(
        int index,
        string sourcePath,
        string destinationPath,
        string relativeName,
        CancellationToken cancellationToken = default)
    {
        var progressView = _step4ProgressView;
        if (progressView == null || progressView.IsDisposed)
        {
            await FileCopyService.CopyFileAsync(sourcePath, destinationPath, new Progress<int>(), cancellationToken);
            return;
        }

        var generation = _step4ProgressGeneration;
        var weight = (uint)index < (uint)_step4FileWeights.Length
            ? _step4FileWeights[index]
            : GetStep4FileWeight(sourcePath);
        var rowIndex = progressView.BeginFile(relativeName);
        _lastStep4FileNames.Add(relativeName);
        _step4CurrentCopyIndex = index;
        var lastOverallPercent = -1;
        var progress = new Progress<int>(percent =>
        {
            if (generation != _step4ProgressGeneration || _step4CurrentCopyIndex != index)
            {
                return;
            }

            progressView.ReportFileProgress(rowIndex, percent);
            var overallPercent = CalculateOverallProgress(_step4CompletedWeight + weight * Math.Clamp(percent, 0, 100) / 100d);
            if (overallPercent != lastOverallPercent)
            {
                lastOverallPercent = overallPercent;
                progressView.SetOverallProgress(overallPercent);
            }
        });

        await FileCopyService.CopyFileAsync(sourcePath, destinationPath, progress, cancellationToken);
        if (generation != _step4ProgressGeneration)
        {
            return;
        }

        _step4CurrentCopyIndex = -1;
        progressView.ReportFileProgress(rowIndex, 100);
        progressView.CompleteFile(rowIndex);
        _step4CompletedWeight += weight;
        progressView.SetOverallProgress(CalculateOverallProgress(_step4CompletedWeight));
    }

    private void CompleteStep4Progress(string message)
    {
        _step4CurrentCopyIndex = -1;
        _step4ProgressView?.Complete(message);
        if (_step4ProgressView != null)
        {
            _step4ProgressTitle = _step4ProgressView.TitleText;
        }
    }

    private void FailStep4Progress(string message)
    {
        _step4CurrentCopyIndex = -1;
        if (_step4ProgressView?.IsRunning == true)
        {
            _step4ProgressView.Fail(message);
        }
    }

    private void AddCompletedStep4File(string relativeName, int completedCount, int totalCount)
    {
        if (_step4ProgressView == null || _step4ProgressView.IsDisposed)
        {
            return;
        }

        var rowIndex = _step4ProgressView.BeginFile(relativeName);
        _lastStep4FileNames.Add(relativeName);
        _step4ProgressView.ReportFileProgress(rowIndex, 100);
        _step4ProgressView.SetOverallProgress(totalCount == 0
            ? 100
            : (int)Math.Round(completedCount * 100d / totalCount));
        _step4ProgressView.CompleteFile(rowIndex);
    }

    private async Task CopyDirectoryWithStep4ProgressAsync(
        string sourceDirectory,
        string destinationDirectory,
        IReadOnlyList<string> sourceFiles,
        int startIndex,
        IReadOnlyList<string> displayNames,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);
        for (var index = 0; index < sourceFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourcePath = sourceFiles[index];
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            await CopyStep4FileAsync(startIndex + index, sourcePath, destinationPath, displayNames[index], cancellationToken);
        }
    }

    private async Task<bool> UninstallInstalledFolderWithProgressAsync(
        string modName,
        string installedDestination,
        string gamePath,
        string modType)
    {
        var normalizedName = ModPackageService.NormalizeDisplayName(modName);
        var removedRecord = ModLoaderService.RemoveInstalledRecord(normalizedName, installedDestination);
        var files = Directory.Exists(installedDestination)
            ? Directory.GetFiles(installedDestination, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();
        var title = _localizationService.GetString("DeletingMod", "Deleting {0}");
        title = string.Format(title, modName);
        BeginStep4Progress(title, files
            .Select(path => (path, Path.GetRelativePath(installedDestination, path)))
            .ToList());

        try
        {
            for (var index = 0; index < files.Length; index++)
            {
                var filePath = files[index];
                File.Delete(filePath);
                AddCompletedStep4File(
                    Path.GetRelativePath(installedDestination, filePath),
                    index + 1,
                    files.Length);
                await Task.Yield();
            }

            var removed = ModLoaderService.TryUninstallInstalledMod(
                modName,
                installedDestination,
                gamePath,
                modType);
            removed |= removedRecord;
            if (removed)
            {
                CompleteStep4Progress(_localizationService.GetString("DeleteProgressComplete", "Deletion complete."));
            }
            else
            {
                FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            }
            return removed;
        }
        catch
        {
            FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            throw;
        }
    }

    private async Task<bool> RestoreReplacementInstallationsWithProgressAsync(string modName)
    {
        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_selectedGamePath));
        var gameRootPrefix = gameRoot + Path.DirectorySeparatorChar;
        var records = ModPackageService.LoadReplacementRecords()
            .Where(record => string.Equals(record.ModName, modName, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(record.GameFolder)
                && string.Equals(Path.GetFullPath(record.GameFolder), gameRoot, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var installationManifestPath = ModLoaderService.GetGameInstallationsManifestPath(gameRoot);
        var installationEntry = ModLoaderService.LoadInstallationManifest(installationManifestPath).Entries
            .LastOrDefault(entry => string.Equals(entry.ModId, modName, StringComparison.OrdinalIgnoreCase));
        if (records.Count == 0 && installationEntry == null)
        {
            return false;
        }

        var backupDirectories = BackupStorageService.GetModBackupDirectories(gameRoot, modName, records);
        var backupFileOperations = backupDirectories
            .SelectMany(directory => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(sourcePath =>
                {
                    var relativePath = Path.GetRelativePath(directory, sourcePath);
                    var destinationPath = Path.GetFullPath(Path.Combine(gameRoot, relativePath));
                    return (SourcePath: sourcePath, DestinationPath: destinationPath, RelativeName: relativePath);
                }))
            .Where(operation => operation.DestinationPath.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(operation => operation.DestinationPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var backupDestinationPaths = backupFileOperations
            .Select(operation => operation.DestinationPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var backupSourcePaths = backupFileOperations
            .Select(operation => Path.GetFullPath(operation.SourcePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var replacementPaths = records
            .Select(record => record.OriginalFilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Concat(backupDestinationPaths)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var installedFilesToDelete = ModLoaderService.GetUnreplacedInstalledFiles(
                installationEntry?.InstalledFiles ?? Enumerable.Empty<string>(),
                replacementPaths)
            .Where(path => path.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            .ToList();
        var historyFallbackOperations = records
            .Where(record => !string.IsNullOrWhiteSpace(record.OriginalFilePath)
                && record.OriginalFilePath.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase)
                && !backupDestinationPaths.Contains(Path.GetFullPath(record.OriginalFilePath))
                && (File.Exists(record.BackupFilePath)
                    ? !backupSourcePaths.Contains(Path.GetFullPath(record.BackupFilePath))
                    : File.Exists(record.OriginalFilePath)))
            .ToList();
        var title = string.Format(_localizationService.GetString("DeletingMod", "Deleting {0}"), modName);
        var progressFiles = installedFilesToDelete
            .Select(path => (SourcePath: path, RelativeName: Path.GetRelativePath(gameRoot, path)))
            .Concat(backupFileOperations
                .Select(operation => (SourcePath: operation.SourcePath, RelativeName: operation.RelativeName)))
            .Concat(historyFallbackOperations
            .Select(record =>
            {
                var backupExists = File.Exists(record.BackupFilePath);
                var sourcePath = backupExists ? record.BackupFilePath : record.OriginalFilePath;
                var relativeName = !string.IsNullOrWhiteSpace(record.GameFolder)
                    ? Path.GetRelativePath(record.GameFolder, record.OriginalFilePath)
                    : Path.GetFileName(record.OriginalFilePath);
                return (SourcePath: sourcePath, RelativeName: relativeName);
            }))
            .ToList();
        BeginStep4Progress(title, progressFiles);

        var restored = false;
        try
        {
            for (var index = 0; index < installedFilesToDelete.Count; index++)
            {
                var installedFile = installedFilesToDelete[index];
                if (!File.Exists(installedFile))
                {
                    continue;
                }

                File.Delete(installedFile);
                AddCompletedStep4File(
                    Path.GetRelativePath(gameRoot, installedFile),
                    index + 1,
                    Math.Max(1, progressFiles.Count));
                restored = true;
                await Task.Yield();
            }

            for (var index = 0; index < backupFileOperations.Count; index++)
            {
                var operation = backupFileOperations[index];
                var progressIndex = installedFilesToDelete.Count + index;
                await CopyStep4FileAsync(
                    progressIndex,
                    operation.SourcePath,
                    operation.DestinationPath,
                    operation.RelativeName);
                restored = true;
            }

            for (var index = 0; index < historyFallbackOperations.Count; index++)
            {
                var record = historyFallbackOperations[index];
                var relativeName = progressFiles[installedFilesToDelete.Count + backupFileOperations.Count + index].RelativeName;
                var progressIndex = installedFilesToDelete.Count + backupFileOperations.Count + index;
                if (File.Exists(record.BackupFilePath))
                {
                    await CopyStep4FileAsync(progressIndex, record.BackupFilePath, record.OriginalFilePath, relativeName);
                    restored = true;
                }
                else if (File.Exists(record.OriginalFilePath))
                {
                    File.Delete(record.OriginalFilePath);
                    AddCompletedStep4File(relativeName, progressIndex + 1, Math.Max(1, progressFiles.Count));
                    restored = true;
                }
            }

            foreach (var backupDirectory in backupDirectories)
            {
                if (Directory.Exists(backupDirectory))
                {
                    Directory.Delete(backupDirectory, recursive: true);
                }
            }

            var installationRecordRemoved = ModLoaderService.RemoveGameInstallationRecord(gameRoot, modName);

            var remaining = ModPackageService.LoadReplacementRecords();
            remaining.RemoveAll(record => string.Equals(record.ModName, modName, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(record.GameFolder)
                && string.Equals(Path.GetFullPath(record.GameFolder), gameRoot, StringComparison.OrdinalIgnoreCase));
            ModPackageService.SaveReplacementRecords(remaining);
            if (restored || installationRecordRemoved || records.Count > 0)
            {
                CompleteStep4Progress(_localizationService.GetString("DeleteProgressComplete", "Deletion complete."));
            }
            else
            {
                FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            }
            return restored || installationRecordRemoved || records.Count > 0;
        }
        catch
        {
            FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            throw;
        }
    }

    private async Task<bool> UninstallByModIdWithProgressAsync(
        string modId,
        string modType,
        string? installedDestination,
        string? gamePath)
    {
        var normalizedType = (modType ?? string.Empty).Trim().ToLowerInvariant();
        var userFilesInstallation = normalizedType == "missiondsl"
            ? ModLoaderService.FindUserFilesInstallation(gamePath ?? string.Empty, modId, "missiondsl")
            : (string.Empty, null);
        var manifestPath = normalizedType == "missiondsl"
            ? userFilesInstallation.Item1
            : normalizedType == "savesandmissions"
            ? ModLoaderService.GetUserFilesInstallationsManifestPath()
            : !string.IsNullOrWhiteSpace(gamePath)
                ? ModLoaderService.GetGameInstallationsManifestPath(gamePath)
                : string.Empty;
        var entry = normalizedType == "missiondsl"
            ? userFilesInstallation.Item2
            : ModLoaderService.LoadInstallationManifest(manifestPath).Entries
                .FirstOrDefault(item => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase));
        var files = (entry?.InstalledFiles ?? new List<string>()).Where(File.Exists).ToList();
        var title = string.Format(_localizationService.GetString("DeletingMod", "Deleting {0}"), modId);
        BeginStep4Progress(title, files
            .Select(path => (path, Path.GetFileName(path)))
            .ToList());

        try
        {
            for (var index = 0; index < files.Count; index++)
            {
                File.Delete(files[index]);
                AddCompletedStep4File(Path.GetFileName(files[index]), index + 1, files.Count);
                await Task.Yield();
            }

            var removed = ModLoaderService.TryUninstallByModId(modId, modType ?? string.Empty, installedDestination, gamePath);
            if (removed)
            {
                CompleteStep4Progress(_localizationService.GetString("DeleteProgressComplete", "Deletion complete."));
            }
            else
            {
                FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            }
            return removed;
        }
        catch
        {
            FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            throw;
        }
    }

    private static double GetStep4FileWeight(string path)
    {
        try
        {
            return Math.Max(1d, new FileInfo(path).Length);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return 1d;
        }
    }

    private int CalculateOverallProgress(double completedWeight)
    {
        if (_step4TotalWeight <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(completedWeight * 100d / _step4TotalWeight), 0, 100);
    }
}