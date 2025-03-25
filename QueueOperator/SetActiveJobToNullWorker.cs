using k8s;
using k8s.Models;
using QueueOperator.CustomResourceDefinitions;

namespace QueueOperator
{
    public class SetActiveJobToNullWorker(Kubernetes client) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jobList = client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                namespaceParameter: "default",
                watch: true,
                cancellationToken: stoppingToken);

            Console.WriteLine($"Starting {nameof(SetActiveJobToNullWorker)}");

            await foreach (var (type, job) in jobList.WatchAsync<V1Job, V1JobList>())
            {
                if (!job.IsDone())
                {
                    Console.WriteLine($"[{nameof(SetActiveJobToNullWorker)}] Job {job.Metadata.Name} is not done.");
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

                    if (job.Metadata.Name != jobQueue.Status.ActiveJob)
                    {
                        Console.WriteLine($"[{nameof(SetActiveJobToNullWorker)}] status.activeJob {jobQueue.Status.ActiveJob} does not match {job.Metadata.Name}");
                        continue;
                    }

                    jobQueue.Status.ActiveJob = null;

                    var patch = new V1Patch(jobQueue, V1Patch.PatchType.MergePatch);
                    await client.PatchNamespacedCustomObjectAsync(
                        body: patch,
                        group: "navitaire.com",
                        version: "v1",
                        namespaceParameter: "default",
                        plural: "jobqueues",
                        name: "sample-queue");

                    Console.WriteLine($"[{nameof(SetActiveJobToNullWorker)}] Updated sample-queue to have no status.activeJob.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SetActiveJob error");
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
