using System.Diagnostics;
using System.Reflection;
using MCEGold.Data.Services.Connector.Commands;
using ConsumerRequestService = MCEGold.Data.Services.Connector.ConsumerRequestService;
using RuntimeSmokeValidation.Configuration;

namespace RuntimeSmokeValidation.Diagnostics;

internal sealed class StepRecorder
{
    private readonly RuntimeOptions options;

    public StepRecorder(RuntimeOptions options)
    {
        this.options = options;
    }

    public void WriteStartup()
    {
        Console.WriteLine("MCEGold Connector runtime validation");
        Console.WriteLine($"Validation run ID: {Guid.NewGuid():N}");
        Console.WriteLine($"Config path: {options.ConfigPath}");
        Console.WriteLine($"Validation mode: {options.Mode.ToString().ToLowerInvariant()}");
        Console.WriteLine($"ReadResponse timeout: {options.TimeoutSeconds}s");
        Console.WriteLine($"ReadResponse poll interval: {options.PollIntervalSeconds}s");
        Console.WriteLine($"Optional RemoveResponse: {Enabled(options.RemoveResponse)}");
        Console.WriteLine($"Optional custom request routing: {Enabled(options.CustomRequestRouting)}");
        Console.WriteLine($"Optional custom publication routing: {Enabled(options.CustomPublicationRouting)}");
        Console.WriteLine($"Optional net8 smoke: {Enabled(options.Net8Smoke)}");
        Console.WriteLine($"Runtime framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
    }

    public void WriteConfigSummary(RuntimeConfig config)
    {
        Console.WriteLine("Config validation: PASS");
        Console.WriteLine("Host: configured");
        Console.WriteLine($"Authentication scheme: {config.AuthenticationScheme}");
        Console.WriteLine($"API key: {Sanitizer.Configured(config.ApiKey)}");
        Console.WriteLine($"Username: {Sanitizer.Configured(config.UserName)}");
        Console.WriteLine($"Password: {Sanitizer.Configured(config.Password)}");
        Console.WriteLine($"Request channel: {Sanitizer.Configured(config.RequestChannel)}");
        Console.WriteLine($"Publication channel: {Sanitizer.Configured(config.PublicationChannel)}");
    }

    public void WriteAssemblySummary()
    {
        Assembly assembly = typeof(ConsumerRequestService).Assembly;
        Console.WriteLine($"Connector assembly: {assembly.FullName}");
        Console.WriteLine($"Connector assembly location: {assembly.Location}");
    }

    public ConnectorCommandResult RunStep(
        string name,
        Func<ConnectorCommandResult> action,
        IReadOnlyCollection<string>? acceptedErrorCodes = null)
    {
        var stopwatch = Stopwatch.StartNew();
        ConnectorCommandResult result;
        try
        {
            result = action();
        }
        catch (Exception exception)
        {
            result = new ConnectorCommandResult
            {
                Success = false,
                Command = name,
                ErrorCode = "UnhandledException",
                ErrorMessage = Sanitizer.Clean(exception.Message)
            };
        }

        stopwatch.Stop();
        WriteStepResult(name, result, stopwatch.Elapsed, acceptedErrorCodes);
        return result;
    }

    public void WriteSyntheticStep(string name, bool passed, string detail)
    {
        Console.WriteLine($"{name}: {(passed ? "PASS" : "FAIL")}; {Sanitizer.Clean(detail)}");
    }

    public void WriteSummary(ValidationSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine("Validation summary");
        foreach (ScenarioOutcome outcome in summary.Outcomes)
        {
            string status = outcome.Passed ? "PASS" : outcome.StopCondition ? "STOP" : "FAIL";
            Console.WriteLine($"{outcome.Name}: {status}");
        }

        Console.WriteLine($"Totals: pass={summary.PassCount}; fail={summary.FailCount}; stop={summary.StopCount}");
    }

    public static bool RequireSuccess(
        string name,
        ConnectorCommandResult result,
        string requiredField,
        Func<ConnectorCommandResult, bool> hasRequiredField)
    {
        if (!result.Success)
        {
            Console.Error.WriteLine($"{name}: FAIL ({FormatFailure(result)})");
            return false;
        }

        if (!hasRequiredField(result))
        {
            Console.Error.WriteLine($"{name}: FAIL ({requiredField} was empty)");
            return false;
        }

        return true;
    }

    public static string FormatFailure(ConnectorCommandResult? result)
    {
        if (result is null)
        {
            return "no result";
        }

        string code = string.IsNullOrWhiteSpace(result.ErrorCode) ? "CommandFailed" : result.ErrorCode;
        string message = string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.ReasonPhrase : result.ErrorMessage;
        return $"{Sanitizer.Clean(code)}: {Sanitizer.Clean(message)}";
    }

    private static void WriteStepResult(
        string name,
        ConnectorCommandResult result,
        TimeSpan elapsed,
        IReadOnlyCollection<string>? acceptedErrorCodes)
    {
        string status = result.StatusCode == 0 ? "n/a" : result.StatusCode.ToString();
        string ids = FormatIds(result);
        bool acceptedFailure = !result.Success &&
            acceptedErrorCodes is not null &&
            acceptedErrorCodes.Contains(result.ErrorCode, StringComparer.OrdinalIgnoreCase);
        string outcome = result.Success || acceptedFailure ? "PASS" : "FAIL";
        string failure = result.Success
            ? string.Empty
            : acceptedFailure
                ? $" (accepted {Sanitizer.Clean(result.ErrorCode)})"
                : $" ({FormatFailure(result)})";
        Console.WriteLine($"{name}: {outcome}; elapsed={elapsed.TotalMilliseconds:0}ms; status={status}{ids}{failure}");
    }

    private static string FormatIds(ConnectorCommandResult result)
    {
        var parts = new List<string>();
        Add(parts, "sessionId", result.SessionId);
        Add(parts, "messageId", result.MessageId);
        Add(parts, "requestMessageId", result.RequestMessageId);
        Add(parts, "responseMessageId", result.ResponseMessageId);
        return parts.Count == 0 ? string.Empty : "; " + string.Join("; ", parts);
    }

    private static void Add(ICollection<string> parts, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{name}={value}");
        }
    }

    private static string Enabled(bool value) => value ? "enabled" : "disabled";
}
