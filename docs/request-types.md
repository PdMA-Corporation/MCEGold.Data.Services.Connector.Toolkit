# Supported Request Types

## Investigation

The Console menus, CLI `RequestTypeNames`, `ConnectorRequestType`, staged `request post` dispatcher, mapper, and validator expose the same seven consumer request operations. The CLI requires `--input`, and every input document requires a `requestType`. The short-form payload is the authoritative source of the request type and mirrors how long-form CCOM/OIIE BODs determine the request interface from the request document itself. An empty JSON object is therefore invalid. All criteria, filters, `maxItems`, `lastNData`, and `payloadProfile` values are optional, so a document containing only `requestType` is valid.

| Request Type | Connector Operation | Expected Short-Form Payload | Payload Required | Empty `{}` Valid |
|---|---|---|---:|---:|
| `get-sites` | `PostGetSitesRequest` | `requestType`; optional `payloadProfile`; optional `maxItems`; optional `filters.siteUuid` | Yes | No |
| `get-segments` | `PostGetSegmentsRequest` | `requestType`; optional `payloadProfile`; optional `maxItems`; optional site, segment, and type UUID filters | Yes | No |
| `get-assets` | `PostGetAssetsRequest` | `requestType`; optional `payloadProfile`; optional `maxItems`; optional asset/site/type UUID and serial-number filters | Yes | No |
| `get-measurement-locations` | `PostGetMeasurementLocationsRequest` | `requestType`; optional `payloadProfile`; optional `maxItems`; optional measurement-location/site/segment/type UUID filters | Yes | No |
| `get-measurements` | `PostGetMeasurementsRequest` | `requestType`; optional `payloadProfile`; optional limits; optional measurement-location/segment UUID and recorded-date filters | Yes | No |
| `get-assessments` | `PostGetAssessmentsRequest` | `requestType`; optional `payloadProfile`; optional limits; optional asset/health-level UUID, assessed-date, and health-range filters | Yes | No |
| `get-asset-segment-events` | `PostGetAssetSegmentEventsRequest` | `requestType`; optional `payloadProfile`; optional limits; optional event/site/segment/asset, serial, installation, and removal filters | Yes | No |

These are connector short-form inputs, not complete OIIE/CCOM BOD documents. The connector validates and maps them before posting.

`payloadProfile` is supported for all listed request types. Valid values are `Full` and `Minimal`; omitted values behave as `Full` and are not serialized. Explicit values are serialized to the generated CCOM request under `applicationArea.userArea.mcegold.payloadProfile`. CLI `--payload-profile` overrides short-form JSON, which overrides optional config `request.payloadProfile`.

## Example Payloads

| Request Type | Example Payload | Description |
|---|---|---|
| `get-sites` | [`payloads/requests/get-sites.example.json`](../payloads/requests/get-sites.example.json) | Retrieve sites, optionally restricted to one site UUID. |
| `get-segments` | [`payloads/requests/get-segments.example.json`](../payloads/requests/get-segments.example.json) | Retrieve segments using site, segment, or segment-type criteria. |
| `get-assets` | [`payloads/requests/get-assets.example.json`](../payloads/requests/get-assets.example.json) | Retrieve assets using identity, site, type, or serial-number criteria. |
| `get-measurement-locations` | [`payloads/requests/get-measurement-locations.example.json`](../payloads/requests/get-measurement-locations.example.json) | Retrieve measurement locations associated with sites or segments. |
| `get-measurements` | [`payloads/requests/get-measurements.example.json`](../payloads/requests/get-measurements.example.json) | Retrieve recent measurements within an optional recorded-time range. |
| `get-assessments` | [`payloads/requests/get-assessments.example.json`](../payloads/requests/get-assessments.example.json) | Retrieve assessments using asset, health-level, date, or numeric criteria. |
| `get-asset-segment-events` | [`payloads/requests/get-asset-segment-events.example.json`](../payloads/requests/get-asset-segment-events.example.json) | Retrieve asset/segment installation and removal events. |

Post an example after opening a request session:

```powershell
python .\samples\python\06_post_request.py `
  --input .\payloads\requests\get-measurements.example.json `
  --payload-profile Minimal
```
