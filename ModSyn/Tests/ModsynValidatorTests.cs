using GtaSaModManager.Modsyn.Parser;
using GtaSaModManager.Modsyn.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class ModsynValidatorTests
{
    [TestMethod]
    public void LanguageDefinition_ContainsOnlySupportedRootProperties()
    {
        var expected = new[]
        {
            "type", "require", "requires", "deleteThis", "deleteThese", "replacements",
            "backup", "backupThis", "backupThese", "dontBackupThis", "dontBackupThese",
            "installThis", "installThese", "ignoreThis", "ignoreThese"
        };

        CollectionAssert.AreEquivalent(expected, ModsynLanguageDefinition.RootProperties.Keys.ToArray());
    }

    [TestMethod]
    public void Validate_NormalizesAliasesAndTypeFormatting()
    {
        AssertNormalizedType("pUt-In_MoD lOaDeR", "PutInModLoader");
        AssertNormalizedType("PIM", "PutInModLoader");
        AssertNormalizedType("RIP", "Replacing");
        AssertNormalizedType("PIC", "PutInCleo");
        AssertNormalizedType("PGF", "PutInGameFolder");
        AssertNormalizedType("PAR", "PutAndReplace");
        AssertNormalizedType("PRS", "PutAndReplaces");
        AssertNormalizedType("VSW", "VehicleAndSkinAndWeapon");
        AssertNormalizedType("VSS", "VehiclesAndSkinsAndWeapons");
        AssertNormalizedType("VehicleAndSkinsAndWeapons", "VehiclesAndSkinsAndWeapons");
        AssertNormalizedType("VehiclesAndSkinsAndWeapons", "VehiclesAndSkinsAndWeapons");
        AssertNormalizedType("SaveAndMission", "SaveAndMission");
        AssertNormalizedType("SavesAndMissions", "SavesAndMissions");
        AssertNormalizedType("SAM", "SaveAndMission");
        AssertNormalizedType("SMS", "SavesAndMissions");
        AssertNormalizedType("DSL", "MissionDsl");
    }

    [TestMethod]
    public void Validate_DefaultsMissingTypeToPutInModLoaderAndRejectsEmptyType()
    {
        Assert.AreEqual("PutInModLoader", Validate("mod {}").NormalizedType);
        var emptyType = Validate("mod { type: \"\" }");
        Assert.AreEqual(1, emptyType.Errors.Count);
        StringAssert.Contains(emptyType.Errors[0].Message, "Unsupported package type");
    }

    [TestMethod]
    public void Validate_RejectsUnknownRootAndRequirementPropertiesWithLocations()
    {
        var result = Validate("mod {\n  mystery: true\n  require: {\n    unknown: false\n  }\n}");

        Assert.AreEqual(2, result.Errors.Count);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(2, 3), result.Errors[0].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(4, 5), result.Errors[1].Location);
        StringAssert.Contains(result.Errors[0].Message, "Unknown root-level property 'mystery'");
        StringAssert.Contains(result.Errors[1].Message, "Unknown requirement property 'unknown'");
    }

    [TestMethod]
    public void Validate_SuggestsSimilarPropertyNamesInEachScope()
    {
        var result = Validate("mod { instalThese: [] require: { checkThes: [\"cleo.asi\"] } }");

        Assert.AreEqual(2, result.Errors.Count);
        StringAssert.Contains(result.Errors[0].Message, "Did you mean 'installThese'?");
        StringAssert.Contains(result.Errors[1].Message, "Did you mean 'checkThese'?");
    }

    [TestMethod]
    public void Validate_RejectsDuplicatePropertiesInEachScope()
    {
        var result = Validate("mod { type: PIM type: DSL require: { reqPath: \"one\" reqPath: \"two\" } replacements: [{ source: \"one\" source: \"two\" target: \"target\" }] }");

        Assert.AreEqual(3, result.Errors.Count);
        StringAssert.Contains(result.Errors[0].Message, "Duplicate root-level property 'type'");
        StringAssert.Contains(result.Errors[1].Message, "Duplicate requirement property 'reqPath'");
        StringAssert.Contains(result.Errors[2].Message, "Duplicate replacement property 'source'");
    }

    [TestMethod]
    public void Validate_RequiresExactThisAndTheseValueShapes()
    {
        var result = Validate("mod {\n  installThis: [\"one\"]\n  ignoreThese: [\"ok\" false]\n  deleteThese: \"one\"\n}");

        Assert.AreEqual(3, result.Errors.Count);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(2, 16), result.Errors[0].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(3, 22), result.Errors[1].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(4, 16), result.Errors[2].Location);
    }

    [TestMethod]
    public void Validate_ResolvesBackupModesAndRequiresSomeForSelectors()
    {
        Assert.AreEqual(ModsynBackupMode.All, Validate("mod { type: Replacing }").BackupMode);
        Assert.AreEqual(ModsynBackupMode.All, Validate("mod { type: Replacing backup: all }").BackupMode);
        Assert.AreEqual(ModsynBackupMode.None, Validate("mod { type: Replacing backup: none }").BackupMode);
        Assert.AreEqual(
            ModsynBackupMode.Some,
            Validate("mod { type: Replacing backup: some backupThis: \"data/file.dat\" }").BackupMode);

        var selectorWithoutSome = Validate("mod { type: Replacing backupThese: [\"data/file.dat\"] }");
        Assert.IsTrue(selectorWithoutSome.Errors.Any(error => error.Message.Contains("requires backup: some", StringComparison.Ordinal)));

        var invalid = Validate("mod { type: PutInModLoader backupThis: \"file.dat\" }");
        Assert.IsTrue(invalid.Errors.Any(error => error.Message.Contains("only valid for types", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Validate_RequiresSelectorsForSomeAndRejectsThemForAllOrNone()
    {
        var missingSelection = Validate("mod { type: Replacing backup: some }");
        var validSome = Validate("mod { type: Replacing backup: some backupThese: [\"one\" \"two\"] }");
        var emptySelection = Validate("mod { type: Replacing backup: some backupThese: [] }");
        var selectorWithAll = Validate("mod { type: Replacing backup: all backupThis: \"one\" }");
        var selectorWithNone = Validate("mod { type: Replacing backup: none dontBackupThis: \"cache\" }");

        Assert.IsFalse(missingSelection.IsValid);
        Assert.IsTrue(missingSelection.Errors.Any(error => error.Message.Contains("backup: some requires", StringComparison.Ordinal)));
        Assert.IsTrue(validSome.IsValid, string.Join(Environment.NewLine, validSome.Errors));
        Assert.IsFalse(emptySelection.IsValid);
        Assert.IsTrue(emptySelection.Errors.Any(error => error.Message.Contains("requires at least one string path", StringComparison.Ordinal)));
        Assert.IsFalse(selectorWithAll.IsValid);
        Assert.IsFalse(selectorWithNone.IsValid);
    }

    [TestMethod]
    public void Validate_RequiresBackupSelectorValuesAndWarnsForSingleThesePath()
    {
        var emptyThis = Validate("mod { type: Replacing backup: some backupThis: \"\" }");
        var emptyTheseItem = Validate("mod { type: Replacing backup: some dontBackupThese: [\"\"] }");
        var singletonThese = Validate("mod { type: Replacing backup: some backupThese: [\"data/file.dat\"] }");
        var multipleThese = Validate("mod { type: Replacing backup: some backupThese: [\"one\" \"two\"] }");

        Assert.IsFalse(emptyThis.IsValid);
        StringAssert.Contains(emptyThis.Errors[0].Message, "requires a non-empty string path");
        Assert.IsFalse(emptyTheseItem.IsValid);
        StringAssert.Contains(emptyTheseItem.Errors[0].Message, "cannot contain an empty string path");
        Assert.IsTrue(singletonThese.IsValid, string.Join(Environment.NewLine, singletonThese.Errors));
        Assert.AreEqual(1, singletonThese.Warnings.Count);
        StringAssert.Contains(singletonThese.Warnings[0].Message, "use 'backupThis' instead");
        Assert.IsTrue(multipleThese.IsValid, string.Join(Environment.NewLine, multipleThese.Errors));
        Assert.AreEqual(0, multipleThese.Warnings.Count);
    }

    [TestMethod]
    public void Validate_ResolvesRequirementFallbacksAndAddressPriority()
    {
        var result = Validate("mod {\n  require: {\n    checkThese: [\"outer-a\" \"outer-b\"]\n    reqPath: \"outer-address\"\n    require: {\n      checkThis: \"nested-check\"\n      checkThese: [\"nested-only\"]\n      reqAddress: \"nested-address\"\n      require: { reqPath: \"deep-address\" }\n    }\n  }\n  requires: [{ reqAddress: \"primary\" reqPath: \"fallback\" }]\n}");

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.AreEqual(2, result.Requirements.Count);
        Assert.AreEqual("nested-check", result.Requirements[0].CheckThis);
        CollectionAssert.AreEqual(new[] { "outer-a", "outer-b" }, result.Requirements[0].CheckThese.ToArray());
        Assert.AreEqual("outer-address", result.Requirements[0].RequestAddress);
        Assert.AreEqual("primary", result.Requirements[1].RequestAddress);
    }

    [TestMethod]
    public void Validate_UsesNestedRequirementValuesWhenOuterPathsAreEmpty()
    {
        var result = Validate("mod { require: { checkThis: \"\" checkThese: [] require: { checkThis: \"nested-check\" checkThese: [\"nested-path\"] } } }");

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.AreEqual("nested-check", result.Requirements[0].CheckThis);
        CollectionAssert.AreEqual(new[] { "nested-path" }, result.Requirements[0].CheckThese.ToArray());
    }

    [TestMethod]
    public void Validate_AcceptsReplacementObjectsAndStringShorthand()
    {
        var result = Validate("mod {\n  type: PutAndReplace\n  backup: some\n  backupThis: \"data\"\n  replacements: [\"same/path\" { source: \"from\" target: \"to\" }]\n}");

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [TestMethod]
    public void Validate_PutAndReplaceRequiresBackupSelectorsButPutAndReplacesDoesNot()
    {
        var missingSelector = Validate("mod { type: PutAndReplace }");
        var selectedBackup = Validate("mod { type: PutAndReplace backup: some backupThis: \"models\" }");
        var automaticBackup = Validate("mod { type: PutAndReplaces }");

        Assert.IsFalse(missingSelector.IsValid);
        Assert.IsTrue(missingSelector.Errors.Any(error => error.Message.Contains("PutAndReplace requires at least one backup selector", StringComparison.Ordinal)));
        Assert.IsTrue(selectedBackup.IsValid, string.Join(Environment.NewLine, selectedBackup.Errors));
        Assert.IsTrue(automaticBackup.IsValid, string.Join(Environment.NewLine, automaticBackup.Errors));
    }

    [TestMethod]
    public void Validate_PutAndReplacesAllowsAutomaticAndSelectiveBackups()
    {
        var automaticBackup = Validate("mod { type: PutAndReplaces }");
        var selectiveBackup = Validate("mod { type: PutAndReplaces backup: some dontBackupThese: [\"data\\\\cache\"] }");

        Assert.IsTrue(automaticBackup.IsValid, string.Join(Environment.NewLine, automaticBackup.Errors));
        Assert.IsTrue(selectiveBackup.IsValid, string.Join(Environment.NewLine, selectiveBackup.Errors));
    }

    [TestMethod]
    public void Validate_ReportsUnknownTypesAndMalformedReplacementObjects()
    {
        var result = Validate("mod {\n  type: NotAType\n  replacements: [{ source: \"from\" }]\n}");

        Assert.AreEqual(2, result.Errors.Count);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(2, 9), result.Errors[0].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(3, 18), result.Errors[1].Location);
        StringAssert.Contains(result.Errors[0].Message, "Unsupported package type");
        StringAssert.Contains(result.Errors[1].Message, "requires a 'target' string property");
    }

    [TestMethod]
    public void Validate_RejectsInvalidBackupAndRequirementValueKinds()
    {
        var result = Validate("mod {\n  type: Replacing\n  backup: \"yes\"\n  require: { checkThese: [\"valid\" true] }\n}");

        Assert.AreEqual(2, result.Errors.Count);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(3, 11), result.Errors[0].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(4, 35), result.Errors[1].Location);
    }

    private static void AssertNormalizedType(string value, string expected)
    {
        var selectors = expected == "PutAndReplace" ? " backup: some backupThis: \"file.dat\"" : string.Empty;
        var result = Validate($"mod {{ type: \"{value}\"{selectors} }}");

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.AreEqual(expected, result.NormalizedType);
    }

    private static ModsynValidationResult Validate(string source)
    {
        return ModsynValidator.Validate(ModsynParser.Parse(source));
    }
}