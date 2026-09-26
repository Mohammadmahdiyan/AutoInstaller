using System.Text.Json;

namespace GtaSaModManager.Services;

public sealed class BackupStoragePlan
{
    public bool HasBackup { get; init; }
    public string BackupRoot { get; init; } = string.Empty;
    public string GameInstanceId { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}

public static class BackupStorageService
{
    private const string ManagerDirectoryName = ".ModManager";
    private const string IdentityFileName = "BackupStorageIdentity.json";
    private const string LegacyIdentityFileName = ".zGtaSaModManager.json";
    private const string BackupFolderName = "zBackupFiles";
    private const string ExternalBackupBase = @"C:\Program Files (x86)\GTA San Andreas\zBackupFiles";

    public static BackupStoragePlan CreatePlan(
        string gameFolder,
        string modName,
        IEnumerable<string> originalFiles,
        string? alternativeRoot = null)
    {
        var files = originalFiles.Where(File.Exists).ToList();
        var actualRequiredBytes = GetTotalSize(files);
        var conservativeRequiredBytes = MultiplySafely(actualRequiredBytes, 3);
        var gameInstanceId = GetOrCreateGameInstanceId(gameFolder);
        var gameBackupRoot = Path.Combine(gameFolder, BackupFolderName);

        if (HasEnoughSpace(gameFolder, actualRequiredBytes))
        {
            return CreateAvailablePlan(gameBackupRoot, gameInstanceId);
        }

        if (HasEnoughSpace(ExternalBackupBase, conservativeRequiredBytes))
        {
            return CreateAvailablePlan(Path.Combine(ExternalBackupBase, gameInstanceId), gameInstanceId);
        }

        if (!string.IsNullOrWhiteSpace(alternativeRoot) && HasEnoughSpace(alternativeRoot, conservativeRequiredBytes))
        {
            return CreateAvailablePlan(Path.Combine(alternativeRoot, BackupFolderName, gameInstanceId), gameInstanceId);
        }

        return new BackupStoragePlan
        {
            GameInstanceId = gameInstanceId,
            ErrorMessage = "There is not enough free space on the game drive or drive C for the required backup."
        };
    }

    private static BackupStoragePlan CreateAvailablePlan(string backupRoot, string gameInstanceId)
    {
        Directory.CreateDirectory(backupRoot);
        SetHidden(backupRoot);
        return new BackupStoragePlan
        {
            HasBackup = true,
            BackupRoot = backupRoot,
            GameInstanceId = gameInstanceId
        };
    }

    private static string GetOrCreateGameInstanceId(string gameFolder)
    {
        var managerDirectory = Path.Combine(gameFolder, ManagerDirectoryName);
        Directory.CreateDirectory(managerDirectory);
        SetHidden(managerDirectory);

        var identityPath = Path.Combine(managerDirectory, IdentityFileName);
        var legacyIdentityPath = Path.Combine(gameFolder, LegacyIdentityFileName);
        if (!File.Exists(identityPath) && File.Exists(legacyIdentityPath))
        {
            File.Copy(legacyIdentityPath, identityPath);
            SetHidden(identityPath);
        }

        if (File.Exists(identityPath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(identityPath));
                if (document.RootElement.TryGetProperty("gameInstanceId", out var id)
                    && id.ValueKind == JsonValueKind.String
                    && Guid.TryParse(id.GetString(), out var existingId))
                {
                    return existingId.ToString("D");
                }
            }
            catch (JsonException)
            {
                // Create a fresh identity below when the existing file is invalid.
            }
        }

        var gameInstanceId = Guid.NewGuid().ToString("D");
        var identity = new
        {
            schemaVersion = 1,
            gameInstanceId
        };
        File.WriteAllText(identityPath, JsonSerializer.Serialize(identity, new JsonSerializerOptions { WriteIndented = true }));
        SetHidden(identityPath);
        return gameInstanceId;
    }

    private static long GetTotalSize(IEnumerable<string> files)
    {
        long total = 0;
        foreach (var file in files)
        {
            var length = new FileInfo(file).Length;
            total = total > long.MaxValue - length ? long.MaxValue : total + length;
        }

        return total;
    }

    private static long MultiplySafely(long value, int multiplier)
    {
        return value > long.MaxValue / multiplier ? long.MaxValue : value * multiplier;
    }

    private static bool HasEnoughSpace(string path, long requiredBytes)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            return new DriveInfo(root).AvailableFreeSpace >= requiredBytes;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void SetHidden(string path)
    {
        try
        {
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        }
        catch (IOException)
        {
            // Hidden is best effort on filesystems that do not support Windows attributes.
        }
        catch (UnauthorizedAccessException)
        {
            // Hidden is best effort when attributes cannot be changed.
        }
    }
}
