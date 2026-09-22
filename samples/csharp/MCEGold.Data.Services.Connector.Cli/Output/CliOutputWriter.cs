using System.Text.Json;

namespace MCEGold.Data.Services.Connector.Cli.Output;

public sealed class CliOutputWriter
{
    private readonly TextWriter output;

    public CliOutputWriter(TextWriter output)
    {
        this.output = output;
    }

    public void Write(CliEnvelope envelope, string? format)
    {
        string normalized = string.IsNullOrWhiteSpace(format) ? "json" : format.Trim().ToLowerInvariant();

        if (normalized == "text")
        {
            output.WriteLine(envelope.Success
                ? $"{envelope.Command}: success"
                : $"{envelope.Command}: {envelope.Fault?.Code}: {envelope.Fault?.Message}");
            return;
        }

        if (normalized == "payload" && envelope.Success && envelope.Data is not null)
        {
            var payload = envelope.Data["payload"] ?? envelope.Data["bod"];
            if (payload is not null)
            {
                output.WriteLine(payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
                return;
            }
        }

        bool pretty = normalized == "json-pretty";
        output.WriteLine(JsonSerializer.Serialize(
            envelope,
            new JsonSerializerOptions(JsonValueConverter.SerializerOptions) { WriteIndented = pretty }));
    }
}
