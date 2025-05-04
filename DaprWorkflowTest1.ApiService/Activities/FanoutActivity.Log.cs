namespace DaprWorkflowTest1.ApiService.Activities;

internal sealed partial class FanoutActivity
{
    public sealed partial class Log(ILogger<FanoutActivity> logger)
    {
        [LoggerMessage(LogLevel.Information, "Starting 'Fanout Activity' with input: {Input}")]
        public partial void Starting(int input);

        [LoggerMessage(LogLevel.Information, "Completed 'Fanout Activity' with output: {Output}")]
        public partial void Completed(int output);
    }
}
