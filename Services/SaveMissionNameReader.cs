using System.Text;

namespace GtaSaModManager.Services;

public static class SaveMissionNameReader
{
    private const int HeaderLength = 128;
    private const int NameOffset = 9;
    private static readonly byte[] GtaSaveSignature = Encoding.ASCII.GetBytes("BLOCK");
    private static readonly byte[] DyomMissionSignature = { 0xFC, 0xFF, 0xFF, 0xFF };

    public static string? TryReadGtaSaveName(string path)
    {
        return TryReadHeaderName(path, GtaSaveSignature, NameOffset, versionOffset: 8);
    }

    public static string? TryReadDyomMissionName(string path)
    {
        return TryReadHeaderName(path, DyomMissionSignature, DyomMissionSignature.Length);
    }

    private static string? TryReadHeaderName(
        string path,
        byte[] signature,
        int nameOffset,
        int? versionOffset = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var header = new byte[HeaderLength];
            var bytesRead = 0;
            while (bytesRead < header.Length)
            {
                var read = stream.Read(header, bytesRead, header.Length - bytesRead);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            if (bytesRead <= nameOffset
                || bytesRead < signature.Length
                || !header.AsSpan(0, signature.Length).SequenceEqual(signature)
                || versionOffset.HasValue
                    && header[versionOffset.Value] is < (byte)'0' or > (byte)'9')
            {
                return null;
            }

            var nameEnd = Array.IndexOf(header, (byte)0, nameOffset, bytesRead - nameOffset);
            if (nameEnd <= nameOffset)
            {
                return null;
            }

            for (var index = nameOffset; index < nameEnd; index++)
            {
                if (header[index] is < 0x20 or > 0x7E)
                {
                    return null;
                }
            }

            var name = Encoding.ASCII.GetString(header, nameOffset, nameEnd - nameOffset).Trim();
            return name.Length > 0 && name.Any(char.IsLetter) ? name : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
