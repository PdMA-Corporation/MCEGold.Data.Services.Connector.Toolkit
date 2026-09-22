using MCEGold.Data.Services.Connector.Cli.Configuration;
using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands;
using MCEGold.Data.Services.Connector.Commands.Requests;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace MCEGold.Data.Services.Connector.Cli.Commands;

public sealed class RequestWorkflow
{
    public CliExecutionResult Run(CliSettings settings, ConnectorPostRequestOptions request, bool includeRaw)
    {
        const string command = "request.run";
        var runner = new CommandRunner();
        var options = WorkflowSupport.CreateOptions(settings, includeRaw);
        var steps = new List<WorkflowStep>();
        ConnectorCommandResult? primaryFailure = null;
        ConnectorCommandResult? closeFailure = null;
        ConnectorCommandResult? post = null;
        ConnectorCommandResult? read = null;
        var rawResponses = new List<object>();
        bool opened = false;

        try
        {
            options.Command = CommandRunner.OpenRequestSession;
            var open = runner.Run(options);
            CaptureRaw("open-request-session", open);
            steps.Add(WorkflowSupport.ToStep("open-request-session", open));
            if (!open.Success)
            {
                primaryFailure = open;
                return Finish();
            }

            opened = true;
            options.Command = PostCommand(request.RequestType);
            options.PostRequestOptions = request;
            post = runner.Run(options);
            CaptureRaw("post-request", post);
            steps.Add(WorkflowSupport.ToStep("post-request", post));
            if (!post.Success)
            {
                primaryFailure = post;
                return Finish();
            }

            if (settings.Request.ReadResponse)
            {
                options.Command = CommandRunner.ReadResponse;
                options.MessageId = post.MessageId;
                read = runner.Run(options);
                CaptureRaw("read-response", read);
                steps.Add(WorkflowSupport.ToStep("read-response", read));
                if (!read.Success)
                {
                    primaryFailure = read;
                    return Finish();
                }

                if (settings.Request.RemoveResponseOnSuccess)
                {
                    options.Command = CommandRunner.RemoveResponse;
                    var remove = runner.Run(options);
                    CaptureRaw("remove-response", remove);
                    steps.Add(WorkflowSupport.ToStep("remove-response", remove));
                    if (!remove.Success)
                    {
                        primaryFailure = remove;
                        return Finish();
                    }
                }
            }

            return Finish();
        }
        catch (Exception exception)
        {
            primaryFailure = new ConnectorCommandResult
            {
                Success = false,
                Command = command,
                ErrorCode = "UnexpectedWorkflowFailure",
                ErrorMessage = exception.Message
            };
            return Finish();
        }

        CliExecutionResult Finish()
        {
            if (opened)
            {
                options.Command = CommandRunner.CloseRequestSession;
                var close = runner.Run(options);
                CaptureRaw("close-request-session", close);
                steps.Add(WorkflowSupport.ToStep("close-request-session", close));
                if (!close.Success)
                {
                    closeFailure = close;
                }
            }

            if (primaryFailure is not null)
            {
                return WorkflowSupport.Failure(
                    command,
                    primaryFailure,
                    closeFailure,
                    includeRaw);
            }

            if (closeFailure is not null)
            {
                return WorkflowSupport.Failure(
                    command,
                    closeFailure,
                    includeRaw: includeRaw);
            }

            var payload = read is null ? null : JsonValueConverter.ParseContent(read.Payload);
            return new CliExecutionResult(
                CliEnvelope.Succeeded(command, new
                {
                    requestType = request.RequestType.ToString(),
                    messageId = post?.MessageId,
                    payload,
                    steps
                }, SelectRaw()),
                ExitCodes.Success);
        }

        void CaptureRaw(string step, ConnectorCommandResult result)
        {
            if (includeRaw && !string.IsNullOrWhiteSpace(result.Raw))
            {
                rawResponses.Add(new
                {
                    step,
                    response = JsonValueConverter.ParseContent(result.Raw)
                });
            }
        }

        System.Text.Json.Nodes.JsonNode? SelectRaw()
        {
            return includeRaw && rawResponses.Count > 0
                ? JsonValueConverter.ToNode(rawResponses)
                : null;
        }
    }

    private static string PostCommand(ConnectorRequestType type)
    {
        return type switch
        {
            ConnectorRequestType.GetSites => CommandRunner.PostGetSitesRequest,
            ConnectorRequestType.GetSegments => CommandRunner.PostGetSegmentsRequest,
            ConnectorRequestType.GetAssets => CommandRunner.PostGetAssetsRequest,
            ConnectorRequestType.GetMeasurementLocations => CommandRunner.PostGetMeasurementLocationsRequest,
            ConnectorRequestType.GetMeasurements => CommandRunner.PostGetMeasurementsRequest,
            ConnectorRequestType.GetAssessments => CommandRunner.PostGetAssessmentsRequest,
            ConnectorRequestType.GetAssetSegmentEvents => CommandRunner.PostGetAssetSegmentEventsRequest,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported request type.")
        };
    }
}
