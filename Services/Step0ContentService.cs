using System.Text;
using System.Text.RegularExpressions;

namespace GtaSaModManager.Services;

public sealed record Step0Section(string Title, string Body, string SourceFile);

public sealed class Step0ContentService
{
    public IReadOnlyList<Step0Section> Load(string languageCode)
    {
        var languageFolder = string.Equals(languageCode, "Persian", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(AppContext.BaseDirectory, "Forms", "Step0", "FA")
            : Path.Combine(AppContext.BaseDirectory, "Forms", "Step0", "EN");
        var sections = LoadFolder(languageFolder);

        if (sections.Count == 0 && string.Equals(languageCode, "Persian", StringComparison.OrdinalIgnoreCase))
        {
            sections = LoadFolder(Path.Combine(AppContext.BaseDirectory, "Forms", "Step0", "EN"));
        }

        return sections;
    }

    private static List<Step0Section> LoadFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return new List<Step0Section>();
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(folderPath, "*.md", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new List<Step0Section>();
        }

        var sections = new List<Step0Section>();
        foreach (var file in files)
        {
            string content;
            try
            {
                content = File.ReadAllText(file, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var firstContentLine = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
            if (firstContentLine < 0)
            {
                continue;
            }

            var heading = lines[firstContentLine];
            string title;
            string body;
            if (heading.StartsWith("#", StringComparison.Ordinal))
            {
                title = heading.TrimStart('#').Trim();
                body = string.Join(Environment.NewLine, lines.Skip(firstContentLine + 1));
            }
            else
            {
                title = Regex.Replace(Path.GetFileNameWithoutExtension(file), @"^\d+[\s._-]*", string.Empty).Trim();
                body = content;
            }

            sections.Add(new Step0Section(title, body, file));
        }

        return sections;
    }
}
