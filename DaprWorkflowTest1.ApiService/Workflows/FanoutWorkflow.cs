using Dapr.Workflow;
using DaprWorkflowTest1.ApiService.Models;

namespace DaprWorkflowTest1.ApiService.Workflows;

internal sealed partial class FanoutWorkflow : Workflow<FanoutPayload, FanoutPayload>
{
    public override async Task<FanoutPayload> RunAsync(WorkflowContext context, FanoutPayload input)
    {
        Log log = new(context.CreateReplaySafeLogger<FanoutWorkflow>());
        log.Starting(input.Value);

        string instanceId = Guid.NewGuid().ToString()[..8];
        log.SchedulingChildWorkflow(input.Value, instanceId);
        FanoutPayload output = await context.CallChildWorkflowAsync<FanoutPayload>(
            nameof(FanoutChildWorkflow),
            input,
            new() { InstanceId = instanceId });
        log.CompletedChildWorkflow(output.Value);

        int result = input.Value + 1;
        log.Completed(result);
        return new(result);
    }
}