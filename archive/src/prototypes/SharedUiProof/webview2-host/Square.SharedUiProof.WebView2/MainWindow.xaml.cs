using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Square.SharedUiProof.WebView2;

public partial class MainWindow : Window
{
    private const string Protocol = "square.shared-ui-proof/1";
    private const string ProofOrigin = "https://square-proof.local";
    private readonly ProgramOptions _options;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<string> _failures = [];
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private ProofInputs? _inputs;
    private JsonElement? _result;
    private int _terminalResizeMessages;
    private int _terminalInputMessages;
    private int _layoutMessages;
    private int _controllerRequests;
    private bool _initialized;
    private bool _completed;

    internal MainWindow(ProgramOptions options)
    {
        _options = options;
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => _lifetime.Cancel();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await InitializeAsync(_lifetime.Token);
            _ = WatchdogAsync(_lifetime.Token);
        }
        catch (Exception exception)
        {
            await CompleteFailureAsync($"WebView2 initialization failed: {exception.Message}");
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var webRoot = Path.Combine(baseDirectory, "dist");
        if (!Directory.Exists(webRoot))
        {
            throw new DirectoryNotFoundException($"Compiled shared UI assets were not found at '{webRoot}'.");
        }
        _inputs = await ProofInputs.LoadAsync(baseDirectory, cancellationToken);
        Directory.CreateDirectory(_options.UserDataDirectory);
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _options.UserDataDirectory);
        await Browser.EnsureCoreWebView2Async(environment);
        var core = Browser.CoreWebView2 ?? throw new InvalidOperationException("WebView2 core did not initialize.");
        core.SetVirtualHostNameToFolderMapping("square-proof.local", webRoot, CoreWebView2HostResourceAccessKind.DenyCors);
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsWebMessageEnabled = true;
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.IsBuiltInErrorPageEnabled = false;

        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.NewWindowRequested += (_, args) => args.Handled = true;
        core.DownloadStarting += (_, args) => args.Cancel = true;
        core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
        core.WebMessageReceived += OnWebMessageReceived;
        core.ProcessFailed += async (_, args) => await CompleteFailureAsync($"WebView2 process failed: {args.ProcessFailedKind}");

        var template = await File.ReadAllTextAsync(Path.Combine(webRoot, "index.template.html"), cancellationToken);
        var runtimeHtml = HtmlTemplateRenderer.Render(template);
        var runtimePath = Path.Combine(webRoot, "index.webview2.html");
        await File.WriteAllTextAsync(runtimePath, runtimeHtml, cancellationToken);
        core.Navigate($"{ProofOrigin}/index.webview2.html");
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (!string.Equals(args.Uri, $"{ProofOrigin}/index.webview2.html", StringComparison.Ordinal))
        {
            args.Cancel = true;
            _failures.Add($"Blocked navigation to '{args.Uri}'.");
        }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            await CompleteFailureAsync($"WebView2 navigation failed: {args.WebErrorStatus}");
        }
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var message = BridgeValidator.Parse(args.WebMessageAsJson);
            switch (message.Type)
            {
                case "proof.ready":
                    if (!string.Equals(message.Root.GetProperty("host").GetString(), "webview2", StringComparison.Ordinal))
                    {
                        await CompleteFailureAsync("Web content reported the wrong host kind.");
                        return;
                    }
                    await StartProofAsync();
                    break;
                case "proof.result":
                    await ReceiveResultAsync(message.Root);
                    break;
                case "proof.error":
                    await CompleteFailureAsync($"{message.Root.GetProperty("code").GetString()}: {message.Root.GetProperty("message").GetString()}");
                    break;
                case "terminal.resize":
                    _terminalResizeMessages++;
                    break;
                case "terminal.input":
                    _terminalInputMessages++;
                    break;
                case "proof.layoutChanged":
                    _layoutMessages++;
                    break;
                case "proof.controllerRequested":
                    _controllerRequests++;
                    break;
            }
        }
        catch (Exception exception)
        {
            await CompleteFailureAsync($"Rejected web message: {exception.Message}");
        }
    }

    private Task StartProofAsync()
    {
        if (_initialized) return Task.CompletedTask;
        _initialized = true;
        var inputs = _inputs ?? throw new InvalidOperationException("Proof inputs are unavailable.");
        Post(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["version"] = Protocol,
            ["type"] = "proof.initialize",
            ["host"] = "webview2",
            ["fixture"] = inputs.Fixture,
            ["benchmark"] = inputs.Benchmark,
            ["expectedFixtureSha256"] = inputs.FixtureSha256
        });
        Post(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["version"] = Protocol,
            ["type"] = "proof.runBenchmark",
            ["runId"] = inputs.RunId
        });
        return Task.CompletedTask;
    }

    private async Task ReceiveResultAsync(JsonElement root)
    {
        var inputs = _inputs ?? throw new InvalidOperationException("Proof inputs are unavailable.");
        if (!string.Equals(root.GetProperty("runId").GetString(), inputs.RunId, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("fixtureSha256").GetString(), inputs.FixtureSha256, StringComparison.Ordinal))
        {
            await CompleteFailureAsync("Result identity did not match the canonical run or fixture.");
            return;
        }
        _result = root.GetProperty("result").Clone();
        if (!ResultPassed(_result.Value)) _failures.Add("Shared UI result reported failure.");
        await CompleteAsync();
    }

    private void Post(object message)
    {
        var core = Browser.CoreWebView2 ?? throw new InvalidOperationException("WebView2 is unavailable.");
        core.PostWebMessageAsJson(JsonSerializer.Serialize(message));
    }

    private async Task CompleteFailureAsync(string failure)
    {
        _failures.Add(failure);
        await CompleteAsync();
    }

    private async Task CompleteAsync()
    {
        if (_completed) return;
        _completed = true;
        _lifetime.Cancel();
        var inputs = _inputs;
        var passed = inputs is not null && _failures.Count == 0 && _result is JsonElement result && ResultPassed(result);
        using var process = Process.GetCurrentProcess();
        var evidence = new
        {
            schemaVersion = "1.0",
            taskId = "SP00-T04",
            hostKind = "webview2",
            status = passed ? "PASS" : "FAIL",
            acceptanceEligible = passed && _options.AcceptanceRun,
            startedAtUtc = _startedAt,
            completedAtUtc = DateTimeOffset.UtcNow,
            fixtureSha256 = inputs?.FixtureSha256,
            benchmarkSha256 = inputs?.BenchmarkSha256,
            environment = new
            {
                osVersion = Environment.OSVersion.VersionString,
                runtimeVersion = Environment.Version.ToString(),
                architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                webView2Version = Browser.CoreWebView2?.Environment.BrowserVersionString,
                userDataDirectory = _options.UserDataDirectory,
                workingSetBytes = process.WorkingSet64,
                privateMemoryBytes = process.PrivateMemorySize64
            },
            bridgeEvents = new
            {
                terminalResizeMessages = _terminalResizeMessages,
                terminalInputMessages = _terminalInputMessages,
                layoutMessages = _layoutMessages,
                controllerRequests = _controllerRequests
            },
            result = _result,
            failures = _failures,
            passed
        };
        await EvidenceWriter.WriteAtomicAsync(_options.EvidencePath, evidence, CancellationToken.None);
        if (_options.Autorun)
        {
            Dispatcher.Invoke(() => Application.Current.Shutdown(passed ? 0 : 1));
        }
        else
        {
            Title = passed
                ? "Square Orchestrator — Shared UI Proof — PASS"
                : "Square Orchestrator — Shared UI Proof — FAIL";
        }
    }

    private async Task WatchdogAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            await CompleteFailureAsync("Shared UI proof exceeded the ten-minute host deadline.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static bool ResultPassed(JsonElement result)
    {
        return result.ValueKind == JsonValueKind.Object
            && result.TryGetProperty("overallPassed", out var value)
            && value.ValueKind == JsonValueKind.True;
    }
}
