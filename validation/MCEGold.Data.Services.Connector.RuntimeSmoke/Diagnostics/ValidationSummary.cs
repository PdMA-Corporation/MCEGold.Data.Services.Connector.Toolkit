namespace RuntimeSmokeValidation.Diagnostics;

internal sealed class ValidationSummary
{
    private readonly List<ScenarioOutcome> outcomes = new();

    public IReadOnlyList<ScenarioOutcome> Outcomes => outcomes;

    public bool Passed => outcomes.Count > 0 && outcomes.All(outcome => outcome.Passed);

    public bool HasStopCondition => outcomes.Any(outcome => outcome.StopCondition);

    public int PassCount => outcomes.Count(outcome => outcome.Passed);

    public int FailCount => outcomes.Count(outcome => !outcome.Passed && !outcome.StopCondition);

    public int StopCount => outcomes.Count(outcome => outcome.StopCondition);

    public void Add(ScenarioOutcome outcome) => outcomes.Add(outcome);

    public void Add(ValidationSummary summary) => outcomes.AddRange(summary.Outcomes);
}

internal sealed record ScenarioOutcome(string Name, bool Passed, bool StopCondition = false)
{
    public static ScenarioOutcome Pass(string name) => new(name, true);

    public static ScenarioOutcome Fail(string name) => new(name, false);

    public static ScenarioOutcome Stop(string name) => new(name, false, true);
}
