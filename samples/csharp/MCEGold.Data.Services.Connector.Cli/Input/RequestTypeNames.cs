using MCEGold.Data.Services.Connector.Commands.Requests;

namespace MCEGold.Data.Services.Connector.Cli.Input;

public static class RequestTypeNames
{
    private static readonly IReadOnlyDictionary<string, ConnectorRequestType> Types =
        new Dictionary<string, ConnectorRequestType>(StringComparer.OrdinalIgnoreCase)
        {
            ["get-sites"] = ConnectorRequestType.GetSites,
            ["get-segments"] = ConnectorRequestType.GetSegments,
            ["get-assets"] = ConnectorRequestType.GetAssets,
            ["get-measurement-locations"] = ConnectorRequestType.GetMeasurementLocations,
            ["get-measurements"] = ConnectorRequestType.GetMeasurements,
            ["get-assessments"] = ConnectorRequestType.GetAssessments,
            ["get-asset-segment-events"] = ConnectorRequestType.GetAssetSegmentEvents
        };

    public static IReadOnlyCollection<string> Supported => Types.Keys.ToArray();

    public static bool TryParse(string value, out ConnectorRequestType requestType) => Types.TryGetValue(value, out requestType);

    public static string ToKebabCase(ConnectorRequestType requestType)
    {
        return Types.First(pair => pair.Value == requestType).Key;
    }
}
