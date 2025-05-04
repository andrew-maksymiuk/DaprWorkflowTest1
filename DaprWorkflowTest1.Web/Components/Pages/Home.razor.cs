using Dapr.Client;
using Microsoft.AspNetCore.Components;

namespace DaprWorkflowTest1.Web.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    public required DaprClient DaprClient { get; set; }

    public async Task InvokeOrderWorkflowAsync()
    {
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(2));
        HttpClient httpClient = DaprClient.CreateInvokableHttpClient("apiservice");
        await httpClient.PostAsync("start-order-workflow", null, cancellationTokenSource.Token);
    }

    public async Task InvokeFanoutWorkflowAsync()
    {
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(2));
        HttpClient httpClient = DaprClient.CreateInvokableHttpClient("apiservice");
        await httpClient.PostAsync("start-fanout-workflow", null, cancellationTokenSource.Token);
    }
}