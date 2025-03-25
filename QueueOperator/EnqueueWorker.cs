using k8s;
using k8s.Models;
using QueueOperator.CustomResourceDefinitions;

namespace QueueOperator
{
    public class EnqueueWorker(Kubernetes client) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jobList = await client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                namespaceParameter: "default",
                watch: true, 
                cancellationToken: stoppingToken);

            jobList.Body.ResourceVersion();

            
            await foreach (var (type, job) in jobList.WatchAsync<V1Job, V1JobList>())
            {
                if (type != WatchEventType.Added)
                {
                    Console.WriteLine($"[{nameof(EnqueueWorker)}] JobType is not Added.");
                    continue;
                }

                if (job.IsDone())
                {
                    Console.WriteLine($"[{nameof(EnqueueWorker)}] {job.Metadata.Name} is done.");
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
                    jobQueue.Metadata.ResourceVersion

                    var patch = new V1Patch(jobQueue, V1Patch.PatchType.MergePatch);
                    var res = await client.CustomObjects.PatchNamespacedCustomObjectStatusAsync(
                        body: patch,
                        group: "navitaire.com",
                        version: "v1",
                        namespaceParameter: "default",
                        plural: "jobqueues",
                        name: "sample-queue");

                    Console.WriteLine($"[{nameof(EnqueueWorker)}] Updated sample-queue to enqueue {job.Metadata.Name}");
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
