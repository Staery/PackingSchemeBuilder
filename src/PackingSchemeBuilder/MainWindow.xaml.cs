using System.Windows;
using PackingSchemeBuilder.Core.ViewModels;

namespace PackingSchemeBuilder;

/// <summary>Main window. All behaviour lives in <see cref="MainViewModel"/>.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeCommand.ExecuteAsync(null);
    }

    // TreeView.SelectedItem is read-only, so the selection is passed on here.
    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e) =>
        _viewModel.SelectedNode = e.NewValue as PackingNode;
}
