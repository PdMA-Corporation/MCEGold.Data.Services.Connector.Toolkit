using MCEGold.Data.Services.Connector.Commands.Requests;
using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace RuntimeSmokeValidation.Scenarios;

internal static class AttachSessionScenarios
{
    public static async Task<ValidationSummary> RunAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var summary = new ValidationSummary();
        Console.WriteLine();
        Console.WriteLine("Attach-session scenarios");

        summary.Add(await RunRequestAttachAsync(config, options, recorder));
        summary.Add(RunPublicationAttach(config, options, recorder));
        return summary;
    }

    private static async Task<ScenarioOutcome> RunRequestAttachAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var runnerA = new CommandRunner();
        var runnerB = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        bool closeSucceeded = false;

        try
        {
            var open = recorder.RunStep(
                "attach.request.runner-a.open-session",
                () => runnerA.Run(ScenarioSupport.RequestOptions(config, options, CommandRunner.OpenRequestSession)));
            if (!StepRecorder.RequireSuccess("attach.request.runner-a.open-session", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                return ScenarioOutcome.Fail("attach.request");
            }

            sessionId = open.SessionId;
            opened = true;

            var postOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.PostGetSitesRequest);
            postOptions.SessionId = sessionId;
            postOptions.PostRequestOptions = ScenarioSupport.MinimalRequest(ConnectorRequestType.GetSites);
            var post = recorder.RunStep("attach.request.runner-a.post-get-sites", () => runnerA.Run(postOptions));
            if (!StepRecorder.RequireSuccess("attach.request.runner-a.post-get-sites", post, "requestMessageId", value => !string.IsNullOrWhiteSpace(value.MessageId)))
            {
                return ScenarioOutcome.Fail("attach.request");
            }

            var read = await ScenarioSupport.PollReadResponseAsync(
                runnerB,
                config,
                sessionId,
                post.MessageId,
                options,
                recorder,
                "attach.request.runner-b.read-response");
            if (!read.Completed)
            {
                Console.Error.WriteLine($"attach.request.runner-b.read-response: FAIL ({read.FailureReason})");
                return read.StopCondition ? ScenarioOutcome.Stop("attach.request") : ScenarioOutcome.Fail("attach.request");
            }

            if (!ScenarioSupport.ValidateReadResponse("attach.request.runner-b.read-response", read.Result!, post.MessageId))
            {
                return ScenarioOutcome.Fail("attach.request");
            }

            var closeOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.CloseRequestSession);
            closeOptions.SessionId = sessionId;
            var close = recorder.RunStep("attach.request.runner-b.close-session", () => runnerB.Run(closeOptions));
            closeSucceeded = close.Success;
            opened = false;
            return closeSucceeded ? ScenarioOutcome.Pass("attach.request") : ScenarioOutcome.Fail("attach.request");
        }
        finally
        {
            if (opened && !closeSucceeded)
            {
                var closeOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.CloseRequestSession);
                closeOptions.SessionId = sessionId;
                var cleanup = recorder.RunStep("attach.request.cleanup-close-session", () => runnerB.Run(closeOptions));
                if (!cleanup.Success)
                {
                    Console.Error.WriteLine("attach.request.cleanup-close-session: cleanup FAIL");
                }
            }
        }
    }

    private static ScenarioOutcome RunPublicationAttach(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var runnerA = new CommandRunner();
        var runnerB = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        bool closeSucceeded = false;

        try
        {
            var open = recorder.RunStep(
                "attach.publication.runner-a.open-subscription",
                () => runnerA.Run(ScenarioSupport.PublicationOptions(config, options, CommandRunner.OpenSubscription)));
            if (!StepRecorder.RequireSuccess("attach.publication.runner-a.open-subscription", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                return ScenarioOutcome.Fail("attach.publication");
            }

            sessionId = open.SessionId;
            opened = true;

            var readOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.ReadPublication);
            readOptions.SessionId = sessionId;
            var read = recorder.RunStep(
                "attach.publication.runner-b.read",
                () => runnerB.Run(readOptions),
                acceptedErrorCodes: ["NoPublicationAvailable"]);

            bool accepted = read.Success ||
                string.Equals(read.ErrorCode, "NoPublicationAvailable", StringComparison.OrdinalIgnoreCase);
            if (!accepted)
            {
                Console.Error.WriteLine($"attach.publication.runner-b.read: FAIL ({StepRecorder.FormatFailure(read)})");
                return ScenarioOutcome.Fail("attach.publication");
            }

            var closeOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.CloseSubscription);
            closeOptions.SessionId = sessionId;
            var close = recorder.RunStep("attach.publication.runner-b.close-subscription", () => runnerB.Run(closeOptions));
            closeSucceeded = close.Success;
            opened = false;
            return closeSucceeded ? ScenarioOutcome.Pass("attach.publication") : ScenarioOutcome.Fail("attach.publication");
        }
        finally
        {
            if (opened && !closeSucceeded)
            {
                var closeOptions = ScenarioSupport.PublicationOptions(config, options, CommandRunner.CloseSubscription);
                closeOptions.SessionId = sessionId;
                var cleanup = recorder.RunStep("attach.publication.cleanup-close-subscription", () => runnerB.Run(closeOptions));
                if (!cleanup.Success)
                {
                    Console.Error.WriteLine("attach.publication.cleanup-close-subscription: cleanup FAIL");
                }
            }
        }
    }
}
