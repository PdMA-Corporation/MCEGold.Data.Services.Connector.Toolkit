using System.Text.Json.Nodes;

namespace MCEGold.Data.Services.Connector.Cli.Output;

/// <summary>
/// Stable top-level JSON object written by every CLI command.
/// </summary>
public sealed class CliEnvelope
{
    public string SchemaVersion { get; init; } = "1.0";
    public bool Success { get; init; }
    public string Command { get; init; } = string.Empty;
    public string TimestampUtc { get; init; } = DateTime.UtcNow.ToString("O");
    public JsonNode? Data { get; init; }
    public CliFault? Fault { get; init; }
    public JsonNode? Raw { get; init; }

    /// <summary>
    /// Creates a successful envelope with command-specific data.
    /// </summary>
    public static CliEnvelope Succeeded(string command, object? data = null, JsonNode? raw = null)
    {
        return new CliEnvelope
        {
            Success = true,
            Command = command,
            Data = JsonValueConverter.ToNode(data),
            Raw = raw
        };
    }

    /// <summary>
    /// Creates a failed envelope with one normalized fault object.
    /// </summary>
    public static CliEnvelope Failed(string command, CliFault fault, JsonNode? raw = null)
    {
        return new CliEnvelope
        {
            Success = false,
            Command = command,
            Fault = fault,
            Raw = raw
        };
    }
}

/// <summary>
/// Normalized failure details exposed by the CLI instead of connector-specific fault shapes.
/// </summary>
public sealed class CliFault
{
    public string Category { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? StatusCode { get; init; }
    public IReadOnlyList<CliFaultDetail> Details { get; init; } = [];
}

/// <summary>
/// Field-level validation or cleanup detail attached to a CLI fault.
/// </summary>
public sealed class CliFaultDetail
{
    public string Field { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Couples the JSON envelope with the process exit code that should be returned.
/// </summary>
public sealed record CliExecutionResult(CliEnvelope Envelope, int ExitCode);
