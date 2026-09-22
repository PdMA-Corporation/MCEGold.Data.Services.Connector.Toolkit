namespace RuntimeSmokeValidation;

internal enum RuntimeMode
{
    Smoke,
    Requests,
    Attach,
    Cli,
    Full
}

internal sealed class RuntimeOptions
{
    public string ConfigPath { get; private init; } = Path.Combine("configs", "connector.config.development.json");
    public RuntimeMode Mode { get; private init; } = RuntimeMode.Full;
    public int TimeoutSeconds { get; private init; } = 60;
    public int PollIntervalSeconds { get; private init; } = 2;
    public int InitialDelaySeconds { get; private init; } = 1;
    public bool RemoveResponse { get; private init; }
    public bool CustomRequestRouting { get; private init; }
    public bool CustomPublicationRouting { get; private init; }
    public bool Net8Smoke { get; private init; }

    public static RuntimeOptions Parse(string[] args)
    {
        string configPath = Path.Combine("configs", "connector.config.development.json");
        RuntimeMode mode = RuntimeMode.Full;
        int timeoutSeconds = 60;
        int pollIntervalSeconds = 2;
        int initialDelaySeconds = 1;
        bool removeResponse = false;
        bool customRequestRouting = false;
        bool customPublicationRouting = false;
        bool net8Smoke = false;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--config":
                    configPath = ReadValue(args, ref index, arg);
                    break;
                case "--mode":
                    mode = ParseMode(ReadValue(args, ref index, arg));
                    break;
                case "--timeout-seconds":
                    timeoutSeconds = ReadPositiveInt(args, ref index, arg);
                    break;
                case "--poll-interval-seconds":
                    pollIntervalSeconds = ReadPositiveInt(args, ref index, arg);
                    break;
                case "--initial-delay-seconds":
                    initialDelaySeconds = ReadPositiveInt(args, ref index, arg);
                    break;
                case "--remove-response":
                    removeResponse = true;
                    break;
                case "--custom-request-routing":
                    customRequestRouting = true;
                    break;
                case "--custom-publication-routing":
                    customPublicationRouting = true;
                    break;
                case "--net8-smoke":
                    net8Smoke = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }
        }

        return new RuntimeOptions
        {
            ConfigPath = configPath,
            Mode = mode,
            TimeoutSeconds = timeoutSeconds,
            PollIntervalSeconds = pollIntervalSeconds,
            InitialDelaySeconds = initialDelaySeconds,
            RemoveResponse = removeResponse,
            CustomRequestRouting = customRequestRouting,
            CustomPublicationRouting = customPublicationRouting,
            Net8Smoke = net8Smoke
        };
    }

    private static RuntimeMode ParseMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "smoke" => RuntimeMode.Smoke,
            "requests" => RuntimeMode.Requests,
            "attach" => RuntimeMode.Attach,
            "cli" => RuntimeMode.Cli,
            "full" => RuntimeMode.Full,
            _ => throw new ArgumentException("--mode must be smoke, requests, attach, cli, or full.")
        };
    }

    private static string ReadValue(string[] args, ref int index, string name)
    {
        index++;
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"{name} requires a value.");
        }

        return args[index];
    }

    private static int ReadPositiveInt(string[] args, ref int index, string name)
    {
        string value = ReadValue(args, ref index, name);
        if (!int.TryParse(value, out int parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{name} must be a positive integer.");
        }

        return parsed;
    }
}
