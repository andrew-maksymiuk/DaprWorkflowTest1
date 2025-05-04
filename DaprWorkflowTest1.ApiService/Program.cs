using Dapr.Workflow;
using DaprWorkflowTest1.ApiService.Workflows;
using DaprWorkflowTest1.ApiService.Activities;
using Microsoft.AspNetCore.Mvc;
using DaprWorkflowTest1.ApiService.Models;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<FanoutActivity.Log>();

builder.Services.AddDaprWorkflow(options =>
{
    // Note that it's also possible to register a lambda function as the workflow
    // or activity implementation instead of a class.
    options.RegisterWorkflow<OrderProcessingWorkflow>();
    options.RegisterWorkflow<FanoutWorkflow>();
    options.RegisterWorkflow<FanoutChildWorkflow>();

    // These are the activities that get invoked by the workflow(s).
    options.RegisterActivity<NotifyActivity>();
    options.RegisterActivity<VerifyInventoryActivity>();
    options.RegisterActivity<RequestApprovalActivity>();
    options.RegisterActivity<ProcessPaymentActivity>();
    options.RegisterActivity<UpdateInventoryActivity>();
    options.RegisterActivity<FanoutActivity>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/start-order-workflow", async ([FromServices] DaprClient daprClient, [FromServices] DaprWorkflowClient workflowClient) =>
{
    // Generate a unique ID for the workflow
    var orderId = Guid.NewGuid().ToString()[..8];
    const string itemToPurchase = "Cars";
    const int amountToPurchase = 1;

    // Populate the store with items
    RestockInventory(itemToPurchase);

    // Construct the order
    var orderInfo = new OrderPayload(itemToPurchase, 5000, amountToPurchase);

    // Start the workflow
    Console.WriteLine($"Starting workflow {orderId} purchasing {amountToPurchase} {itemToPurchase}");

    await workflowClient.ScheduleNewWorkflowAsync(
        name: nameof(OrderProcessingWorkflow),
        instanceId: orderId,
        input: orderInfo);

    // Wait for the workflow to start and confirm the input
    var state = await workflowClient.WaitForWorkflowStartAsync(
        instanceId: orderId);

    Console.WriteLine($"Your workflow has started. Here is the status of the workflow: {Enum.GetName(typeof(WorkflowRuntimeStatus), state.RuntimeStatus)}");

    // Wait for the workflow to complete
    state = await workflowClient.WaitForWorkflowCompletionAsync(
        instanceId: orderId);

    Console.WriteLine("Workflow Status: {0}", Enum.GetName(typeof(WorkflowRuntimeStatus), state.RuntimeStatus));

    void RestockInventory(string itemToPurchase)
    {
        daprClient.SaveStateAsync("etcd-state-store", itemToPurchase, new OrderPayload(Name: itemToPurchase, TotalCost: 50000, Quantity: 10));
    }
})
.WithName("StartOrderWorkflow");

app.MapPost("/start-fanout-workflow", async ([FromServices] DaprClient daprClient, [FromServices] DaprWorkflowClient workflowClient) =>
{
    // Generate a unique ID for the workflow
    var instanceId = Guid.NewGuid().ToString()[..8];

    // Start the workflow
    Console.WriteLine($"Starting workflow {instanceId}");

    await workflowClient.ScheduleNewWorkflowAsync(
        name: nameof(FanoutWorkflow),
        instanceId: instanceId,
        input: new FanoutPayload(5));

    // Wait for the workflow to start and confirm the input
    var state = await workflowClient.WaitForWorkflowStartAsync(
        instanceId: instanceId);

    Console.WriteLine($"Your workflow has started. Here is the status of the workflow: {Enum.GetName(typeof(WorkflowRuntimeStatus), state.RuntimeStatus)}");

    // Wait for the workflow to complete
    state = await workflowClient.WaitForWorkflowCompletionAsync(
        instanceId: instanceId);

    Console.WriteLine("Workflow Status: {0}", Enum.GetName(typeof(WorkflowRuntimeStatus), state.RuntimeStatus));
})
.WithName("StartFanoutWorkflow");

app.MapSubscribeHandler();
app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
