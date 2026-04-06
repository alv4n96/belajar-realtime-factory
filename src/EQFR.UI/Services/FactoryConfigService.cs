using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EQFR.UI.Services;

public sealed class FactoryConfigService(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<FactoryConfigBundle> GetAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("MockBackend");
        var payload = await client.GetFromJsonAsync<ConfigResponse>("/api/config", JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Config response was empty.");

        return new FactoryConfigBundle(
            payload.ConfigDir ?? string.Empty,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["layout"] = ToPrettyJson(payload.Bundle?.Layout),
                ["routes"] = ToPrettyJson(payload.Bundle?.Routes),
                ["transport"] = ToPrettyJson(payload.Bundle?.Transport),
                ["process"] = ToPrettyJson(payload.Bundle?.Process),
                ["simulation"] = ToPrettyJson(payload.Bundle?.Simulation)
            });
    }

    public async Task<T?> GetSectionAsync<T>(string section, CancellationToken cancellationToken = default)
    {
        var bundle = await GetAsync(cancellationToken);
        if (!bundle.Sections.TryGetValue(section, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SaveSectionAsync(string section, string json, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(json);
        var client = httpClientFactory.CreateClient("MockBackend");
        using var content = new StringContent(document.RootElement.GetRawText(), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"/api/config/{section}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task SaveSectionAsync<T>(string section, T payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return SaveSectionAsync(section, json, cancellationToken);
    }

    private static string ToPrettyJson(JsonElement? element)
    {
        if (element is null) return "{}";
        return JsonSerializer.Serialize(element.Value, JsonOptions);
    }

    private sealed record ConfigResponse(string? ConfigDir, ConfigBundlePayload? Bundle);

    private sealed record ConfigBundlePayload(
        JsonElement Layout,
        JsonElement Routes,
        JsonElement Process,
        JsonElement Simulation,
        JsonElement Transport);
}

public sealed record FactoryConfigBundle(string ConfigDir, IReadOnlyDictionary<string, string> Sections);
