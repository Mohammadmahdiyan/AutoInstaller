using System.Diagnostics;

namespace GtaSaModManager.Services;

public class GameService
{
    public static bool IsValidGameFolder(string? gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            return false;
        }

        var supportedExecutables = new[] { "gta_sa.exe", "GTA 5 FARSI.exe" };
        return supportedExecutables.Any(executable => File.Exists(Path.Combine(gamePath, executable)));
    }

    public static string GetModLoaderFolder(string gamePath)
    {
        return Path.Combine(gamePath, "modloader");
    }

    public static void EnsureModLoaderFolder(string gamePath)
    {
        Directory.CreateDirectory(GetModLoaderFolder(gamePath));
    }

    public static bool OpenGameFolder(string? gamePath)
    {
        if (!IsValidGameFolder(gamePath))
        {
            return false;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = gamePath,
            UseShellExecute = true,
            Verb = "open"
        });

        return true;
    }

    public static bool LaunchGame(string? gamePath)
    {
        if (!IsValidGameFolder(gamePath))
        {
            return false;
        }

        var gtaExecutable = Path.Combine(gamePath!, "gta_sa.exe");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = gtaExecutable,
                WorkingDirectory = gamePath,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
