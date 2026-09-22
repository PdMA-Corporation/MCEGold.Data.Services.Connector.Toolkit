using System.Text.Json;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using MCEGold.Data.Services.Connector.Enums;
using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;
using CommandOptions = MCEGold.Data.Services.Connector.Commands.ConnectorCommandOptions;
using CommandResult = MCEGold.Data.Services.Connector.Commands.ConnectorCommandResult;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace RuntimeSmokeValidation.Scenarios;

internal static class ScenarioSupport
{
    public static readonly ConnectorRequestType[] RequestTypes =
    [
        ConnectorRequestType.GetSites,
        ConnectorRequestType.GetSegments,
        ConnectorRequestType.GetAssets,
        ConnectorRequestType.GetMeasurementLocations,
        ConnectorRequestType.GetMeasurements,
        ConnectorRequestType.GetAssessments,
        ConnectorRequestType.GetAssetSegmentEvents
    ];

    public static CommandOptions BaseOptions(RuntimeConfig config, string command)
    {
        return new CommandOptions
        {
            Command = command,
            Host = config.Host,
            AuthenticationScheme = config.AuthenticationScheme,
            ApiKey = config.ApiKey,
            UserName = config.UserName,
            Password = config.Password,
            IncludeRawResponse = false
        };
    }

    public static CommandOptions RequestOptions(RuntimeConfig config, RuntimeOptions options, string command)
    {
        var commandOptions = BaseOptions(config, command);
        if (options.CustomRequestRouting)
        {
            if (string.IsNullOrWhiteSpace(config.RequestChannel))
            {
                throw new InvalidOperationException("Custom request routing requires request.channel in runtime config.");
            }

            commandOptions.ChannelId = config.RequestChannel;
        }

        return commandOptions;
    }

    public static CommandOptions PublicationOptions(RuntimeConfig config, RuntimeOptions options, string command)
    {
        var commandOptions = BaseOptions(config, command);
        if (options.CustomPublicationRouting)
        {
            throw new InvalidOperationException("Custom publication routing is not implemented because runtime config does not define safe subscription topics.");
        }

        return commandOptions;
    }

    public static bool RemoveResponseIfRequested(
        CommandRunner runner,
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder,
        string sessionId,
        string requestMessageId,
        string stepName)
    {
        if (!options.RemoveResponse)
        {
            return true;
        }

        var removeOptions = RequestOptions(config, options, CommandRunner.RemoveResponse);
        removeOptions.SessionId = sessionId;
        removeOptions.MessageId = requestMessageId;
        var remove = recorder.RunStep(stepName, () => runner.Run(removeOptions));
        if (!remove.Success)
        {
            Console.Error.WriteLine($"{stepName}: cleanup FAIL ({StepRecorder.FormatFailure(remove)})");
        }

        return remove.Success;
    }

    public static ConnectorPostRequestOptions MinimalRequest(ConnectorRequestType requestType)
    {
        var options = new ConnectorPostRequestOptions
        {
            RequestType = requestType,
            PayloadProfile = PayloadProfileFor(requestType),
            MaxItems = MaxItemsFor(requestType)
        };

        short? lastNData = LastNDataFor(requestType);
        if (lastNData.HasValue)
        {
            options.LastNData = lastNData.Value;
        }

        return options;
    }

    private static string PayloadProfileFor(ConnectorRequestType requestType)
    {
        return requestType is ConnectorRequestType.GetMeasurements or
            ConnectorRequestType.GetAssessments or
            ConnectorRequestType.GetAssetSegmentEvents
            ? PayloadProfile.Full.ToString()
            : PayloadProfile.Minimal.ToString();
    }

    private static short MaxItemsFor(ConnectorRequestType requestType)
    {
        return requestType is ConnectorRequestType.GetMeasurements or
            ConnectorRequestType.GetAssessments or
            ConnectorRequestType.GetAssetSegmentEvents
            ? (short)100
            : (short)1;
    }

    private static short? LastNDataFor(ConnectorRequestType requestType)
    {
        return requestType switch
        {
            ConnectorRequestType.GetMeasurements => 3,
            ConnectorRequestType.GetAssessments => 5,
            ConnectorRequestType.GetAssetSegmentEvents => 5,
            _ => null
        };
    }

    public static string PostCommand(ConnectorRequestType type)
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

    public static async Task<ReadPollResult> PollReadResponseAsync(
        CommandRunner runner,
        RuntimeConfig config,
        string sessionId,
        string requestMessageId,
        RuntimeOptions options,
        StepRecorder recorder,
        string stepName)
    {
        await Task.Delay(TimeSpan.FromSeconds(options.InitialDelaySeconds));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        CommandResult? last = null;

        while (stopwatch.Elapsed <= TimeSpan.FromSeconds(options.TimeoutSeconds))
        {
            var readOptions = RequestOptions(config, options, CommandRunner.ReadResponse);
            readOptions.SessionId = sessionId;
            readOptions.MessageId = requestMessageId;

            last = recorder.RunStep(stepName, () => runner.Run(readOptions));
            if (last.Success)
            {
                return ReadPollResult.Done(last);
            }

            if (!IsRetryableReadResponse(last))
            {
                return ReadPollResult.Failed(
                    $"terminal or ambiguous read failure: {StepRecorder.FormatFailure(last)}",
                    stopCondition: IsAmbiguousReadResponse(last));
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds));
        }

        return ReadPollResult.Failed(
            $"response was not available within {options.TimeoutSeconds}s; last result: {StepRecorder.FormatFailure(last)}",
            stopCondition: false);
    }

    public static bool ValidateReadResponse(
        string stepName,
        CommandResult read,
        string requestMessageId)
    {
        if (!StepRecorder.RequireSuccess(stepName, read, "responseMessageId", value => !string.IsNullOrWhiteSpace(value.ResponseMessageId)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(read.RequestMessageId) &&
            !string.Equals(read.RequestMessageId, requestMessageId, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{stepName}: FAIL (response requestMessageId did not match posted request message ID)");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(read.Payload))
        {
            try
            {
                JsonDocument.Parse(read.Payload).Dispose();
            }
            catch (JsonException exception)
            {
                Console.Error.WriteLine($"{stepName}: FAIL (payload was not parseable JSON: {Sanitizer.Clean(exception.Message)})");
                return false;
            }
        }

        return true;
    }

    public static bool IsRetryableReadResponse(CommandResult result)
    {
        string code = result.ErrorCode ?? string.Empty;
        return string.Equals(code, "NoResponseAvailable", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "ResponseNotAvailable", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "ResponsePending", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAmbiguousReadResponse(CommandResult result)
    {
        return result.StatusCode == 404 &&
            string.Equals(result.ErrorCode, "CommandFailed", StringComparison.OrdinalIgnoreCase);
    }

    public sealed record ReadPollResult(bool Completed, CommandResult? Result, string FailureReason, bool StopCondition)
    {
        public static ReadPollResult Done(CommandResult result) => new(true, result, string.Empty, false);

        public static ReadPollResult Failed(string reason, bool stopCondition) => new(false, null, reason, stopCondition);
    }
}
