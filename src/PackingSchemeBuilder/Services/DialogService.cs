using System.Windows;
using Microsoft.Win32;
using PackingSchemeBuilder.Core.Abstractions;

namespace PackingSchemeBuilder.Services;

/// <summary>Message boxes and file dialogs, owned by the main window.</summary>
internal sealed class DialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow is { IsVisible: true } window ? window : null;

    public void ShowError(string title, string message) => Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string title, string message) =>
        Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? PickCodesFile() => ShowDialog(new OpenFileDialog
    {
        Title = "Import unit codes",
        Filter = "Text files (*.txt;*.csv)|*.txt;*.csv|All files (*.*)|*.*",
    });

    public string? PickSavePath(string suggestedFileName, string filter) => ShowDialog(new SaveFileDialog
    {
        FileName = suggestedFileName,
        Filter = filter,
    });

    private static string? ShowDialog(FileDialog dialog)
    {
        var owner = Owner;
        var accepted = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return accepted == true ? dialog.FileName : null;
    }

    private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image) =>
        Owner is { } owner ? MessageBox.Show(owner, message, title, buttons, image) : MessageBox.Show(message, title, buttons, image);
}
