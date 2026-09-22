using MCEGold.Data.Services.Connector.Commands.Requests;
using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace RuntimeSmokeValidation.Scenarios;

internal static class RequestScenarios
{
    public static async Task<ValidationSummary> RunAllAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var summary = new ValidationSummary();
        Console.WriteLine();
        Console.WriteLine("All request type scenarios");

        var runner = new CommandRunner();
        string sessionId = string.Empty;
        bool opened = false;
        bool cleanupFailed = false;
        ConnectorRequestType activeRequestType = ConnectorRequestType.GetSites;

        try
        {
            var open = recorder.RunStep(
                "requests.open-session",
                () => runner.Run(ScenarioSupport.RequestOptions(config, options, CommandRunner.OpenRequestSession)));

            if (!StepRecorder.RequireSuccess("requests.open-session", open, "sessionId", value => !string.IsNullOrWhiteSpace(value.SessionId)))
            {
                summary.Add(ScenarioOutcome.Fail("requests.open-session"));
                return summary;
            }

            sessionId = open.SessionId;
            opened = true;

            foreach (ConnectorRequestType requestType in ScenarioSupport.RequestTypes)
            {
                activeRequestType = requestType;
                summary.Add(await RunOneRequestAsync(runner, config, options, recorder, sessionId, requestType));
                if (!summary.Outcomes[^1].Passed)
                {
                    Console.Error.WriteLine($"requests: stopping after failed request type {requestType}");
                    break;
                }
            }
        }
        finally
        {
            if (opened)
            {
                var closeOptions = ScenarioSupport.RequestOptions(config, options, CommandRunner.CloseRequestSession);
                closeOptions.SessionId = sessionId;
                var close = recorder.RunStep("requests.close-session", () => runner.Run(closeOptions));
                cleanupFailed = !close.Success;
                if (cleanupFailed)
                {
                    Console.Error.WriteLine($"requests.close-session: cleanup FAIL while active request type was {activeRequestType}");
                    summary.Add(ScenarioOutcome.Fail("requests.cleanup"));
                }
            }
        }

        return summary;
    }

    private static async Task<ScenarioOutcome> RunOneRequestAsync(
        CommandRunner runner,
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder,
        string sessionId,
        ConnectorRequestType requestType)
    {
        string scenarioName = $"request.{requestType}";
        var postOptions = ScenarioSupport.RequestOptions(config, options, ScenarioSupport.PostCommand(requestType));
        postOptions.SessionId = sessionId;
        postOptions.PostRequestOptions = ScenarioSupport.MinimalRequest(requestType);

        var post = recorder.RunStep($"{scenarioName}.post", () => runner.Run(postOptions));
        if (!StepRecorder.RequireSuccess($"{scenarioName}.post", post, "requestMessageId", value => !string.IsNullOrWhiteSpace(value.MessageId)))
        {
            return ScenarioOutcome.Fail(scenarioName);
        }

        var read = await ScenarioSupport.PollReadResponseAsync(
            runner,
            config,
            sessionId,
            post.MessageId,
            options,
            recorder,
            $"{scenarioName}.read-response");
        if (!read.Completed)
        {
            Console.Error.WriteLine($"{scenarioName}.read-response: FAIL ({read.FailureReason})");
            return read.StopCondition ? ScenarioOutcome.Stop(scenarioName) : ScenarioOutcome.Fail(scenarioName);
        }

        bool readValid = ScenarioSupport.ValidateReadResponse($"{scenarioName}.read-response", read.Result!, post.MessageId);
        bool removed = readValid && ScenarioSupport.RemoveResponseIfRequested(
            runner,
            config,
            options,
            recorder,
            sessionId,
            post.MessageId,
            $"{scenarioName}.remove-response");
        return readValid && removed ? ScenarioOutcome.Pass(scenarioName) : ScenarioOutcome.Fail(scenarioName);
    }
}
