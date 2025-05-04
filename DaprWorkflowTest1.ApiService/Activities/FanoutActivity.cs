using Dapr.Workflow;
using DaprWorkflowTest1.ApiService.Models;

namespace DaprWorkflowTest1.ApiService.Activities;

internal sealed partial class FanoutActivity(FanoutActivity.Log log) : WorkflowActivity<FanoutPayload, FanoutPayload>
{
    public override async Task<FanoutPayload> RunAsync(WorkflowActivityContext context, FanoutPayload input)
    {
        log.Starting(input.Value);

        // Simulate slow processing
        await Task.Delay(TimeSpan.FromMilliseconds(input.Value * 500));

        int result = input.Value + 1;
        log.Completed(result);
        return new(result);
    }
}
