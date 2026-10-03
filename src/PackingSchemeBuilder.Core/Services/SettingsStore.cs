using System.Text.Json;

namespace PackingSchemeBuilder.Core.Services;

public sealed record AppSettings(string ApiBaseUrl)
{
    public static AppSettings Default { get; } = new("http://promark94.marking.by/");
}

/// <summary>Remembers settings between sessions.</summary>
public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}

public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    public static JsonSettingsStore CreateDefault() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PackingSchemeBuilder", "settings.json"));

    public AppSettings Load()
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? AppSettings.Default : AppSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing a setting is not worth interrupting the user for.
        }
    }
}
