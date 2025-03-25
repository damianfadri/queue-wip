using k8s;
using k8s.Models;
using QueueOperator.CustomResourceDefinitions;

namespace QueueOperator
{
    public class EnqueueWorker(Kubernetes client) : ThreadedHostedService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jobList = client.BatchV1.ListNamespacedJobWithHttpMessagesAsync(
                namespaceParameter: "default",
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
                    await EnqueueOrRun(job);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Enqueue error");
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private async Task EnqueueOrRun(V1Job job)
        {
            var deployedJobs = await client.BatchV1.ListNamespacedJobAsync("default");

            var notAllCompleted = deployedJobs.Items
                .Where(deployedJob => deployedJob.Metadata.Name != job.Metadata.Name)
                .Any(deployedJob => !deployedJob.IsDone());

            if (notAllCompleted)
            {
                var patchStr = @"
{
    ""spec"": {
        ""suspend"": true
    }
}";

                var patch = new V1Patch(patchStr, V1Patch.PatchType.MergePatch);
                await client.BatchV1.PatchNamespacedJobAsync(
                    body: patch,
                    name: job.Metadata.Name,
                    namespaceParameter: "default");
            }
            else
            {
                var patchStr = @"
{
    ""spec"": {
        ""suspend"": false
    }
}";

                var patch = new V1Patch(patchStr, V1Patch.PatchType.MergePatch);
                await client.BatchV1.PatchNamespacedJobAsync(
                    body: patch,
                    name: job.Metadata.Name,
                    namespaceParameter: "default");
            }

        }
    }
}
