using System.Text.Json;
using System.Text.Json.Serialization;
using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands.Requests;

namespace MCEGold.Data.Services.Connector.Cli.Input;

/// <summary>
/// Loads the short request payload used by preview, request run, and staged request post.
/// </summary>
public sealed class RequestInputLoader
{
    private readonly TextReader input;

    public RequestInputLoader(TextReader input)
    {
        this.input = input;
    }

    /// <summary>
    /// Reads request JSON from a file or stdin and converts it to connector request options.
    /// </summary>
    public ConnectorPostRequestOptions Load(string path)
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
            // The CLI needs the request type before it can build preview JSON or select post behavior.
            if (!document.RootElement.TryGetProperty("requestType", out _))
            {
                throw new CliInputException(
                    "MissingRequestType",
                    "The short-form payload must contain requestType.",
                    "requestType");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            };
            options.Converters.Add(new JsonStringEnumConverter());

            var result = JsonSerializer.Deserialize<ConnectorPostRequestOptions>(json, options)
                ?? throw new CliInputException("InvalidInput", "The input document is empty.", "input");

            return result;
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

/// <summary>
/// Converts CLI input problems into the same fault envelope shape as command execution.
/// </summary>
public sealed class CliInputException : Exception
{
    public CliInputException(string code, string message, string field)
        : base(message)
    {
        Code = code;
        Field = field;
    }

    public string Code { get; }
    public string Field { get; }

    public CliFault ToFault()
    {
        return new CliFault
        {
            Category = "validation",
            Code = Code,
            Message = Message,
            Details = [new CliFaultDetail { Field = Field, Code = Code, Message = Message }]
        };
    }
}
