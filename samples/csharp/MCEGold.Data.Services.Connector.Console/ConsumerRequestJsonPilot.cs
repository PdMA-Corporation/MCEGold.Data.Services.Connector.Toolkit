using System.Text.Json;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using MCEGold.Data.Services.Connector.Enums;

public static class ConsumerRequestJsonPilot
{
    public static OpenRequestSessionCommand CreateOpenRequestSessionCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string? channelId,
        bool includeRawResponse)
    {
        return new OpenRequestSessionCommand
        {
            Command = ConnectorCommandRunner.OpenRequestSession,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            ChannelId = channelId,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static ReadResponseCommand CreateReadResponseCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        string messageId,
        bool includeRawResponse)
    {
        return new ReadResponseCommand
        {
            Command = ConnectorCommandRunner.ReadResponse,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            SessionId = sessionId,
            MessageId = messageId,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static PostRequestCommand CreatePostRequestCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        ConnectorPostRequestOptions requestOptions,
        bool includeRawResponse)
    {
        return new PostRequestCommand
        {
            Command = ConnectorCommandRunner.PostRequest,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            SessionId = sessionId,
            RequestType = requestOptions.RequestType,
            PayloadProfile = requestOptions.PayloadProfile,
            MaxItems = requestOptions.MaxItems,
            LastNData = requestOptions.LastNData,
            Filters = requestOptions.Filters,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static RemoveResponseCommand CreateRemoveResponseCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        string messageId,
        bool includeRawResponse)
    {
        return new RemoveResponseCommand
        {
            Command = ConnectorCommandRunner.RemoveResponse,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            SessionId = sessionId,
            MessageId = messageId,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static CloseRequestSessionCommand CreateCloseRequestSessionCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        bool includeRawResponse)
    {
        return new CloseRequestSessionCommand
        {
            Command = ConnectorCommandRunner.CloseRequestSession,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            SessionId = sessionId,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static Task<ConnectorCommandResult> RunJsonCommandAsync(
        object command,
        Func<string, Task<ConnectorCommandResult>> runJsonAsync,
        JsonSerializerOptions jsonOptions)
    {
        string json = JsonSerializer.Serialize(command, command.GetType(), jsonOptions);

        return runJsonAsync(json);
    }
}
