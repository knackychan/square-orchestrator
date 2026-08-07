using System.Windows;

namespace Square.SharedUiProof.WebView2;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = new MainWindow(ProgramOptions.Parse(e.Args));
        MainWindow.Show();
    }
}
