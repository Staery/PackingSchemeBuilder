using PackingSchemeBuilder.Core.Abstractions;
using PackingSchemeBuilder.Core.Data;
using PackingSchemeBuilder.Core.Models;
using PackingSchemeBuilder.Core.Services;
using PackingSchemeBuilder.Core.ViewModels;

namespace PackingSchemeBuilder.Core.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "psb-vm-" + Guid.NewGuid().ToString("N"));
    private readonly FakeApi _api = new();
    private readonly MemoryRepository _repository = new();
    private readonly MemorySettings _settings = new();
    private readonly FakeDialogs _dialogs = new();

    public MainViewModelTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private MainViewModel Create() => new(_api, _repository, _settings, _dialogs, TimeProvider.System);

    private string WriteCodes(PackagingTask task, int count, int seed = 1)
    {
        var path = Path.Combine(_folder, $"codes-{seed}.txt");
        File.WriteAllLines(path, SampleCodes.Generate(task.Gtin, count, new Random(seed)));
        return path;
    }

    [Fact]
    public async Task DemoTask_ThenImport_BuildsLayoutAndSaves()
    {
        var vm = Create();
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 100);

        await vm.ImportCodesCommand.ExecuteAsync(null);

        Assert.Equal(100, vm.Result!.BottleCount);
        Assert.Equal(9, vm.Result.BoxCount);
        Assert.Equal(2, vm.Pallets.Count);
        Assert.Equal(100, _repository.Saved!.BottleCount);
        Assert.Same(vm.Pallets[0], vm.SelectedNode);
        Assert.Equal("8 of 8 boxes · 96 bottles", vm.Pallets[0].Details);
        Assert.True(vm.Pallets[0].IsComplete);
        Assert.False(vm.Pallets[1].IsComplete);
        Assert.Contains("100 new codes", vm.StatusMessage);
    }

    [Fact]
    public async Task LoadDemoData_FromScratch_TakesDemoTaskAndPacksGeneratedCodes()
    {
        var vm = Create();
        Assert.True(vm.LoadDemoDataCommand.CanExecute(null));

        await vm.LoadDemoDataCommand.ExecuteAsync(null);

        var task = PackagingTask.Demo;
        var expected = task.BottlesPerPallet * 2 + task.BoxFormat * 3 + 5;
        Assert.Equal(task, vm.Task);
        Assert.Equal(expected, vm.Result!.BottleCount);
        Assert.Equal(3, vm.Pallets.Count);
        Assert.Same(vm.Pallets[0], vm.SelectedNode);
        Assert.Equal(expected, _repository.Saved!.BottleCount);
        Assert.StartsWith("Demo codes:", vm.StatusMessage);
        Assert.Contains("4", vm.StatusMessage);
    }

    [Fact]
    public async Task LoadDemoData_AddsToTheCurrentLayout()
    {
        var vm = Create();
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 10);
        await vm.ImportCodesCommand.ExecuteAsync(null);

        await vm.LoadDemoDataCommand.ExecuteAsync(null);

        var task = PackagingTask.Demo;
        Assert.Equal(10 + task.BottlesPerPallet * 2 + task.BoxFormat * 3 + 5, vm.Result!.BottleCount);
    }

    [Fact]
    public async Task SecondImport_AppendsAndSkipsDuplicates()
    {
        var vm = Create();
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 10, seed: 1);
        await vm.ImportCodesCommand.ExecuteAsync(null);

        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 10, seed: 1);
        await vm.ImportCodesCommand.ExecuteAsync(null);
        Assert.Equal(10, vm.Result!.BottleCount);
        Assert.Contains("10 duplicates", vm.StatusMessage);

        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 5, seed: 2);
        await vm.ImportCodesCommand.ExecuteAsync(null);
        Assert.Equal(15, vm.Result.BottleCount);
    }

    [Fact]
    public async Task LoadTask_FromApi_SavesAddress()
    {
        var vm = Create();
        vm.ApiBaseUrl = "http://marking.local/";

        await vm.LoadTaskCommand.ExecuteAsync(null);

        Assert.Equal(_api.Task, vm.Task);
        Assert.Equal("http://marking.local/", _settings.Saved?.ApiBaseUrl);
        Assert.Contains("marking.local", vm.TaskSource);
    }

    [Fact]
    public async Task LoadTask_ErrorIsShown()
    {
        _api.Error = new MarkingApiException("Could not reach server");
        var vm = Create();

        await vm.LoadTaskCommand.ExecuteAsync(null);

        Assert.Null(vm.Task);
        Assert.Contains("Could not reach", Assert.Single(_dialogs.Errors));
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Import_IsDisabledWithoutTask()
    {
        var vm = Create();

        Assert.False(vm.ImportCodesCommand.CanExecute(null));
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        Assert.True(vm.ImportCodesCommand.CanExecute(null));
    }

    [Fact]
    public async Task Initialize_RestoresSavedLayout()
    {
        var codes = SampleCodes.Generate(PackagingTask.Demo.Gtin, 20, new Random(3)).Select(Gs1.Clean).ToList();
        _repository.Saved = PackingPlanner.Plan(PackagingTask.Demo, codes);
        var vm = Create();

        await vm.InitializeCommand.ExecuteAsync(null);

        Assert.Equal(PackagingTask.Demo, vm.Task);
        Assert.Equal(20, vm.Result!.BottleCount);
        Assert.Single(vm.Pallets);
    }

    [Fact]
    public async Task Clear_AsksAndRemovesLayout()
    {
        var vm = Create();
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 10);
        await vm.ImportCodesCommand.ExecuteAsync(null);

        _dialogs.ConfirmResult = false;
        await vm.ClearCommand.ExecuteAsync(null);
        Assert.True(vm.HasLayout);

        _dialogs.ConfirmResult = true;
        await vm.ClearCommand.ExecuteAsync(null);
        Assert.False(vm.HasLayout);
        Assert.True(_repository.Cleared);
    }

    [Fact]
    public async Task Export_WritesJson()
    {
        var vm = Create();
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        _dialogs.OpenPath = WriteCodes(PackagingTask.Demo, 30);
        await vm.ImportCodesCommand.ExecuteAsync(null);
        _dialogs.SavePath = Path.Combine(_folder, "layout.json");

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Contains("\"pallet\"", await File.ReadAllTextAsync(_dialogs.SavePath));
    }

    [Fact]
    public async Task SampleFile_ContainsCodesForTaskAndAnotherProduct()
    {
        var vm = Create();
        await vm.UseDemoTaskCommand.ExecuteAsync(null);
        _dialogs.SavePath = Path.Combine(_folder, "sample.txt");

        await vm.CreateSampleFileCommand.ExecuteAsync(null);

        var report = CodeImporter.Import(await File.ReadAllLinesAsync(_dialogs.SavePath), PackagingTask.Demo.Gtin);
        Assert.Equal(4, report.OtherProduct);
        Assert.True(report.Accepted.Count > PackagingTask.Demo.BottlesPerPallet * 2);
    }

    private sealed class FakeApi : IMarkingApiClient
    {
        public PackagingTask Task { get; } = new("Juice", "04810000123458", "1.0", 6, 10);

        public MarkingApiException? Error { get; set; }

        public Task<PackagingTask> GetCurrentTaskAsync(Uri baseAddress, CancellationToken cancellationToken = default) =>
            Error is null ? System.Threading.Tasks.Task.FromResult(Task) : System.Threading.Tasks.Task.FromException<PackagingTask>(Error);
    }

    private sealed class MemoryRepository : IPackingRepository
    {
        public PackingResult? Saved { get; set; }

        public bool Cleared { get; private set; }

        public string Location => "memory";

        public Task<PackingResult?> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Saved);

        public Task SaveAsync(PackingResult result, CancellationToken cancellationToken = default)
        {
            Saved = result;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Cleared = true;
            Saved = null;
            return Task.CompletedTask;
        }
    }

    private sealed class MemorySettings : ISettingsStore
    {
        public AppSettings? Saved { get; private set; }

        public AppSettings Load() => Saved ?? AppSettings.Default;

        public void Save(AppSettings settings) => Saved = settings;
    }

    private sealed class FakeDialogs : IDialogService
    {
        public List<string> Errors { get; } = [];

        public bool ConfirmResult { get; set; } = true;

        public string? OpenPath { get; set; }

        public string? SavePath { get; set; }

        public void ShowError(string title, string message) => Errors.Add(message);

        public bool Confirm(string title, string message) => ConfirmResult;

        public string? PickCodesFile() => OpenPath;

        public string? PickSavePath(string suggestedFileName, string filter) => SavePath;
    }
}
