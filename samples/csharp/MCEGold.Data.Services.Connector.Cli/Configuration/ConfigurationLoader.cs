using System.Text.Json;
using MCEGold.Data.Services.Connector.Cli.Output;

namespace MCEGold.Data.Services.Connector.Cli.Configuration;

public sealed class ConfigurationLoader
{
    private readonly Func<string, string?> getEnvironmentVariable;

    public ConfigurationLoader(Func<string, string?>? getEnvironmentVariable = null)
    {
        this.getEnvironmentVariable = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
    }

    public CliSettings Load(string path, string? apiKeyFile = null, string? passwordFile = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CliConfigurationException("ConfigPathRequired", "A config path is required.", "config");
        }

        if (!File.Exists(path))
        {
            throw new CliConfigurationException("ConfigNotFound", $"Config file was not found: {path}", "config");
        }

        CliSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<CliSettings>(
                File.ReadAllText(path),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
                }) ?? new CliSettings();
        }
        catch (JsonException exception)
        {
            throw new CliConfigurationException("InvalidConfigJson", exception.Message, "config");
        }
        catch (IOException exception)
        {
            throw new CliConfigurationException("ConfigReadFailed", exception.Message, "config");
        }

        settings.Host = Pick(getEnvironmentVariable("MCEGOLD_HOST"), settings.Host);
        settings.AuthenticationScheme = Pick(getEnvironmentVariable("MCEGOLD_AUTH_SCHEME"), settings.AuthenticationScheme);
        settings.ApiKey = Pick(getEnvironmentVariable("MCEGOLD_API_KEY"), settings.ApiKey);
        settings.UserName = Pick(getEnvironmentVariable("MCEGOLD_USERNAME"), settings.UserName);
        settings.Password = Pick(getEnvironmentVariable("MCEGOLD_PASSWORD"), settings.Password);

        settings.ApiKey = ReadSecretFile(apiKeyFile, settings.ApiKey, "api-key-file");
        settings.Password = ReadSecretFile(passwordFile, settings.Password, "password-file");
        settings.Publication ??= new PublicationSettings();
        settings.Request ??= new RequestWorkflowSettings();
        settings.Output ??= new OutputSettings();
        settings.Publication.PayloadProfile = Pick(null, settings.Publication.PayloadProfile);
        settings.Request.PayloadProfile = NullIfWhiteSpace(settings.Request.PayloadProfile);
        return settings;
    }

    private static string ReadSecretFile(string? path, string fallback, string field)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return fallback;
        }

        try
        {
            return File.ReadAllText(path).TrimEnd('\r', '\n');
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CliConfigurationException("SecretFileReadFailed", exception.Message, field);
        }
    }

    private static string Pick(string? preferred, string fallback)
    {
        return string.IsNullOrWhiteSpace(preferred) ? fallback ?? string.Empty : preferred;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

public sealed class CliConfigurationException : Exception
{
    public CliConfigurationException(string code, string message, string field)
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
            Category = "configuration",
            Code = Code,
            Message = Message,
            Details = [new CliFaultDetail { Field = Field, Code = Code, Message = Message }]
        };
    }
}
