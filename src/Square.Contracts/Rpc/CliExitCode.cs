namespace Square.Contracts.Rpc;

public enum CliExitCode
{
    Success = 0,
    Validation = 2,
    IncompatibleProtocol = 3,
    DaemonUnavailable = 4,
    InteractionRequired = 5,
    PolicyDenied = 6,
    Conflict = 7,
    Timeout = 8,
    InternalFailure = 10
}
