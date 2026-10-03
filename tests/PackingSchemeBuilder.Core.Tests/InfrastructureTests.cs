using System.Net;
using System.Text;
using PackingSchemeBuilder.Core.Data;
using PackingSchemeBuilder.Core.Models;
using PackingSchemeBuilder.Core.Services;

namespace PackingSchemeBuilder.Core.Tests;

public sealed class InfrastructureTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "psb-tests-" + Guid.NewGuid().ToString("N"));

    public InfrastructureTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task Repository_RoundTripsAndReplaces()
    {
        var repository = PackingRepository.ForFile(Path.Combine(_folder, "test.db"));
        var task = PackagingTask.Demo;
        var codes = SampleCodes.Generate(task.Gtin, 30, new Random(2)).Select(Gs1.Clean).ToList();

        Assert.Null(await repository.LoadAsync());

        await repository.SaveAsync(PackingPlanner.Plan(task, codes));
        await repository.SaveAsync(PackingPlanner.Plan(task, codes.Take(13).ToList()));
        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(task, loaded.Task);
        Assert.Equal(13, loaded.BottleCount);
        Assert.Equal(codes.Take(13), loaded.Bottles.Select(b => b.Code));
        Assert.Equal(PackingPlanner.Plan(task, codes.Take(13).ToList()).Boxes.Select(b => b.Code), loaded.Boxes.Select(b => b.Code));

        await repository.ClearAsync();
        Assert.Null(await repository.LoadAsync());
    }

    [Fact]
    public async Task ApiClient_ParsesTask()
    {
        var handler = new StubHandler(_ => Json("""
            {"mission":{"lot":{"package":{"volume":"0.5","boxFormat":12,"palletFormat":40},"product":{"name":"Water","gtin":"04810000123458"}}}}
            """));
        var client = new MarkingApiClient(new HttpClient(handler));

        var task = await client.GetCurrentTaskAsync(new Uri("http://server/"));

        Assert.Equal(new PackagingTask("Water", "04810000123458", "0.5", 12, 40), task);
        Assert.Equal("http://server/client/api/get/task/", handler.LastUri);
    }

    [Theory]
    [InlineData("""{"mission":null}""", "no active")]
    [InlineData("""{"mission":{"lot":{"package":{"boxFormat":0,"palletFormat":1},"product":{"name":"A","gtin":"04810000123458"}}}}""", "invalid")]
    [InlineData("<html>", "unexpected format")]
    public async Task ApiClient_ReportsBadResponses(string body, string expected)
    {
        var client = new MarkingApiClient(new HttpClient(new StubHandler(_ => Json(body))));

        var error = await Assert.ThrowsAsync<MarkingApiException>(() => client.GetCurrentTaskAsync(new Uri("http://server/")));

        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public async Task ApiClient_ReportsNetworkErrors()
    {
        var client = new MarkingApiClient(new HttpClient(new StubHandler(_ => throw new HttpRequestException("refused"))));

        var error = await Assert.ThrowsAsync<MarkingApiException>(() => client.GetCurrentTaskAsync(new Uri("http://server/")));

        Assert.Contains("Could not reach server", error.Message);
    }

    [Fact]
    public void Settings_RoundTripAndFallBackToDefault()
    {
        var store = new JsonSettingsStore(Path.Combine(_folder, "nested", "settings.json"));
        Assert.Equal(AppSettings.Default, store.Load());

        store.Save(new AppSettings("http://other/"));
        Assert.Equal("http://other/", store.Load().ApiBaseUrl);

        File.WriteAllText(Path.Combine(_folder, "nested", "settings.json"), "{broken");
        Assert.Equal(AppSettings.Default, store.Load());
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri?.ToString();
            return Task.FromResult(respond(request));
        }
    }
}
