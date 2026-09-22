namespace MCEGold.Data.Services.Connector.Cli.Configuration;

public sealed class CliSettings
{
    public string Host { get; set; } = string.Empty;
    public string AuthenticationScheme { get; set; } = "BasicApi";
    public string ApiKey { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public PublicationSettings Publication { get; set; } = new();
    public RequestWorkflowSettings Request { get; set; } = new();
    public OutputSettings Output { get; set; } = new();
}

public sealed class PublicationSettings
{
    public string Channel { get; set; } = string.Empty;
    public string PayloadProfile { get; set; } = "Full";
}

public sealed class RequestWorkflowSettings
{
    public string Channel { get; set; } = string.Empty;
    public string? PayloadProfile { get; set; }
    public bool ReadResponse { get; set; } = true;
    public bool RemoveResponseOnSuccess { get; set; } = true;
}

public sealed class OutputSettings
{
    public string Format { get; set; } = "json";
    public bool IncludeRaw { get; set; }
}
