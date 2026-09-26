# CLI JSON Contract

JSON modes write exactly one document to stdout. The v1 envelope is stable across successful commands, validation failures, configuration failures, transport failures, authentication failures, remote failures, and unexpected errors.

See the [CLI Reference](cli-reference.md) for commands and options. For implementation-level input and output mappings, use the [Advanced CLI JSON Shapes](connector-cli-json-shapes.md) reference.

## Success

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "request.preview",
  "timestampUtc": "2026-06-18T00:00:00.0000000Z",
  "data": {},
  "fault": null,
  "raw": null
}
```

Valid JSON payloads and generated BODs are nested JSON values, not escaped JSON strings.

Short-form requests may include an optional `payloadProfile` value:

```json
{
  "requestType": "GetSites",
  "payloadProfile": "Minimal"
}
```

Valid values are `Full` and `Minimal`; matching is case-insensitive and generated output is canonicalized. Omitted values behave as `Full` but do not serialize `payloadProfile`. Explicit values are serialized under `applicationArea.userArea.mcegold.payloadProfile`.

Precedence is CLI `--payload-profile`, then short-form JSON `payloadProfile`, then optional config `request.payloadProfile`, then connector default `Full` behavior.

## Staged session data

Open commands return an explicit session ID:

```json
{
  "statusCode": 201,
  "sessionId": "session-id"
}
```

`publication.read` returns `sessionId`, `messageId`, and `payload`. `request.post` returns `sessionId`, `requestType`, and `requestMessageId`. `request.read-response` keeps correlation identifiers distinct:

```json
{
  "statusCode": 200,
  "sessionId": "request-session-id",
  "requestMessageId": "request-message-id",
  "responseMessageId": "response-message-id",
  "payload": {}
}
```

Remove and close commands echo the supplied session ID and return a boolean `removed` or `closed` flag after a successful connector operation.

## Failure

```json
{
  "schemaVersion": "1.0",
  "success": false,
  "command": "request.run",
  "timestampUtc": "2026-06-18T00:00:00.0000000Z",
  "data": null,
  "fault": {
    "category": "validation",
    "code": "InvalidInput",
    "message": "Input validation failed.",
    "statusCode": null,
    "details": [
      {
        "field": "filters.assetUuid",
        "code": "InvalidUuid",
        "message": "filters.assetUuid must be a valid UUID."
      }
    ]
  },
  "raw": null
}
```

Fault categories are `validation`, `configuration`, `authentication`, `authorization`, `transport`, `remote`, and `internal`. Codes are stable identifiers; messages are explanatory text.

Every failure has `data: null` and one normalized `fault` object. Cleanup failures use `fault.details[].field = "cleanup"`; they never replace the original fault. The contract does not emit top-level `error`, `transportFault`, or separate protocol-fault nodes.

The CLI preserves fault and HTTP status information. Retry, backoff, polling, cancellation, and timeout policies are determined by the calling application. For publication and response reads, a caller may wait and issue another read according to its own polling policy.

## Exit codes

| Code | Meaning |
|---:|---|
| 0 | Success |
| 2 | Usage or input validation failure |
| 3 | Configuration or secret-loading failure |
| 4 | Authentication or authorization failure |
| 5 | Network, transport, or timeout failure |
| 6 | Remote connector or ISBM operation failure |
| 10 | Unexpected internal failure |

Automation should check the process exit code first and then parse the envelope for details.

## Stream rules

- stdout contains the selected command result only.
- stderr may contain diagnostics that are not part of the JSON contract.
- Secrets, authorization headers, API keys, passwords, encryption material, and raw local config are excluded.
- `--include-raw` is opt-in and affects only connector response material, not credentials.
- Without `--include-raw`, `raw` remains `null`; enabling full JSON output does not enable raw collection.
- Staged operations expose their parsed `ISBMHTTPResponse` when available. Successful atomic workflows expose per-step response entries because they perform multiple connector operations.
