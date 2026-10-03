using GtaSaModManager.Modsyn.Ast;
using GtaSaModManager.Modsyn.Lexer;
using GtaSaModManager.Modsyn.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class ModsynParserTests
{
    [TestMethod]
    public void Parse_BasicModDocumentWithProperties()
    {
        var document = Parse("mod {\n  type: PutInModLoader\n  installThis: \"models/example.dff\"\n  ignoreThis: \"readme.txt\"\n}");

        Assert.AreEqual(new ModsynSourceLocation(1, 1), document.Location);
        Assert.AreEqual(3, document.Body.Properties.Count);
        Assert.AreEqual("type", document.Body.Properties[0].Name);
        Assert.AreEqual("installThis", document.Body.Properties[1].Name);
        Assert.AreEqual("ignoreThis", document.Body.Properties[2].Name);
        Assert.IsInstanceOfType<ModsynIdentifierNode>(document.Body.Properties[0].Value);
        Assert.AreEqual("PutInModLoader", ((ModsynIdentifierNode)document.Body.Properties[0].Value).Name);
        Assert.AreEqual("models/example.dff", ((ModsynStringNode)document.Body.Properties[1].Value).Value);
    }

    [TestMethod]
    public void Parse_AcceptsCommaSeparatedObjectProperties()
    {
        var document = Parse("mod { type: PutAndReplace, backup: true, replacements: [{ source: \"data/handling.cfg\", target: \"data/handling.cfg\" }] }");
        var validation = GtaSaModManager.Modsyn.Validation.ModsynValidator.Validate(document);

        Assert.AreEqual(3, document.Body.Properties.Count);
        Assert.IsTrue(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
    }

    [TestMethod]
    public void Parse_RecognizesStringsIdentifiersBooleansAndNull()
    {
        var document = Parse("mod {\n  text: \"value\"\n  name: SomeIdentifier\n  enabled: true\n  disabled: false\n  optional: null\n}");
        var properties = document.Body.Properties;

        Assert.IsInstanceOfType<ModsynStringNode>(properties[0].Value);
        Assert.IsInstanceOfType<ModsynIdentifierNode>(properties[1].Value);
        Assert.AreEqual("SomeIdentifier", ((ModsynIdentifierNode)properties[1].Value).Name);
        Assert.AreEqual(true, ((ModsynBooleanNode)properties[2].Value).Value);
        Assert.AreEqual(false, ((ModsynBooleanNode)properties[3].Value).Value);
        Assert.IsInstanceOfType<ModsynNullNode>(properties[4].Value);
    }

    [TestMethod]
    public void Parse_AcceptsArraysWithAndWithoutCommas()
    {
        var document = Parse("mod {\n  spaced: [\n    \"one\"\n    \"two\"\n  ]\n  commaSeparated: [\"one\", \"two\"]\n}");

        var spaced = (ModsynArrayNode)document.Body.Properties[0].Value;
        var commaSeparated = (ModsynArrayNode)document.Body.Properties[1].Value;
        Assert.AreEqual(2, spaced.Items.Count);
        Assert.AreEqual(2, commaSeparated.Items.Count);
        Assert.AreEqual("one", ((ModsynStringNode)spaced.Items[0]).Value);
        Assert.AreEqual("two", ((ModsynStringNode)commaSeparated.Items[1]).Value);
    }

    [TestMethod]
    public void Parse_RecursivelyParsesNestedObjectsAndArrays()
    {
        var document = Parse("mod {\n  config: {\n    files: [\n      \"one\"\n      { nested: [true null] }\n    ]\n  }\n}");

        var config = (ModsynObjectNode)document.Body.Properties[0].Value;
        var files = (ModsynArrayNode)config.Properties[0].Value;
        var nestedObject = (ModsynObjectNode)files.Items[1];
        var nestedArray = (ModsynArrayNode)nestedObject.Properties[0].Value;

        Assert.AreEqual("files", config.Properties[0].Name);
        Assert.AreEqual(2, files.Items.Count);
        Assert.AreEqual(2, nestedArray.Items.Count);
        Assert.IsInstanceOfType<ModsynBooleanNode>(nestedArray.Items[0]);
        Assert.IsInstanceOfType<ModsynNullNode>(nestedArray.Items[1]);
    }

    [TestMethod]
    public void Parse_RetainsLocationsForNestedAstNodes()
    {
        var document = Parse("mod {\n  config: {\n    files: [\n      \"one\"\n    ]\n  }\n}");
        var configProperty = document.Body.Properties[0];
        var config = (ModsynObjectNode)configProperty.Value;
        var filesArray = (ModsynArrayNode)config.Properties[0].Value;
        var stringNode = (ModsynStringNode)filesArray.Items[0];

        Assert.AreEqual(new ModsynSourceLocation(2, 3), configProperty.Location);
        Assert.AreEqual(new ModsynSourceLocation(2, 11), config.Location);
        Assert.AreEqual(new ModsynSourceLocation(3, 12), filesArray.Location);
        Assert.AreEqual(new ModsynSourceLocation(4, 7), stringNode.Location);
    }

    [TestMethod]
    public void Parse_ReportsMissingColonAtUnexpectedToken()
    {
        var exception = Assert.ThrowsException<ModsynParseException>(
            () => Parse("mod { type PutInModLoader }"));

        Assert.AreEqual(1, exception.Line);
        Assert.AreEqual(12, exception.Column);
        StringAssert.Contains(exception.Message, "Expected ':' after the property name");
        StringAssert.Contains(exception.Message, "line 1, column 12");
    }

    [TestMethod]
    public void Parse_ReportsUnclosedArrayAtUnexpectedBrace()
    {
        var exception = Assert.ThrowsException<ModsynParseException>(
            () => Parse("mod {\n  values: [\"one\"\n}"));

        Assert.AreEqual(3, exception.Line);
        Assert.AreEqual(1, exception.Column);
        StringAssert.Contains(exception.Message, "Expected a value");
    }

    [TestMethod]
    public void Parse_ReportsTrailingContentAtItsLocation()
    {
        var exception = Assert.ThrowsException<ModsynParseException>(
            () => Parse("mod {} extra"));

        Assert.AreEqual(1, exception.Line);
        Assert.AreEqual(8, exception.Column);
        StringAssert.Contains(exception.Message, "end of file after the mod document");
    }

    private static ModsynDocumentNode Parse(string source)
    {
        return ModsynParser.Parse(source);
    }
}