using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCEGold.Data.Services.Connector.Commands.Requests;
using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace RuntimeSmokeValidation.Scenarios;

internal static class CliContractScenarios
{
    public static async Task<ValidationSummary> RunAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var summary = new ValidationSummary();
        Console.WriteLine();
        Console.WriteLine("Minimal CLI contract scenarios");

        summary.Add(CheckVersion(recorder));
        summary.Add(CheckConfigValidation(config, recorder));
        summary.Add(CheckStdinPreviewContract(recorder));
        summary.Add(await CheckStagedRequestLifecycle(config, options, recorder));
        summary.Add(CheckStagedPublicationLifecycle(config, options, recorder));
        return summary;
    }

    private static ScenarioOutcome CheckVersion(StepRecorder recorder)
    {
        string connectorVersion = typeof(CommandRunner).Assembly.GetName().Version?.ToString() ?? string.Empty;
        bool passed = connectorVersion.StartsWith("1.2.0.0", StringComparison.Ordinal);
        recorder.WriteSyntheticStep("cli.version", passed, $"connectorVersion={connectorVersion}");
        return passed ? ScenarioOutcome.Pass("cli.version") : ScenarioOutcome.Fail("cli.version");
    }

    private static ScenarioOutcome CheckConfigValidation(RuntimeConfig config, StepRecorder recorder)
    {
        bool passed = Uri.TryCreate(config.Host, UriKind.Absolute, out Uri? uri) &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(config.UserName) &&
            !string.IsNullOrWhiteSpace(config.Password);
        recorder.WriteSyntheticStep("cli.config.validate", passed, "required non-secret configuration shape is valid");
        return passed ? ScenarioOutcome.Pass("cli.config.validate") : ScenarioOutcome.Fail("cli.config.validate");
    }

    private static ScenarioOutcome CheckStdinPreviewContract(StepRecorder recorder)
    {
        const string stdinJson = "{\"requestType\":\"GetSites\",\"payloadProfile\":\"Minimal\",\"maxItems\":1}";
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        ConnectorPostRequestOptions? request = JsonSerializer.Deserialize<ConnectorPostRequestOptions>(stdinJson, jsonOptions);
        bool parsed = request is not null;
        bool valid = parsed && new ConnectorRequestValidator().Validate(request!).IsValid;
        bool bodParseable = false;
        if (valid)
        {
            string bod = new ConnectorRequestJsonBuilder().BuildPreviewJson(request!);
            JsonDocument.Parse(bod).Dispose();
            bodParseable = true;
        }

        bool passed = parsed && valid && bodParseable;
        recorder.WriteSyntheticStep("cli.request.preview.stdin", passed, "short JSON from stdin validates and produces parseable preview BOD");
        return passed ? ScenarioOutcome.Pass("cli.request.preview.stdin") : ScenarioOutcome.Fail("cli.request.preview.stdin");
    }

    private static async Task<ScenarioOutcome> CheckStagedRequestLifecycle(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var runner = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        bool cleanupFailed = false;
        ScenarioOutcome outcome = ScenarioOutcome.Fail("cli.request.lifecycle");

        try
        {
            var open = recorder.RunStep(
                "cli.request.open-session",
                () => runner.Run(ScenarioSupport.RequestOptions(config, options, CommandRunner.OpenRequestSession)));
            if (!StepRecorder.RequireSuccess("cli.request.open-session", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                return outcome;
            }

            sessionId = open.SessionId;
            opened = true;

            var postOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.PostGetSitesRequest);
            postOptions.SessionId = sessionId;
            postOptions.PostRequestOptions = ScenarioSupport.MinimalRequest(ConnectorRequestType.GetSites);
            var post = recorder.RunStep("cli.request.post", () => runner.Run(postOptions));
            if (!StepRecorder.RequireSuccess("cli.request.post", post, "requestMessageId", value => !string.IsNullOrWhiteSpace(value.MessageId)))
            {
                return outcome;
            }

            var read = await ScenarioSupport.PollReadResponseAsync(
                runner,
                config,
                sessionId,
                post.MessageId,
                options,
                recorder,
                "cli.request.read-response");
            if (!read.Completed)
            {
                Console.Error.WriteLine($"cli.request.read-response: FAIL ({read.FailureReason})");
                outcome = read.StopCondition ? ScenarioOutcome.Stop("cli.request.lifecycle") : ScenarioOutcome.Fail("cli.request.lifecycle");
                return outcome;
            }

            outcome = ScenarioSupport.ValidateReadResponse("cli.request.read-response", read.Result!, post.MessageId)
                ? ScenarioOutcome.Pass("cli.request.lifecycle")
                : ScenarioOutcome.Fail("cli.request.lifecycle");
            return outcome;
        }
        finally
        {
            if (opened)
            {
                var closeOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.CloseRequestSession);
                closeOptions.SessionId = sessionId;
                var close = recorder.RunStep("cli.request.close-session", () => runner.Run(closeOptions));
                cleanupFailed = !close.Success;
                if (!close.Success)
                {
                    Console.Error.WriteLine("cli.request.close-session: cleanup FAIL");
                }
            }

            if (outcome.Passed && cleanupFailed)
            {
                Console.Error.WriteLine("cli.request.lifecycle: FAIL (cleanup failed)");
            }
        }
    }

    private static ScenarioOutcome CheckStagedPublicationLifecycle(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var runner = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        ScenarioOutcome outcome = ScenarioOutcome.Fail("cli.publication.lifecycle");
        bool cleanupFailed = false;

        try
        {
            var open = recorder.RunStep(
                "cli.publication.open-subscription",
                () => runner.Run(ScenarioSupport.PublicationOptions(config, options, CommandRunner.OpenSubscription)));
            if (!StepRecorder.RequireSuccess("cli.publication.open-subscription", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                return outcome;
            }

            sessionId = open.SessionId;
            opened = true;

            var readOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.ReadPublication);
            readOptions.SessionId = sessionId;
            var read = recorder.RunStep(
                "cli.publication.read",
                () => runner.Run(readOptions),
                acceptedErrorCodes: ["NoPublicationAvailable"]);

            bool passed = read.Success ||
                string.Equals(read.ErrorCode, "NoPublicationAvailable", StringComparison.OrdinalIgnoreCase);
            outcome = passed ? ScenarioOutcome.Pass("cli.publication.lifecycle") : ScenarioOutcome.Fail("cli.publication.lifecycle");
            return outcome;
        }
        finally
        {
            if (opened)
            {
                var closeOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.CloseSubscription);
                closeOptions.SessionId = sessionId;
                var close = recorder.RunStep("cli.publication.close-subscription", () => runner.Run(closeOptions));
                cleanupFailed = !close.Success;
                if (!close.Success)
                {
                    Console.Error.WriteLine("cli.publication.close-subscription: cleanup FAIL");
                }
            }

            if (outcome.Passed && cleanupFailed)
            {
                Console.Error.WriteLine("cli.publication.lifecycle: FAIL (cleanup failed)");
            }
        }
    }
}
