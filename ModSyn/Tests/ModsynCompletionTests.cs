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
    public void GetCompletions_FiltersRootPropertiesBySingleCharacterPrefix()
    {
        var completions = GetAtMarker("mod { i| }");

        CollectionAssert.AreEquivalent(
            new[] { "installThis", "installThese", "ignoreThis", "ignoreThese" },
            completions.Select(item => item.Label).ToArray());
    }

    [TestMethod]
    public void GetCompletions_TPrefixSuggestsOnlyType()
    {
        var completions = GetAtMarker("""
            mod {
                installThis: "models/example.dff"
                replacements: [{ source: "data/handling.cfg" target: "data/handling.cfg" }]
                ignoreThis: "Trainer.txt"
                t|
            }
            """);

        CollectionAssert.AreEqual(new[] { "type" }, completions.Select(item => item.Label).ToArray());
    }

    [TestMethod]
    public void GetCompletions_DoesNotRepeatExistingTypeKeyAfterCommaOrNewline()
    {
        var comma = GetAtMarker("mod { type: PutAndReplace, | }");
        var newline = GetAtMarker("mod {\n  type: PutAndReplace\n  |\n}");
        var expected = ModsynLanguageDefinition.RootProperties.Keys
            .Where(name => name != "type")
            .ToArray();

        CollectionAssert.AreEquivalent(expected, comma.Select(item => item.Label).ToArray());
        CollectionAssert.AreEquivalent(expected, newline.Select(item => item.Label).ToArray());
    }

    [TestMethod]
    public void GetCompletions_DoesNotRepeatAnyExistingKey()
    {
        var root = GetAtMarker("mod { type: PIM, installThis: \"cleo/main.cs\", | }");
        var requirement = GetAtMarker("mod { require: { checkThis: \"cleo.asi\", | } }");

        Assert.IsFalse(root.Any(item => item.Label is "type" or "installThis"));
        Assert.IsFalse(requirement.Any(item => item.Label == "checkThis"));
    }

    [TestMethod]
    public void GetCompletions_FiltersRootPropertiesByLongerPrefixes()
    {
        CollectionAssert.AreEquivalent(
            new[] { "installThis", "installThese" },
            GetAtEnd("mod { in").Select(item => item.Label).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "ignoreThis", "ignoreThese" },
            GetAtEnd("mod { ign").Select(item => item.Label).ToArray());
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
        CollectionAssert.AreEquivalent(
            expected.Where(value => value.StartsWith("Put", StringComparison.OrdinalIgnoreCase)).ToArray(),
            quotedValue.Select(item => item.Label).ToArray());
        Assert.IsTrue(identifierValue.All(item => item.Kind == ModsynCompletionKind.Type));
        Assert.IsTrue(identifierValue.All(item => !string.IsNullOrWhiteSpace(item.Description)));
    }

    [TestMethod]
    public void GetCompletions_PathValueContextDoesNotSuggestKeywords()
    {
        var context = ModsynCompletionContextDetector.Detect("mod { installThis: \"models\\fo", "mod { installThis: \"models\\fo".Length);
        var completions = GetAtEnd("mod { installThis: \"models\\fo");

        Assert.AreEqual(ModsynCompletionContext.PathValues, context);
        Assert.AreEqual(0, completions.Count);
    }

    [TestMethod]
    public void GetCompletions_PathContextUsesOnlyExplicitPathCandidates()
    {
        var source = "mod { installThis: \"models\\fo";
        var completions = ModsynCompletionService.GetCompletions(
            source,
            source.Length,
            new[] { "models\\foo.dff", "models\\bar.txd", "data\\handling.cfg" });

        CollectionAssert.AreEqual(new[] { "models\\foo.dff" }, completions.Select(item => item.Label).ToArray());
        Assert.AreEqual("\"models\\foo.dff\"", completions[0].InsertText);
        Assert.AreEqual(ModsynCompletionKind.Path, completions[0].Kind);
    }

    [TestMethod]
    public void GetCompletions_DocumentWordsAreNeverKeywordCandidates()
    {
        var source = "mod { installThis: \"models\\example.dff\" replacements: [{ source: \"data/handling.cfg\" target: \"data/handling.cfg\" }] i";
        var completions = GetAtEnd(source).Select(item => item.Label).ToHashSet(StringComparer.OrdinalIgnoreCase);

        CollectionAssert.AreEquivalent(
            new[] { "installThese", "ignoreThis", "ignoreThese" },
            completions.ToArray());
        Assert.IsFalse(new[] { "cfg", "data", "dff", "example", "handling", "models", "source", "target", "txd" }
            .Any(completions.Contains));
    }

    [TestMethod]
    public void GetCompletions_ColonSuggestsValuesForOnlyThatProperty()
    {
        var typeValues = GetAtEnd("mod { type: ");
        var backupValues = GetAtEnd("mod { type: Replacing backup: ");
        var requireValue = GetAtEnd("mod { require: ");

        Assert.IsTrue(typeValues.All(item => item.Kind == ModsynCompletionKind.Type));
        CollectionAssert.AreEquivalent(new[] { "all", "none", "some" }, backupValues.Select(item => item.Label).ToArray());
        CollectionAssert.AreEqual(new[] { "{" }, requireValue.Select(item => item.Label).ToArray());
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

    private static IReadOnlyList<ModsynCompletionItem> GetAtMarker(string markedSource)
    {
        var cursorOffset = markedSource.IndexOf('|');
        Assert.IsTrue(cursorOffset >= 0);
        return ModsynCompletionService.GetCompletions(markedSource.Remove(cursorOffset, 1), cursorOffset);
    }
}