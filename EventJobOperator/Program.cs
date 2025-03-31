// See https://aka.ms/new-console-template for more information
using Confluent.Kafka;

using EventJobOperator;

using k8s;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services
            .AddHostedService<WatchEventJobsWorker>();

        services.AddSingleton(s => new Kubernetes(
            KubernetesClientConfiguration.BuildDefaultConfig()));

        services.AddSingleton(s => new ConsumerBuilder<string, string>(
            new ConsumerConfig
            {
                GroupId = "event-job-operator-group",
                BootstrapServers = "localhost:9092"
            }).Build());
    })
    .Build()
    .Run();