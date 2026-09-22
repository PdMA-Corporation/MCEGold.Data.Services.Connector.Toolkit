namespace MCEGold.Data.Services.Connector.Cli.Output;

public static class ExitCodes
{
    public const int Success = 0;
    public const int UsageOrValidation = 2;
    public const int Configuration = 3;
    public const int Authentication = 4;
    public const int Transport = 5;
    public const int RemoteOperation = 6;
    public const int Unexpected = 10;
}
