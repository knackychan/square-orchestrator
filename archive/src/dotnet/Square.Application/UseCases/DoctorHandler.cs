namespace Square.Application.UseCases;

/// <summary>Environment inspection for the M1 doctor command.</summary>
public sealed record DoctorReport(string Git, string Python, string Repository, string StateDb);

/// <summary>Doctor use case: reports the environment without creating runtime state.</summary>
public static class DoctorHandler
{
    /// <exception cref="ApplicationError">Thrown with VALIDATION_FAILED exit 3 when git is unavailable.</exception>
    public static DoctorReport Run(string repository, string stateDb)
    {
        string gitVersion;
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo("git")
            {
                WorkingDirectory = repository,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("--version");
            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException("git process could not start");
            gitVersion = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("git --version failed");
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new ApplicationError("VALIDATION_FAILED", "Git is unavailable.", exitCode: 3);
        }

        return new DoctorReport(
            Git: gitVersion,
            Python: Environment.Version.ToString(),
            Repository: repository,
            StateDb: stateDb);
    }
}
