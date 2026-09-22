# MCEGold.Data.Services.Connector.Console

This project is a developer test/demo console sample for the command-style wrapper interface in `MCEGold.Data.Services.Connector`.

It is not the production automation CLI. Do not use this project as the long-term automation bridge for Python, shell scripts, AI agents, or subprocess automation. Future Python, shell, AI-agent, and subprocess automation should use a separate `MCEGold.Data.Services.Connector.Cli` project.

The sample currently provides this menu-based workflow:

```text
1. Consumer Publication Workflow
2. Consumer Request Workflow
3. Exit
```

The Consumer Publication Workflow menu supports:

```text
1. Open Subscription
2. Read Publication
3. Remove Publication
4. Close Subscription
0. Back
```

All publication workflow commands run in the same console process because the connector currently stores subscription session state in process memory. Opening a subscription in one process and then reading, removing, or closing it from another process is not supported by this sample.

`Remove Publication` removes the currently retrieved publication according to the existing connector/ISBM workflow. It does not require a message ID parameter in this command wrapper.

The Consumer Request Workflow menu is session-aware. Before a request session is open, it shows:

```text
1. Open Request Session
0. Back
```

After a request session is open, it shows:

```text
1. Request Type Workflows
2. Close Request Session
0. Back
```

The Request Type Workflows menu supports:

```text
1. GetSites Workflow
2. GetSegments Workflow
3. GetAssets Workflow
4. GetMeasurementLocations Workflow
5. GetMeasurements Workflow
6. GetAssessments Workflow
7. GetAssetSegmentEvents Workflow
0. Back
```

The GetSites Workflow menu supports:

```text
1. Preview Short-Form GetSites Input
2. Edit Short-Form GetSites Input
3. Preview Generated GetSites BOD JSON
4. Post GetSites Request
5. Read GetSites Response
6. Remove GetSites Response
7. Back
```

The GetSegments Workflow menu supports:

```text
1. Preview Short-Form GetSegments Input
2. Edit Short-Form GetSegments Input
3. Preview Generated GetSegments BOD JSON
4. Post GetSegments Request
5. Read GetSegments Response
6. Remove GetSegments Response
7. Back
```

The GetAssets Workflow menu supports:

```text
1. Preview Short-Form GetAssets Input
2. Edit Short-Form GetAssets Input
3. Preview Generated GetAssets BOD JSON
4. Post GetAssets Request
5. Read GetAssets Response
6. Remove GetAssets Response
7. Back
```

The GetMeasurementLocations Workflow menu supports:

```text
1. Preview Short-Form GetMeasurementLocations Input
2. Edit Short-Form GetMeasurementLocations Input
3. Preview Generated GetMeasurementLocations BOD JSON
4. Post GetMeasurementLocations Request
5. Read GetMeasurementLocations Response
6. Remove GetMeasurementLocations Response
0. Back
```

The GetMeasurements Workflow menu supports:

```text
1. Preview Short-Form GetMeasurements Input
2. Edit Short-Form GetMeasurements Input
3. Preview Generated GetMeasurements BOD JSON
4. Post GetMeasurements Request
5. Read GetMeasurements Response
6. Remove GetMeasurements Response
0. Back
```

The GetAssessments Workflow menu supports:

```text
1. Preview Short-Form GetAssessments Input
2. Edit Short-Form GetAssessments Input
3. Preview Generated GetAssessments BOD JSON
4. Post GetAssessments Request
5. Read GetAssessments Response
6. Remove GetAssessments Response
0. Back
```

The GetAssetSegmentEvents Workflow menu supports:

```text
1. Preview Short-Form GetAssetSegmentEvents Input
2. Edit Short-Form GetAssetSegmentEvents Input
3. Preview Generated GetAssetSegmentEvents BOD JSON
4. Post GetAssetSegmentEvents Request
5. Read GetAssetSegmentEvents Response
6. Remove GetAssetSegmentEvents Response
0. Back
```

Request workflow commands must run in the same console process because the connector currently stores request session state in process memory. A future automation surface should use workflow commands or interactive/session mode unless session persistence is explicitly designed.

Request type workflows are shown only after opening a request session. Generated BOD preview and post request operations depend on an active request session because the existing BOD builder uses the request session ID in `applicationArea.sender.logicalID`, and posting requires an open request session.

All post request commands, including `PostGetAssetSegmentEventsRequest`, require an active request session in the same console process. They post mapped request criteria through the existing connector service and return a `messageId`.

Request-type workflows group preview, edit, post, read, and remove actions together for easier manual testing. Each read/remove pair uses the last posted message ID for its matching request type, including GetAssessments. Post the matching request first, then use the read/remove options in the same console process. Later full workflow commands can open, post, read, optionally remove, and close as one higher-level operation.

Closing the request session returns the Consumer Request Workflow to the pre-open menu state and clears the independently tracked message IDs for every request type, including GetAssetSegmentEvents.

The short-form preview actions show the simplified JSON shape loaded from config and any in-memory edits made during the current console session. This is the input shape future CLI users should provide.

All `Edit Short-Form ... Input` actions let you choose a payload profile for that request:

```text
Payload Profile
Current: Default / omitted (Full behavior)

1. Default / omitted
2. Full
3. Minimal
Enter = keep current
```

`Default / omitted` leaves `payloadProfile` blank in `ConnectorPostRequestOptions`, does not serialize `payloadProfile` in the short-form preview, and preserves the effective `Full` behavior. Choosing `Full` explicitly serializes `"payloadProfile": "Full"`. Choosing `Minimal` serializes `"payloadProfile": "Minimal"`.

`Edit Short-Form GetSites Input` also lets you change `maxItems` and `filters.siteUuid` for the current console session. `Edit Short-Form GetSegments Input` also lets you change `maxItems`, `filters.siteUuid`, `filters.segmentUuid`, and `filters.typeUuid`. `Edit Short-Form GetAssets Input` also lets you change `maxItems`, `filters.assetUuid`, `filters.siteUuid`, `filters.typeUuid`, and `filters.serialNumber`. `Edit Short-Form GetMeasurementLocations Input` also lets you change `maxItems`, `filters.measurementLocationUuid`, `filters.siteUuid`, `filters.segmentUuid`, and `filters.typeUuid`. `Edit Short-Form GetMeasurements Input` also lets you change `maxItems`, `lastNData`, `filters.measurementLocationUuid`, `filters.segmentUuid`, `filters.recordedFrom`, and `filters.recordedTo`. `Edit Short-Form GetAssessments Input` also lets you change `maxItems`, `lastNData`, `filters.assetUuid`, `filters.healthLevelTypeUuid`, `filters.assessedFrom`, `filters.assessedTo`, `filters.healthLevelMin`, and `filters.healthLevelMax`. Edits are held in memory only and are not saved back to `appsettings.Development.json`. The console validates the edited short-form input and keeps the previous valid values if validation fails.

The generated BOD preview actions for all supported request types, including GetAssessments, show the full OIIE/CCOM BOD JSON generated internally by the connector. These previews may require an active request session because the existing BOD builders use the active request session ID in `applicationArea.sender.logicalID`.

## Configuration

Copy the placeholder example file:

```powershell
Copy-Item appsettings.example.json appsettings.Development.json
```

Replace the placeholder values in `appsettings.Development.json` with local developer values:

- `host`
- `authenticationScheme`
- `apiKey`
- `userName`
- `password`
- `includeRawResponse`
- `consoleOutput`
- `getSitesRequest`
- `getSegmentsRequest`
- `getAssetsRequest`
- `getMeasurementLocationsRequest`
- `getMeasurementsRequest`
- `getAssessmentsRequest`
- `getAssetSegmentEventsRequest`

Do not commit `appsettings.Development.json`. It is intended for local secrets and environment-specific values only.

The `consoleOutput` section controls display formatting only:

```json
"consoleOutput": {
  "showFullCommandResult": false
}
```

By default, command results are displayed as a concise command summary. If the result payload is a JSON string, the console prints a readable `Pretty Payload` section. If the payload is not valid JSON, the console prints it under `Payload`.

Set `consoleOutput.showFullCommandResult` to `true` to also print the full formatted `ConnectorCommandResult` JSON. This is display-only and does not change the connector result contract.

`includeRawResponse` is separate. It controls whether the raw ISBM response is included in the command result. `consoleOutput.showFullCommandResult` controls how much of the command result the developer console prints.

The `getSitesRequest` section uses short-form request options:

```json
"getSitesRequest": {
  "requestType": "GetSites",
  "maxItems": 10,
  "filters": {
    "siteUuid": ""
  }
}
```

Request sections may optionally include `"payloadProfile": "Full"` or `"payloadProfile": "Minimal"`. The example config intentionally omits `payloadProfile` so the default short-form preview shows the omitted/default shape.

`siteUuid` is optional. Empty filters are allowed and preserve existing connector behavior, including the default TypeUUID filter.

The `getSegmentsRequest` section uses short-form request options:

```json
"getSegmentsRequest": {
  "requestType": "GetSegments",
  "maxItems": 10,
  "filters": {
    "siteUuid": "",
    "segmentUuid": "",
    "typeUuid": ""
  }
}
```

For GetSegments, `siteUuid`, `segmentUuid`, and `typeUuid` are optional. Empty filters are allowed and preserve existing connector behavior.

The `getAssetsRequest` section uses short-form request options:

```json
"getAssetsRequest": {
  "requestType": "GetAssets",
  "maxItems": 10,
  "filters": {
    "assetUuid": "",
    "siteUuid": "",
    "typeUuid": "",
    "serialNumber": ""
  }
}
```

For GetAssets, `assetUuid`, `siteUuid`, `typeUuid`, and `serialNumber` are optional. Empty filters are allowed and preserve existing connector behavior.

The `getMeasurementLocationsRequest` section uses short-form request options:

```json
"getMeasurementLocationsRequest": {
  "requestType": "GetMeasurementLocations",
  "maxItems": 10,
  "filters": {
    "measurementLocationUuid": "",
    "siteUuid": "",
    "segmentUuid": "",
    "typeUuid": ""
  }
}
```

For GetMeasurementLocations, `measurementLocationUuid`, `siteUuid`, `segmentUuid`, and `typeUuid` are optional. Empty filters are allowed and preserve existing connector behavior. `assetUuid` is intentionally not exposed for GetMeasurementLocations because the existing request criteria class does not currently wire it through.

The `getMeasurementsRequest` section uses short-form request options:

```json
"getMeasurementsRequest": {
  "requestType": "GetMeasurements",
  "maxItems": 100,
  "lastNData": 3,
  "filters": {
    "measurementLocationUuid": "",
    "segmentUuid": "",
    "recordedFrom": "",
    "recordedTo": ""
  }
}
```

For GetMeasurements, `measurementLocationUuid`, `segmentUuid`, `recordedFrom`, and `recordedTo` are optional. `maxItems` and `lastNData` are optional but should be used to avoid very large responses. Date filters should be parseable date/time values, preferably ISO-style strings with an offset such as `2026-06-17T00:00:00Z` or `2026-06-17T00:00:00-04:00`. The console intentionally does not expose `assetUuid`, `siteUuid`, `typeUuid`, `measurementUuid`, text filters, numeric filters, or status filters for GetMeasurements.

The `getAssessmentsRequest` section uses short-form request options:

```json
"getAssessmentsRequest": {
  "requestType": "GetAssessments",
  "maxItems": 100,
  "lastNData": 5,
  "filters": {
    "assetUuid": "",
    "healthLevelTypeUuid": "",
    "assessedFrom": "",
    "assessedTo": "",
    "healthLevelMin": null,
    "healthLevelMax": null
  }
}
```

For GetAssessments, all six filters are optional. UUID values must be valid UUIDs. Assessment dates must be parseable date/time values, such as `2026-06-17T00:00:00Z` or `2026-06-17T00:00:00-04:00`, and are normalized to UTC for the generated BOD; `assessedFrom` must not be later than `assessedTo`. Health-level bounds must be finite numbers, and `healthLevelMin` must not exceed `healthLevelMax`. The mapper preserves the existing criteria model by using MinInclusive and MaxInclusive filters for dates and health levels. GetAssessments does not expose `siteUuid`, `segmentUuid`, `measurementLocationUuid`, `assessmentUuid`, `status`, or `severity`.

The `getAssetSegmentEventsRequest` section uses short-form request options:

```json
"getAssetSegmentEventsRequest": {
  "requestType": "GetAssetSegmentEvents",
  "maxItems": 100,
  "lastNData": 5,
  "filters": {
    "eventUuid": "",
    "siteUuid": "",
    "segmentUuid": "",
    "assetUuid": "",
    "serialNumber": "",
    "installedNow": null,
    "installedFrom": "",
    "installedTo": "",
    "removedFrom": "",
    "removedTo": ""
  }
}
```

All GetAssetSegmentEvents filters are optional. UUID fields must contain valid UUIDs when supplied. Date fields accept parseable date/time values such as `2026-06-17T00:00:00Z` and are normalized to UTC; each `From` value must not be later than its matching `To` value. `installedNow` accepts `true`, `false`, or `null`; the mapper emits exactly one Boolean filter when a value is present. This short form supports one value per field in this pass, even though some underlying criteria containers support lists. It intentionally does not expose `eventTypeUuid`, `status`, `severity`, `typeUuid`, `measurementLocationUuid`, or numeric health/measurement filters. The existing request channel `/pdmaeye/request` and topic `OIIE:S1:V2.0/CCOM-JSON: GetAssetSegmentEvents:V1.0` remain unchanged, including the embedded space after `CCOM-JSON:`.

Users edit only the short-form request input in this console. The full generated BOD JSON is produced internally by the connector and is not edited directly.

## Typed JSON Pilot

The developer console also accepts `--input <path>` or `--input -` for short operation-specific JSON. The console reads the JSON text and passes it unchanged to `MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner.RunJsonAsync(string, CancellationToken)`.

Consumer Publication commands:

- `OpenSubscription`
- `ReadPublication`
- `RemovePublication`
- `CloseSubscription`

Consumer Request commands:

- `OpenRequestSession`
- `PostRequest`
- `ReadResponse`
- `RemoveResponse`
- `CloseRequestSession`

The interactive Consumer Publication and Consumer Request menu actions use that same short-JSON runner contract: they build an operation-specific short command object from console settings/session/message state, serialize it with the console JSON options, and pass the JSON to `RunJsonAsync`. `ConnectorCommandOptions` is created only inside the runner for this short-JSON path.

These inputs use the shapes in:

- `samples/json/publication/open-subscription-input.example.json`
- `samples/json/publication/read-publication-input.example.json`
- `samples/json/publication/remove-publication-input.example.json`
- `samples/json/publication/close-subscription-input.example.json`
- `samples/json/request/open-request-session-input.example.json`
- `samples/json/request/post-request-input.example.json`
- `samples/json/request/read-response-input.example.json`
- `samples/json/request/remove-response-input.example.json`
- `samples/json/request/close-request-session-input.example.json`

Schemas are checked in at:

- `docs/json-schema/publication/open-subscription-input.schema.json`
- `docs/json-schema/publication/read-publication-input.schema.json`
- `docs/json-schema/publication/remove-publication-input.schema.json`
- `docs/json-schema/publication/close-subscription-input.schema.json`
- `docs/json-schema/request/open-request-session-input.schema.json`
- `docs/json-schema/request/post-request-input.schema.json`
- `docs/json-schema/request/read-response-input.schema.json`
- `docs/json-schema/request/remove-response-input.schema.json`
- `docs/json-schema/request/close-request-session-input.schema.json`

`ReadPublication` and `RemovePublication` require the same connection and credential fields as `CloseSubscription`, plus `sessionId`; `RemovePublication` does not accept `messageId`. `PostRequest` requires `sessionId` and `requestType`; optional `payloadProfile`, `maxItems`, `lastNData`, and `filters` are mapped to the existing request options inside the runner. `ReadResponse` and `RemoveResponse` require `sessionId` and `messageId`, where `messageId` is the original request message ID returned by `PostRequest`. `CloseRequestSession` requires `sessionId`.

Local input failures return the standard `ConnectorCommandResult` `ParameterFault`; valid remote failures keep the existing remote `CommandFailed` result or other current service mapping. Request preview, edit, BOD generation, payload-profile handling, and filters still use the existing short-form request options model; post actions now serialize the short `PostRequest` command and execute it through `RunJsonAsync`. Do not commit real credentials in JSON samples or config files.

## Run From Source

From the Toolkit repository root:

```powershell
dotnet run --project samples/csharp/MCEGold.Data.Services.Connector.Console/MCEGold.Data.Services.Connector.Console.csproj
```

The console prints a command summary for each result. If a result payload is itself a JSON string, the console also prints a readable `Pretty Payload` section. Enable `consoleOutput.showFullCommandResult` to print the full formatted `ConnectorCommandResult` JSON as additional debug output.
