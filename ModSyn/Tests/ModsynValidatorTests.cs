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
        AssertNormalizedType("VSS", "VehicleAndSkinAndWeapon");
        AssertNormalizedType("VehicleAndSkinsAndWeapons", "VehicleAndSkinAndWeapon");
        AssertNormalizedType("VehiclesAndSkinsAndWeapons", "VehiclesAndSkinsAndWeapons");
        AssertNormalizedType("SAM", "SavesAndMissions");
        AssertNormalizedType("DSL", "MissionDsl");
    }

    [TestMethod]
    public void Validate_DefaultsMissingAndEmptyTypeToPutInModLoader()
    {
        Assert.AreEqual("PutInModLoader", Validate("mod {}").NormalizedType);
        Assert.AreEqual("PutInModLoader", Validate("mod { type: \"\" }").NormalizedType);
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
    public void Validate_RequiresExactThisAndTheseValueShapes()
    {
        var result = Validate("mod {\n  installThis: [\"one\"]\n  ignoreThese: [\"ok\" false]\n  deleteThese: \"one\"\n}");

        Assert.AreEqual(3, result.Errors.Count);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(2, 16), result.Errors[0].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(3, 22), result.Errors[1].Location);
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(4, 16), result.Errors[2].Location);
    }

    [TestMethod]
    public void Validate_AppliesBackupDefaultsAndTypeRestrictions()
    {
        Assert.IsTrue(Validate("mod { type: Replacing }").BackupEnabled);
        Assert.IsTrue(Validate("mod { type: Replacing backup: null }").BackupEnabled);
        Assert.IsFalse(Validate("mod { type: PutAndReplace backup: false }").BackupEnabled);

        var invalid = Validate("mod { type: PutInModLoader backupThis: \"file.dat\" }");
        Assert.AreEqual(1, invalid.Errors.Count);
        StringAssert.Contains(invalid.Errors[0].Message, "only valid for types");
        Assert.AreEqual(new GtaSaModManager.Modsyn.Lexer.ModsynSourceLocation(1, 28), invalid.Errors[0].Location);
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
        var result = Validate("mod {\n  type: PutAndReplace\n  replacements: [\"same/path\" { source: \"from\" target: \"to\" }]\n}");

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
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
        var result = Validate($"mod {{ type: \"{value}\" }}");

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.AreEqual(expected, result.NormalizedType);
    }

    private static ModsynValidationResult Validate(string source)
    {
        return ModsynValidator.Validate(ModsynParser.Parse(source));
    }
}