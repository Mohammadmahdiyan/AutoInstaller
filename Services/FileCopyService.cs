using System.Diagnostics;

namespace GtaSaModManager.Services;

public static class FileCopyService
{
    private const int BufferSize = 1024 * 1024;
    private const int MinimumReportIntervalMilliseconds = 24;
    private static readonly IProgress<int> NoProgress = new NullProgress();

    public static Task CopyFileAsync(
        string source,
        string destination,
        CancellationToken cancellationToken = default)
    {
        return CopyFileAsync(source, destination, NoProgress, cancellationToken);
    }

    public static async Task CopyFileAsync(
        string source,
        string destination,
        IProgress<int> percentProgress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(percentProgress);

        var sourcePath = Path.GetFullPath(source);
        var destinationPath = Path.GetFullPath(destination);
        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException();
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        await using var sourceStream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destinationStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var totalBytes = sourceStream.Length;
        if (totalBytes == 0)
        {
            percentProgress.Report(100);
            return;
        }

        var buffer = GC.AllocateUninitializedArray<byte>(BufferSize);
        long copiedBytes = 0;
        var lastReportedPercent = -1;
        var lastReportTimestamp = Stopwatch.GetTimestamp();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            copiedBytes += bytesRead;
            var percent = (int)Math.Min(100, copiedBytes * 100d / totalBytes);
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(lastReportTimestamp).TotalMilliseconds;
            if (percent != lastReportedPercent
                && (elapsedMilliseconds >= MinimumReportIntervalMilliseconds || percent == 100))
            {
                percentProgress.Report(percent);
                lastReportedPercent = percent;
                lastReportTimestamp = Stopwatch.GetTimestamp();
            }
        }

        if (lastReportedPercent != 100)
        {
            percentProgress.Report(100);
        }
    }

    private sealed class NullProgress : IProgress<int>
    {
        public void Report(int value)
        {
        }
    }
}