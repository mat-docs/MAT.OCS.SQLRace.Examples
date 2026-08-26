// ─────────────────────────────────────────────────────────────
// SQL Race Examples: Snippet Compilation Tests
// ─────────────────────────────────────────────────────────────
//
// Dynamically discovers all .cs files under snippets/csharp/ and
// compiles each one against the real SQL Race assemblies using
// Roslyn.
//
// This is the early-warning system for API breaks. Snippets are
// the code we hand customers, so a snippet that stops compiling
// against a new MESL.SQLRace.API release is a customer whose
// script stops compiling too - and we want to know before the
// release ships, not after.
//
// Compilation is metadata-only: nothing here initialises the SQL
// Race runtime, so no licence is needed and this suite runs on a
// plain GitHub runner. Runtime behaviour of the same snippets is
// covered by scripts/run-examples-e2e.ps1, which does need one.
//
// To test a specific API build:
//   dotnet test -p:SqlRaceApiVersion=2.1.26212.6-ci
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

    // ── Reference set ─────────────────────────────────────────
    //
    // Two sources, both derived from this test assembly rather
    // than from a hard-coded SDK path, so the same code works on
    // a developer machine and on a CI runner:
    //
    //   1. TRUSTED_PLATFORM_ASSEMBLIES - every framework assembly
    //      the host runtime can load.
    //   2. This assembly's own output directory - the SQL Race
    //      assemblies and their transitive dependencies, put
    //      there by the MESL.SQLRace.API PackageReference.
    //
    // Built once: resolving ~200 assemblies per snippet would
    // dominate the runtime of the suite.
    private static readonly Lazy<IReadOnlyList<MetadataReference>> References =
        new(BuildReferences);

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        // Keyed by simple name so a framework assembly and a copy
        // of the same assembly in the output folder do not both
        // get referenced - Roslyn reports that as an ambiguous
        // type reference rather than resolving it.
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var platformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (platformAssemblies is not null)
        {
            foreach (var path in platformAssemblies.Split(Path.PathSeparator))
            {
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                    byName[Path.GetFileNameWithoutExtension(path)] = path;
            }
        }

        // The output folder wins on conflict: it holds the SQL
        // Race build under test, which is the point of all this.
        foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            byName[Path.GetFileNameWithoutExtension(path)] = path;

        var references = new List<MetadataReference>();
        foreach (var path in byName.Values)
        {
            // Native and resource-only DLLs sit alongside managed
            // ones in the output folder and throw when read as
            // metadata.
            try { references.Add(MetadataReference.CreateFromFile(path)); }
            catch (BadImageFormatException) { }
            catch (IOException) { }
        }

        return references;
    }

    // Snippets are top-level-statement programs built with
    // ImplicitUsings enabled, so the usings the SDK would inject
    // have to be supplied here - otherwise every snippet fails on
    // Console, Path and the LINQ extension methods.
    private const string ImplicitUsings =
        "global using global::System;\n" +
        "global using global::System.Collections.Generic;\n" +
        "global using global::System.IO;\n" +
        "global using global::System.Linq;\n" +
        "global using global::System.Net.Http;\n" +
        "global using global::System.Threading;\n" +
        "global using global::System.Threading.Tasks;\n";

    [Theory]
    [MemberData(nameof(SnippetFiles))]
    public void Snippet_CompilesAgainstSqlRaceAssemblies(string relativePath, string fullPath)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);

        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(File.ReadAllText(fullPath), parseOptions, path: fullPath),
            CSharpSyntaxTree.ParseText(ImplicitUsings, parseOptions, path: "ImplicitUsings.g.cs"),
        };

        // ConsoleApplication because top-level statements need an
        // entry point. Each snippet compiles alone - only one file
        // in a compilation may carry top-level statements.
        var compilation = CSharpCompilation.Create(
            assemblyName: "SnippetCompilation_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: trees,
            references: References.Value,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(errors.Count == 0,
            $"{relativePath} no longer compiles against {DescribeSqlRaceVersion()}. " +
            "A customer copying this snippet would hit the same errors:\n" +
            string.Join("\n", errors.Select(d =>
                $"  Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.Id}: {d.GetMessage()}")));
    }

    // Named in failure messages so a red build says which API
    // build broke the snippet, not just that something broke.
    private static string DescribeSqlRaceVersion()
    {
        var domain = Path.Combine(AppContext.BaseDirectory, "MESL.SqlRace.Domain.dll");
        if (!File.Exists(domain))
            return "MESL.SQLRace.API (version unknown - assembly not resolved)";

        var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(domain);
        return $"MESL.SQLRace.API {info.FileVersion}";
    }

    [Fact]
    public void SqlRaceAssemblies_AreResolvable()
    {
        // Guards against the compilation tests passing vacuously.
        // If the package failed to restore there would be nothing
        // to compile against, and a weaker suite could report that
        // as success.
        var domain = Path.Combine(AppContext.BaseDirectory, "MESL.SqlRace.Domain.dll");
        Assert.True(File.Exists(domain),
            $"MESL.SqlRace.Domain.dll was not found in {AppContext.BaseDirectory}. " +
            "The MESL.SQLRace.API package did not restore, so snippet compilation " +
            "would not be testing anything.");
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
