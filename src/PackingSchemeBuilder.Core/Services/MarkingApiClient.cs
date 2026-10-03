using System.Net.Http.Json;
using System.Text.Json;
using PackingSchemeBuilder.Core.Models;

namespace PackingSchemeBuilder.Core.Services;

/// <summary>Source of the current packaging task.</summary>
public interface IMarkingApiClient
{
    Task<PackagingTask> GetCurrentTaskAsync(Uri baseAddress, CancellationToken cancellationToken = default);
}

public sealed class MarkingApiException(string message, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Reads the current task from the marking server (<c>GET client/api/get/task/</c>).</summary>
public sealed class MarkingApiClient(HttpClient http) : IMarkingApiClient
{
    public const string TaskPath = "client/api/get/task/";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<PackagingTask> GetCurrentTaskAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        TaskResponse? response;
        try
        {
            using var message = await http.GetAsync(new Uri(baseAddress, TaskPath), cancellationToken).ConfigureAwait(false);
            if (!message.IsSuccessStatusCode)
            {
                throw new MarkingApiException($"The marking server returned {(int)message.StatusCode} {message.ReasonPhrase}.");
            }

            response = await message.Content.ReadFromJsonAsync<TaskResponse>(Options, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new MarkingApiException($"Could not reach {baseAddress.Host}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MarkingApiException("The marking server did not respond in time.", ex);
        }
        catch (JsonException ex)
        {
            throw new MarkingApiException("The marking server returned data in an unexpected format.", ex);
        }

        var lot = response?.Mission?.Lot;
        if (lot?.Product is null || lot.Package is null)
        {
            throw new MarkingApiException("The marking server has no active packaging task.");
        }

        var task = new PackagingTask(lot.Product.Name ?? string.Empty, lot.Product.Gtin ?? string.Empty, lot.Package.Volume ?? string.Empty, lot.Package.BoxFormat, lot.Package.PalletFormat);
        return task.Validate() is { } error ? throw new MarkingApiException($"The task from the server is invalid: {error}") : task;
    }

    private sealed record TaskResponse(MissionDto? Mission);

    private sealed record MissionDto(LotDto? Lot);

    private sealed record LotDto(PackageDto? Package, ProductDto? Product);

    private sealed record PackageDto(string? Volume, int BoxFormat, int PalletFormat);

    private sealed record ProductDto(string? Name, string? Gtin);
}
