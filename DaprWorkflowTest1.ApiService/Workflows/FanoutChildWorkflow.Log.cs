namespace DaprWorkflowTest1.ApiService.Workflows;

internal sealed partial class FanoutChildWorkflow
{
    public sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(LogLevel.Information, "Starting 'Fanout Child Workflow' with input: {Input}")]
        public partial void Starting(int input);

        [LoggerMessage(LogLevel.Information, "Starting child activity with input: {Input}")]
        public partial void SchedulingActivity(int input);

        [LoggerMessage(LogLevel.Information, "Completed child activity, received output: {Output}")]
        public partial void CompletedActivity(int output);

        [LoggerMessage(LogLevel.Information, "Completed 'Fanout Child Workflow' with output: {Output}")]
        public partial void Completed(int output);
    }
}
