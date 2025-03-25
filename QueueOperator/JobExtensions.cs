using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;

namespace QueueOperator
{
    public static class JobExtensions
    {
        public static bool IsDone(this V1Job job)
        {
            return (job.Status.Active == null || job.Status.Active == 0)
                && (job.Status.Failed == job.Spec.BackoffLimit + 1 || job.Status.Succeeded == job.Spec.Completions);
        }
    }
}
