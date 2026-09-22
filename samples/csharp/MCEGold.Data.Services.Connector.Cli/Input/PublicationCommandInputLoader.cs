using System.Text.Json;
using System.Text.Json.Serialization;
using MCEGold.Data.Services.Connector.Commands;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace MCEGold.Data.Services.Connector.Cli.Input;

/// <summary>
/// Loads publication short-command JSON supplied to CLI sample paths.
/// </summary>
public sealed class PublicationCommandInputLoader
{
    private readonly TextReader input;

    public PublicationCommandInputLoader(TextReader input)
    {
        this.input = input;
    }

    /// <summary>
    /// Reads a file or stdin and returns the connector DTO matching the command discriminator.
    /// </summary>
    public object Load(string path)
    {
        string json;
        try
        {
            json = path == "-" ? input.ReadToEnd() : File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new CliInputException("InputReadFailed", exception.Message, "input");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            // This loader chooses the DTO; connector RunJsonAsync owns execution validation.
            if (!document.RootElement.TryGetProperty("command", out var commandElement) ||
                commandElement.ValueKind != JsonValueKind.String)
            {
                throw new CliInputException(
                    "MissingCommand",
                    "The typed command input must contain command.",
                    "command");
            }

            string? command = commandElement.GetString();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            };
            options.Converters.Add(new JsonStringEnumConverter());

            // Keep this branch list small and explicit so sample JSON maps visibly to connector DTOs.
            if (string.Equals(command, CommandRunner.OpenSubscription, StringComparison.OrdinalIgnoreCase))
            {
                return JsonSerializer.Deserialize<OpenSubscriptionSessionCommand>(json, options)
                    ?? throw new CliInputException("InvalidInput", "The input document is empty.", "input");
            }

            if (string.Equals(command, CommandRunner.CloseSubscription, StringComparison.OrdinalIgnoreCase))
            {
                return JsonSerializer.Deserialize<CloseSubscriptionSessionCommand>(json, options)
                    ?? throw new CliInputException("InvalidInput", "The input document is empty.", "input");
            }

            throw new CliInputException(
                "UnsupportedCommand",
                "Typed JSON input supports only OpenSubscription and CloseSubscription.",
                "command");
        }
        catch (CliInputException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new CliInputException("InvalidInputJson", exception.Message, "input");
        }
    }
}
