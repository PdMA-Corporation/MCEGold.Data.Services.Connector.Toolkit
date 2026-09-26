# Advanced Reference: MCEGold Connector CLI JSON Shapes

## 1. Purpose And Scope

This advanced reference documents the detailed JSON inputs consumed and outputs produced by the current MCEGold Data Services Connector CLI. Start with the public [CLI JSON Contract](json-contract.md) when you only need the stable envelope and stream rules.

It covers only the existing Consumer Request Service and Consumer Publication Service command surfaces:

- Consumer Request: `OpenConsumerRequestSession`, `PostRequest`, `ReadResponse`, `RemoveResponse`, `CloseConsumerRequestSession`
- Consumer Publication: `OpenSubscriptionSession`, `ReadPublication`, `RemovePublication`, `CloseSubscriptionSession`

Provider Publication commands are outside the scope of this reference. This document describes current behavior only; proposed provider or publication enhancements are listed only as known limitations or compatibility guidance.

## 2. Shape Layers

Do not treat the CLI JSON contract as a direct serialization of a connector service DTO. The current implementation has several layers:

```text
CLI arguments/input file
    ->
ConnectorCommandOptions
    ->
ConnectorCommandRunner
    ->
Consumer service
    ->
Response DTO
    ->
ConnectorCommandResult
    ->
CLI data projection
    ->
CliEnvelope
```

Layer definitions:

| Layer | Meaning | Source |
|---|---|---|
| CLI arguments | Command-line options such as `--config`, `--session-id`, and `--include-raw`. | `samples/csharp/MCEGold.Data.Services.Connector.Cli/CliApplication.cs` |
| CLI input JSON file | The short-form JSON file consumed by `request preview`, `request run`, and `request post`; the developer console also passes Consumer Publication and Consumer Request short JSON unchanged to `ConnectorCommandRunner.RunJsonAsync`. | `Input/RequestInputLoader.cs`; `samples/csharp/MCEGold.Data.Services.Connector.Console/Program.cs` |
| `ConnectorCommandOptions` | In-memory command options passed to `MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner`. No JSON attributes are defined on this type. | `Commands/ConnectorCommandOptions.cs` |
| Service model | Existing connector request criteria objects and publication session state used by `ConsumerRequestService` and `ConsumerPublicationService`. | `ConsumerRequestService.cs`, `ConsumerPublicationService.cs` |
| Response DTO | Connector response types under `ResponseType/`, using public fields such as `StatusCode`, `ReasonPhrase`, `ISBMHTTPResponse`, `SessionID`, `MessageID`, `MessageContent`, and `Topics`. | `ResponseType/*.cs` |
| `ConnectorCommandResult` | Shared command-runner result with status, identifiers, payload, raw response, and error fields. | `Commands/ConnectorCommandResult.cs` |
| CLI data projection | Anonymous per-command objects created by `StagedSessionCommands`, `RequestWorkflow`, and `PublicationWorkflow`. This is where final data field names such as `requestMessageId`, `removed`, and `closed` are selected. | `Commands/StagedSessionCommands.cs` |
| `CliEnvelope` | Common final CLI JSON wrapper. | `Output/CliEnvelope.cs` |

Evidence: command options and command results are provided by the public `MCEGold.Data.Services.Connector` package; final CLI envelope fields are defined in `samples/csharp/MCEGold.Data.Services.Connector.Cli/Output/CliEnvelope.cs`.

Library/console short-JSON pilot:

- Public library entry points: `ConnectorCommandRunner.RunJson(string)` and `RunJsonAsync(string, CancellationToken)`.
- Supported Consumer Publication command values: `OpenSubscription`, `ReadPublication`, `RemovePublication`, and `CloseSubscription`.
- Supported Consumer Request command values: `OpenRequestSession`, `PostRequest`, `ReadResponse`, `RemoveResponse`, and `CloseRequestSession`.
- Input schemas: `docs/json-schema/publication/open-subscription-input.schema.json`, `docs/json-schema/publication/read-publication-input.schema.json`, `docs/json-schema/publication/remove-publication-input.schema.json`, and `docs/json-schema/publication/close-subscription-input.schema.json`.
- Samples: `samples/json/publication/open-subscription-input.example.json`, `samples/json/publication/read-publication-input.example.json`, `samples/json/publication/remove-publication-input.example.json`, and `samples/json/publication/close-subscription-input.example.json`.
- Request input schemas: `docs/json-schema/request/open-request-session-input.schema.json`, `docs/json-schema/request/post-request-input.schema.json`, `docs/json-schema/request/read-response-input.schema.json`, `docs/json-schema/request/remove-response-input.schema.json`, and `docs/json-schema/request/close-request-session-input.schema.json`.
- Request samples: `samples/json/request/open-request-session-input.example.json`, `samples/json/request/post-request-input.example.json`, `samples/json/request/read-response-input.example.json`, `samples/json/request/remove-response-input.example.json`, and `samples/json/request/close-request-session-input.example.json`.
- `ConnectorCommandOptions` is built only inside the runner for this pilot path; it is an in-memory adapter, not a public JSON document.
- `ReadPublication` and `RemovePublication` require `sessionId`; `RemovePublication` does not accept `messageId`.
- `PostRequest` requires `sessionId` and `requestType`; optional `payloadProfile`, `maxItems`, `lastNData`, and `filters` are mapped to `ConnectorPostRequestOptions` inside the runner. `ReadResponse` and `RemoveResponse` require `sessionId` and `messageId`; `messageId` is the original request message ID. `CloseRequestSession` requires `sessionId`.
- Local input failures return `ConnectorCommandResult` with `statusCode = 400`, `reasonPhrase = "Bad Request"`, `errorCode = "ParameterFault"`, and `errorMessage = "Invalid command input."`.
- Remote service failures remain remote results such as `CommandFailed`; they are not rewritten to `ParameterFault`.

## 3. Common CLI Envelope

Every CLI invocation writes exactly one envelope to stdout by default.

Minimal successful shape:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.open-session",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 201,
    "sessionId": "00000000-0000-0000-0000-000000000000"
  },
  "fault": null,
  "raw": null
}
```

Failure shape:

```json
{
  "schemaVersion": "1.0",
  "success": false,
  "command": "request.open-session",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": null,
  "fault": {
    "category": "configuration",
    "code": "ConfigNotFound",
    "message": "Config file was not found: example.json",
    "statusCode": null,
    "details": [
      {
        "field": "config",
        "code": "ConfigNotFound",
        "message": "Config file was not found: example.json"
      }
    ]
  },
  "raw": null
}
```

Envelope field reference:

| Field | Type | Required | Nullable | Description | Source |
|---|---|---:|---:|---|---|
| `schemaVersion` | string | Yes | No | Current envelope schema version, currently `"1.0"`. | `CliEnvelope.SchemaVersion`, `CliEnvelope.cs` lines 5-13 |
| `success` | boolean | Yes | No | True for successful command execution. | `CliEnvelope.Success` |
| `command` | string | Yes | No | CLI command name, e.g. `request.post`. | Command constants/projections in `CliApplication.cs` and `StagedSessionCommands.cs` |
| `timestampUtc` | string | Yes | No | UTC timestamp from `DateTime.UtcNow.ToString("O")`. | `CliEnvelope.TimestampUtc`, line 10 |
| `data` | object or null | Yes | Yes | Operation-specific data on success; null on failure. | `CliEnvelope.Succeeded`/`Failed`, lines 15-35 |
| `fault` | object or null | Yes | Yes | Structured fault on failure; null on success. | `CliFault`, lines 38-53 |
| `raw` | object, array, string, number, boolean, or null | Yes | Yes | Parsed raw connector response only when raw output is enabled and available. | `StagedSessionCommands.Execute`, lines 200-207 |

Serialization behavior:

- Final CLI JSON uses System.Text.Json.
- Property names are camel-case because `JsonValueConverter.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.
- Nulls are serialized because `DefaultIgnoreCondition = JsonIgnoreCondition.Never`.
- Enums are serialized as camel-case strings by `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`.
- Message payload and raw response strings are parsed through `JsonValueConverter.ParseContent`: valid JSON becomes embedded JSON; non-JSON remains a JSON string.

Evidence: `samples/csharp/MCEGold.Data.Services.Connector.Cli/Output/JsonValueConverter.cs` lines 7-52 and `CliOutputWriter.cs` lines 14-40.

## 4. Common Authentication And Connection Inputs

These inputs are shared by staged Consumer Request and Consumer Publication operations.

| Concept | CLI input | `ConnectorCommandOptions` property | C# type | Required | Consumed by | Validation | Output | Sensitive |
|---|---|---|---|---:|---|---|---|---:|
| Host | config `host`, env `MCEGOLD_HOST` | `Host` | `string` | Network commands | Open commands, explicit-session attach | Config validator requires absolute HTTPS URI. Connector runner requires host for open commands. | No | No |
| Authentication scheme | config `authenticationScheme`, env `MCEGOLD_AUTH_SCHEME` | `AuthenticationScheme` | `AuthenticationSchemeType` | Defaults to BasicApi | Open commands, session attach | Must be `Basic` or `BasicApi` in CLI config. | No | No |
| API key | config/env/`--api-key-file` | `ApiKey` | `string` | Required for BasicApi network commands | Open commands, session attach | Required for BasicApi when secrets are required. | No | Yes |
| User name | config/env | `UserName` | `string` | Network commands | Open commands, session attach | Required when secrets are required. | No | Yes |
| Password | config/env/`--password-file` | `Password` | `string` | Network commands | Open commands, session attach | Required when secrets are required. | No | Yes |
| Session ID | `--session-id` | `SessionId` | `string` | Staged post/read/remove/close commands | `AttachRequestSession` or `AttachSubscriptionSession` | Required by CLI for staged session commands; runner also requires it when no active session exists. | Yes, as `sessionId` | No |
| Include raw response | `--include-raw` or config `output.includeRaw` | `IncludeRawResponse` | `bool` | No | All network commands | Boolean flag; no content validation. | Controls top-level `raw` | Raw may contain sensitive diagnostics |

Short-form JSON commands may omit `authenticationScheme`; the command runner treats a missing or null value as `BasicApi`. `BasicApi` requires `apiKey`, `userName`, and `password`; `Basic` requires only `userName` and `password`, though an `apiKey` value may still be accepted and copied into command options.

Evidence: CLI config load and secret-file handling are in `Configuration/ConfigurationLoader.cs` lines 15-60; config validation is in `Configuration/ConfigurationValidator.cs` lines 8-57; command options are populated in `Commands/WorkflowSupport.cs` lines 12-23.

## 5. Consumer Request Service

### 5.1 Open Consumer Request Session

| Item | Current behavior |
|---|---|
| CLI command | `request open-session` |
| CLI arguments | `--config`; optional `--api-key-file`, `--password-file`, `--include-raw`, global `--output` |
| `ConnectorCommandType` | `OpenRequestSession` |
| Consumed options | `Command`, `Host`, `AuthenticationScheme`, `ApiKey`, `UserName`, `Password`, `IncludeRawResponse` |
| Service method | `ConsumerRequestService.OpenConsumerRequestSession(...)` |
| Response DTO | `OpenConsumerRequestSessionResponse` with `StatusCode`, `ReasonPhrase`, `ISBMHTTPResponse`, `SessionID` |
| Result fields | `Success`, `Command`, `StatusCode`, `ReasonPhrase`, `Raw`, `SessionId`, error fields |
| CLI data shape | `statusCode`, `sessionId` |

Success envelope:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.open-session",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 201,
    "sessionId": "00000000-0000-0000-0000-000000000000"
  },
  "fault": null,
  "raw": null
}
```

Raw-response example:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.open-session",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 201,
    "sessionId": "00000000-0000-0000-0000-000000000000"
  },
  "fault": null,
  "raw": {
    "sessionId": "00000000-0000-0000-0000-000000000000"
  }
}
```

Representative fault envelope:

```json
{
  "schemaVersion": "1.0",
  "success": false,
  "command": "request.open-session",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": null,
  "fault": {
    "category": "authentication",
    "code": "CommandFailed",
    "message": "Unauthorized",
    "statusCode": 401,
    "details": []
  },
  "raw": null
}
```

Evidence: command registration in `CliApplication.cs` lines 307-326; runner mapping in `Commands/ConnectorCommandRunner.cs` lines 293-325; request service implementation in `ConsumerRequestService.cs` lines 72-119 and 410-536; CLI projection in `StagedSessionCommands.cs` lines 73-82.

### 5.2 Post Request

CLI command: `request post --config <path> --session-id <id> --input <path-or->`.

The stable CLI request-post JSON input file shape remains the request post short-form input for `request post`. The library/console short-JSON pilot uses command value `PostRequest` with connection, session, request type, optional payload controls, and filters at the top level.

Input JSON field reference:

| JSON field | C# property | Type | Required | Allowed values/default | Notes |
|---|---|---|---:|---|---|
| `requestType` | `RequestType` | enum | Yes by CLI loader | `GetSites`, `GetSegments`, `GetAssets`, `GetMeasurementLocations`, `GetMeasurements`, `GetAssessments`, `GetAssetSegmentEvents`; constructor default is `GetSites` but loader requires field presence. | Case-insensitive property name; enum converter accepts string names. |
| `payloadProfile` | `PayloadProfile` | string or null | No | `Full`, `Minimal`, or omitted/null | CLI `--payload-profile` overrides input. Config default can apply. |
| `maxItems` | `MaxItems` | integer or null | No | >= 0 | `short?` in C#. |
| `lastNData` | `LastNData` | integer or null | No | >= 0 | `short?` in C#. |
| `filters` | `Filters` | object | No | Default empty filter object | Unknown nested fields are rejected by deserialization. |
| `filters.assetUuid` | `Filters.AssetUuid` | string | No | UUID when supplied | Supported for `GetAssets`, `GetAssessments`, `GetAssetSegmentEvents`. |
| `filters.measurementLocationUuid` | `Filters.MeasurementLocationUuid` | string | No | UUID when supplied | Supported for `GetMeasurementLocations`, `GetMeasurements`. |
| `filters.siteUuid` | `Filters.SiteUuid` | string | No | UUID when supplied | Not supported for `GetMeasurements` or `GetAssessments`. |
| `filters.segmentUuid` | `Filters.SegmentUuid` | string | No | UUID when supplied | Supported for `GetSegments`, `GetMeasurementLocations`, `GetMeasurements`, `GetAssetSegmentEvents`. |
| `filters.typeUuid` | `Filters.TypeUuid` | string | No | UUID when supplied | Not supported for `GetSites`, `GetMeasurements`, `GetAssessments`, `GetAssetSegmentEvents`. |
| `filters.serialNumber` | `Filters.SerialNumber` | string | No | string | Supported for `GetAssets`, `GetAssetSegmentEvents`. |
| `filters.recordedFrom` / `filters.recordedTo` | `RecordedFrom` / `RecordedTo` | string | No | parseable date/time | Supported for `GetMeasurements`. |
| `filters.assessedFrom` / `filters.assessedTo` | `AssessedFrom` / `AssessedTo` | string | No | parseable date/time | Supported for `GetAssessments`. |
| `filters.healthLevelTypeUuid` | `HealthLevelTypeUuid` | string | No | UUID when supplied | Supported for `GetAssessments`. |
| `filters.healthLevelMin` / `filters.healthLevelMax` | `HealthLevelMin` / `HealthLevelMax` | number or null | No | double | Supported for `GetAssessments`. |
| `filters.eventUuid` | `EventUuid` | string | No | UUID when supplied | Supported for `GetAssetSegmentEvents`. |
| `filters.installedNow` | `InstalledNow` | boolean or null | No | boolean | Supported for `GetAssetSegmentEvents`. |
| `filters.installedFrom` / `filters.installedTo` | `InstalledFrom` / `InstalledTo` | string | No | parseable date/time | Supported for `GetAssetSegmentEvents`. |
| `filters.removedFrom` / `filters.removedTo` | `RemovedFrom` / `RemovedTo` | string | No | parseable date/time | Supported for `GetAssetSegmentEvents`. |

Unknown properties are rejected: `RequestInputLoader` sets `UnmappedMemberHandling = Disallow`. Property names are case-insensitive. Evidence: `Input/RequestInputLoader.cs` lines 29-60.

Minimal input:

```json
{
  "requestType": "GetSites"
}
```

Full input example:

```json
{
  "requestType": "GetMeasurements",
  "payloadProfile": "Minimal",
  "maxItems": 10,
  "lastNData": 5,
  "filters": {
    "measurementLocationUuid": "00000000-0000-0000-0000-000000000000",
    "segmentUuid": "00000000-0000-0000-0000-000000000001",
    "recordedFrom": "2026-07-28T00:00:00Z",
    "recordedTo": "2026-07-28T12:00:00Z"
  }
}
```

Materially different request-type examples:

```json
{ "requestType": "GetAssets", "filters": { "assetUuid": "00000000-0000-0000-0000-000000000000", "serialNumber": "example-serial" } }
```

```json
{ "requestType": "GetAssessments", "filters": { "assetUuid": "00000000-0000-0000-0000-000000000000", "healthLevelMin": 0.1, "healthLevelMax": 0.9 } }
```

```json
{ "requestType": "GetAssetSegmentEvents", "filters": { "eventUuid": "00000000-0000-0000-0000-000000000000", "installedNow": true } }
```

Successful output:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.post",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 201,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "requestType": "get-sites",
    "requestMessageId": "00000000-0000-0000-0000-000000000001"
  },
  "fault": null,
  "raw": null
}
```

Evidence: request post registration in `CliApplication.cs` lines 328-396; command selection in `StagedSessionCommands.cs` lines 85-105 and 217-230; connector post methods in `Commands/ConnectorCommandRunner.cs` lines 362-631; service post implementation in `ConsumerRequestService.cs` lines 152-329 and 539-584.

### 5.3 Read Response

CLI command: `request read-response --config <path> --session-id <id> --request-id <request-message-id>`.

| Layer | Shape |
|---|---|
| CLI argument | `--request-id` |
| `ConnectorCommandOptions` | `MessageId` receives the request ID |
| Service method | `ConsumerRequestService.ReadResponse(string requestMessageId)` |
| Response DTO | `ReadResponseResponse.StatusCode`, `ReasonPhrase`, `ISBMHTTPResponse`, `MessageID`, `MessageContent` |
| `ConnectorCommandResult` | `MessageId`, `RequestMessageId`, `ResponseMessageId`, `Payload` |
| Final data | `statusCode`, `sessionId`, `requestMessageId`, `responseMessageId`, `payload` |

Success example:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.read-response",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 200,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "requestMessageId": "00000000-0000-0000-0000-000000000001",
    "responseMessageId": "00000000-0000-0000-0000-000000000002",
    "payload": {
      "example": true
    }
  },
  "fault": null,
  "raw": null
}
```

Evidence: CLI argument name in `CliApplication.cs` lines 555-588; runner read mapping in `Commands/ConnectorCommandRunner.cs` lines 648-670; service read parsing in `ConsumerRequestService.cs` lines 338-372; CLI projection in `StagedSessionCommands.cs` lines 107-128.

### 5.4 Remove Response

CLI command: `request remove-response --config <path> --session-id <id> --request-id <request-message-id>`.

Output:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.remove-response",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 204,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "requestMessageId": "00000000-0000-0000-0000-000000000001",
    "removed": true
  },
  "fault": null,
  "raw": null
}
```

Evidence: runner remove mapping in `Commands/ConnectorCommandRunner.cs` lines 687-707; service method in `ConsumerRequestService.cs` lines 382-406; CLI projection in `StagedSessionCommands.cs` lines 130-145.

### 5.5 Close Consumer Request Session

CLI command: `request close-session --config <path> --session-id <id>`.

Output:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.close-session",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 204,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "closed": true
  },
  "fault": null,
  "raw": null
}
```

Evidence: runner close mapping in `Commands/ConnectorCommandRunner.cs` lines 339-350; service method in `ConsumerRequestService.cs` lines 130-150; CLI projection in `StagedSessionCommands.cs` lines 148-158.

## 6. Consumer Publication Service

### 6.1 Open Subscription Session

| Item | Current behavior |
|---|---|
| CLI command | `publication open-subscription` |
| `ConnectorCommandType` | `OpenSubscription` |
| CLI arguments | `--config`; optional `--api-key-file`, `--password-file`, `--include-raw` |
| Consumed options | `Command`, `Host`, `AuthenticationScheme`, `ApiKey`, `UserName`, `Password`, `PayloadProfile`, `IncludeRawResponse` |
| Request body | No CLI JSON input document. The wrapper calls adapter `OpenSubscriptionSession(host, channelId, topics)`. |
| Channel | Hard-coded to `/pdmaeye/publication`; config `publication.channel` is not propagated into `ConnectorCommandOptions`. |
| Topics | Hard-coded default topics in `ConsumerPublicationService`, then sent to the adapter. |
| Payload profile | Present on `ConnectorCommandOptions` and validated as `Full` or `Minimal`; not transported in the open-subscription adapter call. |
| Response DTO | `OpenSubscriptionSessionResponse` with `StatusCode`, `ReasonPhrase`, `ISBMHTTPResponse`, `SessionID` |
| CLI data | `statusCode`, `sessionId` |

Success output:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "publication.open-subscription",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 201,
    "sessionId": "00000000-0000-0000-0000-000000000000"
  },
  "fault": null,
  "raw": null
}
```

Evidence: command registration in `CliApplication.cs` lines 471-490; runner mapping in `Commands/ConnectorCommandRunner.cs` lines 172-204; channel/topics in `ConsumerPublicationService.cs` lines 23-28 and 272-284; adapter call in `ConsumerPublicationService.cs` line 456; payload-profile validation in `Commands/ConnectorCommandRunner.cs` lines 790-794.

### 6.2 Read Publication

CLI command: `publication read --config <path> --session-id <id>`.

Propagation table:

| Field | Service response | Response DTO | `ConnectorCommandResult` | CLI data | Status |
|---|---|---|---|---|---|
| `sessionId` | Session state, not read response body | Not present | From CLI/session attach only | `sessionId` | Preserved from CLI argument |
| `messageId` | `messageId` | `MessageID` | `MessageId` | `messageId` | Preserved |
| `topics` | `topics` array | `Topics` | Not present | Not present | Dropped |
| `messageContent` | `messageContent.content` | `MessageContent` | `Payload` | `payload` | Renamed and parsed |
| `mediaType` | Not mapped | Not present | Not present | Not present | Not available |
| `expiry` | Not mapped | Not present | Not present | Not present | Not available |
| `statusCode` | Adapter status | `StatusCode` | `StatusCode` | `statusCode` | Preserved |
| `raw` | Raw HTTP body | `ISBMHTTPResponse` | `Raw` when enabled | top-level `raw` | Optional |

Current service-level shape after parsing:

```json
{
  "statusCode": 200,
  "reasonPhrase": "OK",
  "isbmHttpResponse": "{\"messageId\":\"00000000-0000-0000-0000-000000000001\",\"topics\":[\"example-topic\"],\"messageContent\":{\"content\":{\"example\":true}}}",
  "messageId": "00000000-0000-0000-0000-000000000001",
  "messageContent": "{\"example\":true}",
  "topics": [
    "example-topic"
  ]
}
```

Current connector result shape if serialized by default System.Text.Json naming:

```json
{
  "success": true,
  "command": "ReadPublication",
  "statusCode": 200,
  "reasonPhrase": "OK",
  "sessionId": "",
  "messageId": "00000000-0000-0000-0000-000000000001",
  "requestMessageId": "",
  "responseMessageId": "",
  "payload": "{\"example\":true}",
  "raw": null,
  "errorCode": "",
  "errorMessage": "",
  "validationErrors": []
}
```

Current final CLI envelope:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "publication.read",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 200,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "messageId": "00000000-0000-0000-0000-000000000001",
    "payload": {
      "example": true
    }
  },
  "fault": null,
  "raw": null
}
```

Known gap: publication topics are available in `ConsumerPublicationService.ReadPublication` but are dropped in `ConnectorCommandRunner` and therefore unavailable in current final CLI JSON. Evidence: topics parsed in `ConsumerPublicationService.cs` lines 201-209; runner maps only `MessageId` and `Payload` in `Commands/ConnectorCommandRunner.cs` lines 215-230; CLI projection includes only `statusCode`, `sessionId`, `messageId`, and `payload` in `StagedSessionCommands.cs` lines 32-47.

### 6.3 Remove Publication

CLI command: `publication remove --config <path> --session-id <id>`.

There is no message ID argument. The service removes the current or last-read publication for the active subscription session.

Output:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "publication.remove",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 204,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "removed": true
  },
  "fault": null,
  "raw": null
}
```

Idempotent behavior is not guaranteed by inspected code; the wrapper forwards the adapter response status. Evidence: service method in `ConsumerPublicationService.cs` lines 231-254; runner mapping in `Commands/ConnectorCommandRunner.cs` lines 246-257; CLI projection in `StagedSessionCommands.cs` lines 49-59.

### 6.4 Close Subscription Session

CLI command: `publication close-subscription --config <path> --session-id <id>`.

Output:

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "publication.close-subscription",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": {
    "statusCode": 204,
    "sessionId": "00000000-0000-0000-0000-000000000000",
    "closed": true
  },
  "fault": null,
  "raw": null
}
```

Evidence: service method in `ConsumerPublicationService.cs` lines 137-156; runner mapping in `Commands/ConnectorCommandRunner.cs` lines 268-279; CLI projection in `StagedSessionCommands.cs` lines 61-71.

## 7. Payload Rendering Behavior

Payload rendering happens after the connector returns a `Payload` string. The final CLI attempts to parse that string as JSON:

| Connector `Payload` string | Final CLI `payload` |
|---|---|
| `{"example":true}` | JSON object |
| `[{"example":true}]` | JSON array |
| `plain text` | JSON string |
| `<root />` | JSON string |
| `{"not valid"` | JSON string |

Examples:

```json
{ "payload": { "example": true } }
```

```json
{ "payload": [ { "example": true } ] }
```

```json
{ "payload": "plain text" }
```

```json
{ "payload": "<root />" }
```

The original service message content is first extracted into `MessageContent` by the service wrappers, then copied to `ConnectorCommandResult.Payload`, then parsed by `JsonValueConverter.ParseContent` for final CLI output. Original whitespace and formatting are not guaranteed to be preserved when content is valid JSON, because it is parsed and re-serialized.

Evidence: request response content extraction in `ConsumerRequestService.cs` lines 358-363; publication content extraction in `ConsumerPublicationService.cs` lines 201-209; CLI parsing in `JsonValueConverter.cs` lines 26-40.

## 8. Fault And Validation Shapes

Common fault object:

```json
{
  "category": "validation",
  "code": "InvalidUsage",
  "message": "Command-line validation failed.",
  "statusCode": null,
  "details": [
    {
      "field": "arguments",
      "code": "InvalidUsage",
      "message": "Required option '--config' is missing."
    }
  ]
}
```

Fault conditions:

| Condition | Exit code | Envelope | Raw | Source |
|---|---:|---|---|---|
| CLI syntax failure | 2 | `success=false`, `data=null`, `fault.category=validation`, `fault.code=InvalidUsage` | null | `CliApplication.Run`, lines 40-65 |
| Missing required CLI option | 2 | Same as syntax failure | null | System.CommandLine parse errors handled in `CliApplication.Run` |
| Invalid input JSON | 2 | `fault.category=validation`, `fault.code=InvalidInputJson` | null | `RequestInputLoader.Load`, lines 56-59; `CliApplication.Execute`, lines 624-627 |
| Unknown input property | 2 | `fault.category=validation`, `fault.code=InvalidInputJson` | null | `UnmappedMemberHandling.Disallow`, `RequestInputLoader.cs` lines 40-44 |
| Invalid configuration | 3 | `fault.category=configuration` | null | `ConfigurationValidator.Validate`, `CliApplication.ExecuteNetwork` lines 592-608 |
| Authentication failure | 4 | `fault.category=authentication`, `statusCode=401`; 403 maps to authorization category but same exit code | optional if `--include-raw` | `WorkflowSupport.Failure`, lines 54-63 |
| Network failure | 5 | `fault.category=transport` when no status code and not validation | optional | `WorkflowSupport.Failure` |
| Timeout | 5 inferred | No explicit timeout classifier found; would likely be transport if represented by failed connector result without status | optional | Inferred from `WorkflowSupport.Failure` |
| ISBM parameter fault | 6 | `fault.category=remote`, status code from service | optional | `MapCommonResponse` and `WorkflowSupport.Failure` |
| Invalid session | 6 | Remote fault if service returns non-2xx status | optional | Same as remote |
| No message available | 6 if non-2xx | Remote fault if service returns non-2xx status | optional | Same as remote; no special classifier found |
| Unexpected exception | 10 | `fault.category=internal`, `fault.code=UnexpectedError` or `UnexpectedWorkflowFailure` | null or optional | `CliApplication.Execute`, lines 628-638; workflow catches |

Representative validation failure:

```json
{
  "schemaVersion": "1.0",
  "success": false,
  "command": "request.preview",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": null,
  "fault": {
    "category": "validation",
    "code": "InvalidInputJson",
    "message": "The JSON value could not be converted.",
    "statusCode": null,
    "details": [
      {
        "field": "input",
        "code": "InvalidInputJson",
        "message": "The JSON value could not be converted."
      }
    ]
  },
  "raw": null
}
```

Representative remote failure with raw:

```json
{
  "schemaVersion": "1.0",
  "success": false,
  "command": "publication.read",
  "timestampUtc": "2026-07-28T12:00:00.0000000Z",
  "data": null,
  "fault": {
    "category": "remote",
    "code": "CommandFailed",
    "message": "Not Found",
    "statusCode": 404,
    "details": []
  },
  "raw": {
    "error": "Not Found"
  }
}
```

## 9. Exit Codes

| Exit code | Name | Meaning | Example conditions |
|---:|---|---|---|
| 0 | `Success` | Successful command. | Open, post, read, remove, close success. |
| 2 | `UsageOrValidation` | CLI syntax or input validation failure. | Missing option, invalid request JSON, invalid payload profile. |
| 3 | `Configuration` | Configuration file or settings problem. | Missing config, invalid host, missing required secret. |
| 4 | `Authentication` | Authentication or authorization failure. | HTTP 401 or 403 from service. |
| 5 | `Transport` | Transport failure without remote HTTP status. | Network failure, likely timeout. |
| 6 | `RemoteOperation` | Remote service returned a non-success HTTP status. | ISBM fault, invalid session, no message if represented by remote non-2xx. |
| 10 | `Unexpected` | Internal unexpected error or invalid connector response. | Exception, missing required success identifier. |

Evidence: `samples/csharp/MCEGold.Data.Services.Connector.Cli/Output/ExitCodes.cs` lines 3-11.

## 10. Field Cross-Reference

| Concept | CLI input name | Options property | Service property | Response DTO property | Result property | Final JSON name |
|---|---|---|---|---|---|---|
| Command | command tokens | `Command` | n/a | n/a | `Command` | `command` |
| Host | config `host` | `Host` | `HostName` | n/a | n/a | not emitted |
| Authentication scheme | config `authenticationScheme` | `AuthenticationScheme` | `AuthenticationSchemeType` | n/a | n/a | not emitted |
| Session ID | `--session-id` | `SessionId` | `Request.SessionID` / `Publication.SessionID` | `SessionID` on open | `SessionId` | `sessionId` |
| Request type | input `requestType` | `PostRequestOptions.RequestType` | request criteria type/topic | n/a | n/a | `requestType` on post output |
| Request message ID | `--request-id` for read/remove | `MessageId` | `requestMessageId` parameter | `PostRequestResponse.MessageID` | `MessageId` / `RequestMessageId` | `requestMessageId` |
| Response message ID | none | n/a | parsed read response | `ReadResponseResponse.MessageID` | `ResponseMessageId` | `responseMessageId` |
| Publication message ID | none on read; future remove not supported | `MessageId` result only | parsed read publication | `ReadPublicationResponse.MessageID` | `MessageId` | `messageId` |
| Topics | none | none | hard-coded subscription topics | `ReadPublicationResponse.Topics` | none | not emitted |
| Payload profile | input/config/`--payload-profile` | `PayloadProfile` or `PostRequestOptions.PayloadProfile` | request BOD user area; publication ignored after validation | n/a | n/a | only appears inside generated request BOD preview |
| Payload | input generated BOD or service content | `PostRequestOptions` for request input | BOD/message content | `MessageContent` | `Payload` | `payload` |
| Status code | none | n/a | adapter response | `StatusCode` | `StatusCode` | `statusCode` |
| Raw response | `--include-raw` | `IncludeRawResponse` | `ISBMHTTPResponse` | `ISBMHTTPResponse` | `Raw` | `raw` |
| Removed | none | n/a | status-driven | n/a | n/a | `removed` |
| Closed | none | n/a | status-driven | n/a | n/a | `closed` |
| Fault code | none | n/a | reason/status/error | n/a | `ErrorCode` | `fault.code` |
| Fault message | none | n/a | reason/status/error | `ReasonPhrase` | `ErrorMessage` | `fault.message` |

## 11. Known Implementation Limitations

- Publication topics are parsed into `ReadPublicationResponse.Topics` but dropped before final CLI JSON.
- `publication.payloadProfile` is validated but not transported in the current subscription adapter request.
- Request and publication channels are hard-coded in service wrappers.
- Publication subscription topics are hard-coded in `ConsumerPublicationService`.
- Consumer publication commands do not expose `mediaType` or `expiry`.
- Publication commands do not consume an input JSON document.
- Raw response is emitted only when `IncludeRawResponse` is true.
- A legacy root-level `MCEGold.Data.Services.Connector.ConnectorCommandRunner` uses a different Newtonsoft.Json envelope and only supports older publication commands. The current CLI uses `MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner`.

Evidence for the legacy runner comes from the public `MCEGold.Data.Services.Connector` package behavior exposed through the Toolkit validation and sample projects.

## 12. Compatibility Rules

- Existing JSON property names must not be renamed without a versioned breaking change.
- New optional output fields should be treated as nonbreaking additions.
- Existing Consumer Request Service shapes must remain stable.
- New Publication fields should reuse established names where semantics match, such as `sessionId`, `messageId`, `statusCode`, `removed`, and `closed`.
- `payload` should remain the current compatibility name unless a versioned migration is approved.
- The common envelope should remain stable.
- Secrets must never be emitted.

## 13. Machine-Readable Schema Index

All schemas use JSON Schema draft 2020-12 and live under `docs/json-schema/`.

| Schema | Operation | Shape | Required fields | References |
|---|---|---|---|---|
| `cli-envelope.schema.json` | common | envelope | `schemaVersion`, `success`, `command`, `timestampUtc`, `data`, `fault`, `raw` | `fault.schema.json` |
| `fault.schema.json` | common | fault | `category`, `code`, `message`, `statusCode`, `details` | none |
| `request-open-session-output.schema.json` | request open session | output envelope | common envelope + data `statusCode`, `sessionId` | common schemas |
| `request-post-input.schema.json` | request post | input file | `requestType` | none |
| `request-post-output.schema.json` | request post | output envelope | common envelope + data `statusCode`, `sessionId`, `requestType`, `requestMessageId` | common schemas |
| `request-read-response-output.schema.json` | request read response | output envelope | common envelope + data `statusCode`, `sessionId`, `requestMessageId`, `responseMessageId`, `payload` | common schemas |
| `request-remove-response-output.schema.json` | request remove response | output envelope | common envelope + data `statusCode`, `sessionId`, `requestMessageId`, `removed` | common schemas |
| `request-close-session-output.schema.json` | request close session | output envelope | common envelope + data `statusCode`, `sessionId`, `closed` | common schemas |
| `publication-open-subscription-output.schema.json` | publication open subscription | output envelope | common envelope + data `statusCode`, `sessionId` | common schemas |
| `publication-read-output.schema.json` | publication read | output envelope | common envelope + data `statusCode`, `sessionId`, `messageId`, `payload` | common schemas |
| `publication-remove-output.schema.json` | publication remove | output envelope | common envelope + data `statusCode`, `sessionId`, `removed` | common schemas |
| `publication-close-subscription-output.schema.json` | publication close subscription | output envelope | common envelope + data `statusCode`, `sessionId`, `closed` | common schemas |

