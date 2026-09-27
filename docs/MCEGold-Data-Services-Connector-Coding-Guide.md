# MCEGold Data Services Connector Coding Guide

`MCEGold.Data.Services.Connector` is the proprietary .NET library for applications that integrate directly with MCEGold Data Services. It exposes two supported programming surfaces:

- The **Direct API** provides typed .NET services, typed requests, criteria, filters, and operation-specific responses. This is the primary API for application development.
- The **Command API** provides command objects, normalized results, asynchronous command operations, and serialized-command support. It is useful for automation, stateless operations, and tooling.

The [MCEGold Data Services Connector Toolkit](../README.md) is a separate public project for the CLI executable, short-form JSON, Python samples, the C# Console, and WinForms samples. This guide focuses on using the NuGet library and links to the Toolkit where its existing documentation is authoritative.

> **Verified against:** `MCEGold.Data.Services.Connector` 1.2.0

## Contents

1. [Installation](#installation)
2. [Choose an API](#choose-an-api)
3. [First Request in Five Minutes](#first-request-in-five-minutes)
4. [Core Concepts](#core-concepts)
5. [Authentication](#authentication)
6. [Direct Request and Response API](#direct-request-and-response-api)
7. [Direct Publication and Subscription API](#direct-publication-and-subscription-api)
8. [Notification Events](#notification-events)
9. [Typed Request Objects](#typed-request-objects)
10. [Criteria and Filters](#criteria-and-filters)
11. [Confirmed Defaults](#confirmed-defaults)
12. [Response Types](#response-types)
13. [Error Handling](#error-handling)
14. [Synchronous and Asynchronous APIs](#synchronous-and-asynchronous-apis)
15. [Complete Request Workflow](#complete-request-workflow)
16. [Filtered GetAssets](#filtered-getassets)
17. [GetMeasurements with a Date Range](#getmeasurements-with-a-date-range)
18. [Complete Publication Workflow](#complete-publication-workflow)
19. [Compatibility: Restoring Existing Sessions](#compatibility-restoring-existing-sessions)
20. [Command API](#command-api)
21. [Toolkit Relationship](#toolkit-relationship)
22. [API Quick Reference](#api-quick-reference)
23. [Troubleshooting](#troubleshooting)
24. [Known Limitations](#known-limitations)

## Installation

Add the package to an SDK-style project:

```xml
<PackageReference Include="MCEGold.Data.Services.Connector" Version="1.2.0" />
```

With the .NET CLI:

```console
dotnet add package MCEGold.Data.Services.Connector --version 1.2.0
```

With the Visual Studio Package Manager Console:

```powershell
Install-Package MCEGold.Data.Services.Connector -Version 1.2.0
```

Version 1.2.0 ships assets for:

- `netstandard2.0`
- `net8.0`

.NET 10 applications can consume a compatible asset; the package does not ship a native `net10.0` asset.

The package dependencies are:

- `Newtonsoft.Json` 13.0.1
- `RapidRedPanda.ISBM.ClientAdapter` 2.0.2.4

These dependencies are transitive when the Connector is installed with `PackageReference`. An application does not need its own explicit Newtonsoft.Json reference unless its code uses Newtonsoft.Json directly, for example to parse returned `MessageContent`.

Common namespaces for the Direct API are:

```csharp
using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.EndpointOptions;
using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.Filters;
using MCEGold.Data.Services.Connector.RequestCriteria;
using MCEGold.Data.Services.Connector.ResponseType;
```

The Command API additionally uses:

```csharp
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
```

## Choose an API

| Use case | Recommended API |
|---|---|
| Direct typed application integration | `ConsumerRequestService` and `ConsumerPublicationService` |
| Automation, stateless execution, or tooling | `MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner` |
| CLI execution, short-form JSON, Python, or packaged workflows | [MCEGold.Data.Services.Connector.Toolkit](../README.md) |

The rest of the guide teaches the Direct API first. See [Command API](#command-api) when an integration benefits from command objects, serialized commands, normalized result objects, or asynchronous command operations.

## First Request in Five Minutes

This example opens a request session with BasicApi authentication, posts a typed `GetSites` request, reads and removes its response, and closes the session.

```csharp
using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.RequestCriteria;

string host = "https://example.mcegold.invalid/connector/1.0";
string apiKey = Environment.GetEnvironmentVariable("MCEGOLD_API_KEY")
    ?? throw new InvalidOperationException("MCEGOLD_API_KEY is not set.");
string userName = Environment.GetEnvironmentVariable("MCEGOLD_USERNAME")
    ?? throw new InvalidOperationException("MCEGOLD_USERNAME is not set.");
string password = Environment.GetEnvironmentVariable("MCEGOLD_PASSWORD")
    ?? throw new InvalidOperationException("MCEGOLD_PASSWORD is not set.");

var service = new ConsumerRequestService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;

bool sessionOpened = false;

try
{
    var open = service.OpenConsumerRequestSession(host, apiKey, userName, password);
    if (open.StatusCode != 201)
    {
        throw new InvalidOperationException($"Open failed: {open.StatusCode} {open.ReasonPhrase}");
    }

    sessionOpened = true;

    var request = new GetSites();

    var post = service.PostRequest(request);
    if (post.StatusCode != 201)
    {
        throw new InvalidOperationException($"Post failed: {post.StatusCode} {post.ReasonPhrase}");
    }

    string requestMessageId = post.MessageID;
    var response = service.ReadResponse(requestMessageId);

    if (response.StatusCode == 200)
    {
        Console.WriteLine(response.MessageContent);
        service.RemoveResponse(requestMessageId);
    }
}
finally
{
    if (sessionOpened)
    {
        service.CloseConsumerRequestSession();
    }
}
```

Responses may not be ready immediately. A production application should own its retry interval, backoff, timeout, and cancellation policy. See [Complete Request Workflow](#complete-request-workflow) for a fuller lifecycle example.

## Core Concepts

### Services are stateful

The Direct API stores active session state in the service instance. Open, post/read, remove, and close operations should normally use the same instance.

### Session and message identifiers

| Identifier | Returned by | Used by |
|---|---|---|
| Request `SessionID` | `OpenConsumerRequestSession` | Retain only when a compatibility or restoration workflow requires it |
| Request `MessageID` | `PostRequest` | `ReadResponse` and `RemoveResponse` |
| Response `MessageID` | `ReadResponse` | Application correlation and diagnostics |
| Subscription `SessionID` | `OpenSubscriptionSession` | Retain only when a compatibility or restoration workflow requires it |
| Publication `MessageID` | `ReadPublication` | Application correlation and diagnostics |

`ReadResponse` and `RemoveResponse` both take the original request message ID returned by `PostRequest`, not the response message ID returned by `ReadResponse`.

### Message content

`ReadResponseResponse.MessageContent` and `ReadPublicationResponse.MessageContent` intentionally return message content as a string. The Connector does not automatically deserialize it into strongly typed MCEGold domain objects. After confirming a successful response, the application is responsible for parsing the string according to the expected payload format.

### Cleanup

Always attempt to close an opened session in `finally`. A failed post, read, or remove operation does not remove the need to close the remote session.

## Authentication

MCEGold Data Services normally uses `BasicApi`. This is the primary authentication mode for applications following this guide. It uses an API key, username, and password:

```csharp
var service = new ConsumerRequestService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;

var open = service.OpenConsumerRequestSession(host, apiKey, userName, password);
```

`AuthenticationSchemeType.BasicApi` is also the configuration default in version 1.2.0. Set it explicitly so the application's intent remains clear.

`Basic` authentication is supported for other compatible ISBM servers where username-and-password authentication is appropriate:

```csharp
var service = new ConsumerRequestService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.Basic;

var open = service.OpenConsumerRequestSession(host, userName, password);
```

The public enum also contains `Custom`, but version 1.2.0 does not document a normal public custom-header configuration workflow. Do not use `Custom` without environment-specific guidance from the service provider.

Security requirements:

- Use HTTPS, especially whenever credentials or an API key are transmitted.
- Load credentials from environment variables, an approved secret store, or protected application configuration.
- Do not hard-code credentials in source or checked-in configuration.
- Do not log passwords or API keys.
- Treat `ISBMHTTPResponse`, command `Raw`, and returned message content as potentially sensitive.

### MCEGold and other ISBM servers

| Target | Authentication | Routing |
|---|---|---|
| MCEGold Data Services | `BasicApi` | Request channels, publication channels, and subscription topics are predefined; normally use the open-session overloads without routing options |
| Other compatible ISBM servers | `Basic` where appropriate | The application may provide `ChannelId` and `SubscriptionTopics` through the open-session option types |

Most readers should follow the MCEGold path. Use caller-supplied routing only when integrating with another compatible ISBM server whose routing is not predefined.

## Direct Request and Response API

Create the service with its parameterless constructor:

```csharp
var service = new ConsumerRequestService();
```

### Open a request session

Public overloads:

```csharp
OpenConsumerRequestSession(string hostname, string userName, string password)
OpenConsumerRequestSession(string hostname, string apiKey, string userName, string password)
OpenConsumerRequestSession(
    string hostname,
    string userName,
    string password,
    OpenConsumerRequestSessionOptions options)
```

Each returns `OpenConsumerRequestSessionResponse`. On success, its `SessionID` identifies the server-side session.

For MCEGold Data Services, request-channel routing is predefined. Use the `BasicApi` overload without `OpenConsumerRequestSessionOptions`, as shown in the beginner workflow.

For another compatible ISBM server where the application must select a request channel, use `OpenConsumerRequestSessionOptions` with Basic authentication:

```csharp
var options = new OpenConsumerRequestSessionOptions
{
    ChannelId = "/example/request-channel"
};

var open = service.OpenConsumerRequestSession(host, userName, password, options);
```

`ChannelId` defaults to an empty string. Confirm the required route with the administrator of the compatible ISBM server.

### Post a typed request

`PostRequest` has one overload for each supported typed request:

```csharp
PostRequestResponse PostRequest(GetSites request)
PostRequestResponse PostRequest(GetSegments request)
PostRequestResponse PostRequest(GetAssets request)
PostRequestResponse PostRequest(GetMeasurementLocations request)
PostRequestResponse PostRequest(GetMeasurements request)
PostRequestResponse PostRequest(GetAssessments request)
PostRequestResponse PostRequest(GetAssetSegmentEvents request)
```

Retain `PostRequestResponse.MessageID`. It is the request message ID needed by both subsequent response operations.

### Read and remove a response

```csharp
ReadResponseResponse ReadResponse(string requestMessageId)
RemoveResponseResponse RemoveResponse(string requestMessageId)
```

`ReadResponse` returns the response message ID and content when a response is available. `RemoveResponse` acknowledges/removes the response associated with the original request ID.

### Close the request session

```csharp
CloseConsumerRequestSessionResponse CloseConsumerRequestSession()
```

The complete lifecycle is:

```text
Open -> Post -> Read -> Remove -> Close
```

## Direct Publication and Subscription API

Create the publication consumer with its parameterless constructor:

```csharp
var service = new ConsumerPublicationService();
```

### Open a subscription

Public overloads:

```csharp
OpenSubscriptionSession(string hostname, string userName, string password)
OpenSubscriptionSession(string hostname, string apiKey, string userName, string password)
OpenSubscriptionSession(
    string hostname,
    string userName,
    string password,
    OpenSubscriptionSessionOptions options)
```

`OpenSubscriptionSessionOptions` contains:

| Property | Type | Confirmed default |
|---|---|---|
| `ChannelId` | `string` | `""` |
| `SubscriptionTopics` | `string[]` | Empty array |

For MCEGold Data Services, publication channels and subscription topics are predefined. Normally use the BasicApi overload without an options object:

```csharp
var open = service.OpenSubscriptionSession(host, apiKey, userName, password);
```

For another compatible ISBM server where routing is caller-supplied, use `OpenSubscriptionSessionOptions` with Basic authentication:

```csharp
var options = new OpenSubscriptionSessionOptions
{
    ChannelId = "/example/publication-channel",
    SubscriptionTopics = new[]
    {
        "OIIE:S30:V1.1/CCOM-JSON:SyncMeasurements:V1.0"
    }
};

var open = service.OpenSubscriptionSession(host, userName, password, options);
```

### Read, remove, and close

```csharp
ReadPublicationResponse ReadPublication()
RemovePublicationResponse RemovePublication()
CloseSubscriptionSessionResponse CloseSubscriptionSession()
```

A successful `ReadPublication` provides `MessageID`, `MessageContent`, and `Topics`. `RemovePublication()` removes or acknowledges the publication associated with the current subscription state and does not require a message ID.

The complete lifecycle is:

```text
Open -> Read -> Process -> Remove -> Close
```

The package does not expose a provider publication API for publishing messages.

## Notification Events

Request and publication consumers expose supported notification callbacks for production applications that prefer event-driven message handling:

```csharp
service.NotificationService.MessageArrivedEvent += OnMessageArrived;
service.NotificationService.AutoRemove = true;
```

`AutoRemove` defaults to `false`. With manual removal, the handler reads and processes the message and application code decides when to remove it.

The request event provides session, response-message, and original request-message identifiers. The publication event provides session, message, and topic values. In version 1.2.0, the events are typed with the compatibility names `RequestNoticificationArgs` and `PublicationNoticificationArgs`.

Always unsubscribe when the handler is no longer needed:

```csharp
service.NotificationService.MessageArrivedEvent -= OnMessageArrived;
```

The public notification objects expose `Dispose()`. Unsubscribe handlers and perform responsible cleanup when notification processing stops. Callback threading, ordering, and shutdown details are not formally documented, so handlers should avoid assumptions about a particular application synchronization context.

## Typed Request Objects

Every typed request exposes:

- `UserArea` for `PayloadProfile` and, where supported, `LastNData`.
- `DataArea.Get.MaxItems` for the overall item limit.
- A request-specific criteria list.
- A read-only `Topic` selected by the request type.

`PayloadProfile` defaults to `PayloadProfile.Full`. Beginners should normally leave this default unchanged. `PayloadProfile.Minimal` is an optional alternate payload shape for applications that explicitly need it:

```csharp
var request = new GetSites();
request.UserArea.MCEGold.PayloadProfile = PayloadProfile.Minimal;
```

### GetSites

Classes: `GetSites`, `SitesCriteria`  
Filters: site `UUID`, `TypeUUID`

```csharp
var request = new GetSites();
request.DataArea.Get.MaxItems = 25;

var criteria = new SitesCriteria();
criteria.UUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "11111111-1111-4111-8111-111111111111"
};
request.DataArea.SitesCriteria.Add(criteria);
```

The Toolkit short-form projection injects an MCEGold site type criterion. The Direct API exposes `TypeUUID` to the caller and should not be assumed to perform the same projection automatically.

### GetSegments

Classes: `GetSegments`, `SegmentsCriteria`  
Filters: segment `UUID`, `TypeUUID`, repeated `SiteUUID`

```csharp
var request = new GetSegments();
request.DataArea.Get.MaxItems = 25;

var criteria = new SegmentsCriteria();
criteria.SiteUUID.Add(new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "11111111-1111-4111-8111-111111111111"
});
request.DataArea.SegmentsCriteria.Add(criteria);
```

### GetAssets

Classes: `GetAssets`, `AssetsCriteria`  
Filters: asset `UUID`, `TypeUUID`, repeated `SiteUUID`, repeated `SerialNumber`

```csharp
var request = new GetAssets();
request.DataArea.Get.MaxItems = 25;

var criteria = new AssetsCriteria();
criteria.SerialNumber.Add(new TextFilter
{
    FilterType = FilterTypes.TextFilter.Equal,
    Value = "DEMO-ASSET-001"
});
request.DataArea.AssetsCriteria.Add(criteria);
```

### GetMeasurementLocations

Classes: `GetMeasurementLocations`, `MeasurementLocationsCriteria`  
Filters: measurement-location `UUID`, `TypeUUID`, repeated `SiteUUID`, `SegmentUUID`

```csharp
var request = new GetMeasurementLocations();
request.DataArea.Get.MaxItems = 25;

var criteria = new MeasurementLocationsCriteria();
criteria.SegmentUUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "22222222-2222-4222-8222-222222222222"
};
request.DataArea.MeasurementLocationsCriteria.Add(criteria);
```

### GetMeasurements

Classes: `GetMeasurements`, `MeasurementsCriteria`  
Filters: `MeasurementLocationUUID`, `SegmentUUID`, repeated `Recorded`

```csharp
var request = new GetMeasurements();
request.UserArea.LastNData = 5;
request.DataArea.Get.MaxItems = 100;

var criteria = new MeasurementsCriteria();
criteria.MeasurementLocationUUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "55555555-5555-4555-8555-555555555555"
};
request.DataArea.MeasurementsCriteria.Add(criteria);
```

### GetAssessments

Classes: `GetAssessments`, `AssessmentsCriteria`  
Filters: `AssetUUID`, `HealthLevelTypeUUID`, repeated `Assessed`, repeated `HealthLevelPrecise`

```csharp
var request = new GetAssessments();
request.UserArea.LastNData = 5;
request.DataArea.Get.MaxItems = 50;

var criteria = new AssessmentsCriteria();
criteria.HealthLevelPrecise.Add(new NumericFilter
{
    FilterType = FilterTypes.NumericFilter.MinInclusive,
    Value = 0.25
});
request.DataArea.AssessmentsCriteria.Add(criteria);
```

### GetAssetSegmentEvents

Classes: `GetAssetSegmentEvents`, `AssetSegmentEventsCriteria`  
Filters: event `UUID`, repeated `SiteUUID`, `SegmentUUID`, `AssetUUID`, repeated `AssetSerialNumber`, `InstalledNow`, `Installed`, and `Removed`

```csharp
var request = new GetAssetSegmentEvents();
request.UserArea.LastNData = 10;
request.DataArea.Get.MaxItems = 100;

var criteria = new AssetSegmentEventsCriteria();
criteria.InstalledNow.Add(new BooleanFilter { Value = true });
request.DataArea.AssetSegmentEventsCriteria.Add(criteria);
```

For the simpler scalar JSON projection used by the Toolkit CLI, see [Connector Request Types](request-types.md).

## Criteria and Filters

### UUIDFilter

```csharp
new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "11111111-1111-4111-8111-111111111111"
}
```

Operators: `Equal`, `NotEqual`.

### TextFilter

```csharp
new TextFilter
{
    FilterType = FilterTypes.TextFilter.SqlLike,
    Value = "DEMO-%"
}
```

Operators: `Equal`, `NotEqual`, `SqlLike`, `SqlNotLike`.

### NumericFilter

```csharp
new NumericFilter
{
    FilterType = FilterTypes.NumericFilter.MaxInclusive,
    Value = 0.75
}
```

Operators: `Equal`, `NotEqual`, `Min`, `MinInclusive`, `Max`, `MaxInclusive`.

### UTCDateTimeFilter

```csharp
new UTCDateTimeFilter
{
    FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
    Value = "2026-01-01T00:00:00Z",
    LocHrDeltaFromUTC = 0,
    LocMinDeltaFromUTC = 0
}
```

Operators: `Equal`, `NotEqual`, `Min`, `MinInclusive`, `Max`, `MaxInclusive`. `LocHrDeltaFromUTC` and `LocMinDeltaFromUTC` are public 16-bit integer fields and default to `0`. Prefer ISO 8601 UTC values ending in `Z` unless the target service requires an explicit local offset.

### BooleanFilter

```csharp
new BooleanFilter { Value = true }
```

`BooleanFilter` contains only `Value`. Its constructor default is `true`, so set it explicitly for readable code.

Some criteria accept one filter object, while others expose `Add(...)` and allow repeated filters. Request data areas also allow multiple criteria objects. The exact server-side combination semantics for repeated values are not formally documented by the package; confirm complex AND/OR behavior with the target service.

The Direct API is richer than the Toolkit JSON projection. In particular, Toolkit JSON intentionally uses scalar equality filters and inclusive range bounds rather than exposing every typed operator and repeated collection.

## Confirmed Defaults

Defaults below are established by constructing the public version 1.2.0 request types:

| Typed request | `MaxItems` | `LastNData` |
|---|---:|---:|
| `GetSites` | `0` | Not present |
| `GetSegments` | `0` | Not present |
| `GetAssets` | `0` | Not present |
| `GetMeasurementLocations` | `0` | Not present |
| `GetMeasurements` | `0` | `3` |
| `GetAssessments` | `0` | `0` |
| `GetAssetSegmentEvents` | `100` | `0` |

`PayloadProfile` defaults to `PayloadProfile.Full` for every typed request. Leave it at `Full` for the normal beginner path. Select `Minimal` only when the application deliberately needs the alternate, reduced payload shape.

These values describe the generated request object. Their service-side limit and paging meaning is controlled by the target MCEGold Data Services endpoint. The Direct API exposes no page number, offset, or continuation token.

## Response Types

Every operation response inherits these public fields from `ISBMResponse`:

| Field | Type | Purpose |
|---|---|---|
| `StatusCode` | `int` | HTTP-style operation status |
| `ReasonPhrase` | `string` | Human-readable status text where available |
| `ISBMHTTPResponse` | `string` | Raw ISBM response data for diagnostics |

Operation-specific fields:

| Return type | Additional fields |
|---|---|
| `OpenConsumerRequestSessionResponse` | `SessionID` |
| `PostRequestResponse` | `MessageID` |
| `ReadResponseResponse` | `MessageID`, `MessageContent` |
| `RemoveResponseResponse` | Common fields only |
| `CloseConsumerRequestSessionResponse` | Common fields only |
| `OpenSubscriptionSessionResponse` | `SessionID` |
| `ReadPublicationResponse` | `MessageID`, `MessageContent`, `Topics` |
| `RemovePublicationResponse` | Common fields only |
| `CloseSubscriptionSessionResponse` | Common fields only |

Observed successful sample outcomes are `201` for opening and posting, `200` for reading, and `204` for removing and closing. Applications should inspect the actual returned status rather than consume operation-specific fields unconditionally.

## Error Handling

### Direct API

Use `StatusCode`, `ReasonPhrase`, and `ISBMHTTPResponse` for normal operation decisions and diagnostics:

```csharp
var response = service.ReadResponse(requestMessageId);

if (response.StatusCode == 200)
{
    Console.WriteLine(response.MessageContent);
}
else
{
    Console.Error.WriteLine($"Read failed: {response.StatusCode} {response.ReasonPhrase}");
}
```

A caller should distinguish:

- Successful HTTP-style operation status
- Authentication or authorization failure
- Remote HTTP/service fault
- No response or publication currently available
- Transport or runtime exception

The package does not expose Connector-specific exception classes. Version 1.2.0 does not formally document which ordinary .NET exceptions may escape direct service calls, so catch only exceptions the application can handle meaningfully and retain cleanup in `finally`.

### Command API

`ConnectorCommandResult` adds a normalized result surface:

- `Success`
- `StatusCode` and `ReasonPhrase`
- `ErrorCode` and `ErrorMessage`
- `ValidationErrors`
- `Command`
- `SessionId`, `MessageId`, `RequestMessageId`, and `ResponseMessageId`
- `Payload`
- optional `Raw`

Observed command error codes include `ValidationFailed`, `ParameterFault`, `NoResponseAvailable`, and `NoPublicationAvailable`. They are not an exhaustive error-code contract. A no-message result may be a normal polling outcome rather than a terminal workflow failure.

## Synchronous and Asynchronous APIs

The Direct API is synchronous. `ConsumerRequestService` and `ConsumerPublicationService` do not expose `Async` methods or cancellation-token parameters.

The Command API exposes:

- `Run(ConnectorCommandOptions)`
- `RunJson(string)`
- Typed methods returning `Task<ConnectorCommandResult>`
- `RunJsonAsync(string, CancellationToken)`

Only `RunJsonAsync` has a public cancellation-token parameter. The typed asynchronous command methods do not accept one in version 1.2.0.

## Complete Request Workflow

This example makes ownership of status checks, identifiers, removal, and cleanup explicit.

```csharp
using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.RequestCriteria;

var service = new ConsumerRequestService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;

bool sessionOpened = false;
string? requestMessageId = null;

try
{
    var open = service.OpenConsumerRequestSession(host, apiKey, userName, password);
    if (open.StatusCode != 201)
    {
        throw new InvalidOperationException(
            $"Could not open request session: {open.StatusCode} {open.ReasonPhrase}");
    }

    sessionOpened = true;

    var request = new GetSites();
    request.DataArea.Get.MaxItems = 25;

    var post = service.PostRequest(request);
    if (post.StatusCode != 201 || string.IsNullOrWhiteSpace(post.MessageID))
    {
        throw new InvalidOperationException(
            $"Could not post request: {post.StatusCode} {post.ReasonPhrase}");
    }

    requestMessageId = post.MessageID;

    // A real application may retry this operation with its own timeout and backoff.
    var read = service.ReadResponse(requestMessageId);
    if (read.StatusCode == 200)
    {
        string responseMessageId = read.MessageID;
        string messageContent = read.MessageContent;

        Console.WriteLine($"Response message: {responseMessageId}");
        Console.WriteLine(messageContent);

        var remove = service.RemoveResponse(requestMessageId);
        if (remove.StatusCode != 204)
        {
            Console.Error.WriteLine(
                $"Response removal failed: {remove.StatusCode} {remove.ReasonPhrase}");
        }
    }
    else
    {
        Console.Error.WriteLine(
            $"Response is not available: {read.StatusCode} {read.ReasonPhrase}");
    }
}
finally
{
    if (sessionOpened)
    {
        var close = service.CloseConsumerRequestSession();
        if (close.StatusCode != 204)
        {
            Console.Error.WriteLine(
                $"Session cleanup failed: {close.StatusCode} {close.ReasonPhrase}");
        }
    }
}
```

## Filtered GetAssets

This request combines an asset UUID, site UUID, type UUID, and exact serial-number filter.

```csharp
var request = new GetAssets();
request.DataArea.Get.MaxItems = 25;

var criteria = new AssetsCriteria();

criteria.UUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "44444444-4444-4444-8444-444444444444"
};

criteria.SiteUUID.Add(new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "11111111-1111-4111-8111-111111111111"
});

criteria.TypeUUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "33333333-3333-4333-8333-333333333333"
};

criteria.SerialNumber.Add(new TextFilter
{
    FilterType = FilterTypes.TextFilter.Equal,
    Value = "DEMO-ASSET-001"
});

request.DataArea.AssetsCriteria.Add(criteria);
PostRequestResponse post = service.PostRequest(request);
```

The direct request types do not expose a public validation result. Treat UUID and serial-number examples as service inputs and validate application data before posting where appropriate.

## GetMeasurements with a Date Range

```csharp
var request = new GetMeasurements();
request.UserArea.LastNData = 10;
request.DataArea.Get.MaxItems = 100;

var criteria = new MeasurementsCriteria();

criteria.MeasurementLocationUUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "55555555-5555-4555-8555-555555555555"
};

criteria.SegmentUUID.UUIDFilter = new UUIDFilter
{
    FilterType = FilterTypes.UUIDFilter.Equal,
    Value = "22222222-2222-4222-8222-222222222222"
};

criteria.Recorded.Add(new UTCDateTimeFilter
{
    FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
    Value = "2026-01-01T00:00:00Z"
});

criteria.Recorded.Add(new UTCDateTimeFilter
{
    FilterType = FilterTypes.UTCDateTimeFilter.MaxInclusive,
    Value = "2026-01-02T00:00:00Z"
});

request.DataArea.MeasurementsCriteria.Add(criteria);
PostRequestResponse post = service.PostRequest(request);
```

Use unambiguous ISO 8601 timestamps. The exact accepted date range and repeated-filter semantics remain server behavior rather than a documented direct-client validation contract.

## Complete Publication Workflow

This example follows the normal MCEGold Data Services path: BasicApi authentication with predefined channel and topic routing.

```csharp
using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.Enums;

var service = new ConsumerPublicationService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;

bool sessionOpened = false;

try
{
    var open = service.OpenSubscriptionSession(host, apiKey, userName, password);
    if (open.StatusCode != 201)
    {
        throw new InvalidOperationException(
            $"Could not open subscription: {open.StatusCode} {open.ReasonPhrase}");
    }

    sessionOpened = true;

    var publication = service.ReadPublication();
    if (publication.StatusCode == 200)
    {
        Console.WriteLine($"Publication: {publication.MessageID}");
        Console.WriteLine(string.Join(", ", publication.Topics));
        Console.WriteLine(publication.MessageContent);

        var remove = service.RemovePublication();
        if (remove.StatusCode != 204)
        {
            Console.Error.WriteLine(
                $"Publication removal failed: {remove.StatusCode} {remove.ReasonPhrase}");
        }
    }
    else
    {
        Console.Error.WriteLine(
            $"Publication is not available: {publication.StatusCode} {publication.ReasonPhrase}");
    }
}
finally
{
    if (sessionOpened)
    {
        var close = service.CloseSubscriptionSession();
        if (close.StatusCode != 204)
        {
            Console.Error.WriteLine(
                $"Subscription cleanup failed: {close.StatusCode} {close.ReasonPhrase}");
        }
    }
}
```

## Compatibility: Restoring Existing Sessions

`AttachRequestSession` and `AttachSubscriptionSession` are compatibility helpers for staged, recovery, or existing-session workflows. They restore local Connector state around a previously known session ID. They do not open a new session and do not validate that the remote session is still active.

These helpers are not part of the normal application lifecycle. New request workflows should use `Open -> Post -> Read -> Remove -> Close`; new publication workflows should use `Open -> Read -> Remove -> Close`.

Request session:

```csharp
var service = new ConsumerRequestService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;
service.AttachRequestSession(host, apiKey, userName, password, sessionId);

var response = service.ReadResponse(requestMessageId);
```

Subscription session:

```csharp
var service = new ConsumerPublicationService();
service.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.BasicApi;
service.AttachSubscriptionSession(host, apiKey, userName, password, sessionId);

var publication = service.ReadPublication();
```

Both methods return `void` and expose the API-key argument shape. `AttachRequestSession` restores the host, credentials, session ID, and local request-session state needed by later operations. `AttachSubscriptionSession` restores the host, credentials, session ID, local active-subscription state, and the API-key header when BasicApi is active. Callers must still handle an expired, invalid, or already-closed remote session.

## Command API

The Command API is a public specialized surface for:

- Automation and tooling
- Stateless command execution
- Serialized-command workflows
- Applications that prefer normalized result objects
- CLI-like integrations without launching a separate executable

It is separate from the Toolkit CLI. The code in this section calls the NuGet library directly.

### Main types

| Type | Purpose |
|---|---|
| `Commands.ConnectorCommandRunner` | Executes command options, typed command DTOs, or serialized commands |
| `Commands.ConnectorCommandOptions` | General synchronous command input |
| `Commands.ConnectorCommandResult` | Normalized success, error, ID, payload, and optional raw data |
| `Commands.Requests.ConnectorPostRequestOptions` | Short command request type, profile, limits, and filters |
| Operation command DTOs | Inputs for typed asynchronous command methods |

Use the `MCEGold.Data.Services.Connector.Commands` namespace. The similarly named root-namespace command APIs are obsolete compatibility types and should not be used in new code.

`ConnectorCommandOptions` carries the complete input for one synchronous command:

| Property | Purpose | Confirmed default |
|---|---|---|
| `Command` | Command name, normally one of the runner constants | `""` |
| `Host` | Connector endpoint | `""` |
| `AuthenticationScheme` | `Basic` or `BasicApi` | `BasicApi` |
| `ApiKey`, `UserName`, `Password` | Credentials for the operation | `""` |
| `ChannelId` | Optional request/publication route | `null` |
| `SubscriptionTopics` | Optional publication topic selection | `null` |
| `PayloadProfile` | Command-level payload profile where used | `"Full"` |
| `SessionId` | Session used by staged operations | `""` |
| `MessageId` | Request message ID used by response operations | `""` |
| `PostRequestOptions` | Typed short command request | `null` |
| `IncludeRawResponse` | Include raw diagnostic content in `Raw` | `false` |

`ConnectorPostRequestOptions` contains `RequestType`, `PayloadProfile`, nullable `MaxItems`, nullable `LastNData`, and `Filters`. Its `Filters` property starts with an empty `ConnectorRequestFilters` instance. See [Connector Request Types](request-types.md) for the Toolkit's authoritative scalar request projection and validation rules.

The operation-specific command DTOs carry the same connection/authentication values plus the channel, session ID, message ID, or request fields required by that operation. This makes typed command calls suitable for staged or stateless execution without relying on one Direct API service instance.

### Synchronous entry points

```csharp
ConnectorCommandResult Run(ConnectorCommandOptions options)
ConnectorCommandResult RunJson(string commandJson)
```

### Asynchronous entry points

```csharp
Task<ConnectorCommandResult> RunOpenRequestSessionAsync(OpenRequestSessionCommand request)
Task<ConnectorCommandResult> RunPostRequestAsync(PostRequestCommand request)
Task<ConnectorCommandResult> RunReadResponseAsync(ReadResponseCommand request)
Task<ConnectorCommandResult> RunRemoveResponseAsync(RemoveResponseCommand request)
Task<ConnectorCommandResult> RunCloseRequestSessionAsync(CloseRequestSessionCommand request)

Task<ConnectorCommandResult> RunOpenSubscriptionSessionAsync(OpenSubscriptionSessionCommand request)
Task<ConnectorCommandResult> RunReadPublicationAsync(ReadPublicationCommand request)
Task<ConnectorCommandResult> RunRemovePublicationAsync(RemovePublicationCommand request)
Task<ConnectorCommandResult> RunCloseSubscriptionSessionAsync(CloseSubscriptionSessionCommand request)

Task<ConnectorCommandResult> RunJsonAsync(
    string commandJson,
    CancellationToken cancellationToken = default)
```

### Command API example

```csharp
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using MCEGold.Data.Services.Connector.Enums;

var runner = new ConnectorCommandRunner();

var open = runner.Run(new ConnectorCommandOptions
{
    Command = ConnectorCommandRunner.OpenRequestSession,
    Host = host,
    AuthenticationScheme = AuthenticationSchemeType.BasicApi,
    ApiKey = apiKey,
    UserName = userName,
    Password = password
});

if (!open.Success)
{
    throw new InvalidOperationException($"{open.ErrorCode}: {open.ErrorMessage}");
}

try
{
    var post = runner.Run(new ConnectorCommandOptions
    {
        Command = ConnectorCommandRunner.PostRequest,
        Host = host,
        AuthenticationScheme = AuthenticationSchemeType.BasicApi,
        ApiKey = apiKey,
        UserName = userName,
        Password = password,
        SessionId = open.SessionId,
        PostRequestOptions = new ConnectorPostRequestOptions
        {
            RequestType = ConnectorRequestType.GetSites,
            MaxItems = 25
        }
    });

    if (!post.Success)
    {
        Console.Error.WriteLine($"{post.ErrorCode}: {post.ErrorMessage}");
        foreach (string validationError in post.ValidationErrors)
        {
            Console.Error.WriteLine(validationError);
        }
    }
    else
    {
        Console.WriteLine($"Request message: {post.MessageId}");
        Console.WriteLine(post.Payload);
    }
}
finally
{
    runner.Run(new ConnectorCommandOptions
    {
        Command = ConnectorCommandRunner.CloseRequestSession,
        Host = host,
        AuthenticationScheme = AuthenticationSchemeType.BasicApi,
        ApiKey = apiKey,
        UserName = userName,
        Password = password,
        SessionId = open.SessionId
    });
}
```

`IncludeRawResponse` defaults to `false`. Enable it only for controlled diagnostics, and protect the resulting `Raw` value as potentially sensitive.

`RunJson` and `RunJsonAsync` execute the library's serialized command contract. For the Toolkit's supported short request documents and stable CLI envelopes, use the [request type reference](request-types.md) and [CLI JSON contract](json-contract.md) rather than duplicating those contracts here.

## Toolkit Relationship

| Connector NuGet library | Connector Toolkit |
|---|---|
| Direct typed .NET services | CLI executable |
| Typed request criteria and filters | Short-form request JSON |
| Direct response DTOs | Stable CLI JSON envelopes |
| Public command runner | Python wrappers and C# Console |
| In-process application integration | Packaged staged and atomic workflows |

Related Toolkit documentation:

- [Connector Request Types](request-types.md)
- [Request Workflow](request-workflow.md)
- [Publication Workflow](publication-workflow.md)
- [CLI Reference](cli-reference.md)
- [CLI JSON Contract](json-contract.md)

## API Quick Reference

### Direct request API

| Method | Input | Return | Purpose |
|---|---|---|---|
| `OpenConsumerRequestSession(...)` | Host, credentials, optional options | `OpenConsumerRequestSessionResponse` | Open and retain request-session state |
| `PostRequest(...)` | One of seven typed requests | `PostRequestResponse` | Post a typed MCEGold request |
| `ReadResponse(requestMessageId)` | Original request ID | `ReadResponseResponse` | Read the corresponding response |
| `RemoveResponse(requestMessageId)` | Original request ID | `RemoveResponseResponse` | Remove the corresponding response |
| `CloseConsumerRequestSession()` | Retained service state | `CloseConsumerRequestSessionResponse` | Close the request session |

### Direct publication API

| Method | Input | Return | Purpose |
|---|---|---|---|
| `OpenSubscriptionSession(...)` | Host, credentials, optional options | `OpenSubscriptionSessionResponse` | Open and retain subscription state |
| `ReadPublication()` | Retained service state | `ReadPublicationResponse` | Read the current publication |
| `RemovePublication()` | Retained service state | `RemovePublicationResponse` | Remove or acknowledge the current publication |
| `CloseSubscriptionSession()` | Retained service state | `CloseSubscriptionSessionResponse` | Close the subscription |

### Compatibility helpers

| Method | Input | Return | Purpose |
|---|---|---|---|
| `AttachRequestSession(...)` | Host, BasicApi credentials, known session ID | `void` | Restore local request-session state for a compatibility workflow |
| `AttachSubscriptionSession(...)` | Host, BasicApi credentials, known session ID | `void` | Restore local subscription state for a compatibility workflow |

### Typed requests

| Request | Criteria | Limit fields |
|---|---|---|
| `GetSites` | `SitesCriteria` | `MaxItems` |
| `GetSegments` | `SegmentsCriteria` | `MaxItems` |
| `GetAssets` | `AssetsCriteria` | `MaxItems` |
| `GetMeasurementLocations` | `MeasurementLocationsCriteria` | `MaxItems` |
| `GetMeasurements` | `MeasurementsCriteria` | `MaxItems`, `LastNData` |
| `GetAssessments` | `AssessmentsCriteria` | `MaxItems`, `LastNData` |
| `GetAssetSegmentEvents` | `AssetSegmentEventsCriteria` | `MaxItems`, `LastNData` |

### Filters

| Class | Value | Operators |
|---|---|---|
| `UUIDFilter` | `string` | Equal, not equal |
| `TextFilter` | `string` | Equal, not equal, SQL-like, SQL-not-like |
| `NumericFilter` | `double` | Equality and minimum/maximum variants |
| `UTCDateTimeFilter` | `string` plus local offset fields | Equality and minimum/maximum variants |
| `BooleanFilter` | `bool` | Exact value |

### Command API

| Method | Input | Return | Purpose |
|---|---|---|---|
| `Run(...)` | `ConnectorCommandOptions` | `ConnectorCommandResult` | Synchronous command execution |
| `RunJson(...)` | Serialized command | `ConnectorCommandResult` | Synchronous serialized execution |
| Typed `Run...Async(...)` methods | Operation command DTO | `Task<ConnectorCommandResult>` | Asynchronous lifecycle operation |
| `RunJsonAsync(...)` | Serialized command and optional token | `Task<ConnectorCommandResult>` | Cancellable serialized execution |

## Troubleshooting

### Authentication fails

- For MCEGold Data Services, confirm `BasicApi` is selected and the API key is present.
- For another compatible ISBM server, confirm whether it expects `Basic` authentication.
- Check secret-store or environment-variable values without printing them.
- Treat `401` and `403` as authentication or authorization failures.

### The host cannot be reached

- Use the complete HTTPS Connector endpoint supplied for the environment.
- Confirm DNS, proxy, firewall, and certificate trust from the calling machine.
- Do not substitute an ISBM adapter URL unless it is the documented Connector endpoint.

### A channel or topic produces no data

- For MCEGold Data Services, do not override the predefined request/publication routing unless specifically instructed.
- For another compatible ISBM server, confirm the caller-supplied `ChannelId` and `SubscriptionTopics` with its administrator.

### A response or publication is not available

- Treat a known no-message outcome as potentially retryable.
- Own retry intervals, backoff, timeout, and cancellation in application code.
- Do not remove a message until processing has succeeded.

### A filter is rejected or returns unexpected data

- Use canonical UUID strings and unambiguous ISO 8601 timestamps.
- Confirm the filter applies to the selected request type.
- Reduce repeated filters or criteria to one value while diagnosing combination behavior.
- Compare the Direct API model with the simpler [Toolkit request reference](request-types.md), but do not assume the contracts are identical.

### Sessions remain open

- Set the opened flag only after a successful open operation.
- Close in `finally`.
- Log cleanup status without logging credentials or sensitive raw response content.

### MessageContent cannot be parsed

- Inspect `StatusCode` before parsing.
- Confirm the expected payload profile and message type.
- Preserve a protected diagnostic copy only when permitted; message content and raw responses may contain sensitive data.

## Known Limitations

The following behavior is not fully established by the public version 1.2.0 metadata:

- Direct-service exception guarantees are not formally documented.
- Service-instance thread safety is not documented.
- Exact repeated-filter and repeated-criteria combination semantics may depend on server behavior.
- `AuthenticationSchemeType.Custom` is exposed but is not documented as a normal supported configuration path.
- Notification callback threading, ordering, and shutdown details are not formally documented.
- Exact Direct API status behavior when no response or publication is ready requires server confirmation.
- The package exposes no direct-service timeout, retry, or cancellation settings.

Do not infer stronger guarantees from the shape of the public API. Confirm deployment-specific channel, topic, authentication, and filtering behavior with the MCEGold Data Services provider.
