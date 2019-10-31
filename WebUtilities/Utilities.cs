using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public static class Utilities
    {
        /// <summary>
        /// Allows using a Cancellation Token as if it were a task.
        /// From https://github.com/docevaad/Anchor/blob/master/Tortuga.Anchor/Tortuga.Anchor.source/shared/TaskUtilities.cs
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that can be canceled, but never completed.</returns>
        public static Task AsTask(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled(), false);
            return tcs.Task;
        }

        public static string GetTimeoutMessage(Uri uri)
        {
            return $"Timeout occurred while waiting for {uri}";
        }
    }
}
