# Connector Request Types

This page is the authoritative JSON input reference for the seven request types accepted by Connector CLI `request preview`, `request run`, and staged `request post` commands. It documents the short-form document supplied through `--input <path>` or `--input -`; connection, authentication, and session values remain CLI/configuration inputs.

## Input Contract

Every request uses the same root shape:

```json
{
  "requestType": "GetSites",
  "payloadProfile": "Full",
  "maxItems": 25,
  "lastNData": 5,
  "filters": {}
}
```

Only `requestType` is required. The other properties and all filters are optional. Use only the properties supported by the selected request type.

## Shared Properties

| JSON property | Type | Required | Allowed values and behavior |
|---|---|---:|---|
| `requestType` | string enum | Yes | One of the PascalCase values in [Name Mapping](#name-mapping). The exact root key `requestType` must be present; there is no CLI default. |
| `payloadProfile` | string or null | No | Public values are `Full` and `Minimal`, matched case-insensitively. Null or blank behaves as omitted. |
| `maxItems` | integer or null | No | Runtime type is a nullable 16-bit signed integer. JSON parsing accepts `-32768..32767`; validation permits `0..32767`. Omitted defaults vary by request type. |
| `lastNData` | integer or null | No | Runtime type is a nullable 16-bit signed integer. Validation permits `0..32767`. It affects only Measurements, Assessments, and Asset Segment Events. |
| `filters` | object or null | No | Omitted, null, or `{}` means no caller-supplied filters. Every supported filter value is scalar; filter arrays are not supported. |

`payloadProfile` precedence, highest first, is:

1. CLI `--payload-profile`.
2. Nonblank JSON `payloadProfile`.
3. Configuration `request.payloadProfile`.
4. Connector default `Full`.

When `Full` is selected only by default, it may not be serialized explicitly in the generated payload.

## Name Mapping

| CLI-friendly name | JSON `requestType` |
|---|---|
| `get-sites` | `GetSites` |
| `get-segments` | `GetSegments` |
| `get-assets` | `GetAssets` |
| `get-measurement-locations` | `GetMeasurementLocations` |
| `get-measurements` | `GetMeasurements` |
| `get-assessments` | `GetAssessments` |
| `get-asset-segment-events` | `GetAssetSegmentEvents` |

CLI-facing names are kebab-case, while JSON values are PascalCase. For example, use:

```json
{"requestType":"GetSites"}
```

Do not use the CLI-friendly name inside JSON:

```json
{"requestType":"get-sites"}
```

## Validation and Null Behavior

- The root input must be a JSON object.
- The exact root key `requestType` is required. Other property names are deserialized case-insensitively, but the documented camelCase names should be used.
- Unknown root properties and unknown filter properties are rejected.
- JSON comments and trailing commas are not enabled.
- Arrays are invalid for `filters` and for every scalar property.
- String filters may be null. Null, empty, and whitespace-only strings behave as omitted.
- Nullable numeric and boolean filters may be null, which behaves as omitted.
- A populated filter that is not supported by the selected request type produces `UnsupportedFilter`. An unsupported string filter that is null or blank is effectively absent.
- No filter is required, and no filter combinations are mutually exclusive.
- Public input must use the canonical string `requestType` values. Numeric enum values are not part of the supported public contract.

UUID fields must parse as valid UUIDs when nonblank. Use the canonical hyphenated form:

```text
11111111-1111-4111-8111-111111111111
```

The runtime parser may accept other UUID representations, but braces, compact forms, and all-zero UUIDs are not recommended public examples.

Date/time filters accept parseable date/time strings. Use ISO 8601/RFC 3339 with `Z` or an explicit offset to avoid host-dependent interpretation:

```text
2026-01-01T00:00:00Z
2026-01-01T00:00:00-05:00
```

## Limits and Defaults

| Request type | Omitted `maxItems` | Omitted `lastNData` | `lastNData` behavior |
|---|---:|---:|---|
| `GetSites` | `0` | Not used | Accepted and validated, then ignored by this payload builder. |
| `GetSegments` | `0` | Not used | Accepted and validated, then ignored by this payload builder. |
| `GetAssets` | `0` | Not used | Accepted and validated, then ignored by this payload builder. |
| `GetMeasurementLocations` | `0` | Not used | Accepted and validated, then ignored by this payload builder. |
| `GetMeasurements` | `0` | `3` | Included in the generated request user area. |
| `GetAssessments` | `0` | `0` | Included in the generated request user area. |
| `GetAssetSegmentEvents` | `100` | `0` | Included in the generated request user area. |

Explicit `maxItems` and `lastNData` values must be integers from `0` through `32767`. The CLI maps the value into the generated request; service-side result and paging behavior remains connector/server behavior. There is no page number, offset, or continuation-token input in this contract.

## Filter Semantics

- UUID and text filters use equality semantics.
- `From` fields are inclusive lower bounds.
- `To` fields are inclusive upper bounds.
- `Min` fields are inclusive lower bounds.
- `Max` fields are inclusive upper bounds.
- All populated supported filters are applied together.
- No input filter arrays are supported.
- Either bound of a range may be used alone. When both are supplied, the lower bound must not exceed the upper bound.

## Get Sites

CLI name: `get-sites`

JSON `requestType`: `GetSites`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetSites`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `lastNData` | integer or null | No | Accepted and validated but ignored by this payload builder. |
| `filters.siteUuid` | string or null | No | Valid UUID when nonblank; equality filter. |

The builder also adds the fixed site type UUID criterion `d99b1a17-ed17-4375-9e82-ccb99481dd8a`. Callers cannot configure this value.

Minimal:

```json
{"requestType":"GetSites"}
```

Complete:

```json
{
  "requestType": "GetSites",
  "payloadProfile": "Full",
  "maxItems": 25,
  "filters": {
    "siteUuid": "11111111-1111-4111-8111-111111111111"
  }
}
```

Checked-in example: [get-sites.example.json](../payloads/requests/get-sites.example.json)

## Get Segments

CLI name: `get-segments`

JSON `requestType`: `GetSegments`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetSegments`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `lastNData` | integer or null | No | Accepted and validated but ignored by this payload builder. |
| `filters.siteUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.segmentUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.typeUuid` | string or null | No | Valid UUID when nonblank; equality filter. |

Empty filter strings behave as omitted and do not create meaningful filter criteria.

Minimal:

```json
{"requestType":"GetSegments"}
```

Complete:

```json
{
  "requestType": "GetSegments",
  "payloadProfile": "Minimal",
  "maxItems": 25,
  "filters": {
    "siteUuid": "11111111-1111-4111-8111-111111111111",
    "segmentUuid": "22222222-2222-4222-8222-222222222222",
    "typeUuid": "33333333-3333-4333-8333-333333333333"
  }
}
```

Checked-in example: [get-segments.example.json](../payloads/requests/get-segments.example.json)

## Get Assets

CLI name: `get-assets`

JSON `requestType`: `GetAssets`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetAssets`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `lastNData` | integer or null | No | Accepted and validated but ignored by this payload builder. |
| `filters.assetUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.siteUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.typeUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.serialNumber` | string or null | No | Exact text equality; no additional length or format validation. |

Minimal:

```json
{"requestType":"GetAssets"}
```

Complete:

```json
{
  "requestType": "GetAssets",
  "payloadProfile": "Full",
  "maxItems": 25,
  "filters": {
    "assetUuid": "44444444-4444-4444-8444-444444444444",
    "siteUuid": "11111111-1111-4111-8111-111111111111",
    "typeUuid": "33333333-3333-4333-8333-333333333333",
    "serialNumber": "DEMO-ASSET-001"
  }
}
```

Checked-in example: [get-assets.example.json](../payloads/requests/get-assets.example.json)

## Get Measurement Locations

CLI name: `get-measurement-locations`

JSON `requestType`: `GetMeasurementLocations`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetMeasurementLocations`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `lastNData` | integer or null | No | Accepted and validated but ignored by this payload builder. |
| `filters.measurementLocationUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.siteUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.segmentUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.typeUuid` | string or null | No | Valid UUID when nonblank; equality filter. |

Minimal:

```json
{"requestType":"GetMeasurementLocations"}
```

Complete:

```json
{
  "requestType": "GetMeasurementLocations",
  "payloadProfile": "Minimal",
  "maxItems": 25,
  "filters": {
    "measurementLocationUuid": "55555555-5555-4555-8555-555555555555",
    "siteUuid": "11111111-1111-4111-8111-111111111111",
    "segmentUuid": "22222222-2222-4222-8222-222222222222",
    "typeUuid": "33333333-3333-4333-8333-333333333333"
  }
}
```

Checked-in example: [get-measurement-locations.example.json](../payloads/requests/get-measurement-locations.example.json)

## Get Measurements

CLI name: `get-measurements`

JSON `requestType`: `GetMeasurements`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetMeasurements`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `lastNData` | integer or null | No | `0..32767`; omitted maps to `3`. |
| `filters.measurementLocationUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.segmentUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.recordedFrom` | string or null | No | Parseable date/time; inclusive lower bound. |
| `filters.recordedTo` | string or null | No | Parseable date/time; inclusive upper bound. |

Either date bound may appear alone. When both are present, `recordedFrom` must be earlier than or equal to `recordedTo`.

Minimal:

```json
{"requestType":"GetMeasurements"}
```

Complete:

```json
{
  "requestType": "GetMeasurements",
  "payloadProfile": "Full",
  "maxItems": 100,
  "lastNData": 10,
  "filters": {
    "measurementLocationUuid": "55555555-5555-4555-8555-555555555555",
    "segmentUuid": "22222222-2222-4222-8222-222222222222",
    "recordedFrom": "2026-01-01T00:00:00Z",
    "recordedTo": "2026-01-02T00:00:00Z"
  }
}
```

Checked-in example: [get-measurements.example.json](../payloads/requests/get-measurements.example.json)

## Get Assessments

CLI name: `get-assessments`

JSON `requestType`: `GetAssessments`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetAssessments`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `lastNData` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `filters.assetUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.healthLevelTypeUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.assessedFrom` | string or null | No | Parseable date/time; inclusive lower bound. |
| `filters.assessedTo` | string or null | No | Parseable date/time; inclusive upper bound. |
| `filters.healthLevelMin` | number or null | No | Inclusive lower bound. |
| `filters.healthLevelMax` | number or null | No | Inclusive upper bound. |

Date and health-level bounds are independently optional. When both bounds are present, `assessedFrom <= assessedTo` and `healthLevelMin <= healthLevelMax`. Current validation does not reject negative health-level values and does not impose a fixed `0..100` range.

Minimal:

```json
{"requestType":"GetAssessments"}
```

Complete:

```json
{
  "requestType": "GetAssessments",
  "payloadProfile": "Full",
  "maxItems": 50,
  "lastNData": 5,
  "filters": {
    "assetUuid": "44444444-4444-4444-8444-444444444444",
    "healthLevelTypeUuid": "66666666-6666-4666-8666-666666666666",
    "assessedFrom": "2026-01-01T00:00:00Z",
    "assessedTo": "2026-01-31T23:59:59Z",
    "healthLevelMin": 0.0,
    "healthLevelMax": 100.0
  }
}
```

Checked-in example: [get-assessments.example.json](../payloads/requests/get-assessments.example.json)

## Get Asset Segment Events

CLI name: `get-asset-segment-events`

JSON `requestType`: `GetAssetSegmentEvents`

| JSON property | Type | Required | Rules |
|---|---|---:|---|
| `requestType` | string | Yes | Must be `GetAssetSegmentEvents`. |
| `payloadProfile` | string or null | No | `Full` or `Minimal`. |
| `maxItems` | integer or null | No | `0..32767`; omitted maps to `100`. |
| `lastNData` | integer or null | No | `0..32767`; omitted maps to `0`. |
| `filters.eventUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.siteUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.segmentUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.assetUuid` | string or null | No | Valid UUID when nonblank; equality filter. |
| `filters.serialNumber` | string or null | No | Exact text equality; no additional length or format validation. |
| `filters.installedNow` | boolean or null | No | Exact boolean filter. |
| `filters.installedFrom` | string or null | No | Parseable date/time; inclusive lower bound. |
| `filters.installedTo` | string or null | No | Parseable date/time; inclusive upper bound. |
| `filters.removedFrom` | string or null | No | Parseable date/time; inclusive lower bound. |
| `filters.removedTo` | string or null | No | Parseable date/time; inclusive upper bound. |

Installed and removed ranges are independent, and each bound may appear alone. `installedNow` may coexist with either range. If both bounds of a range are supplied, its `From` value must be earlier than or equal to its `To` value.

Minimal:

```json
{"requestType":"GetAssetSegmentEvents"}
```

Complete:

```json
{
  "requestType": "GetAssetSegmentEvents",
  "payloadProfile": "Full",
  "maxItems": 50,
  "lastNData": 10,
  "filters": {
    "eventUuid": "77777777-7777-4777-8777-777777777777",
    "siteUuid": "11111111-1111-4111-8111-111111111111",
    "segmentUuid": "22222222-2222-4222-8222-222222222222",
    "assetUuid": "44444444-4444-4444-8444-444444444444",
    "serialNumber": "DEMO-ASSET-001",
    "installedNow": true,
    "installedFrom": "2026-01-01T00:00:00Z",
    "installedTo": "2026-01-31T23:59:59Z",
    "removedFrom": "2026-02-01T00:00:00Z",
    "removedTo": "2026-02-28T23:59:59Z"
  }
}
```

Checked-in example: [get-asset-segment-events.example.json](../payloads/requests/get-asset-segment-events.example.json)

## Validation Error Reference

CLI request validation failures use top-level fault code `InvalidInput`. The specific cause appears in `fault.details`:

| Condition | Detail code |
|---|---|
| Missing exact `requestType` key | `MissingRequestType` |
| Malformed JSON, unknown property, or wrong JSON type | `InvalidInputJson` |
| Unknown request type | `UnsupportedRequestType` |
| Invalid `payloadProfile` | `InvalidPayloadProfile` |
| Negative `maxItems` | `InvalidMaxItems` |
| Negative `lastNData` | `InvalidLastNData` |
| Malformed UUID | `InvalidUuid` |
| Unparseable date/time | `InvalidDateTime` |
| Reversed date range | `InvalidDateRange` |
| Reversed health-level range | `InvalidNumericRange` |
| Populated unsupported filter | `UnsupportedFilter` |

## Example Payload Index

- [Get Sites](../payloads/requests/get-sites.example.json)
- [Get Segments](../payloads/requests/get-segments.example.json)
- [Get Assets](../payloads/requests/get-assets.example.json)
- [Get Measurement Locations](../payloads/requests/get-measurement-locations.example.json)
- [Get Measurements](../payloads/requests/get-measurements.example.json)
- [Get Assessments](../payloads/requests/get-assessments.example.json)
- [Get Asset Segment Events](../payloads/requests/get-asset-segment-events.example.json)
