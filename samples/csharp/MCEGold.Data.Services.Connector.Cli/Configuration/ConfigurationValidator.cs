using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands.Requests;

namespace MCEGold.Data.Services.Connector.Cli.Configuration;

public static class ConfigurationValidator
{
    public static IReadOnlyList<CliFaultDetail> Validate(CliSettings settings, bool requireSecrets)
    {
        var errors = new List<CliFaultDetail>();

        if (!Uri.TryCreate(settings.Host, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            Add(errors, "host", "InvalidHost", "host must be an absolute HTTPS URI.");
        }

        bool basic = string.Equals(settings.AuthenticationScheme, "Basic", StringComparison.OrdinalIgnoreCase);
        bool basicApi = string.Equals(settings.AuthenticationScheme, "BasicApi", StringComparison.OrdinalIgnoreCase);
        if (!basic && !basicApi)
        {
            Add(errors, "authenticationScheme", "InvalidAuthenticationScheme", "authenticationScheme must be Basic or BasicApi.");
        }

        if (requireSecrets)
        {
            if (string.IsNullOrWhiteSpace(settings.UserName))
            {
                Add(errors, "userName", "SecretRequired", "A username is required. Prefer MCEGOLD_USERNAME.");
            }

            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                Add(errors, "password", "SecretRequired", "A password is required. Prefer MCEGOLD_PASSWORD or --password-file.");
            }

            if (basicApi && string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                Add(errors, "apiKey", "SecretRequired", "An API key is required for BasicApi. Prefer MCEGOLD_API_KEY or --api-key-file.");
            }
        }

        if (!new[] { "json", "json-pretty", "text", "payload" }.Contains(settings.Output.Format, StringComparer.OrdinalIgnoreCase))
        {
            Add(errors, "output.format", "InvalidOutputFormat", "output.format must be json, json-pretty, text, or payload.");
        }

        if (!PayloadProfileParser.TryParse(settings.Publication.PayloadProfile, out _))
        {
            Add(errors, "publication.payloadProfile", "InvalidPayloadProfile", "publication.payloadProfile must be Full or Minimal.");
        }

        if (!PayloadProfileParser.TryParse(settings.Request.PayloadProfile, out _))
        {
            Add(errors, "request.payloadProfile", "InvalidPayloadProfile", "request.payloadProfile must be Full or Minimal.");
        }

        return errors;
    }

    public static object Redact(CliSettings settings)
    {
        return new
        {
            Host = RedactUri(settings.Host),
            settings.AuthenticationScheme,
            ApiKey = Redacted(settings.ApiKey),
            UserName = Redacted(settings.UserName),
            Password = Redacted(settings.Password),
            settings.Publication,
            settings.Request,
            settings.Output
        };
    }

    private static string Redacted(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : "***REDACTED***";

    private static string RedactUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        var authority = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        return $"{uri.Scheme}://{authority}{uri.AbsolutePath}";
    }

    private static void Add(ICollection<CliFaultDetail> errors, string field, string code, string message)
    {
        errors.Add(new CliFaultDetail { Field = field, Code = code, Message = message });
    }
}
