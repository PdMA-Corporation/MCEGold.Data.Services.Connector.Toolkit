using System.CommandLine;
using System.Reflection;
using MCEGold.Data.Services.Connector.Cli.Commands;
using MCEGold.Data.Services.Connector.Cli.Configuration;
using MCEGold.Data.Services.Connector.Cli.Input;
using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace MCEGold.Data.Services.Connector.Cli;

/// <summary>
/// Builds the command-line surface and adapts parsed arguments into connector-library calls.
/// </summary>
public sealed class CliApplication
{
    private readonly TextReader input;
    private readonly TextWriter error;
    private readonly CliOutputWriter output;
    private readonly ConfigurationLoader configurationLoader;
    private readonly RootCommand rootCommand;
    private readonly Option<string> outputOption;

    /// <summary>
    /// Creates an application instance with injectable streams for tests and scripted hosting.
    /// </summary>
    public CliApplication(
        TextReader input,
        TextWriter output,
        TextWriter error,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        this.input = input;
        this.error = error;
        this.output = new CliOutputWriter(output);
        configurationLoader = new ConfigurationLoader(getEnvironmentVariable);
        outputOption = new Option<string>("--output")
        {
            Description = "Output format: json, json-pretty, text, or payload.",
            Recursive = true
        };
        outputOption.AcceptOnlyFromAmong("json", "json-pretty", "text", "payload");

        rootCommand = BuildCommandTree();
    }

    /// <summary>
    /// Parses CLI arguments, emits a CLI envelope on failure, and invokes the selected command.
    /// </summary>
    public int Run(string[] args)
    {
        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            var details = parseResult.Errors
                .Select(parseError => new CliFaultDetail
                {
                    Field = "arguments",
                    Code = "InvalidUsage",
                    Message = parseError.Message
                })
                .ToArray();

            var envelope = CliEnvelope.Failed(
                CommandName(args),
                new CliFault
                {
                    Category = "validation",
                    Code = "InvalidUsage",
                    Message = "Command-line validation failed.",
                    Details = details
                });
            output.Write(envelope, FindOutputFormat(args));
            return ExitCodes.UsageOrValidation;
        }

        return parseResult.Invoke();
    }

    private RootCommand BuildCommandTree()
    {
        var root = new RootCommand("MCEGold Data Services Connector automation CLI");
        root.Add(outputOption);
        root.SetAction(parseResult => WriteFailure(
            "MCEGold.Data.Services.Connector.Cli",
            "InvalidUsage",
            "A command is required.",
            parseResult.GetValue(outputOption)));

        root.Add(BuildVersionCommand());
        root.Add(BuildTypedJsonCommand());
        root.Add(BuildConfigCommand());
        root.Add(BuildRequestCommand());
        root.Add(BuildPublicationCommand());
        return root;
    }

    private Command BuildVersionCommand()
    {
        var command = new Command("version", "Show CLI and connector versions.");
        command.SetAction(parseResult =>
        {
            string cliVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            string connectorVersion = typeof(MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner)
                .Assembly.GetName().Version?.ToString() ?? "unknown";
            return Write(
                new CliExecutionResult(
                    CliEnvelope.Succeeded("version", new { cliVersion, connectorVersion }),
                    ExitCodes.Success),
                parseResult.GetValue(outputOption));
        });
        return command;
    }

    private Command BuildTypedJsonCommand()
    {
        var command = new Command("run", "Run a typed JSON pilot command.");
        var inputOption = RequiredStringOption("--input", "Typed command JSON path, or - for stdin.");
        command.Add(inputOption);
        command.SetAction(parseResult => Execute(
            "run",
            parseResult.GetValue(outputOption),
            () =>
            {
                string json = LoadJsonInput(parseResult.GetValue(inputOption)!);
                // The generic path is intentionally thin: caller JSON goes straight to the connector.
                ConnectorCommandResult result = new CommandRunner()
                    .RunJsonAsync(json)
                    .GetAwaiter()
                    .GetResult();

                return ToJsonRunResult(result);
            }));
        return command;
    }

    private Command BuildConfigCommand()
    {
        var config = new Command("config", "Validate or display effective configuration.");
        config.SetAction(parseResult => WriteFailure(
            "config",
            "InvalidUsage",
            "A config subcommand is required.",
            parseResult.GetValue(outputOption)));

        var validate = new Command("validate", "Validate required non-secret configuration.");
        var validateConfig = RequiredStringOption("--config", "Path to the JSON configuration file.");
        validate.Add(validateConfig);
        validate.SetAction(parseResult => Execute(
            "config.validate",
            parseResult.GetValue(outputOption),
            () =>
            {
                var settings = configurationLoader.Load(parseResult.GetValue(validateConfig)!);
                var errors = ConfigurationValidator.Validate(settings, requireSecrets: false);
                if (errors.Count > 0)
                {
                    return ValidationFailure("config.validate", "InvalidConfiguration", "Configuration validation failed.", errors, ExitCodes.Configuration);
                }

                return Success("config.validate", new
                {
                    valid = true,
                    settings.Host,
                    settings.AuthenticationScheme,
                    apiKeyConfigured = !string.IsNullOrWhiteSpace(settings.ApiKey),
                    userNameConfigured = !string.IsNullOrWhiteSpace(settings.UserName),
                    passwordConfigured = !string.IsNullOrWhiteSpace(settings.Password)
                });
            }));

        var show = new Command("show", "Show effective configuration with secrets redacted.");
        var showConfig = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var redact = new Option<bool>("--redact") { Description = "Required safety switch; secrets are always redacted.", Required = true };
        var showApiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var showPasswordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        show.Add(showConfig);
        show.Add(redact);
        show.Add(showApiKeyFile);
        show.Add(showPasswordFile);
        show.SetAction(parseResult => Execute(
            "config.show",
            parseResult.GetValue(outputOption),
            () =>
            {
                var settings = configurationLoader.Load(
                    parseResult.GetValue(showConfig)!,
                    parseResult.GetValue(showApiKeyFile),
                    parseResult.GetValue(showPasswordFile));
                return Success("config.show", ConfigurationValidator.Redact(settings));
            }));

        config.Add(validate);
        config.Add(show);
        return config;
    }

    private Command BuildRequestCommand()
    {
        var request = new Command("request", "Preview or run an atomic MCEGold request workflow.");
        request.SetAction(parseResult => WriteFailure(
            "request",
            "InvalidUsage",
            "A request subcommand is required.",
            parseResult.GetValue(outputOption)));

        var preview = new Command("preview", "Validate short-form input and generate BOD JSON without network access.");
        var previewInput = RequiredStringOption("--input", "Input JSON path, or - for stdin.");
        var previewConfig = OptionalStringOption("--config", "Optional configuration file for request payloadProfile defaults.");
        var previewPayloadProfile = PayloadProfileOption();
        preview.Add(previewInput);
        preview.Add(previewConfig);
        preview.Add(previewPayloadProfile);
        preview.SetAction(parseResult => Execute(
            "request.preview",
            parseResult.GetValue(outputOption),
            () =>
            {
                var requestOptions = new RequestInputLoader(input).Load(parseResult.GetValue(previewInput)!);
                // Request preview is CLI-owned because it creates local BOD JSON without service I/O.
                var profileFailure = ApplyRequestPayloadProfile(
                    "request.preview",
                    requestOptions,
                    parseResult.GetValue(previewPayloadProfile),
                    null);
                if (profileFailure is not null)
                {
                    return profileFailure;
                }

                var validationFailure = ValidateRequest("request.preview", requestOptions);
                if (validationFailure is not null)
                {
                    return validationFailure;
                }

                string? configPath = parseResult.GetValue(previewConfig);
                if (!string.IsNullOrWhiteSpace(configPath))
                {
                    var settings = configurationLoader.Load(configPath);
                    var configErrors = ConfigurationValidator.Validate(settings, requireSecrets: false);
                    if (configErrors.Count > 0)
                    {
                        return ValidationFailure("request.preview", "InvalidConfiguration", "Configuration validation failed.", configErrors, ExitCodes.Configuration);
                    }

                    profileFailure = ApplyRequestPayloadProfile(
                        "request.preview",
                        requestOptions,
                        parseResult.GetValue(previewPayloadProfile),
                        settings);
                    if (profileFailure is not null)
                    {
                        return profileFailure;
                    }

                    validationFailure = ValidateRequest("request.preview", requestOptions);
                    if (validationFailure is not null)
                    {
                        return validationFailure;
                    }
                }

                string bod = new ConnectorRequestJsonBuilder().BuildPreviewJson(requestOptions);
                return Success("request.preview", new
                {
                    requestType = RequestTypeNames.ToKebabCase(requestOptions.RequestType),
                    bod = JsonValueConverter.ParseContent(bod)
                });
            },
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(previewConfig))));

        var run = new Command("run", "Open, post, optionally read/remove, and close in one process.");
        var runInput = RequiredStringOption("--input", "Input JSON path, or - for stdin.");
        var runConfig = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var runApiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var runPasswordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var runIncludeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        var runPayloadProfile = PayloadProfileOption();
        run.Add(runInput);
        run.Add(runConfig);
        run.Add(runApiKeyFile);
        run.Add(runPasswordFile);
        run.Add(runIncludeRaw);
        run.Add(runPayloadProfile);
        run.SetAction(parseResult => Execute(
            "request.run",
            parseResult.GetValue(outputOption),
            () =>
            {
                var requestOptions = new RequestInputLoader(input).Load(parseResult.GetValue(runInput)!);
                var profileFailure = ApplyRequestPayloadProfile(
                    "request.run",
                    requestOptions,
                    parseResult.GetValue(runPayloadProfile),
                    null);
                if (profileFailure is not null)
                {
                    return profileFailure;
                }

                var validationFailure = ValidateRequest("request.run", requestOptions);
                if (validationFailure is not null)
                {
                    return validationFailure;
                }

                var settings = configurationLoader.Load(
                    parseResult.GetValue(runConfig)!,
                    parseResult.GetValue(runApiKeyFile),
                    parseResult.GetValue(runPasswordFile));
                var configErrors = ConfigurationValidator.Validate(settings, requireSecrets: true);
                if (configErrors.Count > 0)
                {
                    return ValidationFailure("request.run", "InvalidConfiguration", "Configuration validation failed.", configErrors, ExitCodes.Configuration);
                }

                profileFailure = ApplyRequestPayloadProfile(
                    "request.run",
                    requestOptions,
                    parseResult.GetValue(runPayloadProfile),
                    settings);
                if (profileFailure is not null)
                {
                    return profileFailure;
                }

                validationFailure = ValidateRequest("request.run", requestOptions);
                if (validationFailure is not null)
                {
                    return validationFailure;
                }

                return new RequestWorkflow().Run(
                    settings,
                    requestOptions,
                    parseResult.GetValue(runIncludeRaw) || settings.Output.IncludeRaw);
            },
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(runConfig))));

        var openSession = new Command("open-session", "Open a consumer request session.");
        var openSessionConfig = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var openSessionApiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var openSessionPasswordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var openSessionIncludeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        openSession.Add(openSessionConfig);
        openSession.Add(openSessionApiKeyFile);
        openSession.Add(openSessionPasswordFile);
        openSession.Add(openSessionIncludeRaw);
        openSession.SetAction(parseResult => Execute(
            "request.open-session",
            parseResult.GetValue(outputOption),
            () => ExecuteNetwork(
                "request.open-session",
                parseResult.GetValue(openSessionConfig)!,
                parseResult.GetValue(openSessionApiKeyFile),
                parseResult.GetValue(openSessionPasswordFile),
                parseResult.GetValue(openSessionIncludeRaw),
                (settings, includeRaw) => new StagedSessionCommands().OpenRequestSession(settings, includeRaw)),
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(openSessionConfig))));

        var post = new Command("post", "Post one request through an existing consumer request session.");
        var postConfig = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var postSessionId = RequiredStringOption("--session-id", "Consumer request session ID returned by open-session.");
        var postInput = RequiredStringOption("--input", "Input JSON path, or - for stdin.");
        var postApiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var postPasswordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var postIncludeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        var postPayloadProfile = PayloadProfileOption();
        post.Add(postConfig);
        post.Add(postSessionId);
        post.Add(postInput);
        post.Add(postApiKeyFile);
        post.Add(postPasswordFile);
        post.Add(postIncludeRaw);
        post.Add(postPayloadProfile);
        post.SetAction(parseResult => Execute(
            "request.post",
            parseResult.GetValue(outputOption),
            () =>
            {
                var requestOptions = new RequestInputLoader(input).Load(parseResult.GetValue(postInput)!);
                var profileFailure = ApplyRequestPayloadProfile(
                    "request.post",
                    requestOptions,
                    parseResult.GetValue(postPayloadProfile),
                    null);
                if (profileFailure is not null)
                {
                    return profileFailure;
                }

                var validationFailure = ValidateRequest("request.post", requestOptions);
                if (validationFailure is not null)
                {
                    return validationFailure;
                }

                return ExecuteNetwork(
                    "request.post",
                    parseResult.GetValue(postConfig)!,
                    parseResult.GetValue(postApiKeyFile),
                    parseResult.GetValue(postPasswordFile),
                    parseResult.GetValue(postIncludeRaw),
                    (settings, includeRaw) =>
                    {
                        profileFailure = ApplyRequestPayloadProfile(
                            "request.post",
                            requestOptions,
                            parseResult.GetValue(postPayloadProfile),
                            settings);
                        if (profileFailure is not null)
                        {
                            return profileFailure;
                        }

                        var configuredValidationFailure = ValidateRequest("request.post", requestOptions);
                        if (configuredValidationFailure is not null)
                        {
                            return configuredValidationFailure;
                        }

                        return new StagedSessionCommands().PostRequest(
                            settings,
                            parseResult.GetValue(postSessionId)!,
                            requestOptions,
                            includeRaw);
                    });
            },
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(postConfig))));

        var readResponse = BuildRequestMessageCommand(
            "read-response",
            "Read the response for a posted request.",
            (settings, sessionId, requestId, includeRaw) =>
                new StagedSessionCommands().ReadResponse(settings, sessionId, requestId, includeRaw));

        var removeResponse = BuildRequestMessageCommand(
            "remove-response",
            "Remove the response for a posted request.",
            (settings, sessionId, requestId, includeRaw) =>
                new StagedSessionCommands().RemoveResponse(settings, sessionId, requestId, includeRaw));

        var closeSession = BuildSessionCommand(
            "close-session",
            "Close an existing consumer request session.",
            "request.close-session",
            (settings, sessionId, includeRaw) =>
                new StagedSessionCommands().CloseRequestSession(settings, sessionId, includeRaw));

        request.Add(preview);
        request.Add(run);
        request.Add(openSession);
        request.Add(post);
        request.Add(readResponse);
        request.Add(removeResponse);
        request.Add(closeSession);
        return request;
    }

    private Command BuildPublicationCommand()
    {
        var publication = new Command("publication", "Receive publications using atomic workflows.");
        publication.SetAction(parseResult => WriteFailure(
            "publication",
            "InvalidUsage",
            "A publication subcommand is required.",
            parseResult.GetValue(outputOption)));

        var receive = new Command("receive", "Open, receive one publication, optionally remove it, and close.");
        var config = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var once = new Option<bool>("--once") { Description = "Receive one publication and exit.", Required = true };
        var remove = new Option<bool>("--remove") { Description = "Remove the publication after a successful read." };
        var apiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var passwordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var includeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        receive.Add(config);
        receive.Add(once);
        receive.Add(remove);
        receive.Add(apiKeyFile);
        receive.Add(passwordFile);
        receive.Add(includeRaw);
        receive.SetAction(parseResult => Execute(
            "publication.receive",
            parseResult.GetValue(outputOption),
            () =>
            {
                var settings = configurationLoader.Load(
                    parseResult.GetValue(config)!,
                    parseResult.GetValue(apiKeyFile),
                    parseResult.GetValue(passwordFile));
                var configErrors = ConfigurationValidator.Validate(settings, requireSecrets: true);
                if (configErrors.Count > 0)
                {
                    return ValidationFailure("publication.receive", "InvalidConfiguration", "Configuration validation failed.", configErrors, ExitCodes.Configuration);
                }

                return new PublicationWorkflow().ReceiveOnce(
                    settings,
                    parseResult.GetValue(remove),
                    parseResult.GetValue(includeRaw) || settings.Output.IncludeRaw);
            },
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(config))));

        var openSubscription = new Command("open-subscription", "Open a consumer publication subscription.");
        var openConfig = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var openApiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var openPasswordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var openIncludeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        openSubscription.Add(openConfig);
        openSubscription.Add(openApiKeyFile);
        openSubscription.Add(openPasswordFile);
        openSubscription.Add(openIncludeRaw);
        openSubscription.SetAction(parseResult => Execute(
            "publication.open-subscription",
            parseResult.GetValue(outputOption),
            () => ExecuteNetwork(
                "publication.open-subscription",
                parseResult.GetValue(openConfig)!,
                parseResult.GetValue(openApiKeyFile),
                parseResult.GetValue(openPasswordFile),
                parseResult.GetValue(openIncludeRaw),
                (settings, includeRaw) => new StagedSessionCommands().OpenSubscription(settings, includeRaw)),
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(openConfig))));

        var read = BuildSessionCommand(
            "read",
            "Read the next publication from an existing subscription.",
            "publication.read",
            (settings, sessionId, includeRaw) =>
                new StagedSessionCommands().ReadPublication(settings, sessionId, includeRaw));

        var removeCommand = BuildSessionCommand(
            "remove",
            "Remove the current publication from an existing subscription.",
            "publication.remove",
            (settings, sessionId, includeRaw) =>
                new StagedSessionCommands().RemovePublication(settings, sessionId, includeRaw));

        var closeSubscription = BuildSessionCommand(
            "close-subscription",
            "Close an existing consumer publication subscription.",
            "publication.close-subscription",
            (settings, sessionId, includeRaw) =>
                new StagedSessionCommands().CloseSubscription(settings, sessionId, includeRaw));

        publication.Add(receive);
        publication.Add(openSubscription);
        publication.Add(read);
        publication.Add(removeCommand);
        publication.Add(closeSubscription);
        return publication;
    }

    private Command BuildSessionCommand(
        string name,
        string description,
        string commandName,
        Func<CliSettings, string, bool, CliExecutionResult> action)
    {
        var command = new Command(name, description);
        var config = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var sessionId = RequiredStringOption("--session-id", "Session ID returned by the matching open command.");
        var apiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var passwordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var includeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        command.Add(config);
        command.Add(sessionId);
        command.Add(apiKeyFile);
        command.Add(passwordFile);
        command.Add(includeRaw);
        command.SetAction(parseResult => Execute(
            commandName,
            parseResult.GetValue(outputOption),
            () => ExecuteNetwork(
                commandName,
                parseResult.GetValue(config)!,
                parseResult.GetValue(apiKeyFile),
                parseResult.GetValue(passwordFile),
                parseResult.GetValue(includeRaw),
                (settings, resolvedIncludeRaw) => action(
                    settings,
                    parseResult.GetValue(sessionId)!,
                    resolvedIncludeRaw)),
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(config))));
        return command;
    }

    private Command BuildRequestMessageCommand(
        string name,
        string description,
        Func<CliSettings, string, string, bool, CliExecutionResult> action)
    {
        string commandName = $"request.{name}";
        var command = new Command(name, description);
        var config = RequiredStringOption("--config", "Path to the JSON configuration file.");
        var sessionId = RequiredStringOption("--session-id", "Consumer request session ID returned by open-session.");
        var requestId = RequiredStringOption("--request-id", "Request message ID returned by request post.");
        var apiKeyFile = OptionalStringOption("--api-key-file", "Read the API key from a file.");
        var passwordFile = OptionalStringOption("--password-file", "Read the password from a file.");
        var includeRaw = new Option<bool>("--include-raw") { Description = "Include the raw connector response when available." };
        command.Add(config);
        command.Add(sessionId);
        command.Add(requestId);
        command.Add(apiKeyFile);
        command.Add(passwordFile);
        command.Add(includeRaw);
        command.SetAction(parseResult => Execute(
            commandName,
            parseResult.GetValue(outputOption),
            () => ExecuteNetwork(
                commandName,
                parseResult.GetValue(config)!,
                parseResult.GetValue(apiKeyFile),
                parseResult.GetValue(passwordFile),
                parseResult.GetValue(includeRaw),
                (settings, resolvedIncludeRaw) => action(
                    settings,
                    parseResult.GetValue(sessionId)!,
                    parseResult.GetValue(requestId)!,
                    resolvedIncludeRaw)),
            fallbackFormat: () => LoadOutputFormat(parseResult.GetValue(config))));
        return command;
    }

    private CliExecutionResult ExecuteNetwork(
        string command,
        string configPath,
        string? apiKeyFile,
        string? passwordFile,
        bool includeRaw,
        Func<CliSettings, bool, CliExecutionResult> action)
    {
        var settings = configurationLoader.Load(configPath, apiKeyFile, passwordFile);
        // The CLI owns config and secret-file errors before the connector sees a command.
        var configErrors = ConfigurationValidator.Validate(settings, requireSecrets: true);
        if (configErrors.Count > 0)
        {
            return ValidationFailure(command, "InvalidConfiguration", "Configuration validation failed.", configErrors, ExitCodes.Configuration);
        }

        return action(settings, includeRaw || settings.Output.IncludeRaw);
    }

    private int Execute(
        string command,
        string? format,
        Func<CliExecutionResult> action,
        Func<string?>? fallbackFormat = null)
    {
        try
        {
            return Write(action(), format ?? fallbackFormat?.Invoke());
        }
        catch (CliConfigurationException exception)
        {
            return Write(new CliExecutionResult(CliEnvelope.Failed(command, exception.ToFault()), ExitCodes.Configuration), format);
        }
        catch (CliInputException exception)
        {
            return Write(new CliExecutionResult(CliEnvelope.Failed(command, exception.ToFault()), ExitCodes.UsageOrValidation), format);
        }
        catch (Exception exception)
        {
            error.WriteLine($"{command}: {exception.GetType().Name}");
            var cliFault = new CliFault
            {
                Category = "internal",
                Code = "UnexpectedError",
                Message = "An unexpected internal error occurred."
            };
            return Write(new CliExecutionResult(CliEnvelope.Failed(command, cliFault), ExitCodes.Unexpected), format);
        }
    }

    private string LoadJsonInput(string path)
    {
        try
        {
            return path == "-" ? input.ReadToEnd() : File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new CliInputException("InputReadFailed", exception.Message, "input");
        }
    }

    private static CliExecutionResult ToJsonRunResult(ConnectorCommandResult result)
    {
        string command = JsonRunCommandName(result.Command);
        bool includeRaw = !string.IsNullOrWhiteSpace(result.Raw);
        if (!result.Success)
        {
            if (string.Equals(result.ErrorCode, "ParameterFault", StringComparison.OrdinalIgnoreCase))
            {
                return new CliExecutionResult(
                    CliEnvelope.Failed(command, new CliFault
                    {
                        Category = "validation",
                        Code = result.ErrorCode,
                        Message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                            ? "Invalid command input."
                            : result.ErrorMessage,
                        StatusCode = result.StatusCode == 0 ? null : result.StatusCode
                    }, includeRaw ? JsonValueConverter.ParseContent(result.Raw) : null),
                    ExitCodes.UsageOrValidation);
            }

            return WorkflowSupport.Failure(command, result, includeRaw: includeRaw);
        }

        return new CliExecutionResult(
            CliEnvelope.Succeeded(
                command,
                JsonRunData(result),
                includeRaw ? JsonValueConverter.ParseContent(result.Raw) : null),
            ExitCodes.Success);
    }

    private static string JsonRunCommandName(string command)
    {
        // Keep the public CLI command names stable even when the connector uses enum-style names.
        if (string.Equals(command, CommandRunner.OpenSubscription, StringComparison.OrdinalIgnoreCase))
        {
            return "publication.open-subscription";
        }

        if (string.Equals(command, CommandRunner.CloseSubscription, StringComparison.OrdinalIgnoreCase))
        {
            return "publication.close-subscription";
        }

        return string.IsNullOrWhiteSpace(command) ? "run" : command;
    }

    private static object JsonRunData(ConnectorCommandResult result)
    {
        // Projection keeps connector result fields from leaking directly into the CLI JSON contract.
        if (string.Equals(result.Command, CommandRunner.OpenSubscription, StringComparison.OrdinalIgnoreCase))
        {
            return new { statusCode = result.StatusCode, sessionId = result.SessionId };
        }

        if (string.Equals(result.Command, CommandRunner.CloseSubscription, StringComparison.OrdinalIgnoreCase))
        {
            return new
            {
                statusCode = result.StatusCode,
                sessionId = result.SessionId,
                closed = true
            };
        }

        return new
        {
            statusCode = result.StatusCode,
            sessionId = result.SessionId,
            requestMessageId = result.RequestMessageId,
            responseMessageId = result.ResponseMessageId,
            messageId = result.MessageId,
            payload = JsonValueConverter.ParseContent(result.Payload)
        };
    }

    private CliExecutionResult? ValidateRequest(string command, ConnectorPostRequestOptions options)
    {
        // Short request payloads are validated here because preview and BOD generation are CLI features.
        var validation = new ConnectorRequestValidator().Validate(options);
        if (validation.IsValid)
        {
            return null;
        }

        var details = validation.Errors.Select(item => new CliFaultDetail
        {
            Field = item.Field,
            Code = item.Code,
            Message = item.Message
        }).ToArray();
        return ValidationFailure(command, "InvalidInput", "Input validation failed.", details, ExitCodes.UsageOrValidation);
    }

    private static CliExecutionResult? ApplyRequestPayloadProfile(
        string command,
        ConnectorPostRequestOptions options,
        string? cliPayloadProfile,
        CliSettings? settings)
    {
        string? selected = null;
        string field = "payloadProfile";
        if (!string.IsNullOrWhiteSpace(cliPayloadProfile))
        {
            selected = cliPayloadProfile;
        }
        else if (!string.IsNullOrWhiteSpace(options.PayloadProfile))
        {
            selected = options.PayloadProfile;
        }
        else if (!string.IsNullOrWhiteSpace(settings?.Request.PayloadProfile))
        {
            selected = settings!.Request.PayloadProfile;
            field = "request.payloadProfile";
        }

        if (string.IsNullOrWhiteSpace(selected))
        {
            return null;
        }

        if (!PayloadProfileParser.TryParse(selected, out var parsed))
        {
            return ValidationFailure(
                command,
                "InvalidInput",
                "Input validation failed.",
                [
                    new CliFaultDetail
                    {
                        Field = field,
                        Code = "InvalidPayloadProfile",
                        Message = $"{field} must be Full or Minimal."
                    }
                ],
                ExitCodes.UsageOrValidation);
        }

        options.PayloadProfile = PayloadProfileParser.ToCanonicalString(parsed);
        return null;
    }

    private static CliExecutionResult Success(string command, object data)
    {
        return new CliExecutionResult(CliEnvelope.Succeeded(command, data), ExitCodes.Success);
    }

    private static CliExecutionResult ValidationFailure(
        string command,
        string code,
        string message,
        IReadOnlyList<CliFaultDetail> details,
        int exitCode)
    {
        return new CliExecutionResult(
            CliEnvelope.Failed(command, new CliFault
            {
                Category = exitCode == ExitCodes.Configuration ? "configuration" : "validation",
                Code = code,
                Message = message,
                Details = details
            }),
            exitCode);
    }

    private int Write(CliExecutionResult result, string? format)
    {
        output.Write(result.Envelope, format);
        return result.ExitCode;
    }

    private int WriteFailure(string command, string code, string message, string? format)
    {
        return Write(
            new CliExecutionResult(
                CliEnvelope.Failed(command, new CliFault
                {
                    Category = "validation",
                    Code = code,
                    Message = message
                }),
                ExitCodes.UsageOrValidation),
            format);
    }

    private string? LoadOutputFormat(string? configPath)
    {
        try
        {
            return string.IsNullOrWhiteSpace(configPath) ? null : configurationLoader.Load(configPath).Output.Format;
        }
        catch (CliConfigurationException)
        {
            return null;
        }
    }

    private static Option<string> PayloadProfileOption()
    {
        return OptionalStringOption("--payload-profile", "Request payload profile: Full or Minimal.");
    }

    private static Option<string> RequiredStringOption(string name, string description)
    {
        return new Option<string>(name) { Description = description, Required = true };
    }

    private static Option<string> OptionalStringOption(string name, string description)
    {
        return new Option<string>(name) { Description = description };
    }

    private static string CommandName(IReadOnlyList<string> args)
    {
        var names = args.Where(arg => !arg.StartsWith("-", StringComparison.Ordinal)).Take(2).ToArray();
        return names.Length == 0 ? "MCEGold.Data.Services.Connector.Cli" : string.Join('.', names);
    }

    private static string? FindOutputFormat(IReadOnlyList<string> args)
    {
        for (int index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--output", StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
