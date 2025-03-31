using Confluent.Kafka;

using k8s;
using k8s.Models;

using QueueOperator.CustomResourceDefinitions;

namespace EventJobOperator
{
    internal class WatchEventJobsWorker(Kubernetes client, IConsumer<string, string> consumer) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var eventJobList = client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync<V1EventJobList>(
                    group: "navitaire.com",
                    version: "v1",
                    namespaceParameter: "default",
                    plural: "eventjobs",
                    watch: true,
                    cancellationToken: stoppingToken);

                await foreach (var (type, eventJob) in eventJobList.WatchAsync<V1EventJob, V1EventJobList>(cancellationToken: stoppingToken))
                {
                    if (type != WatchEventType.Added)
                    {
                        continue;
                    }

                    try
                    {
                        var job = new V1Job
                        {
                            Metadata = eventJob.Spec.JobTemplate.Metadata,
                            Spec = eventJob.Spec.JobTemplate.Spec,
                        };



                        // subscribe to topic
                        consumer.Subscribe(eventJob.Spec.EventName);

                        _ = Task.Run(() =>
                        {
                            while (!stoppingToken.IsCancellationRequested)
                            {
                                // TODO: Get event data and pass as parameters of job
                                var result = consumer.Consume(stoppingToken);
                                var message = result.Message.Value;

                                job.Metadata.Uid = "";
                                job.Metadata.Name = $"{eventJob.Metadata.Name}-{Guid.NewGuid().ToString().Split('-')[0]}";
                                //job.Metadata.OwnerReferences = new List<V1OwnerReference>
                                //{
                                //    new V1OwnerReference(
                                //        eventJob.ApiVersion,
                                //        eventJob.Kind,
                                //        eventJob.Metadata.Name,
                                //        eventJob.Metadata.Uid)
                                //};

                                client.BatchV1.CreateNamespacedJob(
                                    body: job,
                                    namespaceParameter: "default");
                                }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Enqueue error");
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
