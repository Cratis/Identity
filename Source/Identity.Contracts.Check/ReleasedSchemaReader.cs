// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Reads immutable Git objects and the corresponding proposed working-tree files.</summary>
/// <param name="root">The directory containing the solution and its Git repository.</param>
internal sealed class ReleasedSchemaReader(string root)
{
    const string CheckerPath = "Source/Identity.Contracts.Check";
    const string CataloguePath = $"{CheckerPath}/catalogue/event-schemas.json";
    const string FixturesPath = $"{CheckerPath}/fixtures";
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>Resolves a requested ref once, then reads its catalogue and every fixture blob.</summary>
    /// <param name="reference">The trusted released or base ref.</param>
    /// <returns>The complete released state.</returns>
    internal async Task<ReleasedSchemaState> ReadReleased(string reference)
    {
        var commit = Encoding.UTF8.GetString(await Git("rev-parse", "--verify", "--end-of-options", $"{reference}^{{commit}}")).Trim();
        var catalogue = Parse(await Git("show", $"{commit}:{CataloguePath}"));
        var paths = Encoding.UTF8.GetString(await Git("ls-tree", "-r", "-z", "--name-only", commit, "--", FixturesPath))
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var fixtures = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var path in paths) fixtures.Add(path[(CheckerPath.Length + 1)..], await Git("show", $"{commit}:{path}"));
        return new(catalogue, fixtures);
    }

    /// <summary>Reads current files rather than trusting potentially stale embedded resources.</summary>
    /// <returns>The complete proposed state.</returns>
    internal ReleasedSchemaState ReadCurrent()
    {
        var cataloguePath = Path.Combine(root, CataloguePath);
        var catalogue = File.Exists(cataloguePath) ? Parse(File.ReadAllBytes(cataloguePath)) : [];
        var fixturesPath = Path.Combine(root, FixturesPath);
        var files = Directory.Exists(fixturesPath) ? Directory.EnumerateFiles(fixturesPath, "*", SearchOption.AllDirectories) : [];
        var fixtures = files.ToDictionary(
            path => Path.GetRelativePath(Path.Combine(root, CheckerPath), path).Replace(Path.DirectorySeparatorChar, '/'),
            File.ReadAllBytes,
            StringComparer.Ordinal);
        return new(catalogue, fixtures);
    }

    static EventSchema[] Parse(byte[] json) => JsonSerializer.Deserialize<EventSchema[]>(json, _options) ?? throw new InvalidDataException();

    static async Task<byte[]> ReadOutput(Stream source)
    {
        await using var bytes = new MemoryStream();
        await source.CopyToAsync(bytes);
        return bytes.ToArray();
    }

    async Task<byte[]> Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("--no-pager");
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var output = ReadOutput(process.StandardOutput.BaseStream);
        var errors = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await Task.WhenAll(output, errors);
        }
        if (process.ExitCode != 0) throw new InvalidOperationException();
        return await output;
    }
}
