using System;
using System.Net;
using System.Threading.Tasks;

namespace PlayWithAsync.Utils
{
    public static class WebClientExtensions
    {
        /// <summary>
        /// Wrap event based asynchronous call with task based asynchronous call
        /// </summary>
        public static Task<byte[]> DownloadDataPuppetTask(this WebClient webClient, string uri)
        {
            var taskPuppet = new TaskCompletionSource<byte[]>();
            
            // init it here to prevent loopback in lambda
            DownloadDataCompletedEventHandler callback = null;
            callback = (sender, args) =>
            {
                // need to unregister it to prevent its raising on the next run
                webClient.DownloadDataCompleted -= callback;
                
                // setting result to 'promise'
                taskPuppet.SetResult(args.Result);
            };
            
            try
            {
                webClient.DownloadDataCompleted += callback;
                webClient.DownloadDataAsync(new Uri(uri));
            }
            catch (Exception e)
            {
                taskPuppet.SetException(e);
            }

            return taskPuppet.Task;
        }
    }
}