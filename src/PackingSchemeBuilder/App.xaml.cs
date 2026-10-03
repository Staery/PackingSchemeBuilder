using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using PackingSchemeBuilder.Core.Data;
using PackingSchemeBuilder.Core.Services;
using PackingSchemeBuilder.Core.ViewModels;
using PackingSchemeBuilder.Services;

namespace PackingSchemeBuilder;

/// <summary>Composition root: wires services and view models together and shows the main window.</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var viewModel = new MainViewModel(
            new MarkingApiClient(new HttpClient { Timeout = TimeSpan.FromSeconds(15) }),
            PackingRepository.CreateDefault(),
            JsonSettingsStore.CreateDefault(),
            new DialogService(),
            TimeProvider.System);

        MainWindow = new MainWindow(viewModel);
        MainWindow.Show();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Something went wrong:\n\n{e.Exception.Message}", "Packing Scheme Builder", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
