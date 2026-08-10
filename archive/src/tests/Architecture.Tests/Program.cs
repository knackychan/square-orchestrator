using System.Xml.Linq;
using Square.TestKit;

return TestRunner.Run(
    ("production projects obey inward dependency boundaries", ProjectReferencesPointInward),
    ("prototypes are not referenced by production projects", ProductionDoesNotReferencePrototypes),
    ("core projects contain no leaf namespace imports", CoreProjectsContainNoLeafImports));

static void ProjectReferencesPointInward()
{
    string root = FindRepositoryRoot();
    Dictionary<string, HashSet<string>> allowed = new(StringComparer.Ordinal)
    {
        ["Square.Domain"] = new(),
        ["Square.Contracts"] = new() { "Square.Domain" },
        ["Square.Application"] = new() { "Square.Domain", "Square.Contracts" }
    };
    foreach ((string projectName, HashSet<string> allowedReferences) in allowed)
    {
        string projectPath = Path.Combine(root, "src", projectName, projectName + ".csproj");
        XDocument document = XDocument.Load(projectPath);
        IEnumerable<string> references = document.Descendants("ProjectReference").Select(element => Path.GetFileNameWithoutExtension(((string?)element.Attribute("Include") ?? string.Empty).Replace('\\', '/')));
        foreach (string reference in references) AssertEx.True(allowedReferences.Contains(reference), $"{projectName} illegally references leaf/outward project {reference}.");
    }
}
static void ProductionDoesNotReferencePrototypes()
{
    string root = FindRepositoryRoot();
    foreach (string projectPath in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories))
        AssertEx.False(File.ReadAllText(projectPath).Contains("prototypes", StringComparison.OrdinalIgnoreCase), $"Production project references a prototype: {projectPath}");
}
static void CoreProjectsContainNoLeafImports()
{
    string root = FindRepositoryRoot();
    string[] forbidden = { "Square.Platform", "Square.Persistence", "Square.Adapters", "Square.Daemon", "Square.Cli", "Square.Desktop", "System.Windows", "Microsoft.Web.WebView2" };
    foreach (string project in new[] { "Square.Domain", "Square.Contracts", "Square.Application" })
        foreach (string sourcePath in Directory.EnumerateFiles(Path.Combine(root, "src", project), "*.cs", SearchOption.AllDirectories))
            foreach (string item in forbidden) AssertEx.False(File.ReadAllText(sourcePath).Contains($"using {item}", StringComparison.Ordinal), $"{sourcePath} imports forbidden namespace {item}.");
}
static string FindRepositoryRoot()
{
    string? explicitRoot = Environment.GetEnvironmentVariable("SQUARE_REPO_ROOT");
    if (!string.IsNullOrWhiteSpace(explicitRoot) && File.Exists(Path.Combine(explicitRoot, "SquareOrchestrator.slnx"))) return explicitRoot;
    DirectoryInfo? current = new(AppContext.BaseDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "SquareOrchestrator.slnx"))) return current.FullName;
        current = current.Parent;
    }
    throw new DirectoryNotFoundException("Could not locate SquareOrchestrator.slnx from the test output directory.");
}
