using System.Text.Json;
using EQFR.Common;
using EQFR.EIFData.Layout;
using EQFR.EIFData.Process;
using EQFR.EIFData.Routes;
using EQFR.EIFData.Simulation;
using EQFR.EIFData.Transport;

namespace EQFR.IO.Config;

public sealed class JsonConfigLoader : IConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Result<ConfigBundle> LoadFromDirectory(string configDirectory)
    {
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            return Result<ConfigBundle>.Failure("config.dir.empty", "Config directory path is empty.");
        }

        if (!Directory.Exists(configDirectory))
        {
            return Result<ConfigBundle>.Failure("config.dir.not_found", $"Config directory not found: '{configDirectory}'.");
        }

        var layoutResult = LoadRequiredJson<FactoryLayoutConfig>(configDirectory, ConfigFiles.Layout);
        if (!layoutResult.IsSuccess) return Result<ConfigBundle>.Failure(layoutResult.Error!.Code, layoutResult.Error!.Message);

        var routesResult = LoadRequiredJson<FactoryRoutesConfig>(configDirectory, ConfigFiles.Routes);
        if (!routesResult.IsSuccess) return Result<ConfigBundle>.Failure(routesResult.Error!.Code, routesResult.Error!.Message);

        var processResult = LoadRequiredJson<FactoryProcessConfig>(configDirectory, ConfigFiles.Process);
        if (!processResult.IsSuccess) return Result<ConfigBundle>.Failure(processResult.Error!.Code, processResult.Error!.Message);

        var simulationResult = LoadRequiredJson<FactorySimulationConfig>(configDirectory, ConfigFiles.Simulation);
        if (!simulationResult.IsSuccess) return Result<ConfigBundle>.Failure(simulationResult.Error!.Code, simulationResult.Error!.Message);

        var transportResult = LoadRequiredJson<FactoryTransportConfig>(configDirectory, ConfigFiles.Transport);
        if (!transportResult.IsSuccess) return Result<ConfigBundle>.Failure(transportResult.Error!.Code, transportResult.Error!.Message);

        var bundle = new ConfigBundle(
            layoutResult.Value!,
            routesResult.Value!,
            processResult.Value!,
            simulationResult.Value!,
            transportResult.Value!
        );

        var validation = ConfigValidator.Validate(bundle);
        if (!validation.IsValid)
        {
            var message = string.Join(Environment.NewLine, validation.Errors.Select(e => $"- [{e.Code}] {e.Message}"));
            return Result<ConfigBundle>.Failure("config.validation.failed", message);
        }

        return Result<ConfigBundle>.Success(bundle);
    }

    private static Result<T> LoadRequiredJson<T>(string configDirectory, string fileName)
    {
        var path = Path.Combine(configDirectory, fileName);
        if (!File.Exists(path))
        {
            return Result<T>.Failure("config.file.missing", $"Missing required config file: '{fileName}'.");
        }

        try
        {
            var json = File.ReadAllText(path);
            var obj = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (obj is null)
            {
                return Result<T>.Failure("config.file.invalid", $"Config file '{fileName}' is invalid or empty.");
            }

            return Result<T>.Success(obj);
        }
        catch (JsonException ex)
        {
            return Result<T>.Failure("config.file.json_error", $"Failed to parse '{fileName}': {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure("config.file.read_error", $"Failed to read '{fileName}': {ex.Message}");
        }
    }
}
