using RuntimeSmokeValidation.Configuration;
using RuntimeSmokeValidation.Diagnostics;
using RuntimeSmokeValidation.Scenarios;

namespace RuntimeSmokeValidation;

internal static class Program
{
    private const int Success = 0;
    private const int Failure = 1;
    private const int StopCondition = 2;

    private static async Task<int> Main(string[] args)
    {
        RuntimeOptions options;
        try
        {
            options = RuntimeOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"Usage error: {Sanitizer.Clean(exception.Message)}");
            return Failure;
        }

        var recorder = new StepRecorder(options);
        recorder.WriteStartup();

        RuntimeConfig config;
        try
        {
            config = RuntimeConfig.Load(options.ConfigPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Config validation: FAIL ({Sanitizer.Clean(exception.Message)})");
            return StopCondition;
        }

        recorder.WriteConfigSummary(config);
        recorder.WriteAssemblySummary();

        ValidationSummary summary = options.Mode switch
        {
            RuntimeMode.Smoke => await SmokeScenarios.RunAsync(config, options, recorder),
            RuntimeMode.Requests => await RequestScenarios.RunAllAsync(config, options, recorder),
            RuntimeMode.Attach => await AttachSessionScenarios.RunAsync(config, options, recorder),
            RuntimeMode.Cli => await CliContractScenarios.RunAsync(config, options, recorder),
            RuntimeMode.Full => await RunFullAsync(config, options, recorder),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Mode))
        };

        recorder.WriteSummary(summary);
        return summary.HasStopCondition ? StopCondition : summary.Passed ? Success : Failure;
    }

    private static async Task<ValidationSummary> RunFullAsync(
        RuntimeConfig config,
        RuntimeOptions options,
        StepRecorder recorder)
    {
        var summary = new ValidationSummary();
        summary.Add(await SmokeScenarios.RunAsync(config, options, recorder));
        summary.Add(await RequestScenarios.RunAllAsync(config, options, recorder));
        summary.Add(await AttachSessionScenarios.RunAsync(config, options, recorder));
        summary.Add(await CliContractScenarios.RunAsync(config, options, recorder));
        summary.Add(FailureScenarios.Run(options, recorder));
        return summary;
    }
}
