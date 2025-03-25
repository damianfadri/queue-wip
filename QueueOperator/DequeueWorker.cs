using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using QueueOperator.CustomResourceDefinitions;

namespace QueueOperator
{
    public class DequeueWorker(Kubernetes client) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jobQueues = client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync<V1JobQueueList>(
                group: "navitaire.com",
                version: "v1",
                namespaceParameter: "default",
                plural: "jobqueues",
                watch: true);

            await foreach (var (type, jobQueue) in jobQueues.WatchAsync<V1JobQueue, V1JobQueueList>())
            {
                if (type != WatchEventType.Modified)
                {
                    Console.WriteLine($"[{nameof(DequeueWorker)}] JobType is not Modified.");
                    continue;
                }

                if (jobQueue.Status.ActiveJob != null)
                {
                    Console.WriteLine($"[{nameof(DequeueWorker)}] status.activeJob = {jobQueue.Status.ActiveJob}.");
                    continue;
                }

                if (jobQueue.Status.Queue.Count == 0)
                {
                    Console.WriteLine($"[{nameof(DequeueWorker)}] status.queue.count = {jobQueue.Status.Queue.Count}");
                    continue;
                }

                try
                {
                    var upcomingJob = jobQueue.Status.Queue.Dequeue();

                    jobQueue.Status.ActiveJob = upcomingJob;

                    var patch = new V1Patch(jobQueue.Status, V1Patch.PatchType.MergePatch);
                    await client.PatchNamespacedCustomObjectStatusAsync(
                        body: patch,
                        group: "navitaire.com",
                        version: "v1",
                        namespaceParameter: "default",
                        plural: "jobqueues",
                        name: "sample-queue");

                    var job = await client.BatchV1.ReadNamespacedJobAsync(upcomingJob, "default");
                    job.Spec.Suspend = false;

                    var patch2 = new V1Patch(job, V1Patch.PatchType.MergePatch);
                    await client.PatchNamespacedJobAsync(
                        body: patch2,
                        name: upcomingJob,
                        namespaceParameter: "default");

                    Console.WriteLine($"[{nameof(DequeueWorker)}] Updated sample-queue to have status.activeJob = {upcomingJob}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Dequeue error");
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
