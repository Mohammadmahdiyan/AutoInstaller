using GtaSaModManager.Modsyn.Lexer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class ModsynLexerTests
{
    [TestMethod]
    public void Tokenize_RecognizesIdentifiersAndKeywords()
    {
        var tokens = Tokenize("mod type installThis PutInModLoader");

        CollectionAssert.AreEqual(
            new[]
            {
                ModsynTokenKind.Identifier,
                ModsynTokenKind.Identifier,
                ModsynTokenKind.Identifier,
                ModsynTokenKind.Identifier,
                ModsynTokenKind.EndOfFile
            },
            tokens.Select(token => token.Kind).ToArray());
    }

    [TestMethod]
    public void Tokenize_PreservesWindowsPathBackslashesInStrings()
    {
        var tokens = Tokenize("\"audio\\Config\" \"models\\file.dff\" \"C:\\Games\\GTA San Andreas\"");

        CollectionAssert.AreEqual(
            new[] { "audio\\Config", "models\\file.dff", "C:\\Games\\GTA San Andreas" },
            tokens.Where(token => token.Kind == ModsynTokenKind.String)
                .Select(token => token.Value)
                .ToArray());
    }

    [TestMethod]
    public void Tokenize_RecognizesBooleanAndNullLiterals()
    {
        var tokens = Tokenize("true false null");

        CollectionAssert.AreEqual(
            new[]
            {
                ModsynTokenKind.True,
                ModsynTokenKind.False,
                ModsynTokenKind.Null,
                ModsynTokenKind.EndOfFile
            },
            tokens.Select(token => token.Kind).ToArray());
    }

    [TestMethod]
    public void Tokenize_RecognizesPunctuation()
    {
        var tokens = Tokenize("{}[]:,");

        CollectionAssert.AreEqual(
            new[]
            {
                ModsynTokenKind.LeftBrace,
                ModsynTokenKind.RightBrace,
                ModsynTokenKind.LeftBracket,
                ModsynTokenKind.RightBracket,
                ModsynTokenKind.Colon,
                ModsynTokenKind.Comma,
                ModsynTokenKind.EndOfFile
            },
            tokens.Select(token => token.Kind).ToArray());
    }

    [TestMethod]
    public void Tokenize_TracksLocationsAcrossCrLfLines()
    {
        var tokens = Tokenize("mod {\r\n  type: true\r\n}");

        CollectionAssert.AreEqual(
            new[]
            {
                new ModsynSourceLocation(1, 1),
                new ModsynSourceLocation(1, 5),
                new ModsynSourceLocation(2, 3),
                new ModsynSourceLocation(2, 7),
                new ModsynSourceLocation(2, 9),
                new ModsynSourceLocation(3, 1),
                new ModsynSourceLocation(3, 2)
            },
            tokens.Select(token => token.Location).ToArray());
    }

    [TestMethod]
    public void Tokenize_ReportsInvalidCharacterLocation()
    {
        var exception = Assert.ThrowsException<ModsynLexException>(
            () => Tokenize("mod {\n  @}"));

        Assert.AreEqual(2, exception.Line);
        Assert.AreEqual(3, exception.Column);
        StringAssert.Contains(exception.Message, "Unexpected character '@'");
        StringAssert.Contains(exception.Message, "line 2, column 3");
    }

    [TestMethod]
    public void Tokenize_ReportsUnterminatedStringLocation()
    {
        var exception = Assert.ThrowsException<ModsynLexException>(
            () => Tokenize("\"unfinished"));

        Assert.AreEqual(1, exception.Line);
        Assert.AreEqual(12, exception.Column);
        StringAssert.Contains(exception.Message, "Unterminated string literal");
    }

    private static IReadOnlyList<ModsynToken> Tokenize(string source)
    {
        return new ModsynLexer(source).Tokenize();
    }
}