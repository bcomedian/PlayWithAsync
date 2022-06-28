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
        /// Wrap APM asynchronous call with TAP asynchronous call
        /// </summary>
        public static Task<byte[]> DownloadDataPuppetTask(this HttpWebRequest webRequest)
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
        /// Read binary data from WebResponse
        /// </summary>
        public static byte[] GetBinaryData(this HttpWebResponse response)
        {
            using var binaryReader = new BinaryReader(response.GetResponseStream());
            return binaryReader.ReadBytes((int) response.ContentLength);
        }
    }
}