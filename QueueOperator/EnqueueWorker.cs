using k8s;
using k8s.Models;
using QueueOperator.CustomResourceDefinitions;

using YamlDotNet.Core.Tokens;

namespace QueueOperator
{
    public class EnqueueWorker(Kubernetes client) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var jobList = client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                    namespaceParameter: "default",
                    labelSelector: "jobs/queue-enabled=true",
                    watch: true,
                    cancellationToken: stoppingToken);

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
                        var deployedJobs = await client.BatchV1.ListNamespacedJobAsync(
                            namespaceParameter: "default",
                            labelSelector: "jobs/queue-enabled=true",
                            cancellationToken: stoppingToken);

                        var notAllCompleted = deployedJobs.Items
                            .Where(deployedJob => deployedJob.Metadata.Name != job.Metadata.Name)
                            .Any(deployedJob => !deployedJob.IsDone());

                        var patch = new V1Patch(new SuspendPatch(notAllCompleted), V1Patch.PatchType.MergePatch);

                        await client.BatchV1.PatchNamespacedJobAsync(
                            body: patch,
                            name: job.Metadata.Name,
                            namespaceParameter: "default",
                            cancellationToken: stoppingToken);
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
