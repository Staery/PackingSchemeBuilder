namespace PackingSchemeBuilder.Core.Abstractions;

/// <summary>User interaction that view models need but must not implement themselves.</summary>
public interface IDialogService
{
    void ShowError(string title, string message);

    bool Confirm(string title, string message);

    /// <summary>Asks for a text file with unit codes; <see langword="null"/> if cancelled.</summary>
    string? PickCodesFile();

    /// <summary>Asks where to save a file; <see langword="null"/> if cancelled.</summary>
    string? PickSavePath(string suggestedFileName, string filter);
}
