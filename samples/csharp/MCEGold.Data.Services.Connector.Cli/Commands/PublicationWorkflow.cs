using MCEGold.Data.Services.Connector.Cli.Configuration;
using MCEGold.Data.Services.Connector.Cli.Output;
using MCEGold.Data.Services.Connector.Commands;
using CommandRunner = MCEGold.Data.Services.Connector.Commands.ConnectorCommandRunner;

namespace MCEGold.Data.Services.Connector.Cli.Commands;

public sealed class PublicationWorkflow
{
    public CliExecutionResult ReceiveOnce(CliSettings settings, bool remove, bool includeRaw)
    {
        const string command = "publication.receive";
        var runner = new CommandRunner();
        var options = WorkflowSupport.CreateOptions(settings, includeRaw);
        var steps = new List<WorkflowStep>();
        ConnectorCommandResult? primaryFailure = null;
        ConnectorCommandResult? closeFailure = null;
        ConnectorCommandResult? read = null;
        var rawResponses = new List<object>();
        bool opened = false;

        try
        {
            options.Command = CommandRunner.OpenSubscription;
            var open = runner.Run(options);
            CaptureRaw("open-subscription", open);
            steps.Add(WorkflowSupport.ToStep("open-subscription", open));
            if (!open.Success)
            {
                primaryFailure = open;
                return Finish();
            }

            opened = true;
            options.Command = CommandRunner.ReadPublication;
            read = runner.Run(options);
            CaptureRaw("read-publication", read);
            steps.Add(WorkflowSupport.ToStep("read-publication", read));
            if (!read.Success)
            {
                primaryFailure = read;
                return Finish();
            }

            if (remove)
            {
                options.Command = CommandRunner.RemovePublication;
                var removeResult = runner.Run(options);
                CaptureRaw("remove-publication", removeResult);
                steps.Add(WorkflowSupport.ToStep("remove-publication", removeResult));
                if (!removeResult.Success)
                {
                    primaryFailure = removeResult;
                    return Finish();
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
                options.Command = CommandRunner.CloseSubscription;
                var close = runner.Run(options);
                CaptureRaw("close-subscription", close);
                steps.Add(WorkflowSupport.ToStep("close-subscription", close));
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

            return new CliExecutionResult(
                CliEnvelope.Succeeded(command, new
                {
                    messageId = read?.MessageId,
                    payload = JsonValueConverter.ParseContent(read?.Payload),
                    removed = remove,
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
}
