using System;
using System.Net;
using System.Threading.Tasks;
using NLog;

namespace PlayWithAsync.Utils
{
    public static class WebClientExtensions
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger(); 
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
                
                _logger.Debug("Web Client async TAP puppet. Finish");
            };
            
            try
            {
                _logger.Debug("Web Client async TAP puppet. Start");
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