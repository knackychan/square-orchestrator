# Creation-environment validation

Date: 2026-08-07

## Executed

- repository graph and authority-hash verifier;
- JSON, XML/MSBuild, and YAML parse audit;
- TypeScript project-reference compilation;
- UI source-policy checks;
- eighteen Node UI/host-contract/workspace tests; and
- five Node length-framing/fragmentation/UTF-8 failure tests.

## Not executed here

- .NET build or C# console tests;
- PowerShell wrapper execution;
- Windows ConPTY, Job Object, named-pipe ACL, WebView2, WPF, and VS Code host tests;
- provider-backed tests or real agent CLI discovery.

The local runtime used Node.js 22.16.0 and TypeScript 5.8.3 for the host-neutral checks. It did not
provide the repository-pinned Node.js 24.19.0, pnpm 11.20.0, TypeScript 6.0.3, the .NET SDK,
PowerShell, or Windows APIs. The pinned JavaScript installation and all Windows/.NET validation
therefore remain required on the target machine. The repository retains explicit validation gates
instead of presenting source inspection or a near-version compiler run as execution evidence.
