using System.Text.RegularExpressions;

namespace CodeCafe.Architecture.Tests;

/// <summary>
/// Feature-slice boundaries. Slices are folders inside one assembly now, so these cannot be assembly
/// reference checks; they are asserted on source imports instead. Weaker than a compile error, which is
/// the deliberate trade for collapsing the module projects, so the rules are spelled out explicitly
/// rather than left to convention.
/// </summary>
public sealed class SliceBoundaryTests
{
    private static readonly string[] Slices = ["Notes", "Identity", "Ai"];

    [Fact]
    public void ApplicationSlices_DoNotImportEachOther()
    {
        // A Notes use case reaching into Identity (or vice versa) is cross-slice work that belongs in
        // the host. Ai is exempt as a consumer: the AI flows are built on top of Notes use cases.
        AssertSliceDoesNotImport("Notes", "CodeCafe.Application.Identity");
        AssertSliceDoesNotImport("Notes", "CodeCafe.Application.Ai");
        AssertSliceDoesNotImport("Identity", "CodeCafe.Application.Notes");
        AssertSliceDoesNotImport("Identity", "CodeCafe.Application.Ai");
    }

    [Fact]
    public void ApplicationSlices_DoNotImportInfrastructure()
    {
        foreach (var slice in Slices)
        {
            AssertSliceDoesNotImport(slice, "CodeCafe.Infrastructure");
        }
    }

    [Fact]
    public void ApplicationHandlers_DoNotNameHttpStatusCodes()
    {
        // Handlers express failure as a *FailureKind; naming a status code would put the transport's
        // vocabulary back into the use case even without an AspNetCore reference.
        var offenders = EnumerateSliceFiles()
            .Where(file => Regex.IsMatch(File.ReadAllText(file), @"StatusCodes\.Status\d"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Application code must not name HTTP status codes, but these do: "
                + string.Join(", ", offenders)
        );
    }

    [Fact]
    public void ApplicationCode_DoesNotNameProviderSdkTypes()
    {
        // Only provider SDK types. System.Text.Json is fine here: handlers legitimately read TipTap
        // document content, which is application data rather than a provider response.
        var forbidden = new[]
        {
            "ClientResultException",
            "OpenAIClient",
            "ChatCompletionOptions",
            "MarkdownPipeline",
        };
        var offenders = new List<string>();

        foreach (var file in EnumerateSliceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            foreach (var token in forbidden)
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetFileName(file)} ({token})");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Application code must not name provider SDK types; adapters translate those into "
                + "AiProviderException. Offenders: "
                + string.Join(", ", offenders)
        );
    }

    /// <summary>
    /// Drops line comments so that documentation explaining why a provider type is banned does not
    /// itself trip the ban.
    /// </summary>
    private static string StripComments(string source)
    {
        var kept = source
            .Split('\n')
            .Select(line =>
            {
                var trimmed = line.TrimStart();
                return trimmed.StartsWith("//", StringComparison.Ordinal) ? string.Empty : line;
            });

        return string.Join("\n", kept);
    }

    private static void AssertSliceDoesNotImport(string slice, string forbiddenNamespace)
    {
        var sliceDirectory = Path.Combine(FindApplicationProjectDirectory(), slice);
        if (!Directory.Exists(sliceDirectory))
        {
            return;
        }

        var offenders = Directory
            .EnumerateFiles(sliceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
                Regex.IsMatch(
                    File.ReadAllText(file),
                    $@"^using\s+{Regex.Escape(forbiddenNamespace)}(\.|;)",
                    RegexOptions.Multiline
                )
            )
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Application/{slice} must not import {forbiddenNamespace}, but these files do: "
                + string.Join(", ", offenders)
        );
    }

    private static IEnumerable<string> EnumerateSliceFiles()
    {
        return Directory.EnumerateFiles(
            FindApplicationProjectDirectory(),
            "*.cs",
            SearchOption.AllDirectories
        );
    }

    private static string FindApplicationProjectDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "server", "src", "CodeCafe.Application");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate CodeCafe.Application from the test output path."
        );
    }
}
