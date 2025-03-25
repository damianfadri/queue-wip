using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using QueueOperator.CustomResourceDefinitions;

namespace QueueOperator
{
    public class EnqueueWorker(Kubernetes client) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jobList = client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                namespaceParameter: "default", 
                watch: true, 
                cancellationToken: stoppingToken);

            Console.WriteLine($"Starting {nameof(EnqueueWorker)}");

            await foreach (var (type, job) in jobList.WatchAsync<V1Job, V1JobList>())
            {
                if (type != WatchEventType.Added)
                {
                    continue;
                }

                if (job.IsDone())
                {
                    continue;
                }

                try
                {
                    var jobQueue = await client.GetNamespacedCustomObjectAsync<V1JobQueue>(
                        group: "navitaire.com",
                        version: "v1",
                        namespaceParameter: "default",
                        plural: "jobqueues",
                        name: "sample-queue");

                    jobQueue.Status.Queue.Enqueue(job.Metadata.Name);

                    var patch = new V1Patch(jobQueue.Status, V1Patch.PatchType.MergePatch);
                    var res = await client.CustomObjects.PatchNamespacedCustomObjectStatusAsync(
                        body: patch,
                        group: "navitaire.com",
                        version: "v1",
                        namespaceParameter: "default",
                        plural: "jobqueues",
                        name: "sample-queue");
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
