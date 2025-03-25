// See https://aka.ms/new-console-template for more information
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QueueOperator;


Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<EnqueueWorker>()
            .AddHostedService<DequeueWorker>()
            .AddHostedService<SetActiveJobToNullWorker>();

        var config = KubernetesClientConfiguration.BuildDefaultConfig();

        services.AddSingleton(s => new Kubernetes(config));
    })
    .Build()
    .Run();