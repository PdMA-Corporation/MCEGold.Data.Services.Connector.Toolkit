using System.Text.Json;
using System.Text.Json.Serialization;
using MCEGold.Data.Services.Connector.Enums;

namespace RuntimeSmokeValidation.Configuration;

internal sealed class RuntimeConfig
{
    public string Host { get; private init; } = string.Empty;
    public AuthenticationSchemeType AuthenticationScheme { get; private init; }
    public string ApiKey { get; private init; } = string.Empty;
    public string UserName { get; private init; } = string.Empty;
    public string Password { get; private init; } = string.Empty;
    public string RequestChannel { get; private init; } = string.Empty;
    public string PublicationChannel { get; private init; } = string.Empty;

    public static RuntimeConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Config file was not found: {path}");
        }

        return LoadJson(File.ReadAllText(path), path);
    }

    public static RuntimeConfig LoadJson(string json, string source)
    {
        ConfigDocument document = JsonSerializer.Deserialize<ConfigDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            }) ?? new ConfigDocument();

        var errors = new List<string>();
        Require(document.Host, "host", errors);
        Require(document.AuthenticationScheme, "authenticationScheme", errors);
        Require(document.UserName, "userName", errors);
        Require(document.Password, "password", errors);

        AuthenticationSchemeType authenticationScheme = AuthenticationSchemeType.BasicApi;
        if (!string.IsNullOrWhiteSpace(document.AuthenticationScheme) &&
            !Enum.TryParse(document.AuthenticationScheme, ignoreCase: true, out authenticationScheme))
        {
            errors.Add("authenticationScheme must be Basic or BasicApi.");
        }

        if (authenticationScheme != AuthenticationSchemeType.Basic &&
            authenticationScheme != AuthenticationSchemeType.BasicApi)
        {
            errors.Add("authenticationScheme must be Basic or BasicApi.");
        }

        if (authenticationScheme == AuthenticationSchemeType.BasicApi)
        {
            Require(document.ApiKey, "apiKey", errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Invalid runtime config ({source}): " + string.Join("; ", errors));
        }

        return new RuntimeConfig
        {
            Host = document.Host!,
            AuthenticationScheme = authenticationScheme,
            ApiKey = document.ApiKey ?? string.Empty,
            UserName = document.UserName!,
            Password = document.Password!,
            RequestChannel = document.Request?.Channel ?? string.Empty,
            PublicationChannel = document.Publication?.Channel ?? string.Empty
        };
    }

    private static void Require(string? value, string field, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
        }
    }

    private sealed class ConfigDocument
    {
        public string? Host { get; set; }
        public string? AuthenticationScheme { get; set; }
        public string? ApiKey { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public ConfigPublication? Publication { get; set; }
        public ConfigRequest? Request { get; set; }
    }

    private sealed class ConfigPublication
    {
        public string? Channel { get; set; }
    }

    private sealed class ConfigRequest
    {
        public string? Channel { get; set; }
        public string? PayloadProfile { get; set; }
        public bool ReadResponse { get; set; }
        public bool RemoveResponseOnSuccess { get; set; }
    }
}
