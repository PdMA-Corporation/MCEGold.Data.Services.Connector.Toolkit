using System.Text.Json;
using System.Text.Json.Serialization;
using MCEGold.Data.Services.Connector.Cli.Configuration;
using MCEGold.Data.Services.Connector.Cli.Input;
using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using MCEGold.Data.Services.Connector.Enums;
using CommandOptions = MCEGold.Data.Services.Connector.Commands.ConnectorCommandOptions;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace MCEGold.Data.Services.Connector.Cli.Commands;

/// <summary>
/// Implements staged session commands while preserving the public CLI envelope contract.
/// </summary>
public sealed class StagedSessionCommands
{
    private static readonly JsonSerializerOptions ConnectorCommandJsonOptions = CreateConnectorCommandJsonOptions();

    private readonly Func<CommandOptions, ConnectorCommandResult> runCommand;
    private readonly Func<string, ConnectorCommandResult> runJsonCommand;

    /// <summary>
    /// Allows tests to intercept connector execution without a live MCEGold service.
    /// </summary>
    public StagedSessionCommands(
        Func<CommandOptions, ConnectorCommandResult>? runCommand = null,
        Func<string, ConnectorCommandResult>? runJsonCommand = null)
    {
        this.runCommand = runCommand ?? (options => new CommandRunner().Run(options));
        this.runJsonCommand = runJsonCommand
            ?? (json => new CommandRunner().RunJsonAsync(json).GetAwaiter().GetResult());
    }

    /// <summary>
    /// Opens a publication subscription by translating CLI settings into connector short JSON.
    /// </summary>
    public CliExecutionResult OpenSubscription(CliSettings settings, bool includeRaw)
    {
        const string command = "publication.open-subscription";
        // CLI args/config -> connector short command DTO -> JSON -> RunJsonAsync.
        var request = new OpenSubscriptionSessionCommand
        {
            Command = CommandRunner.OpenSubscription,
            Host = settings.Host,
            AuthenticationScheme = ParseAuthenticationScheme(settings.AuthenticationScheme),
            ApiKey = settings.ApiKey,
            UserName = settings.UserName,
            Password = settings.Password,
            IncludeRawResponse = includeRaw
        };

        return ExecuteShortJson(
            command,
            request,
            result => new { statusCode = result.StatusCode, sessionId = result.SessionId },
            result => RequiredIdentifier(result.SessionId, "sessionId"));
    }

    /// <summary>
    /// Runs a loaded publication open command through the same short-JSON connector path.
    /// </summary>
    public CliExecutionResult OpenSubscription(OpenSubscriptionSessionCommand request)
    {
        const string command = "publication.open-subscription";
        return ExecuteShortJson(
            command,
            request,
            result => new { statusCode = result.StatusCode, sessionId = result.SessionId },
            result => RequiredIdentifier(result.SessionId, "sessionId"));
    }

    /// <summary>
    /// Reads one publication while keeping the public CLI command name in the output envelope.
    /// </summary>
    public CliExecutionResult ReadPublication(CliSettings settings, string sessionId, bool includeRaw)
    {
        const string command = "publication.read";
        // The envelope keeps CLI names; the short JSON uses connector command names.
        var request = new ReadPublicationCommand
        {
            Command = CommandRunner.ReadPublication,
            Host = settings.Host,
            AuthenticationScheme = ParseAuthenticationScheme(settings.AuthenticationScheme),
            ApiKey = settings.ApiKey,
            UserName = settings.UserName,
            Password = settings.Password,
            SessionId = sessionId,
            IncludeRawResponse = includeRaw
        };

        return ExecuteShortJson(
            command,
            request,
            result => new
            {
                statusCode = result.StatusCode,
                sessionId,
                messageId = result.MessageId,
                payload = JsonValueConverter.ParseContent(result.Payload)
            },
            result => RequiredIdentifier(result.MessageId, "messageId"));
    }

    /// <summary>
    /// Removes the current publication through connector short JSON.
    /// </summary>
    public CliExecutionResult RemovePublication(CliSettings settings, string sessionId, bool includeRaw)
    {
        const string command = "publication.remove";
        var request = new RemovePublicationCommand
        {
            Command = CommandRunner.RemovePublication,
            Host = settings.Host,
            AuthenticationScheme = ParseAuthenticationScheme(settings.AuthenticationScheme),
            ApiKey = settings.ApiKey,
            UserName = settings.UserName,
            Password = settings.Password,
            SessionId = sessionId,
            IncludeRawResponse = includeRaw
        };

        return ExecuteShortJson(command, request, result => new
        {
            statusCode = result.StatusCode,
            sessionId,
            removed = true
        });
    }

    /// <summary>
    /// Closes a publication subscription through connector short JSON.
    /// </summary>
    public CliExecutionResult CloseSubscription(CliSettings settings, string sessionId, bool includeRaw)
    {
        const string command = "publication.close-subscription";
        var request = new CloseSubscriptionSessionCommand
        {
            Command = CommandRunner.CloseSubscription,
            Host = settings.Host,
            AuthenticationScheme = ParseAuthenticationScheme(settings.AuthenticationScheme),
            ApiKey = settings.ApiKey,
            UserName = settings.UserName,
            Password = settings.Password,
            SessionId = sessionId,
            IncludeRawResponse = includeRaw
        };

        return ExecuteShortJson(command, request, result => new
        {
            statusCode = result.StatusCode,
            sessionId,
            closed = true
        });
    }

    /// <summary>
    /// Runs a loaded publication close command through the same short-JSON connector path.
    /// </summary>
    public CliExecutionResult CloseSubscription(CloseSubscriptionSessionCommand request)
    {
        const string command = "publication.close-subscription";
        return ExecuteShortJson(
            command,
            request,
            result => new
            {
                statusCode = result.StatusCode,
                sessionId = request.SessionId,
                closed = true
            });
    }

    public CliExecutionResult OpenRequestSession(CliSettings settings, bool includeRaw)
    {
        const string command = "request.open-session";
        var options = WorkflowSupport.CreateOptions(settings, includeRaw);
        options.Command = CommandRunner.OpenRequestSession;
        return Execute(
            command,
            options,
            result => new { statusCode = result.StatusCode, sessionId = result.SessionId },
            result => RequiredIdentifier(result.SessionId, "sessionId"));
    }

    public CliExecutionResult PostRequest(
        CliSettings settings,
        string sessionId,
        ConnectorPostRequestOptions request,
        bool includeRaw)
    {
        const string command = "request.post";
        var options = SessionOptions(settings, sessionId, includeRaw, PostCommand(request.RequestType));
        options.PostRequestOptions = request;
        return Execute(
            command,
            options,
            result => new
            {
                statusCode = result.StatusCode,
                sessionId,
                requestType = RequestTypeNames.ToKebabCase(request.RequestType),
                requestMessageId = result.MessageId
            },
            result => RequiredIdentifier(result.MessageId, "requestMessageId"));
    }

    public CliExecutionResult ReadResponse(
        CliSettings settings,
        string sessionId,
        string requestId,
        bool includeRaw)
    {
        const string command = "request.read-response";
        var options = SessionOptions(settings, sessionId, includeRaw, CommandRunner.ReadResponse);
        options.MessageId = requestId;
        return Execute(
            command,
            options,
            result => new
            {
                statusCode = result.StatusCode,
                sessionId,
                requestMessageId = requestId,
                responseMessageId = result.ResponseMessageId,
                payload = JsonValueConverter.ParseContent(result.Payload)
            },
            result => RequiredIdentifier(result.ResponseMessageId, "responseMessageId"));
    }

    public CliExecutionResult RemoveResponse(
        CliSettings settings,
        string sessionId,
        string requestId,
        bool includeRaw)
    {
        const string command = "request.remove-response";
        var options = SessionOptions(settings, sessionId, includeRaw, CommandRunner.RemoveResponse);
        options.MessageId = requestId;
        return Execute(command, options, result => new
        {
            statusCode = result.StatusCode,
            sessionId,
            requestMessageId = requestId,
            removed = true
        });
    }

    public CliExecutionResult CloseRequestSession(CliSettings settings, string sessionId, bool includeRaw)
    {
        const string command = "request.close-session";
        var options = SessionOptions(settings, sessionId, includeRaw, CommandRunner.CloseRequestSession);
        return Execute(command, options, result => new
        {
            statusCode = result.StatusCode,
            sessionId,
            closed = true
        });
    }

    private static CommandOptions SessionOptions(
        CliSettings settings,
        string sessionId,
        bool includeRaw,
        string command)
    {
        var options = WorkflowSupport.CreateOptions(settings, includeRaw);
        options.Command = command;
        options.SessionId = sessionId;
        return options;
    }

    private CliExecutionResult Execute(
        string command,
        CommandOptions options,
        Func<ConnectorCommandResult, object> selectData,
        Func<ConnectorCommandResult, string?>? validateSuccess = null)
    {
        var result = runCommand(options);
        if (!result.Success)
        {
            return WorkflowSupport.Failure(
                command,
                result,
                includeRaw: options.IncludeRawResponse);
        }

        string? contractError = validateSuccess?.Invoke(result);
        if (contractError is not null)
        {
            return new CliExecutionResult(
                CliEnvelope.Failed(command, new CliFault
                {
                    Category = "internal",
                    Code = "InvalidConnectorResponse",
                    Message = contractError
                }),
                ExitCodes.Unexpected);
        }

        return new CliExecutionResult(
            CliEnvelope.Succeeded(
                command,
                selectData(result),
                options.IncludeRawResponse
                    ? JsonValueConverter.ParseContent(result.Raw)
                    : null),
            ExitCodes.Success);
    }

    private CliExecutionResult ExecuteShortJson<TRequest>(
        string command,
        TRequest request,
        Func<ConnectorCommandResult, object> selectData,
        Func<ConnectorCommandResult, string?>? validateSuccess = null)
    {
        bool includeRaw = IncludeRaw(request);
        string json = JsonSerializer.Serialize(request, ConnectorCommandJsonOptions);
        var result = runJsonCommand(json);
        if (!result.Success)
        {
            return WorkflowSupport.Failure(
                command,
                result,
                includeRaw: includeRaw);
        }

        string? contractError = validateSuccess?.Invoke(result);
        if (contractError is not null)
        {
            return new CliExecutionResult(
                CliEnvelope.Failed(command, new CliFault
                {
                    Category = "internal",
                    Code = "InvalidConnectorResponse",
                    Message = contractError
                }),
                ExitCodes.Unexpected);
        }

        return new CliExecutionResult(
            CliEnvelope.Succeeded(
                command,
                // The selector is the boundary between connector results and the public CLI shape.
                selectData(result),
                includeRaw
                    ? JsonValueConverter.ParseContent(result.Raw)
                    : null),
            ExitCodes.Success);
    }

    private static bool IncludeRaw<TRequest>(TRequest request)
    {
        return request switch
        {
            OpenSubscriptionSessionCommand open => open.IncludeRawResponse,
            ReadPublicationCommand read => read.IncludeRawResponse,
            RemovePublicationCommand remove => remove.IncludeRawResponse,
            CloseSubscriptionSessionCommand close => close.IncludeRawResponse,
            _ => false
        };
    }

    private static AuthenticationSchemeType ParseAuthenticationScheme(string value)
    {
        return string.Equals(value, "Basic", StringComparison.OrdinalIgnoreCase)
            ? AuthenticationSchemeType.Basic
            : AuthenticationSchemeType.BasicApi;
    }

    private static JsonSerializerOptions CreateConnectorCommandJsonOptions()
    {
        // Match the connector's short-JSON names while using System.Text.Json in the CLI.
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string? RequiredIdentifier(string value, string field)
    {
        return string.IsNullOrWhiteSpace(value)
            ? $"The connector returned success without a required {field}."
            : null;
    }

    private static string PostCommand(ConnectorRequestType type)
    {
        return type switch
        {
            ConnectorRequestType.GetSites => CommandRunner.PostGetSitesRequest,
            ConnectorRequestType.GetSegments => CommandRunner.PostGetSegmentsRequest,
            ConnectorRequestType.GetAssets => CommandRunner.PostGetAssetsRequest,
            ConnectorRequestType.GetMeasurementLocations => CommandRunner.PostGetMeasurementLocationsRequest,
            ConnectorRequestType.GetMeasurements => CommandRunner.PostGetMeasurementsRequest,
            ConnectorRequestType.GetAssessments => CommandRunner.PostGetAssessmentsRequest,
            ConnectorRequestType.GetAssetSegmentEvents => CommandRunner.PostGetAssetSegmentEventsRequest,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported request type.")
        };
    }
}
