using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackingSchemeBuilder.Core.Abstractions;
using PackingSchemeBuilder.Core.Data;
using PackingSchemeBuilder.Core.Models;
using PackingSchemeBuilder.Core.Services;

namespace PackingSchemeBuilder.Core.ViewModels;

/// <summary>Task, code import, packing layout and export.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IMarkingApiClient _api;
    private readonly IPackingRepository _repository;
    private readonly ISettingsStore _settings;
    private readonly IDialogService _dialogs;
    private readonly TimeProvider _time;
    private readonly List<string> _codes = [];

    [ObservableProperty]
    private string _apiBaseUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTask))]
    [NotifyCanExecuteChangedFor(nameof(ImportCodesCommand), nameof(CreateSampleFileCommand))]
    private PackagingTask? _task;

    [ObservableProperty]
    private string _taskSource = "No task loaded";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLayout))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand), nameof(ClearCommand))]
    private PackingResult? _result;

    [ObservableProperty]
    private PackingNode? _selectedNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadTaskCommand), nameof(ImportCodesCommand), nameof(LoadDemoDataCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MainViewModel(IMarkingApiClient api, IPackingRepository repository, ISettingsStore settings, IDialogService dialogs, TimeProvider time)
    {
        _api = api;
        _repository = repository;
        _settings = settings;
        _dialogs = dialogs;
        _time = time;
        _apiBaseUrl = settings.Load().ApiBaseUrl;
    }

    public ObservableCollection<PalletNode> Pallets { get; } = [];

    public bool HasTask => Task is not null;

    public bool HasLayout => Result is { BottleCount: > 0 };

    public string StorageLocation => _repository.Location;

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            if (await _repository.LoadAsync() is { } saved)
            {
                Task = saved.Task;
                TaskSource = "Restored from the local database";
                _codes.AddRange(saved.Bottles.Select(bottle => bottle.Code));
                ShowResult(saved);
                StatusMessage = $"Restored {saved.BottleCount:N0} bottles in {saved.BoxCount:N0} boxes on {saved.PalletCount:N0} pallets.";
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        {
            _dialogs.ShowError("The local database could not be read", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task LoadTaskAsync()
    {
        if (!Uri.TryCreate(ApiBaseUrl?.Trim(), UriKind.Absolute, out var address))
        {
            _dialogs.ShowError("Invalid address", "Enter the full address of the marking server, e.g. http://server/.");
            return;
        }

        IsBusy = true;
        StatusMessage = $"Requesting the current task from {address.Host}…";
        try
        {
            var task = await _api.GetCurrentTaskAsync(address);
            _settings.Save(new AppSettings(address.ToString()));
            await StartTaskAsync(task, $"Loaded from {address.Host}");
        }
        catch (MarkingApiException ex)
        {
            _dialogs.ShowError("The task could not be loaded", ex.Message);
            StatusMessage = "Loading the task failed. You can try the demo task instead.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UseDemoTaskAsync() => await StartTaskAsync(PackagingTask.Demo, "Demo task");

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportCodesAsync()
    {
        var path = _dialogs.PickCodesFile();
        if (path is null)
        {
            return;
        }

        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Codes could not be imported", ex.Message);
            return;
        }

        await ImportLinesAsync(lines, Path.GetFileName(path));
    }

    /// <summary>
    /// One click for a first look: takes the demo task (unless a task is already loaded) and packs a set of
    /// generated codes, including a few codes of another product that the importer filters out.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task LoadDemoDataAsync()
    {
        if (Task is null)
        {
            await StartTaskAsync(PackagingTask.Demo, "Demo task");
        }

        if (Task is null)
        {
            return;
        }

        await ImportLinesAsync(GenerateSampleCodes(Task), "Demo codes");
    }

    private async Task ImportLinesAsync(IEnumerable<string> lines, string source)
    {
        IsBusy = true;
        try
        {
            var report = CodeImporter.Import(lines, Task!.Gtin, new HashSet<string>(_codes, StringComparer.Ordinal));

            _codes.AddRange(report.Accepted);
            var result = PackingPlanner.Plan(Task, _codes);
            await _repository.SaveAsync(result);
            ShowResult(result);

            StatusMessage = $"{source}: {report.Summary}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        {
            _dialogs.ShowError("Codes could not be imported", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Two full pallets, three more boxes and a few loose bottles, plus four codes of another product.</summary>
    private static IEnumerable<string> GenerateSampleCodes(PackagingTask task)
    {
        var count = task.BottlesPerPallet * 2 + task.BoxFormat * 3 + 5;
        return SampleCodes.Generate(task.Gtin, count, Random.Shared)
            .Concat(SampleCodes.Generate(Gs1.WithCheckDigit("0460000000000"), 4, Random.Shared));
    }

    private bool CanImport() => HasTask && !IsBusy;

    [RelayCommand(CanExecute = nameof(HasTask))]
    private async Task CreateSampleFileAsync()
    {
        var path = _dialogs.PickSavePath($"{Task!.Gtin}_sample_codes.txt", "Text file (*.txt)|*.txt");
        if (path is null)
        {
            return;
        }

        var lines = GenerateSampleCodes(Task).ToList();
        await File.WriteAllLinesAsync(path, lines);
        StatusMessage = $"Created {lines.Count - 4:N0} sample codes (plus 4 for another product) in {path}";
    }

    [RelayCommand(CanExecute = nameof(HasLayout))]
    private async Task ExportAsync()
    {
        var path = _dialogs.PickSavePath(LayoutMapExporter.SuggestFileName(Result!.Task, _time.GetLocalNow()), "JSON file (*.json)|*.json");
        if (path is null)
        {
            return;
        }

        try
        {
            await LayoutMapExporter.ExportAsync(Result, path);
            StatusMessage = $"Layout map with {Result.PalletCount} pallets saved to {path}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Export failed", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(HasLayout))]
    private async Task ClearAsync()
    {
        if (!_dialogs.Confirm("Clear layout", "Remove all imported codes and the packing layout?"))
        {
            return;
        }

        _codes.Clear();
        await _repository.ClearAsync();
        ShowResult(Task is null ? null : PackingResult.Empty(Task));
        StatusMessage = "Layout cleared.";
    }

    private bool IsIdle() => !IsBusy;

    private async Task StartTaskAsync(PackagingTask task, string source)
    {
        if (task.Validate() is { } error)
        {
            _dialogs.ShowError("Invalid task", error);
            return;
        }

        if (_codes.Count > 0 && Task != task)
        {
            if (!_dialogs.Confirm("New task", "Starting a new task removes the current layout. Continue?"))
            {
                return;
            }

            _codes.Clear();
            await _repository.ClearAsync();
        }

        Task = task;
        TaskSource = source;
        ShowResult(PackingPlanner.Plan(task, _codes));
        StatusMessage = $"Task: {task.ProductName}. Import a file with unit codes to build the layout.";
    }

    private void ShowResult(PackingResult? result)
    {
        Result = result;
        Pallets.Clear();
        if (result is not null)
        {
            foreach (var pallet in result.Pallets)
            {
                Pallets.Add(new PalletNode(pallet, result.Task));
            }
        }

        SelectedNode = Pallets.FirstOrDefault();
    }
}
