using System.Text.Json.Nodes;
using MCEGold.Data.Services.Connector.Cli.Configuration;
using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Enums;
using CommandOptions = MCEGold.Data.Services.Connector.Commands.ConnectorCommandOptions;

namespace MCEGold.Data.Services.Connector.Cli.Commands;

/// <summary>
/// Shared projection helpers for workflow-style connector results.
/// </summary>
internal static class WorkflowSupport
{
    /// <summary>
    /// Builds legacy connector options still used by staged Request commands and atomic workflows.
    /// </summary>
    public static CommandOptions CreateOptions(CliSettings settings, bool includeRaw)
    {
        return new CommandOptions
        {
            Host = settings.Host,
            AuthenticationScheme = ParseAuthenticationScheme(settings.AuthenticationScheme),
            ApiKey = settings.ApiKey,
            UserName = settings.UserName,
            Password = settings.Password,
            PayloadProfile = settings.Publication.PayloadProfile,
            IncludeRawResponse = includeRaw
        };
    }

    public static WorkflowStep ToStep(string name, ConnectorCommandResult result)
    {
        return new WorkflowStep
        {
            Name = name,
            Success = result.Success,
            StatusCode = result.StatusCode == 0 ? null : result.StatusCode,
            SessionId = EmptyToNull(result.SessionId),
            MessageId = EmptyToNull(result.MessageId),
            Payload = JsonValueConverter.ParseContent(result.Payload)
        };
    }

    public static CliExecutionResult Failure(
        string command,
        ConnectorCommandResult result,
        ConnectorCommandResult? cleanupFailure = null,
        bool includeRaw = false)
    {
        int exitCode;
        string category;

        // The connector reports operation status; the CLI maps it to stable process categories.
        if (string.Equals(result.ErrorCode, "UnexpectedWorkflowFailure", StringComparison.OrdinalIgnoreCase))
        {
            category = "internal";
            exitCode = ExitCodes.Unexpected;
        }
        else if (result.StatusCode == 401)
        {
            category = "authentication";
            exitCode = ExitCodes.Authentication;
        }
        else if (result.StatusCode == 403)
        {
            category = "authorization";
            exitCode = ExitCodes.Authentication;
        }
        else if (result.StatusCode > 0)
        {
            category = "remote";
            exitCode = ExitCodes.RemoteOperation;
        }
        else if (string.Equals(result.ErrorCode, "ValidationFailed", StringComparison.OrdinalIgnoreCase))
        {
            category = "validation";
            exitCode = ExitCodes.UsageOrValidation;
        }
        else
        {
            category = "transport";
            exitCode = ExitCodes.Transport;
        }

        var details = result.ValidationErrors
            .Select(message => new CliFaultDetail { Field = string.Empty, Code = result.ErrorCode, Message = message })
            .ToList();

        if (cleanupFailure is not null)
        {
            details.Add(new CliFaultDetail
            {
                Field = "cleanup",
                Code = string.IsNullOrWhiteSpace(cleanupFailure.ErrorCode) ? "CleanupFailed" : cleanupFailure.ErrorCode,
                Message = string.IsNullOrWhiteSpace(cleanupFailure.ErrorMessage)
                    ? cleanupFailure.ReasonPhrase
                    : cleanupFailure.ErrorMessage
            });
        }

        var fault = new CliFault
        {
            Category = category,
            Code = string.IsNullOrWhiteSpace(result.ErrorCode) ? "CommandFailed" : result.ErrorCode,
            Message = string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.ReasonPhrase : result.ErrorMessage,
            StatusCode = result.StatusCode == 0 ? null : result.StatusCode,
            Details = details
        };

        // Raw transport data is opt-in and remains outside the normalized fault object.
        var envelope = new CliEnvelope
        {
            Success = false,
            Command = command,
            Fault = fault,
            Raw = includeRaw ? JsonValueConverter.ParseContent(result.Raw) : null
        };

        return new CliExecutionResult(envelope, exitCode);
    }

    private static AuthenticationSchemeType ParseAuthenticationScheme(string value)
    {
        return string.Equals(value, "Basic", StringComparison.OrdinalIgnoreCase)
            ? AuthenticationSchemeType.Basic
            : AuthenticationSchemeType.BasicApi;
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

internal sealed class WorkflowStep
{
    public string Name { get; init; } = string.Empty;
    public bool Success { get; init; }
    public int? StatusCode { get; init; }
    public string? SessionId { get; init; }
    public string? MessageId { get; init; }
    public JsonNode? Payload { get; init; }
}
