using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NLog;

namespace PlayWithAsync.Utils
{
    public static class WebRequestExtensions
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Wrap APM asynchronous call with TAP asynchronous call,
        /// this wrap was made naively, consider using Task.Factory.FromAsync()
        /// </summary>
        public static Task<byte[]> DownloadDataUsingNaivePuppetTaskAsync(this HttpWebRequest webRequest)
        {
            var taskPuppet = new TaskCompletionSource<byte[]>();

            try
            {
                _logger.Debug("Web Request async TAP puppet. Start");
                webRequest.BeginGetResponse((ar) =>
                {
                    var req = (HttpWebRequest) ar.AsyncState;
                    var webResponse = (HttpWebResponse) req.EndGetResponse(ar);
                    
                    // setting result to 'promise'
                    taskPuppet.SetResult(webResponse.GetBinaryData());
                    
                    _logger.Debug("Web request async TAP puppet. Finish");
                }, webRequest);
            }
            catch (Exception e)
            {
                taskPuppet.SetException(e);
            }
            
            return taskPuppet.Task;
        }
        
        /// <summary>
        /// Wrap APM asynchronous call with TAP asynchronous call using .NET utility Factory.FromAsync
        /// </summary>
        public static Task<byte[]> DownloadDataUsingPuppetTaskAsync(this HttpWebRequest webRequest)
        {
            var task = Task<WebResponse>.Factory
                .FromAsync(
                    webRequest.BeginGetResponse,
                    webRequest.EndGetResponse, null);
            return task.ContinueWith(result =>
            {
                var webResponse = (HttpWebResponse) result.Result;
                return webResponse.GetBinaryData();
            });
        }

        /// <summary>
        /// Read binary data from WebResponse
        /// </summary>
        public static byte[] GetBinaryData(this HttpWebResponse response)
        {
            using var binaryReader = new BinaryReader(response.GetResponseStream());
            return binaryReader.ReadBytes((int) response.ContentLength);
        }
    }
}