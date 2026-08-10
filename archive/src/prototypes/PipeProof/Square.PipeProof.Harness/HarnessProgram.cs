using System.Diagnostics;
using Square.PipeProof.Protocol;

namespace Square.PipeProof.Harness;

internal static class HarnessProgram
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("SP00-T03 PipeProof harness requires Windows.");
            return 2;
        }

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        HarnessOptions options;
        try
        {
            options = HarnessOptions.Parse(args);
            PrepareOutputDirectory(options.OutputDirectory);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }

        await using EvidenceWriter evidence = new(options.OutputDirectory);
        ServerProcess? server = null;
        try
        {
            ScenarioManifestDocument manifest = ProtocolJson.DeserializeText<ScenarioManifestDocument>(
                await File.ReadAllTextAsync(options.ScenarioManifest).ConfigureAwait(false));
            ValidateManifest(manifest);
            HarnessEnvironmentEvidence environment = await EnvironmentCollector.CollectAsync(options)
                .ConfigureAwait(false);
            await evidence.WriteEnvironmentAsync(environment).ConfigureAwait(false);
            string inputs = Path.Combine(options.OutputDirectory, "inputs");
            Directory.CreateDirectory(inputs);
            File.Copy(options.DispatchPacket, Path.Combine(inputs, "dispatch.packet.json"), overwrite: true);
            File.Copy(options.ScenarioManifest, Path.Combine(inputs, "scenario-manifest.json"), overwrite: true);

            string pipeName = $"square-pipeproof-{Environment.ProcessId}-{Guid.NewGuid():N}";
            string stateDirectory = Path.Combine(options.OutputDirectory, "server-state");
            server = new ServerProcess(options, pipeName, stateDirectory);
            _ = await server.StartAsync().ConfigureAwait(false);
            ProofScenarios scenarios = new(options, server);

            List<ScenarioEvidence> results = [];
            IReadOnlyList<ScenarioDefinition> selected = options.Quick
                ? manifest.Scenarios.Where(static scenario => scenario.RequiredInQuickMode).ToArray()
                : manifest.Scenarios;
            foreach (ScenarioDefinition definition in selected)
            {
                if (!server.IsRunning && !string.Equals(definition.Id, "graceful-shutdown", StringComparison.Ordinal))
                {
                    _ = await server.StartAsync().ConfigureAwait(false);
                }
                ScenarioEvidence result = await RunScenarioAsync(
                    scenarios,
                    definition,
                    options.ScenarioTimeout).ConfigureAwait(false);
                results.Add(result);
                await evidence.AppendScenarioAsync(result).ConfigureAwait(false);
                Console.Out.WriteLine($"{(result.Passed ? "PASS" : "FAIL")} {definition.Id} — {definition.Title}");
            }

            if (server.IsRunning)
            {
                _ = await server.KillAsync("harness post-scenario cleanup").ConfigureAwait(false);
            }

            List<string> ineligibility = BuildIneligibilityReasons(
                options,
                environment,
                manifest,
                selected,
                results);
            bool allPassed = results.All(static result => result.Passed);
            bool eligible = allPassed && ineligibility.Count == 0;
            string status = eligible ? "PASS" : allPassed ? "DIAGNOSTIC_PASS" : "FAIL";
            string conclusion = eligible
                ? "The complete normal-user Windows proof passed with pinned toolchains, explicit ACL denial, cross-language parity, bounded backpressure, durable replay, and restart reconnect evidence."
                : allPassed
                    ? "All executed scenarios passed, but this run is diagnostic because one or more acceptance-shape requirements were not met."
                    : "One or more required protocol proof scenarios failed; SP00-T03 is not accepted.";
            HarnessSummary summary = new(
                "1.0",
                "SP00-T03",
                status,
                eligible,
                ineligibility,
                startedAt,
                DateTimeOffset.UtcNow,
                results.Count,
                results.Count(static result => result.Passed),
                results.Count(static result => !result.Passed),
                environment.DispatchSha256,
                environment.ScenarioManifestSha256,
                "evidence-manifest.sha256",
                results.Select(static result => result.ScenarioId).ToArray(),
                conclusion);
            await evidence.WriteSummaryAsync(summary).ConfigureAwait(false);
            _ = await evidence.WriteEvidenceManifestAsync().ConfigureAwait(false);
            Console.Out.WriteLine($"SP00-T03 {status}: {options.OutputDirectory}");
            return status switch
            {
                "PASS" => 0,
                "DIAGNOSTIC_PASS" => 2,
                _ => 1
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            if (server is not null && server.IsRunning)
            {
                _ = await server.KillAsync("fatal harness failure").ConfigureAwait(false);
            }
            try
            {
                HarnessSummary summary = new(
                    "1.0",
                    "SP00-T03",
                    "FAIL",
                    false,
                    ["The harness terminated before completing its declared scenario set."],
                    startedAt,
                    DateTimeOffset.UtcNow,
                    0,
                    0,
                    1,
                    File.Exists(options.DispatchPacket)
                        ? await EvidenceWriter.ComputeSha256Async(options.DispatchPacket).ConfigureAwait(false)
                        : "unavailable",
                    File.Exists(options.ScenarioManifest)
                        ? await EvidenceWriter.ComputeSha256Async(options.ScenarioManifest).ConfigureAwait(false)
                        : "unavailable",
                    "evidence-manifest.sha256",
                    [],
                    exception.Message);
                await evidence.WriteSummaryAsync(summary).ConfigureAwait(false);
                _ = await evidence.WriteEvidenceManifestAsync().ConfigureAwait(false);
            }
            catch (Exception evidenceFailure)
            {
                Console.Error.WriteLine(evidenceFailure);
            }
            return 1;
        }
        finally
        {
            if (server is not null)
            {
                await server.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<ScenarioEvidence> RunScenarioAsync(
        ProofScenarios scenarios,
        ScenarioDefinition definition,
        TimeSpan timeout)
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        Stopwatch stopwatch = Stopwatch.StartNew();
        using CancellationTokenSource cancellation = new(timeout);
        try
        {
            object? details = await scenarios.ExecuteAsync(definition.Id, cancellation.Token).ConfigureAwait(false);
            stopwatch.Stop();
            return new(
                "1.0",
                definition.Id,
                definition.Title,
                started,
                DateTimeOffset.UtcNow,
                stopwatch.ElapsedMilliseconds,
                true,
                details,
                null,
                null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new(
                "1.0",
                definition.Id,
                definition.Title,
                started,
                DateTimeOffset.UtcNow,
                stopwatch.ElapsedMilliseconds,
                false,
                null,
                exception.GetType().FullName,
                exception.Message);
        }
    }

    private static List<string> BuildIneligibilityReasons(
        HarnessOptions options,
        HarnessEnvironmentEvidence environment,
        ScenarioManifestDocument manifest,
        IReadOnlyList<ScenarioDefinition> selected,
        IReadOnlyList<ScenarioEvidence> results)
    {
        List<string> reasons = [];
        if (options.Quick)
        {
            reasons.Add("Quick mode omits the full acceptance scenario set.");
        }
        if (!string.Equals(
                environment.DispatchSha256,
                ProofSourceIdentity.CanonicalDispatchPacketSha256,
                StringComparison.Ordinal))
        {
            reasons.Add("The dispatch packet hash differs from the canonical SP00-T03 source identity.");
        }
        if (!string.Equals(
                environment.ScenarioManifestSha256,
                ProofSourceIdentity.CanonicalScenarioManifestSha256,
                StringComparison.Ordinal))
        {
            reasons.Add("The scenario manifest hash differs from the canonical SP00-T03 source identity.");
        }
        if (environment.Elevated)
        {
            reasons.Add("The proof must run as a normal non-elevated per-user process.");
        }
        if (!string.Equals(environment.DotNetSdkVersion, "10.0.302", StringComparison.Ordinal))
        {
            reasons.Add($".NET SDK '{environment.DotNetSdkVersion}' does not match pinned 10.0.302.");
        }
        if (!string.Equals(environment.NodeVersion, "v24.19.0", StringComparison.Ordinal))
        {
            reasons.Add($"Node '{environment.NodeVersion}' does not match pinned v24.19.0.");
        }
        if (!string.Equals(environment.OsArchitecture, "X64", StringComparison.Ordinal)
            || !string.Equals(environment.ProcessArchitecture, "X64", StringComparison.Ordinal))
        {
            reasons.Add("The acceptance run must use an x64 process on x64 Windows.");
        }
        if (selected.Count != manifest.Scenarios.Count)
        {
            reasons.Add("Not every canonical scenario was selected.");
        }
        string[] missing = manifest.Scenarios
            .Select(static scenario => scenario.Id)
            .Except(results.Select(static result => result.ScenarioId), StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0)
        {
            reasons.Add($"Missing scenario evidence: {string.Join(", ", missing)}.");
        }
        foreach (ScenarioEvidence failed in results.Where(static result => !result.Passed))
        {
            reasons.Add($"Scenario '{failed.ScenarioId}' failed: {failed.ErrorMessage}");
        }
        return reasons;
    }

    private static void ValidateManifest(ScenarioManifestDocument manifest)
    {
        if (!string.Equals(manifest.SchemaVersion, "1.0", StringComparison.Ordinal)
            || !string.Equals(manifest.TaskId, "SP00-T03", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Scenario manifest identity is invalid.");
        }
        if (manifest.Scenarios.Count == 0)
        {
            throw new InvalidDataException("Scenario manifest is empty.");
        }
        if (manifest.Scenarios.Select(static scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count()
            != manifest.Scenarios.Count)
        {
            throw new InvalidDataException("Scenario manifest contains duplicate IDs.");
        }
    }

    private static void PrepareOutputDirectory(string outputDirectory)
    {
        string fullPath = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(fullPath)
            && Directory.EnumerateFileSystemEntries(fullPath).Any())
        {
            throw new InvalidOperationException($"Evidence directory must be empty: '{fullPath}'.");
        }
        Directory.CreateDirectory(fullPath);
    }
}
