namespace DaprWorkflowTest1.ApiService.Workflows;

internal sealed partial class FanoutWorkflow
{
    public sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(LogLevel.Information, "Starting 'Fanout Workflow' with input: {Input}")]
        public partial void Starting(int input);

        [LoggerMessage(LogLevel.Information, "Starting child workflow with input: {Input}, instance id: '{InstanceId}'")]
        public partial void SchedulingChildWorkflow(int input, string instanceId);

        [LoggerMessage(LogLevel.Information, "Completed child workflow, received output: {Output}")]
        public partial void CompletedChildWorkflow(int output);

        [LoggerMessage(LogLevel.Information, "Completed 'Fanout Workflow' with output: {Output}")]
        public partial void Completed(int output);
    }
}
