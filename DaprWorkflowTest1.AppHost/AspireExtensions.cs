using CommunityToolkit.Aspire.Hosting.Dapr;
using System.Globalization;

namespace DaprWorkflowTest1.AppHost;

/// <summary>
/// Extensions for use in Aspire Host applications
/// </summary>
public static class AspireExtensions
{
    private static List<IResourceBuilder<IResource>> ExternalResources { get; } = [];
    private static List<DaprComponentReferenceAnnotation> DaprAnnotations { get; } = [];
    private static IResourceBuilder<IResource>? DaprPlacement { get; set; }
    private static IResourceBuilder<IResource>? DaprScheduler { get; set; }

    /// <summary>
    /// Adds necessary external services to the application model
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns></returns>
    public static IDistributedApplicationBuilder AddExternalServices(this IDistributedApplicationBuilder builder)
    {
        builder.AddDaprPlacementExecutable();
        builder.AddDaprSchedulerExecutable();

        //builder.AddRabbitMqContainer()
        //    .WithLifetime(ContainerLifetime.Persistent);

        builder.AddEtcdContainer()
            .WithLifetime(ContainerLifetime.Persistent);

        return builder;
    }

    private static IResourceBuilder<RabbitMQServerResource> AddRabbitMqContainer(this IDistributedApplicationBuilder builder)
    {
        var rabbitMqUsername = builder.AddParameter("RabbitMqUsername");
        var rabbitMqPassword = builder.AddParameter("RabbitMqPassword");

        IResourceBuilder<RabbitMQServerResource> result = builder.AddRabbitMQ("rabbitmq", rabbitMqUsername, rabbitMqPassword, 5672)
            .WithManagementPlugin(15672);

        DaprComponentResource rabbitMqPublicComponent = new("rabbitmq-public", "pubsub.rabbitmq");
        DaprComponentReferenceAnnotation rabbitMqPublicAnnotation = new(rabbitMqPublicComponent);
        rabbitMqPublicAnnotation.Component.Annotations.Add(new WaitAnnotation(result.Resource, WaitType.WaitUntilHealthy));

        DaprComponentResource rabbitMqPrivateComponent = new("rabbitmq-private", "pubsub.rabbitmq");
        DaprComponentReferenceAnnotation rabbitMqPrivateAnnotation = new(rabbitMqPrivateComponent);
        rabbitMqPrivateAnnotation.Component.Annotations.Add(new WaitAnnotation(result.Resource, WaitType.WaitUntilHealthy));

        ExternalResources.Add(result);
        DaprAnnotations.Add(rabbitMqPublicAnnotation);
        DaprAnnotations.Add(rabbitMqPrivateAnnotation);

        return result;
    }

    private static IResourceBuilder<ValkeyResource> AddValkeyContainer(this IDistributedApplicationBuilder builder)
    {
        var valkeyPassword = builder.AddParameter("ValkeyPassword");

        IResourceBuilder<ValkeyResource> result = builder.AddValkey("valkey", 6379, valkeyPassword);

        DaprComponentResource component = new("valkey-state-store", "state.redis");
        DaprComponentReferenceAnnotation annotation = new(component);
        annotation.Component.Annotations.Add(new WaitAnnotation(result.Resource, WaitType.WaitUntilHealthy));

        ExternalResources.Add(result);
        DaprAnnotations.Add(annotation);

        return result;
    }

    private static IResourceBuilder<ContainerResource> AddDaprPlacementContainer(this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ContainerResource> scheduler = builder.AddContainer("dapr-placement", "daprio/dapr")
            .WithArgs("./placement", 
                "--port", "50005",
                "--healthz-port", "8801",
                "--metrics-port", "8802",
                "--enable-metrics", "false")
            .WithEndpoint(port: 50005, targetPort: 50005, name: "placement");

        DaprScheduler = scheduler;
        ExternalResources.Add(scheduler);

        return scheduler;
    }

    private static IResourceBuilder<ContainerResource> AddDaprSchedulerContainer(this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ContainerResource> scheduler = builder.AddContainer("dapr-scheduler", "daprio/dapr")
            .WithArgs("./scheduler",
                "--port", "50006",
                "--etcd-data-dir", "/data",
                "--healthz-port", "8803",
                "--metrics-port", "8804",
                "--enable-metrics", "false")
            .WithVolume("dapr-scheduler-data", "/data")
            .WithEndpoint(port: 50006, targetPort: 50006, name: "scheduler");

        DaprScheduler = scheduler;
        ExternalResources.Add(scheduler);

        return scheduler;
    }

    private static IResourceBuilder<ExecutableResource> AddDaprPlacementExecutable(this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ExecutableResource> placement = builder.AddExecutable(
            "dapr-placement", "placement",
            """C:\Users\maksymiuk_a\source\repos\DaprWorkflowTest1""",
            "--port", "50005",
            "--healthz-port", "8801",
            "--metrics-port", "8802",
            "--enable-metrics", "false");

        DaprPlacement = placement;
        ExternalResources.Add(placement);

        return placement;
    }

    private static IResourceBuilder<ExecutableResource> AddDaprSchedulerExecutable(this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ExecutableResource> scheduler = builder.AddExecutable(
            "dapr-scheduler", "scheduler",
            """C:\Users\maksymiuk_a\source\repos\DaprWorkflowTest1""",
            "--port", "50006",
            "--etcd-client-port", "2381",
            "--etcd-initial-cluster", "dapr-scheduler-server-0=http://localhost:2382",
            "--etcd-data-dir", "/data",
            "--healthz-port", "8803",
            "--metrics-port", "8804",
            "--enable-metrics", "false");

        DaprScheduler = scheduler;
        ExternalResources.Add(scheduler);

        return scheduler;
    }

    
    private static IResourceBuilder<ContainerResource> AddEtcdContainer(this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ContainerResource> result = builder.AddEtcd();

        DaprComponentResource component = new("etcd-state-store", "state.etcd");
        DaprComponentReferenceAnnotation annotation = new(component);
        annotation.Component.Annotations.Add(new WaitAnnotation(result.Resource, WaitType.WaitUntilHealthy));

        ExternalResources.Add(result);
        DaprAnnotations.Add(annotation);

        return result;
    }

    public static IResourceBuilder<ContainerResource> AddEtcd(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name = "etcd",
        int clientPort = 2379,
        int peerPort = 2380)
    {
        string cPort = clientPort.ToString(CultureInfo.CurrentCulture);
        string pPort = peerPort.ToString(CultureInfo.CurrentCulture);
        return builder.AddContainer(name, "quay.io/coreos/etcd:v3.5.21")
            .WithEnvironment("ETCD_NAME", name)
            .WithEnvironment("ETCD_DATA_DIR", "/etcd-data")
            .WithEnvironment("ETCD_LISTEN_CLIENT_URLS", $"http://0.0.0.0:{cPort}")
            .WithEnvironment("ETCD_ADVERTISE_CLIENT_URLS", $"http://localhost:{cPort}")
            .WithEnvironment("ETCD_LISTEN_PEER_URLS", $"http://0.0.0.0:{pPort}")
            .WithEnvironment("ETCD_INITIAL_ADVERTISE_PEER_URLS", $"http://{name}:{pPort}")
            .WithEnvironment("ETCD_INITIAL_CLUSTER", $"{name}=http://{name}:{pPort}")
            .WithEnvironment("ETCD_INITIAL_CLUSTER_STATE", "new")
            .WithEnvironment("ETCD_INITIAL_CLUSTER_TOKEN", "aspire-etcd-cluster")
            .WithVolume("etcd-data", "/etcd-data")
            .WithEndpoint(clientPort, targetPort: 2379, name: "etcd-client")
            .WithEndpoint(peerPort, targetPort: 2380, name: "etcd-peer");
    }

    private static IResourceBuilder<ContainerResource> AddExternalMongoDb(this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ContainerResource> result = builder.AddMongoDb();

        DaprComponentResource component = new("mongodb-state-store", "state.mongodb");
        DaprComponentReferenceAnnotation annotation = new(component);
        annotation.Component.Annotations.Add(new WaitAnnotation(result.Resource, WaitType.WaitUntilHealthy));

        ExternalResources.Add(result);
        DaprAnnotations.Add(annotation);

        return result;
    }

    public static IResourceBuilder<ContainerResource> AddMongoDb(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name = "mongodb",
        int port = 27017)
    {
        return builder.AddContainer(name, "mongo:8.0.6")
            .WithEndpoint(port, targetPort: 27017, scheme: "mongodb", name: "mongodb")
            .WithVolume("mongo-data", "/data/db")
            .WithEnvironment("MONGO_INITDB_DATABASE", "daprstate");
    }
    

    public static IResourceBuilder<T> WaitForExternalServices<T>(this IResourceBuilder<T> resourceBuilder) where T : IResourceWithWaitSupport
    {
        foreach (IResourceBuilder<IResource> item in ExternalResources)
            resourceBuilder.WaitFor(item);
        foreach (DaprComponentReferenceAnnotation item in DaprAnnotations)
            resourceBuilder.WithAnnotation(item);

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a Test project to the application model, configures it and attaches sidecars
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="name">The name of the resource</param>
    public static IResourceBuilder<ProjectResource> AddTestProject<TProject>(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>(name)
            .WithHttpHealthCheck("/health")
            .WithStandardEnvironment()
            .WithSidecarServices()
            .WaitForExternalServices();
    }

    /// <summary>
    /// Adds a Test executable to the application model, configures it and attaches sidecars
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="name">The name of the resource</param>
    /// <param name="path">Path to the executable</param>
    /// <param name="port">Optional HTTP port to designate for the application</param>
    public static IResourceBuilder<ExecutableResource> AddTestExecutable(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string path,
        int? port = null)
    {
        string fullPath = Path.GetFullPath(path);
        string dir = Path.GetDirectoryName(fullPath)!;
        return builder.AddExecutable(name, fullPath, dir)
            .WithHttpEndpoint(port, env: "ASPNETCORE_HTTP_PORTS")
            .WithHttpHealthCheck("/health")
            .WithStandardEnvironment()
            .WithSidecarServices()
            .WaitForExternalServices();
    }

    /// <summary>
    /// Adds standard environment variables
    /// </summary>
    /// <param name="builder">The application builder</param>
    public static IResourceBuilder<T> WithStandardEnvironment<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.ApplicationBuilder.Configuration["ASPNETCORE_ENVIRONMENT"])
            .WithEnvironment("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "true")
            .WithOtlpExporter();

        return builder;
    }

    /// <summary>
    /// Attaches and configures sidecar services for Test application parts
    /// </summary>
    /// <param name="builder">The application builder</param>
    public static IResourceBuilder<T> WithSidecarServices<T>(this IResourceBuilder<T> builder)
        where T : IResource
    {
        return builder.WithDaprSidecar(new DaprSidecarOptions
        {
            ResourcesPaths = ["../.dapr/components"],
            Config = "../.dapr/config.yaml",
            PlacementHostAddress = "localhost:50005",
            SchedulerHostAddress = "localhost:50006",
            EnableApiLogging = true
        });
    }
}