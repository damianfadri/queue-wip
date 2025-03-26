using k8s;
using k8s.Models;

namespace QueueOperator
{
    public class DequeueWorker(Kubernetes client) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jobList = client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                namespaceParameter: "default",
                labelSelector: "jobs/queue-enabled=true",
                watch: true,
                cancellationToken: stoppingToken);

            await foreach (var (type, job) in jobList.WatchAsync<V1Job, V1JobList>(cancellationToken: stoppingToken))
            {
                if (type != WatchEventType.Modified)
                {
                    continue;
                }

                try
                {
                    if (!job.IsDone())
                    {
                        continue;
                    }

                    // get next job to run
                    var deployedJobs = await client.BatchV1.ListNamespacedJobAsync(
                        namespaceParameter: "default",
                        labelSelector: "jobs/queue-enabled=true",
                        cancellationToken: stoppingToken);

                    var upcomingJob = deployedJobs.Items
                        .Where(deployedJob => !deployedJob.IsDone())
                        .OrderBy(deployedJob => deployedJob.Metadata.CreationTimestamp)
                        .FirstOrDefault();

                    if (upcomingJob == null)
                    {
                        continue;
                    }

                    var patch = new V1Patch(new SuspendPatch(false), V1Patch.PatchType.MergePatch);
                    await client.BatchV1.PatchNamespacedJobAsync(
                        body: patch,
                        name: upcomingJob.Metadata.Name,
                        namespaceParameter: "default",
                        cancellationToken: stoppingToken);

                    Console.WriteLine($"{upcomingJob.Metadata.Name} is next, running.");

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
