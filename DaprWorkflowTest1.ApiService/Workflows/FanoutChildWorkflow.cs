using Dapr.Workflow;
using DaprWorkflowTest1.ApiService.Activities;
using DaprWorkflowTest1.ApiService.Models;

namespace DaprWorkflowTest1.ApiService.Workflows;

internal sealed partial class FanoutChildWorkflow : Workflow<FanoutPayload, FanoutPayload>
{
    public override async Task<FanoutPayload> RunAsync(WorkflowContext context, FanoutPayload input)
    {
        Log log = new(context.CreateReplaySafeLogger<FanoutWorkflow>());
        log.Starting(input.Value);

        List<Task<int>> tasks = [];
        for (int i = 0; i < input.Value; i++)
        {
            //log.SchedulingActivity(input.Value);
            tasks.Add(context.CallActivityAsync<FanoutPayload>(
                nameof(FanoutActivity),
                new FanoutPayload(i))
                .ContinueWith(i =>
                {
                    log.CompletedActivity(i.Result.Value);
                    return i.Result.Value;
                }));
        }
        await Task.WhenAll(tasks);

        int result = input.Value + 1;
        log.Completed(result);
        return new(result);
    }
}