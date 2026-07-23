using System.Text.RegularExpressions;

namespace Application.Tests.Architecture;

/// <summary>
/// Enforces the "named args for record-ctor Guid pairs in the doctor→patient
/// path" convention introduced in PR #263 / PR #265 and applied uniformly
/// across the doctor→patient record ctors in PR #266.
///
/// The convention exists because several of these records take two or three
/// <see cref="Guid"/> parameters in a row. With positional arguments a future
/// refactor that reorders the ctor parameters (e.g. swaps <c>DoctorId</c> and
/// <c>PatientId</c>) compiles clean and silently routes every call to the
/// wrong record. Named arguments make that refactor a build break.
///
/// Limitations (file-content grep, not a Roslyn AST):
/// <list type="bullet">
///   <item><description>Fragile to file moves — if a protected file is renamed
///     or relocated, update the path resolution below.</description></item>
///   <item><description>Only enforces the protected record ctors listed in
///     <see cref="ProtectedCtors"/>. Adding new record ctors to the list is a
///     conscious decision.</description></item>
///   <item><description>Only enforces the protected record ctors listed in
///     <see cref="ProtectedCtors"/>. Adding new record ctors to the list is a
///     conscious decision.</description></item>
///   <item><description>The "any `Name:` token" heuristic accepts a partial
///     named-args call. A <c>new PatientByIdQuery(DoctorId: doctorId,
///     PatientId: patientId, ...)</c> call passes; a <c>new PatientByIdQuery(
///     doctorId, patientId, DoctorId: doctorId, PatientId: patientId)</c>
///     call also passes (it has the named arg, even though it's redundant).
///     Strict matching would require parsing.</description></item>
/// </list>
/// If the source files are not found (e.g. the test is run from an
/// unexpected working directory), the test fails with an actionable message
/// pointing at `TryFindRepoRoot`. CI runs `dotnet test` from the repo root,
/// so the test will resolve correctly there; a local run from a non-standard
/// CWD will see this error rather than a silent pass.
/// </summary>
public class NamedArgsConventionTests
{
    // Protected record ctors: positional Guids of the same type that must
    // be passed by name. Add to this list when adopting the convention for
    // a new record ctor.
    private static readonly string[] ProtectedCtors =
    {
        "PatientByIdQuery",
        "GetLinkedPatientProfileQuery",
        "CreateClinicalGoalCommand",
        "GetClinicalGoalsQuery",
        "UpdateClinicalGoalCommand",
        "DeleteClinicalGoalCommand",
    };

    // Files scanned for the convention. Paths are relative to the repo root
    // (resolved by walking up from AppContext.BaseDirectory).
    private static readonly string[] ScannedFiles =
    {
        "src/API/Modules/DoctorModule.cs",
        "tests/Application.Tests/Patient/PatientByIdQueryHandlerTests.cs",
        "tests/Application.Tests/Doctor/GetLinkedPatientProfileQueryHandlerTests.cs",
    };

    [Fact]
    public void DoctorPatientRecordCtors_UseNamedArgumentsAtAllCallSites()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            throw new InvalidOperationException(
                $"Could not resolve repo root from AppContext.BaseDirectory = '{AppContext.BaseDirectory}'. " +
                "Run `dotnet test` from the repo root.");
        }

        foreach (var relativePath in ScannedFiles)
        {
            var fullPath = Path.Combine(repoRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException(
                    $"Expected source file not found: '{fullPath}'. " +
                    $"The {nameof(ScannedFiles)} list in this test class needs updating.");
            }
        }

        var violations = new List<string>();

        foreach (var relativePath in ScannedFiles)
        {
            var fullPath = Path.Combine(repoRoot, relativePath);
            var content = File.ReadAllText(fullPath);
            // Use a single regex pass to find every protected ctor call site,
            // then look at the content from each match position until the
            // matching closing paren to find a named-arg token. This handles
            // multi-line ctor calls (e.g. CreateClinicalGoalCommand in
            // DoctorModule.cs which spans 8 lines).
            foreach (var ctor in ProtectedCtors)
            {
                var pattern = $@"(?<![\w])new {Regex.Escape(ctor)}\(";
                foreach (Match match in Regex.Matches(content, pattern))
                {
                    var openParenIndex = match.Index + match.Length - 1; // points at `(`
                    var closeParenIndex = FindMatchingCloseParen(content, openParenIndex);
                    if (closeParenIndex < 0)
                    {
                        violations.Add(
                            $"{relativePath}:{LineNumberAt(content, match.Index)}: " +
                            $"could not find matching close paren for `new {ctor}(` " +
                            $"(likely a syntax error in source — investigate before adding to this test).");
                        continue;
                    }

                    // Window from after the open paren to the close paren,
                    // exclusive of the close paren itself.
                    var ctorBody = content[(openParenIndex + 1)..closeParenIndex];

                    if (!Regex.IsMatch(ctorBody, @"\b\w+\s*:"))
                    {
                        violations.Add(
                            $"{relativePath}:{LineNumberAt(content, match.Index)}: " +
                            $"positional call to `new {ctor}(...)` — at least one named argument required.");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "every call to a protected record ctor in the doctor→patient path " +
            "must use at least one named argument. See the convention comment " +
            "on this test class for the rationale and the protected-ctor list.");
    }

    private static string? TryFindRepoRoot()
    {
        // AppContext.BaseDirectory is e.g.
        // /path/to/repo/tests/Application.Tests/bin/Debug/net9.0/ when running
        // `dotnet test`. Walk up at most 8 levels looking for a directory that
        // contains both src/ and tests/.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }
        }
        return null;
    }

    // Walks forward from `openParenIndex` (which must point at `(`), counting
    // nested parens, and returns the index of the matching `)`. Skips over
    // chars inside string literals ("..." and @"...") and char literals ('x')
    // to avoid false matches on parens in default values. Returns -1 if no
    // match is found within 4KB of source.
    private static int FindMatchingCloseParen(string content, int openParenIndex)
    {
        var depth = 0;
        var i = openParenIndex;
        var limit = Math.Min(content.Length, openParenIndex + 4096);
        while (i < limit)
        {
            var c = content[i];
            if (c == '(') { depth++; i++; continue; }
            if (c == ')')
            {
                depth--;
                if (depth == 0) return i;
                i++;
                continue;
            }
            if (c == '"')
            {
                // Skip a regular string literal.
                i++;
                while (i < limit && content[i] != '"')
                {
                    if (content[i] == '\\' && i + 1 < limit) i++;
                    i++;
                }
                i++;
                continue;
            }
            if (c == '@' && i + 1 < limit && content[i + 1] == '"')
            {
                // Skip a verbatim string literal.
                i += 2;
                while (i < limit)
                {
                    if (content[i] == '"')
                    {
                        if (i + 1 < limit && content[i + 1] == '"') { i += 2; continue; }
                        i++; break;
                    }
                    i++;
                }
                continue;
            }
            if (c == '\'')
            {
                // Skip a char literal.
                i++;
                if (i < limit && content[i] == '\\') i++;
                if (i < limit) i++;
                if (i < limit && content[i] == '\'') i++;
                continue;
            }
            if (c == '/' && i + 1 < limit && content[i + 1] == '/')
            {
                // Skip a line comment.
                while (i < limit && content[i] != '\n') i++;
                continue;
            }
            if (c == '/' && i + 1 < limit && content[i + 1] == '*')
            {
                // Skip a block comment.
                i += 2;
                while (i + 1 < limit && !(content[i] == '*' && content[i + 1] == '/')) i++;
                i += 2;
                continue;
            }
            i++;
        }
        return -1;
    }

    private static int LineNumberAt(string content, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < content.Length; i++)
        {
            if (content[i] == '\n') line++;
        }
        return line;
    }
}
