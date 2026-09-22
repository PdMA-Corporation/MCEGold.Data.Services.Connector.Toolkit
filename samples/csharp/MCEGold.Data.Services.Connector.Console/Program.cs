using System.Text.Json;
using System.Text.Json.Serialization;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using MCEGold.Data.Services.Connector.Enums;

var runner = new ConnectorCommandRunner();

var jsonOptions = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

if (TryGetInputPath(args, out string inputPath))
{
    var result = await RunTypedJsonInputAsync(inputPath, runner, jsonOptions);
    PrintCommandResult(result, showFullCommandResult: true, jsonOptions);
    return result.Success ? 0 : 1;
}

var settings = ConsoleSettings.Load("appsettings.Development.json");
await RunMainMenu(settings, runner, jsonOptions);
return 0;

static bool TryGetInputPath(string[] args, out string inputPath)
{
    inputPath = string.Empty;
    for (int index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], "--input", StringComparison.OrdinalIgnoreCase))
        {
            inputPath = args[index + 1];
            return true;
        }
    }

    return false;
}

static async Task<ConnectorCommandResult> RunTypedJsonInputAsync(
    string inputPath,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions)
{
    try
    {
        string json = inputPath == "-"
            ? System.Console.In.ReadToEnd()
            : File.ReadAllText(inputPath);

        return await runner.RunJsonAsync(json);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
    {
        return ValidationResult(string.Empty, exception.Message);
    }
}

static ConnectorCommandResult ValidationResult(string command, string message)
{
    return new ConnectorCommandResult
    {
        Success = false,
        Command = command,
        ErrorCode = "ValidationFailed",
        ErrorMessage = message,
        ValidationErrors = new List<string> { message }
    };
}

static async Task RunMainMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("MCEGold.Data.Services.Connector Developer Console");
        System.Console.WriteLine("1. Consumer Publication Workflow");
        System.Console.WriteLine("2. Consumer Request Workflow");
        System.Console.WriteLine("3. Exit");
        System.Console.Write("Select a workflow: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                await RunConsumerPublicationMenu(settings, runner, jsonOptions);
                break;
            case "2":
                await RunConsumerRequestMenu(settings, runner, jsonOptions);
                break;
            case "3":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunConsumerPublicationMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions)
{
    var activePublicationSessionId = string.Empty;

    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Consumer Publication Workflow");
        System.Console.WriteLine("1. Open Subscription");
        System.Console.WriteLine("2. Read Publication");
        System.Console.WriteLine("3. Remove Publication");
        System.Console.WriteLine("4. Close Subscription");
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                var openResult = await RunOpenSubscriptionJsonAsync(settings, runner, jsonOptions);
                if (openResult.Success && !string.IsNullOrWhiteSpace(openResult.SessionId))
                {
                    activePublicationSessionId = openResult.SessionId;
                }
                break;
            case "2":
                await RunReadPublicationJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activePublicationSessionId);
                break;
            case "3":
                await RunRemovePublicationJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activePublicationSessionId);
                break;
            case "4":
                var closeResult = await RunCloseSubscriptionJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activePublicationSessionId);
                if (closeResult.Success)
                {
                    activePublicationSessionId = string.Empty;
                }
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunConsumerRequestMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions)
{
    var requestState = new RequestConsoleState
    {
        CurrentGetSitesRequest = CloneGetSitesRequest(settings.GetSitesRequest),
        CurrentGetSegmentsRequest = CloneGetSegmentsRequest(settings.GetSegmentsRequest),
        CurrentGetAssetsRequest = CloneGetAssetsRequest(settings.GetAssetsRequest),
        CurrentGetMeasurementLocationsRequest =
            CloneGetMeasurementLocationsRequest(settings.GetMeasurementLocationsRequest),
        CurrentGetMeasurementsRequest = CloneGetMeasurementsRequest(settings.GetMeasurementsRequest),
        CurrentGetAssessmentsRequest = CloneGetAssessmentsRequest(settings.GetAssessmentsRequest),
        CurrentGetAssetSegmentEventsRequest =
            CloneGetAssetSegmentEventsRequest(settings.GetAssetSegmentEventsRequest)
    };
    var isRequestSessionOpen = false;
    var activeRequestSessionId = string.Empty;

    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Consumer Request Workflow");
        if (isRequestSessionOpen)
        {
            System.Console.WriteLine("1. Request Type Workflows");
            System.Console.WriteLine("2. Close Request Session");
        }
        else
        {
            System.Console.WriteLine("1. Open Request Session");
        }
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                if (isRequestSessionOpen)
                {
                    await RunRequestTypeWorkflowsMenu(
                        settings,
                        runner,
                        jsonOptions,
                        activeRequestSessionId,
                        requestState);
                }
                else
                {
                    var openResult = await RunOpenRequestSessionJsonAsync(settings, runner, jsonOptions);
                    if (openResult.Success)
                    {
                        isRequestSessionOpen = true;
                        activeRequestSessionId = openResult.SessionId;
                    }
                }
                break;
            case "2":
                if (!isRequestSessionOpen)
                {
                    System.Console.WriteLine("Open a request session before using request type workflows.");
                    break;
                }

                var closeResult = await RunCloseRequestSessionJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId);
                if (closeResult.Success)
                {
                    isRequestSessionOpen = false;
                    activeRequestSessionId = string.Empty;
                    requestState.ClearMessageIds();
                }
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunRequestTypeWorkflowsMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Request Type Workflows");
        System.Console.WriteLine("1. GetSites Workflow");
        System.Console.WriteLine("2. GetSegments Workflow");
        System.Console.WriteLine("3. GetAssets Workflow");
        System.Console.WriteLine("4. GetMeasurementLocations Workflow");
        System.Console.WriteLine("5. GetMeasurements Workflow");
        System.Console.WriteLine("6. GetAssessments Workflow");
        System.Console.WriteLine("7. GetAssetSegmentEvents Workflow");
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select a request type workflow: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                await RunGetSitesWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "2":
                await RunGetSegmentsWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "3":
                await RunGetAssetsWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "4":
                await RunGetMeasurementLocationsWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "5":
                await RunGetMeasurementsWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "6":
                await RunGetAssessmentsWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "7":
                await RunGetAssetSegmentEventsWorkflowMenu(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState);
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetSitesWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetSites Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetSites Input");
        System.Console.WriteLine("2. Edit Short-Form GetSites Input");
        System.Console.WriteLine("3. Preview Generated GetSites BOD JSON");
        System.Console.WriteLine("4. Post GetSites Request");
        System.Console.WriteLine("5. Read GetSites Response");
        System.Console.WriteLine("6. Remove GetSites Response");
        System.Console.WriteLine("7. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput("GetSites", requestState.CurrentGetSitesRequest, jsonOptions);
                break;
            case "2":
                requestState.CurrentGetSitesRequest = EditShortFormRequestInput("GetSites", requestState.CurrentGetSitesRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetSites", requestState.CurrentGetSitesRequest);
                break;
            case "4":
                var postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetSitesRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetSitesMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunGetSitesMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetSitesMessageId,
                    "Post a GetSites request before reading the GetSites response.");
                break;
            case "6":
                await RunGetSitesMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetSitesMessageId,
                    "Post a GetSites request before removing the GetSites response.");
                break;
            case "7":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetSegmentsWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetSegments Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetSegments Input");
        System.Console.WriteLine("2. Edit Short-Form GetSegments Input");
        System.Console.WriteLine("3. Preview Generated GetSegments BOD JSON");
        System.Console.WriteLine("4. Post GetSegments Request");
        System.Console.WriteLine("5. Read GetSegments Response");
        System.Console.WriteLine("6. Remove GetSegments Response");
        System.Console.WriteLine("7. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput("GetSegments", requestState.CurrentGetSegmentsRequest, jsonOptions);
                break;
            case "2":
                requestState.CurrentGetSegmentsRequest = EditShortFormRequestInput("GetSegments", requestState.CurrentGetSegmentsRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetSegments", requestState.CurrentGetSegmentsRequest);
                break;
            case "4":
                var postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetSegmentsRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetSegmentsMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetSegmentsMessageId,
                    "Post a GetSegments request before reading the GetSegments response.",
                    "MessageId is required from the last posted GetSegments request.");
                break;
            case "6":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetSegmentsMessageId,
                    "Post a GetSegments request before removing the GetSegments response.",
                    "MessageId is required from the last posted GetSegments request.");
                break;
            case "7":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetAssetsWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetAssets Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetAssets Input");
        System.Console.WriteLine("2. Edit Short-Form GetAssets Input");
        System.Console.WriteLine("3. Preview Generated GetAssets BOD JSON");
        System.Console.WriteLine("4. Post GetAssets Request");
        System.Console.WriteLine("5. Read GetAssets Response");
        System.Console.WriteLine("6. Remove GetAssets Response");
        System.Console.WriteLine("7. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput("GetAssets", requestState.CurrentGetAssetsRequest, jsonOptions);
                break;
            case "2":
                requestState.CurrentGetAssetsRequest = EditShortFormRequestInput("GetAssets", requestState.CurrentGetAssetsRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetAssets", requestState.CurrentGetAssetsRequest);
                break;
            case "4":
                var postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetAssetsRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetAssetsMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetAssetsMessageId,
                    "Post a GetAssets request before reading the GetAssets response.",
                    "MessageId is required from the last posted GetAssets request.");
                break;
            case "6":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetAssetsMessageId,
                    "Post a GetAssets request before removing the GetAssets response.",
                    "MessageId is required from the last posted GetAssets request.");
                break;
            case "7":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetMeasurementLocationsWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetMeasurementLocations Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetMeasurementLocations Input");
        System.Console.WriteLine("2. Edit Short-Form GetMeasurementLocations Input");
        System.Console.WriteLine("3. Preview Generated GetMeasurementLocations BOD JSON");
        System.Console.WriteLine("4. Post GetMeasurementLocations Request");
        System.Console.WriteLine("5. Read GetMeasurementLocations Response");
        System.Console.WriteLine("6. Remove GetMeasurementLocations Response");
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput(
                    "GetMeasurementLocations",
                    requestState.CurrentGetMeasurementLocationsRequest,
                    jsonOptions);
                break;
            case "2":
                requestState.CurrentGetMeasurementLocationsRequest = EditShortFormRequestInput(
                    "GetMeasurementLocations",
                    requestState.CurrentGetMeasurementLocationsRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetMeasurementLocations", requestState.CurrentGetMeasurementLocationsRequest);
                break;
            case "4":
                var postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetMeasurementLocationsRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetMeasurementLocationsMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetMeasurementLocationsMessageId,
                    "Post a GetMeasurementLocations request before reading the GetMeasurementLocations response.",
                    "MessageId is required from the last posted GetMeasurementLocations request.");
                break;
            case "6":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetMeasurementLocationsMessageId,
                    "Post a GetMeasurementLocations request before removing the GetMeasurementLocations response.",
                    "MessageId is required from the last posted GetMeasurementLocations request.");
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetMeasurementsWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetMeasurements Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetMeasurements Input");
        System.Console.WriteLine("2. Edit Short-Form GetMeasurements Input");
        System.Console.WriteLine("3. Preview Generated GetMeasurements BOD JSON");
        System.Console.WriteLine("4. Post GetMeasurements Request");
        System.Console.WriteLine("5. Read GetMeasurements Response");
        System.Console.WriteLine("6. Remove GetMeasurements Response");
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput("GetMeasurements", requestState.CurrentGetMeasurementsRequest, jsonOptions);
                break;
            case "2":
                requestState.CurrentGetMeasurementsRequest = EditShortFormRequestInput(
                    "GetMeasurements",
                    requestState.CurrentGetMeasurementsRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetMeasurements", requestState.CurrentGetMeasurementsRequest);
                break;
            case "4":
                var postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetMeasurementsRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetMeasurementsMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetMeasurementsMessageId,
                    "Post a GetMeasurements request before reading the GetMeasurements response.",
                    "MessageId is required from the last posted GetMeasurements request.");
                break;
            case "6":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetMeasurementsMessageId,
                    "Post a GetMeasurements request before removing the GetMeasurements response.",
                    "MessageId is required from the last posted GetMeasurements request.");
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetAssessmentsWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetAssessments Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetAssessments Input");
        System.Console.WriteLine("2. Edit Short-Form GetAssessments Input");
        System.Console.WriteLine("3. Preview Generated GetAssessments BOD JSON");
        System.Console.WriteLine("4. Post GetAssessments Request");
        System.Console.WriteLine("5. Read GetAssessments Response");
        System.Console.WriteLine("6. Remove GetAssessments Response");
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select an action: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput("GetAssessments", requestState.CurrentGetAssessmentsRequest, jsonOptions);
                break;
            case "2":
                requestState.CurrentGetAssessmentsRequest = EditShortFormRequestInput(
                    "GetAssessments",
                    requestState.CurrentGetAssessmentsRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetAssessments", requestState.CurrentGetAssessmentsRequest);
                break;
            case "4":
                var postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetAssessmentsRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetAssessmentsMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetAssessmentsMessageId,
                    "Post a GetAssessments request before reading the GetAssessments response.",
                    "MessageId is required from the last posted GetAssessments request.");
                break;
            case "6":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetAssessmentsMessageId,
                    "Post a GetAssessments request before removing the GetAssessments response.",
                    "MessageId is required from the last posted GetAssessments request.");
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static async Task RunGetAssetSegmentEventsWorkflowMenu(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    RequestConsoleState requestState)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("GetAssetSegmentEvents Workflow");
        System.Console.WriteLine("1. Preview Short-Form GetAssetSegmentEvents Input");
        System.Console.WriteLine("2. Edit Short-Form GetAssetSegmentEvents Input");
        System.Console.WriteLine("3. Preview Generated GetAssetSegmentEvents BOD JSON");
        System.Console.WriteLine("4. Post GetAssetSegmentEvents Request");
        System.Console.WriteLine("5. Read GetAssetSegmentEvents Response");
        System.Console.WriteLine("6. Remove GetAssetSegmentEvents Response");
        System.Console.WriteLine("0. Back");
        System.Console.Write("Select an action: ");

        string? choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                PreviewShortFormRequestInput("GetAssetSegmentEvents", requestState.CurrentGetAssetSegmentEventsRequest, jsonOptions);
                break;
            case "2":
                requestState.CurrentGetAssetSegmentEventsRequest = EditShortFormRequestInput("GetAssetSegmentEvents", requestState.CurrentGetAssetSegmentEventsRequest);
                break;
            case "3":
                PreviewGeneratedBodJson("GetAssetSegmentEvents", requestState.CurrentGetAssetSegmentEventsRequest);
                break;
            case "4":
                ConnectorCommandResult postResult = await RunPostRequestJsonAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    requestState.CurrentGetAssetSegmentEventsRequest);
                if (!string.IsNullOrWhiteSpace(postResult.MessageId))
                {
                    requestState.LastGetAssetSegmentEventsMessageId = postResult.MessageId;
                }
                break;
            case "5":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.ReadResponse,
                    requestState.LastGetAssetSegmentEventsMessageId,
                    "Post a GetAssetSegmentEvents request before reading the response.",
                    "MessageId is required from the last posted GetAssetSegmentEvents request.");
                break;
            case "6":
                await RunRequestMessageIdCommandAsync(
                    settings,
                    runner,
                    jsonOptions,
                    activeRequestSessionId,
                    ConnectorCommandRunner.RemoveResponse,
                    requestState.LastGetAssetSegmentEventsMessageId,
                    "Post a GetAssetSegmentEvents request before removing the response.",
                    "MessageId is required from the last posted GetAssetSegmentEvents request.");
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static ConnectorPostRequestOptions CloneGetSitesRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetSites;

    return clone;
}

static ConnectorPostRequestOptions CloneGetSegmentsRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetSegments;

    return clone;
}

static ConnectorPostRequestOptions CloneGetAssetsRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetAssets;

    return clone;
}

static ConnectorPostRequestOptions CloneGetMeasurementLocationsRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetMeasurementLocations;

    return clone;
}

static ConnectorPostRequestOptions CloneGetMeasurementsRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetMeasurements;

    return clone;
}

static ConnectorPostRequestOptions CloneGetAssessmentsRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetAssessments;

    return clone;
}

static ConnectorPostRequestOptions CloneGetAssetSegmentEventsRequest(ConnectorPostRequestOptions source)
{
    var clone = ClonePostRequest(source);
    clone.RequestType = ConnectorRequestType.GetAssetSegmentEvents;

    return clone;
}

static ConnectorPostRequestOptions ClonePostRequest(ConnectorPostRequestOptions source)
{
    return new ConnectorPostRequestOptions
    {
        RequestType = source.RequestType,
        PayloadProfile = source.PayloadProfile,
        MaxItems = source.MaxItems,
        LastNData = source.LastNData,
        Filters = new ConnectorRequestFilters
        {
            AssetUuid = source.Filters == null ? string.Empty : source.Filters.AssetUuid,
            MeasurementLocationUuid = source.Filters == null ? string.Empty : source.Filters.MeasurementLocationUuid,
            SiteUuid = source.Filters == null ? string.Empty : source.Filters.SiteUuid,
            SegmentUuid = source.Filters == null ? string.Empty : source.Filters.SegmentUuid,
            TypeUuid = source.Filters == null ? string.Empty : source.Filters.TypeUuid,
            SerialNumber = source.Filters == null ? string.Empty : source.Filters.SerialNumber,
            RecordedFrom = source.Filters == null ? string.Empty : source.Filters.RecordedFrom,
            RecordedTo = source.Filters == null ? string.Empty : source.Filters.RecordedTo,
            HealthLevelTypeUuid = source.Filters == null ? string.Empty : source.Filters.HealthLevelTypeUuid,
            AssessedFrom = source.Filters == null ? string.Empty : source.Filters.AssessedFrom,
            AssessedTo = source.Filters == null ? string.Empty : source.Filters.AssessedTo,
            HealthLevelMin = source.Filters == null ? null : source.Filters.HealthLevelMin,
            HealthLevelMax = source.Filters == null ? null : source.Filters.HealthLevelMax,
            EventUuid = source.Filters == null ? string.Empty : source.Filters.EventUuid,
            InstalledNow = source.Filters == null ? null : source.Filters.InstalledNow,
            InstalledFrom = source.Filters == null ? string.Empty : source.Filters.InstalledFrom,
            InstalledTo = source.Filters == null ? string.Empty : source.Filters.InstalledTo,
            RemovedFrom = source.Filters == null ? string.Empty : source.Filters.RemovedFrom,
            RemovedTo = source.Filters == null ? string.Empty : source.Filters.RemovedTo
        }
    };
}

static async Task<ConnectorCommandResult> RunGetSitesMessageIdCommandAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    string command,
    string messageId,
    string missingMessage)
{
    if (string.IsNullOrWhiteSpace(messageId))
    {
        System.Console.WriteLine();
        System.Console.WriteLine(missingMessage);

        var validationResult = new ConnectorCommandResult
        {
            Success = false,
            Command = command,
            ErrorCode = "ValidationFailed",
            ErrorMessage = missingMessage,
        ValidationErrors = new List<string>
        {
            "MessageId is required from the last posted GetSites request."
        }
    };

        PrintCommandResult(validationResult, settings.ShowFullCommandResult, jsonOptions);

        return validationResult;
    }

    System.Console.WriteLine();
    System.Console.WriteLine("Using last GetSites request message ID: " + messageId);

    return await RunResponseMessageCommandJsonAsync(
        settings,
        runner,
        jsonOptions,
        activeRequestSessionId,
        command,
        messageId);
}

static async Task<ConnectorCommandResult> RunRequestMessageIdCommandAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    string command,
    string messageId,
    string missingMessage,
    string validationMessage)
{
    if (string.IsNullOrWhiteSpace(messageId))
    {
        System.Console.WriteLine();
        System.Console.WriteLine(missingMessage);

        var validationResult = new ConnectorCommandResult
        {
            Success = false,
            Command = command,
            ErrorCode = "ValidationFailed",
            ErrorMessage = missingMessage,
            ValidationErrors = new List<string>
            {
                validationMessage
            }
        };

        PrintCommandResult(validationResult, settings.ShowFullCommandResult, jsonOptions);

        return validationResult;
    }

    System.Console.WriteLine();
    System.Console.WriteLine("Using last request message ID: " + messageId);

    return await RunResponseMessageCommandJsonAsync(
        settings,
        runner,
        jsonOptions,
        activeRequestSessionId,
        command,
        messageId);
}

static Task<ConnectorCommandResult> RunResponseMessageCommandJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string activeRequestSessionId,
    string command,
    string messageId)
{
    if (string.Equals(command, ConnectorCommandRunner.ReadResponse, StringComparison.OrdinalIgnoreCase))
    {
        return RunReadResponseJsonAsync(settings, runner, jsonOptions, activeRequestSessionId, messageId);
    }

    return RunRemoveResponseJsonAsync(settings, runner, jsonOptions, activeRequestSessionId, messageId);
}

static void PreviewShortFormRequestInput(
    string requestName,
    ConnectorPostRequestOptions request,
    JsonSerializerOptions jsonOptions)
{
    System.Console.WriteLine();
    System.Console.WriteLine("Preview Short-Form " + requestName + " Input result:");
    System.Console.WriteLine(JsonSerializer.Serialize(BuildShortFormPreview(request), jsonOptions));
}

static object BuildShortFormPreview(ConnectorPostRequestOptions request)
{
    Dictionary<string, object?> BuildPreview(
        object filters,
        bool includeLastNData)
    {
        var preview = new Dictionary<string, object?>
        {
            ["requestType"] = request.RequestType
        };

        if (!string.IsNullOrWhiteSpace(request.PayloadProfile))
        {
            preview["payloadProfile"] = NormalizePayloadProfileForDisplay(request.PayloadProfile);
        }

        preview["maxItems"] = request.MaxItems;

        if (includeLastNData)
        {
            preview["lastNData"] = request.LastNData;
        }

        preview["filters"] = filters;

        return preview;
    }

    if (request.RequestType == ConnectorRequestType.GetSites)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["siteUuid"] = GetSiteUuid(request)
        },
            includeLastNData: false);
    }

    if (request.RequestType == ConnectorRequestType.GetSegments)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["siteUuid"] = GetSiteUuid(request),
            ["segmentUuid"] = GetSegmentUuid(request),
            ["typeUuid"] = GetTypeUuid(request)
        },
            includeLastNData: false);
    }

    if (request.RequestType == ConnectorRequestType.GetAssets)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["assetUuid"] = GetAssetUuid(request),
            ["siteUuid"] = GetSiteUuid(request),
            ["typeUuid"] = GetTypeUuid(request),
            ["serialNumber"] = GetSerialNumber(request)
        },
            includeLastNData: false);
    }

    if (request.RequestType == ConnectorRequestType.GetMeasurementLocations)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["measurementLocationUuid"] = GetMeasurementLocationUuid(request),
            ["siteUuid"] = GetSiteUuid(request),
            ["segmentUuid"] = GetSegmentUuid(request),
            ["typeUuid"] = GetTypeUuid(request)
        },
            includeLastNData: false);
    }

    if (request.RequestType == ConnectorRequestType.GetMeasurements)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["measurementLocationUuid"] = GetMeasurementLocationUuid(request),
            ["segmentUuid"] = GetSegmentUuid(request),
            ["recordedFrom"] = GetRecordedFrom(request),
            ["recordedTo"] = GetRecordedTo(request)
        },
            includeLastNData: true);
    }

    if (request.RequestType == ConnectorRequestType.GetAssessments)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["assetUuid"] = GetAssetUuid(request),
            ["healthLevelTypeUuid"] = GetHealthLevelTypeUuid(request),
            ["assessedFrom"] = GetAssessedFrom(request),
            ["assessedTo"] = GetAssessedTo(request),
            ["healthLevelMin"] = GetHealthLevelMin(request),
            ["healthLevelMax"] = GetHealthLevelMax(request)
        },
            includeLastNData: true);
    }

    if (request.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        return BuildPreview(
            new Dictionary<string, object?>
        {
            ["eventUuid"] = GetEventUuid(request),
            ["siteUuid"] = GetSiteUuid(request),
            ["segmentUuid"] = GetSegmentUuid(request),
            ["assetUuid"] = GetAssetUuid(request),
            ["serialNumber"] = GetSerialNumber(request),
            ["installedNow"] = GetInstalledNow(request),
            ["installedFrom"] = GetInstalledFrom(request),
            ["installedTo"] = GetInstalledTo(request),
            ["removedFrom"] = GetRemovedFrom(request),
            ["removedTo"] = GetRemovedTo(request)
        },
            includeLastNData: true);
    }

    return request;
}

static ConnectorPostRequestOptions EditShortFormRequestInput(
    string requestName,
    ConnectorPostRequestOptions currentRequest)
{
    System.Console.WriteLine();
    System.Console.WriteLine("Edit Short-Form " + requestName + " Input");
    System.Console.WriteLine("Current payloadProfile: " + FormatPayloadProfile(currentRequest.PayloadProfile));
    System.Console.WriteLine("Current maxItems: " + FormatNullableShort(currentRequest.MaxItems));
    if (currentRequest.RequestType == ConnectorRequestType.GetMeasurements ||
        currentRequest.RequestType == ConnectorRequestType.GetAssessments ||
        currentRequest.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        System.Console.WriteLine("Current lastNData: " + FormatNullableShort(currentRequest.LastNData));
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetAssets ||
        currentRequest.RequestType == ConnectorRequestType.GetAssessments ||
        currentRequest.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        System.Console.WriteLine("Current assetUuid: \"" + GetAssetUuid(currentRequest) + "\"");
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetMeasurementLocations ||
        currentRequest.RequestType == ConnectorRequestType.GetMeasurements)
    {
        System.Console.WriteLine("Current measurementLocationUuid: \"" + GetMeasurementLocationUuid(currentRequest) + "\"");
    }
    if (currentRequest.RequestType != ConnectorRequestType.GetMeasurements &&
        currentRequest.RequestType != ConnectorRequestType.GetAssessments)
    {
        System.Console.WriteLine("Current siteUuid: \"" + GetSiteUuid(currentRequest) + "\"");
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetSegments)
    {
        System.Console.WriteLine("Current segmentUuid: \"" + GetSegmentUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current typeUuid: \"" + GetTypeUuid(currentRequest) + "\"");
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetAssets)
    {
        System.Console.WriteLine("Current typeUuid: \"" + GetTypeUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current serialNumber: \"" + GetSerialNumber(currentRequest) + "\"");
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetMeasurementLocations)
    {
        System.Console.WriteLine("Current segmentUuid: \"" + GetSegmentUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current typeUuid: \"" + GetTypeUuid(currentRequest) + "\"");
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetMeasurements)
    {
        System.Console.WriteLine("Current segmentUuid: \"" + GetSegmentUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current recordedFrom: \"" + GetRecordedFrom(currentRequest) + "\"");
        System.Console.WriteLine("Current recordedTo: \"" + GetRecordedTo(currentRequest) + "\"");
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetAssessments)
    {
        System.Console.WriteLine("Current healthLevelTypeUuid: \"" + GetHealthLevelTypeUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current assessedFrom: \"" + GetAssessedFrom(currentRequest) + "\"");
        System.Console.WriteLine("Current assessedTo: \"" + GetAssessedTo(currentRequest) + "\"");
        System.Console.WriteLine("Current healthLevelMin: " + FormatNullableDouble(GetHealthLevelMin(currentRequest)));
        System.Console.WriteLine("Current healthLevelMax: " + FormatNullableDouble(GetHealthLevelMax(currentRequest)));
    }
    if (currentRequest.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        System.Console.WriteLine("Current eventUuid: \"" + GetEventUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current segmentUuid: \"" + GetSegmentUuid(currentRequest) + "\"");
        System.Console.WriteLine("Current serialNumber: \"" + GetSerialNumber(currentRequest) + "\"");
        System.Console.WriteLine("Current installedNow: " + FormatNullableBool(GetInstalledNow(currentRequest)));
        System.Console.WriteLine("Current installedFrom: \"" + GetInstalledFrom(currentRequest) + "\"");
        System.Console.WriteLine("Current installedTo: \"" + GetInstalledTo(currentRequest) + "\"");
        System.Console.WriteLine("Current removedFrom: \"" + GetRemovedFrom(currentRequest) + "\"");
        System.Console.WriteLine("Current removedTo: \"" + GetRemovedTo(currentRequest) + "\"");
    }

    ConnectorPostRequestOptions editedRequest = ClonePostRequest(currentRequest);

    if (!TryEditPayloadProfile(editedRequest.PayloadProfile, out string? payloadProfile))
    {
        return currentRequest;
    }
    editedRequest.PayloadProfile = payloadProfile;

    System.Console.Write("Enter maxItems, or press Enter to keep current: ");
    string maxItemsInput = System.Console.ReadLine() ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(maxItemsInput))
    {
        if (short.TryParse(maxItemsInput, out short maxItems))
        {
            editedRequest.MaxItems = maxItems;
        }
        else
        {
            System.Console.WriteLine("maxItems must be a whole number.");
            return currentRequest;
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetMeasurements ||
        editedRequest.RequestType == ConnectorRequestType.GetAssessments ||
        editedRequest.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        System.Console.Write("Enter lastNData, 'clear' to clear it, or press Enter to keep current: ");
        string lastNDataInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(lastNDataInput))
        {
            if (string.Equals(lastNDataInput, "clear", StringComparison.OrdinalIgnoreCase))
            {
                editedRequest.LastNData = null;
            }
            else if (short.TryParse(lastNDataInput, out short lastNData))
            {
                editedRequest.LastNData = lastNData;
            }
            else
            {
                System.Console.WriteLine("lastNData must be a whole number.");
                return currentRequest;
            }
        }
    }

    if (editedRequest.RequestType != ConnectorRequestType.GetMeasurements &&
        editedRequest.RequestType != ConnectorRequestType.GetAssessments)
    {
        System.Console.Write("Enter siteUuid, 'clear' to clear it, or press Enter to keep current: ");
        string siteUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(siteUuidInput))
        {
            editedRequest.Filters.SiteUuid = string.Equals(siteUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : siteUuidInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetAssets ||
        editedRequest.RequestType == ConnectorRequestType.GetAssessments ||
        editedRequest.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        System.Console.Write("Enter assetUuid, 'clear' to clear it, or press Enter to keep current: ");
        string assetUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(assetUuidInput))
        {
            editedRequest.Filters.AssetUuid = string.Equals(assetUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : assetUuidInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetMeasurementLocations ||
        editedRequest.RequestType == ConnectorRequestType.GetMeasurements)
    {
        System.Console.Write("Enter measurementLocationUuid, 'clear' to clear it, or press Enter to keep current: ");
        string measurementLocationUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(measurementLocationUuidInput))
        {
            editedRequest.Filters.MeasurementLocationUuid = string.Equals(
                measurementLocationUuidInput,
                "clear",
                StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : measurementLocationUuidInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetSegments)
    {
        System.Console.Write("Enter segmentUuid, 'clear' to clear it, or press Enter to keep current: ");
        string segmentUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(segmentUuidInput))
        {
            editedRequest.Filters.SegmentUuid = string.Equals(segmentUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : segmentUuidInput.Trim();
        }

        System.Console.Write("Enter typeUuid, 'clear' to clear it, or press Enter to keep current: ");
        string typeUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(typeUuidInput))
        {
            editedRequest.Filters.TypeUuid = string.Equals(typeUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : typeUuidInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetMeasurementLocations)
    {
        System.Console.Write("Enter segmentUuid, 'clear' to clear it, or press Enter to keep current: ");
        string segmentUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(segmentUuidInput))
        {
            editedRequest.Filters.SegmentUuid = string.Equals(segmentUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : segmentUuidInput.Trim();
        }

        System.Console.Write("Enter typeUuid, 'clear' to clear it, or press Enter to keep current: ");
        string typeUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(typeUuidInput))
        {
            editedRequest.Filters.TypeUuid = string.Equals(typeUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : typeUuidInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetMeasurements)
    {
        System.Console.Write("Enter segmentUuid, 'clear' to clear it, or press Enter to keep current: ");
        string segmentUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(segmentUuidInput))
        {
            editedRequest.Filters.SegmentUuid = string.Equals(segmentUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : segmentUuidInput.Trim();
        }

        System.Console.Write("Enter recordedFrom, 'clear' to clear it, or press Enter to keep current: ");
        string recordedFromInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(recordedFromInput))
        {
            editedRequest.Filters.RecordedFrom = string.Equals(recordedFromInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : recordedFromInput.Trim();
        }

        System.Console.Write("Enter recordedTo, 'clear' to clear it, or press Enter to keep current: ");
        string recordedToInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(recordedToInput))
        {
            editedRequest.Filters.RecordedTo = string.Equals(recordedToInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : recordedToInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetAssets)
    {
        System.Console.Write("Enter typeUuid, 'clear' to clear it, or press Enter to keep current: ");
        string typeUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(typeUuidInput))
        {
            editedRequest.Filters.TypeUuid = string.Equals(typeUuidInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : typeUuidInput.Trim();
        }

        System.Console.Write("Enter serialNumber, 'clear' to clear it, or press Enter to keep current: ");
        string serialNumberInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(serialNumberInput))
        {
            editedRequest.Filters.SerialNumber = string.Equals(serialNumberInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : serialNumberInput.Trim();
        }
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetAssessments)
    {
        System.Console.Write("Enter healthLevelTypeUuid, 'clear' to clear it, or press Enter to keep current: ");
        string healthLevelTypeUuidInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(healthLevelTypeUuidInput))
        {
            editedRequest.Filters.HealthLevelTypeUuid = string.Equals(
                healthLevelTypeUuidInput,
                "clear",
                StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : healthLevelTypeUuidInput.Trim();
        }

        System.Console.Write("Enter assessedFrom, 'clear' to clear it, or press Enter to keep current: ");
        string assessedFromInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(assessedFromInput))
        {
            editedRequest.Filters.AssessedFrom = string.Equals(assessedFromInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : assessedFromInput.Trim();
        }

        System.Console.Write("Enter assessedTo, 'clear' to clear it, or press Enter to keep current: ");
        string assessedToInput = System.Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(assessedToInput))
        {
            editedRequest.Filters.AssessedTo = string.Equals(assessedToInput, "clear", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : assessedToInput.Trim();
        }

        if (!TryEditNullableDouble("healthLevelMin", GetHealthLevelMin(editedRequest), out double? healthLevelMin))
        {
            return currentRequest;
        }
        editedRequest.Filters.HealthLevelMin = healthLevelMin;

        if (!TryEditNullableDouble("healthLevelMax", GetHealthLevelMax(editedRequest), out double? healthLevelMax))
        {
            return currentRequest;
        }
        editedRequest.Filters.HealthLevelMax = healthLevelMax;
    }

    if (editedRequest.RequestType == ConnectorRequestType.GetAssetSegmentEvents)
    {
        EditStringFilter("eventUuid", GetEventUuid(editedRequest), value => editedRequest.Filters.EventUuid = value);
        EditStringFilter("segmentUuid", GetSegmentUuid(editedRequest), value => editedRequest.Filters.SegmentUuid = value);
        EditStringFilter("serialNumber", GetSerialNumber(editedRequest), value => editedRequest.Filters.SerialNumber = value);

        if (!TryEditNullableBool("installedNow", GetInstalledNow(editedRequest), out bool? installedNow))
        {
            return currentRequest;
        }
        editedRequest.Filters.InstalledNow = installedNow;

        EditStringFilter("installedFrom", GetInstalledFrom(editedRequest), value => editedRequest.Filters.InstalledFrom = value);
        EditStringFilter("installedTo", GetInstalledTo(editedRequest), value => editedRequest.Filters.InstalledTo = value);
        EditStringFilter("removedFrom", GetRemovedFrom(editedRequest), value => editedRequest.Filters.RemovedFrom = value);
        EditStringFilter("removedTo", GetRemovedTo(editedRequest), value => editedRequest.Filters.RemovedTo = value);
    }

    ConnectorRequestValidationResult validationResult =
        new ConnectorRequestValidator().Validate(editedRequest);

    if (!validationResult.IsValid)
    {
        System.Console.WriteLine("Short-form " + requestName + " input was not updated.");
        foreach (ConnectorRequestValidationError error in validationResult.Errors)
        {
            System.Console.WriteLine(error.Field + ": " + error.Message);
        }

        return currentRequest;
    }

    System.Console.WriteLine("Short-form " + requestName + " input updated.");

    return editedRequest;
}

static string FormatNullableShort(short? value)
{
    return value.HasValue ? value.Value.ToString() : "";
}

static string FormatPayloadProfile(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "Default / omitted (Full behavior)";
    }

    return NormalizePayloadProfileForDisplay(value);
}

static string NormalizePayloadProfileForDisplay(string value)
{
    return PayloadProfileParser.TryParse(value, out var payloadProfile)
        ? PayloadProfileParser.ToCanonicalString(payloadProfile)
        : value.Trim();
}

static bool TryEditPayloadProfile(string? currentValue, out string? value)
{
    value = string.IsNullOrWhiteSpace(currentValue)
        ? null
        : NormalizePayloadProfileForDisplay(currentValue);

    System.Console.WriteLine();
    System.Console.WriteLine("Payload Profile");
    System.Console.WriteLine("Current: " + FormatPayloadProfile(currentValue));
    System.Console.WriteLine();
    System.Console.WriteLine("1. Default / omitted");
    System.Console.WriteLine("2. Full");
    System.Console.WriteLine("3. Minimal");
    System.Console.Write("Enter = keep current: ");

    string input = System.Console.ReadLine() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(input))
    {
        return true;
    }

    switch (input.Trim())
    {
        case "1":
            value = null;
            return true;
        case "2":
            value = PayloadProfileParser.ToCanonicalString(PayloadProfile.Full);
            return true;
        case "3":
            value = PayloadProfileParser.ToCanonicalString(PayloadProfile.Minimal);
            return true;
        default:
            if (PayloadProfileParser.TryParse(input, out var payloadProfile))
            {
                value = PayloadProfileParser.ToCanonicalString(payloadProfile);
                return true;
            }

            System.Console.WriteLine("payloadProfile must be Default / omitted, Full, or Minimal.");
            return false;
    }
}

static string FormatNullableDouble(double? value)
{
    return value.HasValue
        ? value.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
        : "";
}

static string FormatNullableBool(bool? value)
{
    return value.HasValue ? value.Value.ToString().ToLowerInvariant() : "";
}

static void EditStringFilter(string fieldName, string currentValue, Action<string> setValue)
{
    System.Console.Write("Enter " + fieldName + ", 'clear' to clear it, or press Enter to keep current: ");
    string input = System.Console.ReadLine() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(input))
    {
        return;
    }

    setValue(string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase)
        ? string.Empty
        : input.Trim());
}

static bool TryEditNullableBool(string fieldName, bool? currentValue, out bool? value)
{
    System.Console.Write("Enter " + fieldName + " (true/false), 'clear' to clear it, or press Enter to keep current: ");
    string input = System.Console.ReadLine() ?? string.Empty;
    value = currentValue;

    if (string.IsNullOrWhiteSpace(input))
    {
        return true;
    }

    if (string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase))
    {
        value = null;
        return true;
    }

    if (bool.TryParse(input, out bool parsed))
    {
        value = parsed;
        return true;
    }

    System.Console.WriteLine(fieldName + " must be true or false.");
    return false;
}

static bool TryEditNullableDouble(string fieldName, double? currentValue, out double? value)
{
    System.Console.Write(
        "Enter " + fieldName + ", 'clear' to clear it, or press Enter to keep current: ");
    string input = System.Console.ReadLine() ?? string.Empty;
    value = currentValue;

    if (string.IsNullOrWhiteSpace(input))
    {
        return true;
    }

    if (string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase))
    {
        value = null;
        return true;
    }

    if (double.TryParse(
        input,
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out double parsed))
    {
        value = parsed;
        return true;
    }

    System.Console.WriteLine(fieldName + " must be a number using invariant decimal format.");
    return false;
}

static string GetSiteUuid(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.SiteUuid;
}

static string GetAssetUuid(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.AssetUuid;
}

static string GetMeasurementLocationUuid(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.MeasurementLocationUuid;
}

static string GetSegmentUuid(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.SegmentUuid;
}

static string GetTypeUuid(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.TypeUuid;
}

static string GetSerialNumber(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.SerialNumber;
}

static string GetRecordedFrom(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.RecordedFrom;
}

static string GetRecordedTo(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.RecordedTo;
}

static string GetHealthLevelTypeUuid(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.HealthLevelTypeUuid;
}

static string GetAssessedFrom(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.AssessedFrom;
}

static string GetAssessedTo(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? string.Empty : request.Filters.AssessedTo;
}

static double? GetHealthLevelMin(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? null : request.Filters.HealthLevelMin;
}

static double? GetHealthLevelMax(ConnectorPostRequestOptions request)
{
    return request.Filters == null ? null : request.Filters.HealthLevelMax;
}

static string GetEventUuid(ConnectorPostRequestOptions request) =>
    request.Filters == null ? string.Empty : request.Filters.EventUuid;

static bool? GetInstalledNow(ConnectorPostRequestOptions request) =>
    request.Filters == null ? null : request.Filters.InstalledNow;

static string GetInstalledFrom(ConnectorPostRequestOptions request) =>
    request.Filters == null ? string.Empty : request.Filters.InstalledFrom;

static string GetInstalledTo(ConnectorPostRequestOptions request) =>
    request.Filters == null ? string.Empty : request.Filters.InstalledTo;

static string GetRemovedFrom(ConnectorPostRequestOptions request) =>
    request.Filters == null ? string.Empty : request.Filters.RemovedFrom;

static string GetRemovedTo(ConnectorPostRequestOptions request) =>
    request.Filters == null ? string.Empty : request.Filters.RemovedTo;

static void PreviewGeneratedBodJson(string requestName, ConnectorPostRequestOptions request)
{
    System.Console.WriteLine();
    System.Console.WriteLine("Generated " + requestName + " BOD JSON:");

    try
    {
        var builder = new ConnectorRequestJsonBuilder();
        System.Console.WriteLine(builder.BuildRequestJson(request));
    }
    catch (Exception ex)
    {
        System.Console.WriteLine(ex.Message);
    }
}

static async Task<ConnectorCommandResult> RunOpenSubscriptionJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.OpenSubscription + " result:");

    var command = ConsumerPublicationJsonPilot.CreateOpenSubscriptionCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        settings.Publication.ChannelId,
        settings.Publication.SubscriptionTopics,
        settings.IncludeRawResponse);

    var result = await ConsumerPublicationJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunReadPublicationJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.ReadPublication + " result:");

    var command = ConsumerPublicationJsonPilot.CreateReadPublicationCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        settings.IncludeRawResponse);

    var result = await ConsumerPublicationJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunRemovePublicationJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.RemovePublication + " result:");

    var command = ConsumerPublicationJsonPilot.CreateRemovePublicationCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        settings.IncludeRawResponse);

    var result = await ConsumerPublicationJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunCloseSubscriptionJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.CloseSubscription + " result:");

    var command = ConsumerPublicationJsonPilot.CreateCloseSubscriptionCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        settings.IncludeRawResponse);

    var result = await ConsumerPublicationJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunOpenRequestSessionJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.OpenRequestSession + " result:");

    var command = ConsumerRequestJsonPilot.CreateOpenRequestSessionCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        settings.Request.ChannelId,
        settings.IncludeRawResponse);

    var result = await ConsumerRequestJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunReadResponseJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId,
    string messageId)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.ReadResponse + " result:");

    var command = ConsumerRequestJsonPilot.CreateReadResponseCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        messageId,
        settings.IncludeRawResponse);

    var result = await ConsumerRequestJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunPostRequestJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId,
    ConnectorPostRequestOptions requestOptions)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.PostRequest + " result:");

    var command = ConsumerRequestJsonPilot.CreatePostRequestCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        requestOptions,
        settings.IncludeRawResponse);

    var result = await ConsumerRequestJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunRemoveResponseJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId,
    string messageId)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.RemoveResponse + " result:");

    var command = ConsumerRequestJsonPilot.CreateRemoveResponseCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        messageId,
        settings.IncludeRawResponse);

    var result = await ConsumerRequestJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static async Task<ConnectorCommandResult> RunCloseRequestSessionJsonAsync(
    ConsoleSettings settings,
    ConnectorCommandRunner runner,
    JsonSerializerOptions jsonOptions,
    string sessionId)
{
    System.Console.WriteLine();
    System.Console.WriteLine(ConnectorCommandRunner.CloseRequestSession + " result:");

    var command = ConsumerRequestJsonPilot.CreateCloseRequestSessionCommand(
        settings.Host,
        settings.AuthenticationScheme,
        settings.ApiKey,
        settings.UserName,
        settings.Password,
        sessionId,
        settings.IncludeRawResponse);

    var result = await ConsumerRequestJsonPilot.RunJsonCommandAsync(
        command,
        json => runner.RunJsonAsync(json),
        jsonOptions);

    PrintCommandResult(result, settings.ShowFullCommandResult, jsonOptions);

    return result;
}

static void PrintCommandResult(
    ConnectorCommandResult result,
    bool showFullCommandResult,
    JsonSerializerOptions jsonOptions)
{
    PrintCommandSummary(result);
    PrintPayload(result.Payload, jsonOptions);

    if (showFullCommandResult)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Full Command Result:");
        System.Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
}

static void PrintCommandSummary(ConnectorCommandResult result)
{
    System.Console.WriteLine("Command Summary:");
    System.Console.WriteLine("Success: " + result.Success.ToString().ToLowerInvariant());
    System.Console.WriteLine("Command: " + result.Command);
    System.Console.WriteLine("StatusCode: " + result.StatusCode);

    if (!string.IsNullOrWhiteSpace(result.ReasonPhrase))
    {
        System.Console.WriteLine("ReasonPhrase: " + result.ReasonPhrase);
    }

    if (!string.IsNullOrWhiteSpace(result.SessionId))
    {
        System.Console.WriteLine("SessionId: " + result.SessionId);
    }

    if (!string.IsNullOrWhiteSpace(result.MessageId))
    {
        System.Console.WriteLine("MessageId: " + result.MessageId);
    }

    if (!string.IsNullOrWhiteSpace(result.ErrorCode))
    {
        System.Console.WriteLine("ErrorCode: " + result.ErrorCode);
    }

    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
    {
        System.Console.WriteLine("ErrorMessage: " + result.ErrorMessage);
    }

    if (result.ValidationErrors.Count > 0)
    {
        System.Console.WriteLine("Validation Errors:");
        foreach (string validationError in result.ValidationErrors)
        {
            System.Console.WriteLine("* " + validationError);
        }
    }
}

static void PrintPayload(string payload, JsonSerializerOptions jsonOptions)
{
    if (string.IsNullOrWhiteSpace(payload))
    {
        return;
    }

    if (!TryFormatJson(payload, jsonOptions, out string formattedJson))
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Payload:");
        System.Console.WriteLine(payload);
        return;
    }

    System.Console.WriteLine();
    System.Console.WriteLine("Pretty Payload:");
    System.Console.WriteLine(formattedJson);
}

static bool TryFormatJson(string json, JsonSerializerOptions jsonOptions, out string formattedJson)
{
    formattedJson = string.Empty;

    try
    {
        using JsonDocument document = JsonDocument.Parse(json);
        formattedJson = JsonSerializer.Serialize(document.RootElement, jsonOptions);

        return true;
    }
    catch (JsonException)
    {
        return false;
    }
}

internal sealed class ConsoleSettings
{
    public string Host { get; set; } = string.Empty;
    public AuthenticationSchemeType AuthenticationScheme { get; set; } = AuthenticationSchemeType.BasicApi;
    public string ApiKey { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IncludeRawResponse { get; set; }
    public bool ShowFullCommandResult { get; set; }
    public PublicationConsoleSettings Publication { get; set; } = new PublicationConsoleSettings();
    public RequestSessionConsoleSettings Request { get; set; } = new RequestSessionConsoleSettings();
    public ConnectorPostRequestOptions GetSitesRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions GetSegmentsRequest { get; set; } = new ConnectorPostRequestOptions
    {
        RequestType = ConnectorRequestType.GetSegments
    };
    public ConnectorPostRequestOptions GetAssetsRequest { get; set; } = new ConnectorPostRequestOptions
    {
        RequestType = ConnectorRequestType.GetAssets
    };
    public ConnectorPostRequestOptions GetMeasurementLocationsRequest { get; set; } = new ConnectorPostRequestOptions
    {
        RequestType = ConnectorRequestType.GetMeasurementLocations
    };
    public ConnectorPostRequestOptions GetMeasurementsRequest { get; set; } = new ConnectorPostRequestOptions
    {
        RequestType = ConnectorRequestType.GetMeasurements
    };
    public ConnectorPostRequestOptions GetAssessmentsRequest { get; set; } = new ConnectorPostRequestOptions
    {
        RequestType = ConnectorRequestType.GetAssessments
    };
    public ConnectorPostRequestOptions GetAssetSegmentEventsRequest { get; set; } = new ConnectorPostRequestOptions
    {
        RequestType = ConnectorRequestType.GetAssetSegmentEvents
    };

    public static ConsoleSettings Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);

        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Configuration file '{fileName}' was not found. Copy appsettings.example.json to {fileName} and fill in local values.",
                fileName);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        return new ConsoleSettings
        {
            Host = GetString(root, "host"),
            AuthenticationScheme = GetAuthenticationScheme(root),
            ApiKey = GetString(root, "apiKey"),
            UserName = GetString(root, "userName"),
            Password = GetString(root, "password"),
            IncludeRawResponse = GetBool(root, "includeRawResponse"),
            ShowFullCommandResult = GetBool(root, "consoleOutput", "showFullCommandResult"),
            Publication = GetPublicationSettings(root),
            Request = GetRequestSessionSettings(root),
            GetSitesRequest = GetPostRequest(root, "getSitesRequest", ConnectorRequestType.GetSites),
            GetSegmentsRequest = GetPostRequest(root, "getSegmentsRequest", ConnectorRequestType.GetSegments),
            GetAssetsRequest = GetPostRequest(root, "getAssetsRequest", ConnectorRequestType.GetAssets),
            GetMeasurementLocationsRequest = GetPostRequest(
                root,
                "getMeasurementLocationsRequest",
                ConnectorRequestType.GetMeasurementLocations),
            GetMeasurementsRequest = GetPostRequest(
                root,
                "getMeasurementsRequest",
                ConnectorRequestType.GetMeasurements),
            GetAssessmentsRequest = GetPostRequest(
                root,
                "getAssessmentsRequest",
                ConnectorRequestType.GetAssessments),
            GetAssetSegmentEventsRequest = GetPostRequest(
                root,
                "getAssetSegmentEventsRequest",
                ConnectorRequestType.GetAssetSegmentEvents)
        };
    }

    private static PublicationConsoleSettings GetPublicationSettings(JsonElement root)
    {
        if (!root.TryGetProperty("publication", out var publicationElement) ||
            publicationElement.ValueKind != JsonValueKind.Object)
        {
            return new PublicationConsoleSettings();
        }

        return new PublicationConsoleSettings
        {
            ChannelId = GetOptionalString(publicationElement, "channelId"),
            SubscriptionTopics = GetOptionalStringArray(publicationElement, "subscriptionTopics")
        };
    }

    private static RequestSessionConsoleSettings GetRequestSessionSettings(JsonElement root)
    {
        if (!root.TryGetProperty("request", out var requestElement) ||
            requestElement.ValueKind != JsonValueKind.Object)
        {
            return new RequestSessionConsoleSettings();
        }

        return new RequestSessionConsoleSettings
        {
            ChannelId = GetOptionalString(requestElement, "channelId")
        };
    }

    private static ConnectorPostRequestOptions GetPostRequest(
        JsonElement root,
        string sectionName,
        ConnectorRequestType defaultRequestType)
    {
        ConnectorPostRequestOptions options = new ConnectorPostRequestOptions
        {
            RequestType = defaultRequestType
        };

        if (!root.TryGetProperty(sectionName, out var requestElement) ||
            requestElement.ValueKind != JsonValueKind.Object)
        {
            return options;
        }

        string requestType = GetString(requestElement, "requestType");
        if (!string.IsNullOrWhiteSpace(requestType) &&
            Enum.TryParse(requestType, ignoreCase: true, out ConnectorRequestType parsedRequestType))
        {
            options.RequestType = parsedRequestType;
        }

        string payloadProfile = GetString(requestElement, "payloadProfile");
        if (!string.IsNullOrWhiteSpace(payloadProfile))
        {
            options.PayloadProfile = PayloadProfileParser.TryParse(payloadProfile, out var parsedPayloadProfile)
                ? PayloadProfileParser.ToCanonicalString(parsedPayloadProfile)
                : payloadProfile.Trim();
        }

        if (requestElement.TryGetProperty("maxItems", out var maxItemsProperty) &&
            maxItemsProperty.ValueKind == JsonValueKind.Number &&
            maxItemsProperty.TryGetInt16(out short maxItems))
        {
            options.MaxItems = maxItems;
        }

        if (requestElement.TryGetProperty("lastNData", out var lastNDataProperty) &&
            lastNDataProperty.ValueKind == JsonValueKind.Number &&
            lastNDataProperty.TryGetInt16(out short lastNData))
        {
            options.LastNData = lastNData;
        }

        if (requestElement.TryGetProperty("filters", out var filtersElement) &&
            filtersElement.ValueKind == JsonValueKind.Object)
        {
            options.Filters.AssetUuid = GetString(filtersElement, "assetUuid");
            options.Filters.MeasurementLocationUuid = GetString(filtersElement, "measurementLocationUuid");
            options.Filters.SiteUuid = GetString(filtersElement, "siteUuid");
            options.Filters.SegmentUuid = GetString(filtersElement, "segmentUuid");
            options.Filters.TypeUuid = GetString(filtersElement, "typeUuid");
            options.Filters.SerialNumber = GetString(filtersElement, "serialNumber");
            options.Filters.RecordedFrom = GetString(filtersElement, "recordedFrom");
            options.Filters.RecordedTo = GetString(filtersElement, "recordedTo");
            options.Filters.HealthLevelTypeUuid = GetString(filtersElement, "healthLevelTypeUuid");
            options.Filters.AssessedFrom = GetString(filtersElement, "assessedFrom");
            options.Filters.AssessedTo = GetString(filtersElement, "assessedTo");
            options.Filters.HealthLevelMin = GetNullableDouble(filtersElement, "healthLevelMin");
            options.Filters.HealthLevelMax = GetNullableDouble(filtersElement, "healthLevelMax");
            options.Filters.EventUuid = GetString(filtersElement, "eventUuid");
            options.Filters.InstalledNow = GetNullableBool(filtersElement, "installedNow");
            options.Filters.InstalledFrom = GetString(filtersElement, "installedFrom");
            options.Filters.InstalledTo = GetString(filtersElement, "installedTo");
            options.Filters.RemovedFrom = GetString(filtersElement, "removedFrom");
            options.Filters.RemovedTo = GetString(filtersElement, "removedTo");
        }

        return options;
    }

    private static AuthenticationSchemeType GetAuthenticationScheme(JsonElement root)
    {
        var value = GetString(root, "authenticationScheme");
        if (string.IsNullOrWhiteSpace(value))
        {
            return AuthenticationSchemeType.BasicApi;
        }

        if (Enum.TryParse(value, ignoreCase: true, out AuthenticationSchemeType scheme))
        {
            return scheme;
        }

        throw new InvalidOperationException(
            $"Unsupported authenticationScheme '{value}'. Use BasicApi or Basic.");
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string[]? GetOptionalStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string?>();
        foreach (JsonElement item in property.EnumerateArray())
        {
            values.Add(item.ValueKind == JsonValueKind.String ? item.GetString() : null);
        }

        return values.ToArray()!;
    }

    private static double? GetNullableDouble(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out double value)
            ? value
            : null;
    }

    private static bool? GetNullableBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) &&
            property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : null;
    }

    private static bool GetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            && property.GetBoolean();
    }

    private static bool GetBool(JsonElement root, string sectionName, string propertyName)
    {
        return root.TryGetProperty(sectionName, out var section)
            && section.ValueKind == JsonValueKind.Object
            && GetBool(section, propertyName);
    }
}

internal sealed class PublicationConsoleSettings
{
    public string? ChannelId { get; set; }
    public string[]? SubscriptionTopics { get; set; }
}

internal sealed class RequestSessionConsoleSettings
{
    public string? ChannelId { get; set; }
}

internal sealed class RequestConsoleState
{
    public string LastGetSitesMessageId { get; set; } = string.Empty;
    public string LastGetSegmentsMessageId { get; set; } = string.Empty;
    public string LastGetAssetsMessageId { get; set; } = string.Empty;
    public string LastGetMeasurementLocationsMessageId { get; set; } = string.Empty;
    public string LastGetMeasurementsMessageId { get; set; } = string.Empty;
    public string LastGetAssessmentsMessageId { get; set; } = string.Empty;
    public string LastGetAssetSegmentEventsMessageId { get; set; } = string.Empty;
    public ConnectorPostRequestOptions CurrentGetSitesRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions CurrentGetSegmentsRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions CurrentGetAssetsRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions CurrentGetMeasurementLocationsRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions CurrentGetMeasurementsRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions CurrentGetAssessmentsRequest { get; set; } = new ConnectorPostRequestOptions();
    public ConnectorPostRequestOptions CurrentGetAssetSegmentEventsRequest { get; set; } = new ConnectorPostRequestOptions();

    public void ClearMessageIds()
    {
        LastGetSitesMessageId = string.Empty;
        LastGetSegmentsMessageId = string.Empty;
        LastGetAssetsMessageId = string.Empty;
        LastGetMeasurementLocationsMessageId = string.Empty;
        LastGetMeasurementsMessageId = string.Empty;
        LastGetAssessmentsMessageId = string.Empty;
        LastGetAssetSegmentEventsMessageId = string.Empty;
    }
}
