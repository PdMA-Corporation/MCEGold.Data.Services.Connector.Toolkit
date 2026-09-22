using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace RuntimeSmokeValidation.Scenarios;

internal static class SmokeScenarios
{
    public static async Task<ValidationSummary> RunAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var summary = new ValidationSummary();
        Console.WriteLine();
        Console.WriteLine("Phase 2 smoke scenarios");

        summary.Add(await RunRequestSmokeAsync(config, options, recorder));
        summary.Add(RunPublicationSmoke(config, options, recorder));
        return summary;
    }

    private static async Task<ScenarioOutcome> RunRequestSmokeAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        Console.WriteLine();
        Console.WriteLine("Request smoke");

        var runner = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        bool cleanupFailed = false;
        ScenarioOutcome outcome = ScenarioOutcome.Fail("smoke.request");

        try
        {
            var open = recorder.RunStep(
                "request.open-session",
                () => runner.Run(ScenarioSupport.RequestOptions(config, options, CommandRunner.OpenRequestSession)));

            if (!StepRecorder.RequireSuccess("request.open-session", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                return outcome;
            }

            sessionId = open.SessionId;
            opened = true;

            var postOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.PostGetSitesRequest);
            postOptions.SessionId = sessionId;
            postOptions.PostRequestOptions = ScenarioSupport.MinimalRequest(MCEGold.Data.Services.Connector.Commands.Requests.ConnectorRequestType.GetSites);

            var post = recorder.RunStep("request.post-get-sites", () => runner.Run(postOptions));
            if (!StepRecorder.RequireSuccess("request.post-get-sites", post, "requestMessageId", value => !string.IsNullOrWhiteSpace(value.MessageId)))
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
                "request.read-response");
            if (!read.Completed)
            {
                Console.Error.WriteLine($"request.read-response: FAIL ({read.FailureReason})");
                outcome = read.StopCondition ? ScenarioOutcome.Stop("smoke.request") : ScenarioOutcome.Fail("smoke.request");
                return outcome;
            }

            bool readValid = ScenarioSupport.ValidateReadResponse("request.read-response", read.Result!, post.MessageId);
            bool removed = readValid && ScenarioSupport.RemoveResponseIfRequested(
                runner,
                config,
                options,
                recorder,
                sessionId,
                post.MessageId,
                "request.remove-response");
            outcome = readValid && removed ? ScenarioOutcome.Pass("smoke.request") : ScenarioOutcome.Fail("smoke.request");
            return outcome;
        }
        finally
        {
            if (opened)
            {
                var closeOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.CloseRequestSession);
                closeOptions.SessionId = sessionId;
                var close = recorder.RunStep("request.close-session", () => runner.Run(closeOptions));
                cleanupFailed = !close.Success;
                if (cleanupFailed)
                {
                    Console.Error.WriteLine("request.close-session: cleanup FAIL");
                }
            }

            if (outcome.Passed && cleanupFailed)
            {
                Console.Error.WriteLine("smoke.request: FAIL (cleanup failed)");
            }
        }
    }

    private static ScenarioOutcome RunPublicationSmoke(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        Console.WriteLine();
        Console.WriteLine("Publication smoke");

        var runner = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        bool cleanupFailed = false;
        ScenarioOutcome outcome = ScenarioOutcome.Fail("smoke.publication");

        try
        {
            var open = recorder.RunStep(
                "publication.open-subscription",
                () => runner.Run(ScenarioSupport.PublicationOptions(config, options, CommandRunner.OpenSubscription)));

            if (!StepRecorder.RequireSuccess("publication.open-subscription", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                return outcome;
            }

            sessionId = open.SessionId;
            opened = true;

            var readOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.ReadPublication);
            readOptions.SessionId = sessionId;
            var read = recorder.RunStep(
                "publication.read",
                () => runner.Run(readOptions),
                acceptedErrorCodes: ["NoPublicationAvailable"]);

            if (read.Success)
            {
                if (string.IsNullOrWhiteSpace(read.MessageId))
                {
                    Console.Error.WriteLine("publication.read: FAIL (messageId was empty on successful read)");
                    return outcome;
                }

                Console.WriteLine("publication.read outcome: publication returned");
                outcome = ScenarioOutcome.Pass("smoke.publication");
                return outcome;
            }

            if (string.Equals(read.ErrorCode, "NoPublicationAvailable", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("publication.read outcome: NoPublicationAvailable");
                outcome = ScenarioOutcome.Pass("smoke.publication");
                return outcome;
            }

            Console.Error.WriteLine($"publication.read: FAIL ({StepRecorder.FormatFailure(read)})");
            return outcome;
        }
        finally
        {
            if (opened)
            {
                var closeOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.CloseSubscription);
                closeOptions.SessionId = sessionId;
                var close = recorder.RunStep("publication.close-subscription", () => runner.Run(closeOptions));
                cleanupFailed = !close.Success;
                if (cleanupFailed)
                {
                    Console.Error.WriteLine("publication.close-subscription: cleanup FAIL");
                }
            }

            if (outcome.Passed && cleanupFailed)
            {
                Console.Error.WriteLine("smoke.publication: FAIL (cleanup failed)");
            }
        }
    }
}
