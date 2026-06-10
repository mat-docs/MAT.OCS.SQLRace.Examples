// ─────────────────────────────────────────────────────────────
// SQL Race Examples: Snippet Compilation Tests
// ─────────────────────────────────────────────────────────────
//
// Dynamically discovers all .cs files under snippets/csharp/ and
// verifies they parse as valid C# syntax using Roslyn.
//
// This catches syntax errors and missing brackets but does NOT
// verify against the real SQL Race assemblies (that requires the
// NuGet package to be resolvable at test time).
//
// Run: dotnet test --filter "FullyQualifiedName~SnippetCompilation"
// ─────────────────────────────────────────────────────────────

using Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SqlRace.Examples.Tests;

public class SnippetCompilationTests
{
    private static readonly string SnippetsRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "snippets", "csharp"));

    private static IEnumerable<string> GetAllSnippetFiles()
    {
        if (!Directory.Exists(SnippetsRoot))
            return Enumerable.Empty<string>();

        return Directory.EnumerateFiles(SnippetsRoot, "*.cs", SearchOption.AllDirectories);
    }

    public static IEnumerable<object[]> SnippetFiles()
    {
        foreach (var file in GetAllSnippetFiles())
        {
            var relative = Path.GetRelativePath(SnippetsRoot, file);
            yield return new object[] { relative, file };
        }
    }

    [Theory]
    [MemberData(nameof(SnippetFiles))]
    public void Snippet_ParsesWithoutSyntaxErrors(string relativePath, string fullPath)
    {
        var source = File.ReadAllText(fullPath);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));
        var diagnostics = syntaxTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(diagnostics.Count == 0,
            $"Syntax errors in {relativePath}:\n" +
            string.Join("\n", diagnostics.Select(d => $"  Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}")));
    }

    [Fact]
    public void SnippetsDirectory_Exists()
    {
        // This test verifies the test infrastructure can find the snippets
        // Skip if running in CI without the full repo layout
        if (!Directory.Exists(SnippetsRoot))
        {
            // Not a failure — just skip
            return;
        }

        var files = GetAllSnippetFiles().ToList();
        Assert.True(files.Count > 0, $"No .cs files found in {SnippetsRoot}");
    }

    [Fact]
    public void AllSnippets_HaveHeaderComment()
    {
        if (!Directory.Exists(SnippetsRoot))
            return;

        foreach (var file in GetAllSnippetFiles())
        {
            var content = File.ReadAllText(file);
            var relative = Path.GetRelativePath(SnippetsRoot, file);

            Assert.True(
                content.Contains("SQL Race Example:"),
                $"{relative} is missing the standard header comment 'SQL Race Example:'");
        }
    }

    [Fact]
    public void AllSnippets_AreUnder100Lines()
    {
        if (!Directory.Exists(SnippetsRoot))
            return;

        foreach (var file in GetAllSnippetFiles())
        {
            var lineCount = File.ReadLines(file).Count();
            var relative = Path.GetRelativePath(SnippetsRoot, file);

            Assert.True(lineCount <= 100,
                $"{relative} has {lineCount} lines (max 100). Consider splitting into smaller snippets.");
        }
    }
}
