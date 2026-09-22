using System.Text.Json;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Enums;

public static class ConsumerPublicationJsonPilot
{
    public static OpenSubscriptionSessionCommand CreateOpenSubscriptionCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string? channelId,
        string[]? subscriptionTopics,
        bool includeRawResponse)
    {
        return new OpenSubscriptionSessionCommand
        {
            Command = ConnectorCommandRunner.OpenSubscription,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            ChannelId = channelId,
            SubscriptionTopics = subscriptionTopics,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static CloseSubscriptionSessionCommand CreateCloseSubscriptionCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        bool includeRawResponse)
    {
        return new CloseSubscriptionSessionCommand
        {
            Command = ConnectorCommandRunner.CloseSubscription,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            SessionId = sessionId,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static ReadPublicationCommand CreateReadPublicationCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        bool includeRawResponse)
    {
        return new ReadPublicationCommand
        {
            Command = ConnectorCommandRunner.ReadPublication,
            Host = host,
            AuthenticationScheme = authenticationScheme,
            ApiKey = apiKey,
            UserName = userName,
            Password = password,
            SessionId = sessionId,
            IncludeRawResponse = includeRawResponse
        };
    }

    public static RemovePublicationCommand CreateRemovePublicationCommand(
        string host,
        AuthenticationSchemeType authenticationScheme,
        string apiKey,
        string userName,
        string password,
        string sessionId,
        bool includeRawResponse)
    {
        return new RemovePublicationCommand
        {
            Command = ConnectorCommandRunner.RemovePublication,
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
