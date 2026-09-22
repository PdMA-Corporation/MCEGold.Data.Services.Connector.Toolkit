using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using MCEGold.Data.Services.Connector.EndpointOptions;
using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.Filters;
using MCEGold.Data.Services.Connector.RequestCriteria;
using MCEGold.Data.Services.Connector.ResponseType;
using CommandOptions = MCEGold.Data.Services.Connector.Commands.ConnectorCommandOptions;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace MCEGold.Data.Services.Connector.PackageConsumer;

internal static class Program
{
    private static async Task<int> Main()
    {
        ValidateEnumsAndFilters();
        ValidateResponseTypes();
        ValidateRequestHelpers();
        await ValidateCommandApiAsync();
        ReferenceServerBoundServiceCallsForCompileOnly();
        ReferenceDeprecatedApisForCompatibilityCompileOnly();

        Console.WriteLine("MCEGold.Data.Services.Connector package consumer validation passed.");
        Console.WriteLine("Package assembly: " + typeof(ConsumerRequestService).Assembly.FullName);
        Console.WriteLine("Package assembly location: " + typeof(ConsumerRequestService).Assembly.Location);
        return 0;
    }

    private static void ValidateEnumsAndFilters()
    {
        AuthenticationSchemeType authScheme = AuthenticationSchemeType.BasicApi;
        PayloadProfile payloadProfile = PayloadProfile.Minimal;
        ConnectorCommandType commandType = ConnectorCommandType.PostGetSitesRequest;
        ConnectorRequestType requestType = ConnectorRequestType.GetSites;

        var uuid = new UUIDFilter
        {
            FilterType = FilterTypes.UUIDFilter.Equal,
            Value = "site-uuid"
        };
        var utcDateTime = new UTCDateTimeFilter
        {
            FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
            Value = "2026-09-01T00:00:00Z",
            LocHrDeltaFromUTC = 0,
            LocMinDeltaFromUTC = 0
        };
        var text = new TextFilter
        {
            FilterType = FilterTypes.TextFilter.Equal,
            Value = "serial"
        };
        var numeric = new NumericFilter
        {
            FilterType = FilterTypes.NumericFilter.MaxInclusive,
            Value = 10.5
        };
        var boolean = new BooleanFilter { Value = true };

        Require(authScheme == AuthenticationSchemeType.BasicApi, "AuthenticationSchemeType is consumable.");
        Require(payloadProfile == PayloadProfile.Minimal, "PayloadProfile is consumable.");
        Require(commandType == ConnectorCommandType.PostGetSitesRequest, "ConnectorCommandType is consumable.");
        Require(requestType == ConnectorRequestType.GetSites, "ConnectorRequestType is consumable.");
        Require(uuid.Value.Length > 0, "UUIDFilter is consumable.");
        Require(utcDateTime.Value.Length > 0, "UTCDateTimeFilter is consumable.");
        Require(text.Value.Length > 0, "TextFilter is consumable.");
        Require(numeric.Value > 0, "NumericFilter is consumable.");
        Require(boolean.Value, "BooleanFilter is consumable.");
    }

    private static void ValidateResponseTypes()
    {
        ISBMResponse response = new OpenConsumerRequestSessionResponse
        {
            StatusCode = 201,
            ReasonPhrase = "Created",
            ISBMHTTPResponse = "{}",
            SessionID = "request-session"
        };
        var openSubscription = new OpenSubscriptionSessionResponse { SessionID = "subscription-session" };
        var closeSubscription = new CloseSubscriptionSessionResponse { StatusCode = 204 };
        var readPublication = new ReadPublicationResponse
        {
            MessageID = "publication-message",
            MessageContent = "{}",
            Topics = new[] { "topic" }
        };
        var removePublication = new RemovePublicationResponse { StatusCode = 204 };
        var postRequest = new PostRequestResponse { MessageID = "request-message" };
        var readResponse = new ReadResponseResponse
        {
            MessageID = "response-message",
            MessageContent = "{}"
        };
        var removeResponse = new RemoveResponseResponse { StatusCode = 204 };
        var closeRequest = new CloseConsumerRequestSessionResponse { StatusCode = 204 };
        var commandResult = new ConnectorCommandResult
        {
            Success = true,
            Command = CommandRunner.PostGetSitesRequest,
            StatusCode = 201,
            MessageId = "message",
            RequestMessageId = "request",
            ResponseMessageId = "response",
            Payload = "{}",
            Raw = "{}"
        };

        Require(response.StatusCode == 201, "ISBMResponse fields are consumable.");
        Require(openSubscription.SessionID.Length > 0, "OpenSubscriptionSessionResponse is consumable.");
        Require(closeSubscription.StatusCode == 204, "CloseSubscriptionSessionResponse is consumable.");
        Require(readPublication.Topics.Length == 1, "ReadPublicationResponse is consumable.");
        Require(removePublication.StatusCode == 204, "RemovePublicationResponse is consumable.");
        Require(postRequest.MessageID.Length > 0, "PostRequestResponse is consumable.");
        Require(readResponse.MessageID.Length > 0, "ReadResponseResponse is consumable.");
        Require(removeResponse.StatusCode == 204, "RemoveResponseResponse is consumable.");
        Require(closeRequest.StatusCode == 204, "CloseConsumerRequestSessionResponse is consumable.");
        Require(commandResult.Success, "ConnectorCommandResult is consumable.");
    }

    private static void ValidateRequestHelpers()
    {
        var broadFilters = new ConnectorRequestFilters
        {
            SiteUuid = "11111111-1111-1111-1111-111111111111",
            SegmentUuid = "22222222-2222-2222-2222-222222222222",
            TypeUuid = "33333333-3333-3333-3333-333333333333",
            AssetUuid = "44444444-4444-4444-4444-444444444444",
            MeasurementLocationUuid = "55555555-5555-5555-5555-555555555555",
            SerialNumber = "serial",
            RecordedFrom = "2026-09-01T00:00:00Z",
            RecordedTo = "2026-09-01T01:00:00Z",
            AssessedFrom = "2026-09-01T00:00:00Z",
            AssessedTo = "2026-09-01T01:00:00Z",
            HealthLevelTypeUuid = "66666666-6666-6666-6666-666666666666",
            HealthLevelMin = 1,
            HealthLevelMax = 2,
            EventUuid = "77777777-7777-7777-7777-777777777777",
            InstalledNow = true,
            InstalledFrom = "2026-09-01T00:00:00Z",
            InstalledTo = "2026-09-01T01:00:00Z",
            RemovedFrom = "2026-09-01T00:00:00Z",
            RemovedTo = "2026-09-01T01:00:00Z"
        };

        var options = new ConnectorPostRequestOptions
        {
            RequestType = ConnectorRequestType.GetSites,
            PayloadProfile = PayloadProfileParser.ToCanonicalString(PayloadProfile.Minimal),
            MaxItems = 10,
            LastNData = 2,
            Filters = new ConnectorRequestFilters
            {
                SiteUuid = broadFilters.SiteUuid
            }
        };

        var validation = new ConnectorRequestValidator().Validate(options);
        Require(validation.IsValid, "ConnectorRequestValidator accepts representative options.");
        Require(PayloadProfileParser.TryParse("Minimal", out PayloadProfile parsed), "PayloadProfileParser parses Minimal.");
        Require(parsed == PayloadProfile.Minimal, "PayloadProfileParser returns expected value.");

        var mapper = new ConnectorRequestMapper();
        GetSites getSites = mapper.MapToGetSites(options);
        GetSegments getSegments = mapper.MapToGetSegments(For(ConnectorRequestType.GetSegments));
        GetAssets getAssets = mapper.MapToGetAssets(For(ConnectorRequestType.GetAssets));
        GetMeasurementLocations getMeasurementLocations =
            mapper.MapToGetMeasurementLocations(For(ConnectorRequestType.GetMeasurementLocations));
        GetMeasurements getMeasurements = mapper.MapToGetMeasurements(For(ConnectorRequestType.GetMeasurements));
        GetAssessments getAssessments = mapper.MapToGetAssessments(For(ConnectorRequestType.GetAssessments));
        GetAssetSegmentEvents getAssetSegmentEvents =
            mapper.MapToGetAssetSegmentEvents(For(ConnectorRequestType.GetAssetSegmentEvents));

        var builder = new ConnectorRequestJsonBuilder();
        string previewJson = builder.BuildPreviewJson(options);
        string sitesJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetSites));
        string segmentsJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetSegments));
        string assetsJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetAssets));
        string locationsJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetMeasurementLocations));
        string measurementsJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetMeasurements));
        string assessmentsJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetAssessments));
        string eventsJson = builder.BuildPreviewJson(For(ConnectorRequestType.GetAssetSegmentEvents));

        Require(getSites.Topic.Length > 0, "GetSites criteria is consumable.");
        Require(getSegments.Topic.Length > 0, "GetSegments criteria is consumable.");
        Require(getAssets.Topic.Length > 0, "GetAssets criteria is consumable.");
        Require(getMeasurementLocations.Topic.Length > 0, "GetMeasurementLocations criteria is consumable.");
        Require(getMeasurements.Topic.Length > 0, "GetMeasurements criteria is consumable.");
        Require(getAssessments.Topic.Length > 0, "GetAssessments criteria is consumable.");
        Require(getAssetSegmentEvents.Topic.Length > 0, "GetAssetSegmentEvents criteria is consumable.");
        Require(previewJson.Contains("getSites", StringComparison.OrdinalIgnoreCase), "BuildPreviewJson works without a server.");
        Require(sitesJson.Length > 0, "BuildGetSites preview path is consumable.");
        Require(segmentsJson.Length > 0, "BuildGetSegments preview path is consumable.");
        Require(assetsJson.Length > 0, "BuildGetAssets preview path is consumable.");
        Require(locationsJson.Length > 0, "BuildGetMeasurementLocations preview path is consumable.");
        Require(measurementsJson.Length > 0, "BuildGetMeasurements preview path is consumable.");
        Require(assessmentsJson.Length > 0, "BuildGetAssessments preview path is consumable.");
        Require(eventsJson.Length > 0, "BuildGetAssetSegmentEvents preview path is consumable.");
    }

    private static async Task ValidateCommandApiAsync()
    {
        var runner = new CommandRunner();
        var options = new CommandOptions
        {
            Command = CommandRunner.PostGetSitesRequest,
            Host = "https://example.invalid",
            AuthenticationScheme = AuthenticationSchemeType.BasicApi,
            ApiKey = "api-key",
            UserName = "user",
            Password = "password",
            SessionId = "session",
            MessageId = "message",
            PayloadProfile = "Minimal",
            ChannelId = "/test/request",
            SubscriptionTopics = new[] { "OIIE:S30:V1.1/CCOM-JSON:SyncMeasurements:V1.0" },
            PostRequestOptions = For(ConnectorRequestType.GetSites),
            IncludeRawResponse = true
        };

        ConnectorCommandResult validationOnlyResult = runner.Run(new CommandOptions());
        ConnectorCommandResult jsonResult = runner.RunJson("{}");
        ConnectorCommandResult asyncJsonResult = await runner.RunJsonAsync("{}");
        ConnectorCommandResult openSubscription =
            await runner.RunOpenSubscriptionSessionAsync(new OpenSubscriptionSessionCommand());
        ConnectorCommandResult closeSubscription =
            await runner.RunCloseSubscriptionSessionAsync(new CloseSubscriptionSessionCommand());
        ConnectorCommandResult readPublication =
            await runner.RunReadPublicationAsync(new ReadPublicationCommand());
        ConnectorCommandResult removePublication =
            await runner.RunRemovePublicationAsync(new RemovePublicationCommand());
        ConnectorCommandResult openRequest =
            await runner.RunOpenRequestSessionAsync(new OpenRequestSessionCommand());
        ConnectorCommandResult postRequest =
            await runner.RunPostRequestAsync(new PostRequestCommand());
        ConnectorCommandResult readResponse =
            await runner.RunReadResponseAsync(new ReadResponseCommand());
        ConnectorCommandResult removeResponse =
            await runner.RunRemoveResponseAsync(new RemoveResponseCommand());
        ConnectorCommandResult closeRequest =
            await runner.RunCloseRequestSessionAsync(new CloseRequestSessionCommand());

        var commands = new object[]
        {
            new OpenSubscriptionSessionCommand
            {
                Command = CommandRunner.OpenSubscription,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                ChannelId = "/test/publication",
                SubscriptionTopics = options.SubscriptionTopics,
                IncludeRawResponse = true
            },
            new CloseSubscriptionSessionCommand
            {
                Command = CommandRunner.CloseSubscription,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                IncludeRawResponse = true
            },
            new ReadPublicationCommand
            {
                Command = CommandRunner.ReadPublication,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                IncludeRawResponse = true
            },
            new RemovePublicationCommand
            {
                Command = CommandRunner.RemovePublication,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                IncludeRawResponse = true
            },
            new OpenRequestSessionCommand
            {
                Command = CommandRunner.OpenRequestSession,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                ChannelId = "/test/request",
                IncludeRawResponse = true
            },
            new PostRequestCommand
            {
                Command = CommandRunner.PostRequest,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                RequestType = ConnectorRequestType.GetSites,
                PayloadProfile = "Minimal",
                MaxItems = 1,
                LastNData = 1,
                Filters = new ConnectorRequestFilters(),
                IncludeRawResponse = true
            },
            new ReadResponseCommand
            {
                Command = CommandRunner.ReadResponse,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                MessageId = options.MessageId,
                IncludeRawResponse = true
            },
            new RemoveResponseCommand
            {
                Command = CommandRunner.RemoveResponse,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                MessageId = options.MessageId,
                IncludeRawResponse = true
            },
            new CloseRequestSessionCommand
            {
                Command = CommandRunner.CloseRequestSession,
                Host = options.Host,
                AuthenticationScheme = options.AuthenticationScheme,
                ApiKey = options.ApiKey,
                UserName = options.UserName,
                Password = options.Password,
                SessionId = options.SessionId,
                IncludeRawResponse = true
            }
        };

        Require(!validationOnlyResult.Success, "ConnectorCommandRunner.Run is consumable without network on invalid input.");
        Require(!jsonResult.Success, "ConnectorCommandRunner.RunJson is consumable without network on invalid input.");
        Require(!asyncJsonResult.Success, "ConnectorCommandRunner.RunJsonAsync is consumable without network on invalid input.");
        Require(!openSubscription.Success, "Typed publication open method is consumable.");
        Require(!closeSubscription.Success, "Typed publication close method is consumable.");
        Require(!readPublication.Success, "Typed publication read method is consumable.");
        Require(!removePublication.Success, "Typed publication remove method is consumable.");
        Require(!openRequest.Success, "Typed request open method is consumable.");
        Require(!postRequest.Success, "Typed request post method is consumable.");
        Require(!readResponse.Success, "Typed response read method is consumable.");
        Require(!removeResponse.Success, "Typed response remove method is consumable.");
        Require(!closeRequest.Success, "Typed request close method is consumable.");
        Require(commands.Length == 9, "Command DTOs are consumable.");
    }

    private static void ReferenceServerBoundServiceCallsForCompileOnly()
    {
        if (DateTime.UtcNow.Year < 0)
        {
            var publication = new ConsumerPublicationService();
            publication.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;
            publication.AttachSubscriptionSession("https://example.invalid", "api-key", "user", "password", "session");
            OpenSubscriptionSessionResponse publicationOpenBasic =
                publication.OpenSubscriptionSession("https://example.invalid", "user", "password");
            OpenSubscriptionSessionResponse publicationOpenBasicApi =
                publication.OpenSubscriptionSession("https://example.invalid", "api-key", "user", "password");
            OpenSubscriptionSessionResponse publicationOpenRouted =
                publication.OpenSubscriptionSession(
                    "https://example.invalid",
                    "user",
                    "password",
                    new OpenSubscriptionSessionOptions
                    {
                        ChannelId = "/test/publication",
                        SubscriptionTopics = new[] { "topic" }
                    });
            ReadPublicationResponse publicationRead = publication.ReadPublication();
            RemovePublicationResponse publicationRemove = publication.RemovePublication();
            CloseSubscriptionSessionResponse publicationClose = publication.CloseSubscriptionSession();

            var request = new ConsumerRequestService();
            request.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;
            request.AttachRequestSession("https://example.invalid", "api-key", "user", "password", "session");
            OpenConsumerRequestSessionResponse requestOpenBasic =
                request.OpenConsumerRequestSession("https://example.invalid", "user", "password");
            OpenConsumerRequestSessionResponse requestOpenBasicApi =
                request.OpenConsumerRequestSession("https://example.invalid", "api-key", "user", "password");
            OpenConsumerRequestSessionResponse requestOpenRouted =
                request.OpenConsumerRequestSession(
                    "https://example.invalid",
                    "user",
                    "password",
                    new OpenConsumerRequestSessionOptions { ChannelId = "/test/request" });
            PostRequestResponse postSites = request.PostRequest(new GetSites());
            PostRequestResponse postSegments = request.PostRequest(new GetSegments());
            PostRequestResponse postAssets = request.PostRequest(new GetAssets());
            PostRequestResponse postLocations = request.PostRequest(new GetMeasurementLocations());
            PostRequestResponse postMeasurements = request.PostRequest(new GetMeasurements());
            PostRequestResponse postAssessments = request.PostRequest(new GetAssessments());
            PostRequestResponse postEvents = request.PostRequest(new GetAssetSegmentEvents());
            ReadResponseResponse responseRead = request.ReadResponse("request-message");
            RemoveResponseResponse responseRemove = request.RemoveResponse("request-message");
            CloseConsumerRequestSessionResponse requestClose = request.CloseConsumerRequestSession();

            Console.WriteLine(
                string.Join(
                    ",",
                    publicationOpenBasic.StatusCode,
                    publicationOpenBasicApi.StatusCode,
                    publicationOpenRouted.StatusCode,
                    publicationRead.StatusCode,
                    publicationRemove.StatusCode,
                    publicationClose.StatusCode,
                    requestOpenBasic.StatusCode,
                    requestOpenBasicApi.StatusCode,
                    requestOpenRouted.StatusCode,
                    postSites.StatusCode,
                    postSegments.StatusCode,
                    postAssets.StatusCode,
                    postLocations.StatusCode,
                    postMeasurements.StatusCode,
                    postAssessments.StatusCode,
                    postEvents.StatusCode,
                    responseRead.StatusCode,
                    responseRemove.StatusCode,
                    requestClose.StatusCode));
        }
    }

    private static void ReferenceDeprecatedApisForCompatibilityCompileOnly()
    {
        if (DateTime.UtcNow.Year < 0)
        {
#pragma warning disable CS0618
            var legacyOptions = new MCEGold.Data.Services.Connector.ConnectorCommandOptions
            {
                Command = "OpenSubscription",
                Host = "https://example.invalid",
                AuthenticationScheme = AuthenticationSchemeType.BasicApi,
                ApiKey = "api-key",
                UserName = "user",
                Password = "password",
                IncludeRawResponse = true
            };
            var legacyRunner = new MCEGold.Data.Services.Connector.ConnectorCommandRunner();
            MCEGold.Data.Services.Connector.ConnectorCommandEnvelope<object> legacyEnvelope =
                legacyRunner.Run(legacyOptions);
            string legacyJson = legacyRunner.RunJson(legacyOptions);
            string obsoleteOpenCommand = CommandRunner.OpenSubscriptionSession;
            string obsoleteCloseCommand = CommandRunner.CloseSubscriptionSession;
            OpenSubscriptionSessionResponse obsoleteOpen =
                new ConsumerPublicationService().InternalOpenSubscriptionSessionResponse(
                    "https://example.invalid",
                    "api-key",
                    "user",
                    "password");
#pragma warning restore CS0618

            Console.WriteLine(
                string.Join(
                    ",",
                    legacyEnvelope.Success,
                    legacyJson,
                    obsoleteOpenCommand,
                    obsoleteCloseCommand,
                    obsoleteOpen.StatusCode));
        }
    }

    private static ConnectorPostRequestOptions For(ConnectorRequestType requestType)
    {
        return new ConnectorPostRequestOptions
        {
            RequestType = requestType,
            PayloadProfile = "Minimal",
            MaxItems = 1,
            LastNData = 1,
            Filters = new ConnectorRequestFilters()
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
