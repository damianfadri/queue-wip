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
            var jobList = client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                namespaceParameter: "default",
                watch: true,
                cancellationToken: stoppingToken);

            await foreach (var (type, job) in jobList.WatchAsync<V1Job, V1JobList>())
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
                    var deployedJobs = await client.BatchV1.ListNamespacedJobAsync("default");

                    var upcomingJob = deployedJobs.Items
                        .OrderBy(deployedJobs => deployedJobs.Metadata.CreationTimestamp)
                        .FirstOrDefault();

                    if (upcomingJob == null)
                    {
                        continue;
                    }

                    // set the job to running
                    upcomingJob.Spec.Suspend = false;
                    await client.BatchV1.PatchNamespacedJobAsync(
                        body: new V1Patch(upcomingJob, V1Patch.PatchType.MergePatch),
                        name: upcomingJob.Metadata.Name,
                        namespaceParameter: "default");

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
