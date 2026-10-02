using GtaSaModManager.Modsyn.Completion;
using GtaSaModManager.Modsyn.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class ModsynCompletionTests
{
    [TestMethod]
    public void GetCompletions_RootContextUsesCentralRootPropertyMetadata()
    {
        var completions = GetAtEnd("mod { ");

        CollectionAssert.AreEquivalent(
            ModsynLanguageDefinition.RootProperties.Keys.ToArray(),
            completions.Select(item => item.Label).ToArray());
        Assert.IsTrue(completions.All(item => item.Kind == ModsynCompletionKind.Property));
        Assert.IsTrue(completions.All(item => !string.IsNullOrWhiteSpace(item.Description)));
    }

    [TestMethod]
    public void GetCompletions_RequirementContextIncludesNestedAndArrayRequirements()
    {
        var direct = GetAtEnd("mod { require: { ");
        var arrayItem = GetAtEnd("mod { requires: [{ ");
        var nested = GetAtEnd("mod { require: { require: { ");

        CollectionAssert.AreEquivalent(
            ModsynLanguageDefinition.RequirementProperties.Keys.ToArray(),
            direct.Select(item => item.Label).ToArray());
        CollectionAssert.AreEquivalent(
            ModsynLanguageDefinition.RequirementProperties.Keys.ToArray(),
            arrayItem.Select(item => item.Label).ToArray());
        CollectionAssert.AreEquivalent(
            ModsynLanguageDefinition.RequirementProperties.Keys.ToArray(),
            nested.Select(item => item.Label).ToArray());
    }

    [TestMethod]
    public void GetCompletions_TypeContextProvidesCanonicalTypesAliasesAndDescriptions()
    {
        var identifierValue = GetAtEnd("mod { type: ");
        var quotedValue = GetAtEnd("mod { type: \"Put");
        var expected = ModsynLanguageDefinition.Types
            .SelectMany(type => new[] { type.Name }.Concat(type.Aliases))
            .ToArray();

        CollectionAssert.AreEquivalent(expected, identifierValue.Select(item => item.Label).ToArray());
        CollectionAssert.AreEquivalent(expected, quotedValue.Select(item => item.Label).ToArray());
        Assert.IsTrue(identifierValue.All(item => item.Kind == ModsynCompletionKind.Type));
        Assert.IsTrue(identifierValue.All(item => !string.IsNullOrWhiteSpace(item.Description)));
    }

    [TestMethod]
    public void GetCompletions_DoesNotSuggestLegacyOrUnsupportedProperties()
    {
        var labels = GetAtEnd("mod { ").Select(item => item.Label)
            .Concat(GetAtEnd("mod { require: { ").Select(item => item.Label))
            .ToHashSet(StringComparer.Ordinal);
        var unsupported = new[]
        {
            "conflict" + "Cleanup", "checkFile", "checkFiles", "checkFolder", "checkFolders",
            "InstallFile", "InstallFiles", "InstallFolder", "InstallFolders",
            "IgnoreFile", "IgnoreFiles", "IgnoreFolder", "IgnoreFolders",
            "backupFile", "backupFiles", "backupFolder", "backupFolders",
            "dontBackupFile", "dontBackupFiles", "dontBackupFolder", "dontBackupFolders"
        };

        Assert.IsFalse(unsupported.Any(labels.Contains));
    }

    [TestMethod]
    public void Detect_ReturnsNoneOutsideSupportedPropertyContexts()
    {
        Assert.AreEqual(ModsynCompletionContext.None, ModsynCompletionContextDetector.Detect("", 0));
        Assert.AreEqual(
            ModsynCompletionContext.None,
            ModsynCompletionContextDetector.Detect("mod { replacements: [{ ", "mod { replacements: [{ ".Length));
        Assert.AreEqual(
            ModsynCompletionContext.RootProperties,
            ModsynCompletionContextDetector.Detect("mod { type: PutInCleo, ", "mod { type: PutInCleo, ".Length));
    }

    private static IReadOnlyList<ModsynCompletionItem> GetAtEnd(string source)
    {
        return ModsynCompletionService.GetCompletions(source, source.Length);
    }
}