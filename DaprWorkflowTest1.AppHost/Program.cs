using DaprWorkflowTest1.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddExternalServices();
builder.AddTestProject<Projects.DaprWorkflowTest1_ApiService>("apiservice");
builder.AddTestProject<Projects.DaprWorkflowTest1_Web>("webfrontend");

builder.Build().Run();
