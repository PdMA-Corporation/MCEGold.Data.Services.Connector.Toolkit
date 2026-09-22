using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;

namespace RuntimeSmokeValidation.Scenarios;

internal static class FailureScenarios
{
    public static ValidationSummary Run(RuntimeOptions options, StepRecorder recorder)
    {
        _ = options;
        var summary = new ValidationSummary();
        Console.WriteLine();
        Console.WriteLine("Non-network contract scenarios");

        summary.Add(CheckMissingConfigField(recorder));
        summary.Add(CheckInvalidAuthenticationScheme(recorder));
        summary.Add(CheckInvalidPayloadProfile(recorder));
        summary.Add(CheckInvalidRequestOptionCombination(recorder));
        summary.Add(CheckInvalidJsonCommand(recorder));
        summary.Add(CheckReadResponseClassifier(recorder));
        return summary;
    }

    private static ScenarioOutcome CheckMissingConfigField(StepRecorder recorder)
    {
        bool passed = ThrowsInvalidConfig("{\"authenticationScheme\":\"BasicApi\",\"apiKey\":\"x\",\"userName\":\"u\",\"password\":\"p\"}");
        recorder.WriteSyntheticStep("contract.config.missing-host", passed, "missing host is rejected without printing secrets");
        return passed ? ScenarioOutcome.Pass("contract.config.missing-host") : ScenarioOutcome.Fail("contract.config.missing-host");
    }

    private static ScenarioOutcome CheckInvalidAuthenticationScheme(StepRecorder recorder)
    {
        bool passed = ThrowsInvalidConfig("{\"host\":\"https://example.invalid\",\"authenticationScheme\":\"Token\",\"apiKey\":\"x\",\"userName\":\"u\",\"password\":\"p\"}");
        recorder.WriteSyntheticStep("contract.config.invalid-auth-scheme", passed, "invalid authenticationScheme is rejected");
        return passed ? ScenarioOutcome.Pass("contract.config.invalid-auth-scheme") : ScenarioOutcome.Fail("contract.config.invalid-auth-scheme");
    }

    private static ScenarioOutcome CheckInvalidPayloadProfile(StepRecorder recorder)
    {
        var validation = new ConnectorRequestValidator().Validate(new ConnectorPostRequestOptions
        {
            RequestType = ConnectorRequestType.GetSites,
            PayloadProfile = "Tiny",
            MaxItems = 1
        });
        bool passed = !validation.IsValid &&
            validation.Errors.Any(error => string.Equals(error.Code, "InvalidPayloadProfile", StringComparison.OrdinalIgnoreCase));
        recorder.WriteSyntheticStep("contract.request.invalid-payload-profile", passed, "invalid payload profile is rejected");
        return passed ? ScenarioOutcome.Pass("contract.request.invalid-payload-profile") : ScenarioOutcome.Fail("contract.request.invalid-payload-profile");
    }

    private static ScenarioOutcome CheckInvalidRequestOptionCombination(StepRecorder recorder)
    {
        var validation = new ConnectorRequestValidator().Validate(new ConnectorPostRequestOptions
        {
            RequestType = ConnectorRequestType.GetSites,
            PayloadProfile = "Minimal",
            MaxItems = -1
        });
        bool passed = !validation.IsValid &&
            validation.Errors.Any(error => string.Equals(error.Code, "InvalidMaxItems", StringComparison.OrdinalIgnoreCase));
        recorder.WriteSyntheticStep("contract.request.invalid-option-combination", passed, "negative maxItems is rejected");
        return passed ? ScenarioOutcome.Pass("contract.request.invalid-option-combination") : ScenarioOutcome.Fail("contract.request.invalid-option-combination");
    }

    private static ScenarioOutcome CheckInvalidJsonCommand(StepRecorder recorder)
    {
        var result = new ConnectorCommandRunner().RunJsonAsync("{\"command\":\"PostRequest\",\"requestType\":\"NotARequest\"}")
            .GetAwaiter()
            .GetResult();
        bool passed = !result.Success &&
            (string.Equals(result.ErrorCode, "ParameterFault", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(result.ErrorCode, "ValidationFailed", StringComparison.OrdinalIgnoreCase));
        recorder.WriteSyntheticStep("contract.command.invalid-json", passed, $"connector returned {Sanitizer.Clean(result.ErrorCode)}");
        return passed ? ScenarioOutcome.Pass("contract.command.invalid-json") : ScenarioOutcome.Fail("contract.command.invalid-json");
    }

    private static ScenarioOutcome CheckReadResponseClassifier(StepRecorder recorder)
    {
        bool knownRetryable = ScenarioSupport.IsRetryableReadResponse(new ConnectorCommandResult
        {
            Success = false,
            ErrorCode = "NoResponseAvailable"
        });
        bool unknownTerminal = !ScenarioSupport.IsRetryableReadResponse(new ConnectorCommandResult
        {
            Success = false,
            StatusCode = 500,
            ErrorCode = "ServerFailure"
        });
        bool passed = knownRetryable && unknownTerminal;
        recorder.WriteSyntheticStep("contract.read-response.classifier", passed, "known not-ready is retryable and unknown failure is terminal");
        return passed ? ScenarioOutcome.Pass("contract.read-response.classifier") : ScenarioOutcome.Fail("contract.read-response.classifier");
    }

    private static bool ThrowsInvalidConfig(string json)
    {
        try
        {
            RuntimeConfig.LoadJson(json, "contract");
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }
}
