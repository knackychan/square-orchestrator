using System.Text.Json;
using Square.Contracts;
using Square.Contracts.Rpc;
using Square.Contracts.Serialization;

return Run(args);

static int Run(string[] arguments)
{
    bool asJson = arguments.Contains("--json", StringComparer.Ordinal);
    if (arguments.Length == 0 || arguments.Contains("--help", StringComparer.Ordinal) || arguments.Contains("-h", StringComparer.Ordinal))
    {
        if (asJson)
            Console.Out.WriteLine(JsonSerializer.Serialize(new { protocol = ContractVersions.RpcProtocol, version = ContractVersions.DraftV1, status = "bootstrap", commands = new[] { "daemon status", "--help", "--version" } }, SquareJson.CreateOptions()));
        else
        {
            Console.Out.WriteLine("Square Orchestrator bootstrap");
            Console.Out.WriteLine("Usage: square <group> <command> [options]");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Implemented now: --help, --version, daemon status");
            Console.Out.WriteLine("State mutation and daemon auto-start begin in SP02 after G0/G1 prerequisites.");
        }
        return (int)CliExitCode.Success;
    }
    if (arguments.Contains("--version", StringComparer.Ordinal))
    {
        Console.Out.WriteLine("square 0.1.0 (contracts 1.0-draft)");
        return (int)CliExitCode.Success;
    }
    string[] command = arguments.Where(value => !string.Equals(value, "--json", StringComparison.Ordinal)).ToArray();
    if (command.SequenceEqual(new[] { "daemon", "status" }))
    {
        if (asJson)
            Console.Out.WriteLine(JsonSerializer.Serialize(new { protocol = ContractVersions.RpcProtocol, version = ContractVersions.DraftV1, state = "not_implemented", task = "SP02-T04" }, SquareJson.CreateOptions()));
        else Console.Out.WriteLine("Daemon status: not implemented (scheduled for SP02-T04).");
        return (int)CliExitCode.DaemonUnavailable;
    }
    Console.Error.WriteLine($"Unknown bootstrap command: {string.Join(' ', command)}");
    return (int)CliExitCode.Validation;
}
