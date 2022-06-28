using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using PlayWithAsync.Utils;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NLog;
using PlayWithAsync.Models;

namespace PlayWithAsync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private static readonly List<string> _picUrls = new ()
        {
            "https://media.gettyimages.com/photos/entrance-to-uncompahgre-national-forest-colorado-circa-1962-picture-id168642438?s=2048x2048",
            "https://media.gettyimages.com/photos/jackson-lake-snake-river-and-mount-moran-in-the-grand-teton-national-picture-id492841003?s=2048x2048",
            "https://media.gettyimages.com/photos/35mm-film-photo-shows-an-automobile-making-its-way-down-an-empty-dirt-picture-id1315492753?s=2048x2048",
            "https://media.gettyimages.com/photos/white-sands-national-monument-new-mexico-usa-circa-1960-picture-id532273617?s=2048x2048",
            "https://media.gettyimages.com/photos/the-colorado-river-wraps-around-horseshoe-bend-in-the-in-glen-canyon-picture-id647219426?s=2048x2048",
            "https://media.gettyimages.com/photos/canyon-de-chelly-navajo-by-edward-s-curtis-depicting-navajo-riders-picture-id530194319?s=2048x2048"
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLoadData_WebClient_Sync_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Web Client sync. Clicked");
            WebClient webClient = new WebClient();
            var dataFromServer = _picUrls.Select(url => webClient.DownloadData(url));
            _logger.Debug("Web Client sync. Data received");
            RenderImages(dataFromServer.ToList());
        }

        private void BtnLoadData_WebClient_Async_EAP_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Web Client async EAP. Clicked");
            var dataFromServer = new List<byte[]>();
            AsyncDownloadData(0, dataFromServer);
        }

        /// <summary>
        /// Event based asynchronous calls need to be done as recursive calls. It's nasty.
        /// </summary>
        private void AsyncDownloadData(int index, List<byte[]> result)
        {
            if (index == _picUrls.Count)
            {
                RenderImages(result);
                return;
            }

            var webClient = new WebClient();

            DownloadDataCompletedEventHandler downloadDataCompletedHandler = null;
            downloadDataCompletedHandler = (o, args) =>
            {
                webClient.DownloadDataCompleted -= downloadDataCompletedHandler;
                result.Add(args.Result);
                _logger.Debug("Web Client async EAP. Finish");
                AsyncDownloadData(++index, result);
            };

            _logger.Debug("Web Client async EAP. Start");
            webClient.DownloadDataCompleted += downloadDataCompletedHandler; 
            webClient.DownloadDataAsync(new Uri(_picUrls.ElementAt(index)));
        }

        private async void BtnLoadData_WebClient_Async_TAP_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Web Client async TAP. Clicked");
            var data = new List<byte[]>();
            
            var webClient = new WebClient();
            foreach (var url in _picUrls)
            {
                data.Add(await webClient.DownloadDataTaskAsync(url));
            }
            _logger.Debug("Web Client async TAP. Received");
            RenderImages(data);
        }

        private async void BtnLoadData_WebClient_Async_TAPPuppetTask_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Web Client async TAP puppet. Clicked");
            var dataFromServer = new List<byte[]>();
            
            var webClient = new WebClient();
            foreach (var faviconUrl in _picUrls)
            {
                dataFromServer.Add(await webClient.DownloadDataPuppetTask(faviconUrl));
            }
            RenderImages(dataFromServer);
        }

        private void BtnLoadData_WebRequest_Sync_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Web Request sync. Clicked");
            var imgData = new List<byte[]>(); 
            foreach (var url in _picUrls)
            {
                var request = (HttpWebRequest) WebRequest.Create(new Uri(url));
                var response = (HttpWebResponse) request.GetResponse();
                using var binaryReader = new BinaryReader(response.GetResponseStream());
                imgData.Add(binaryReader.ReadBytes((int) response.ContentLength));
            }
            _logger.Debug("Web Request sync. Received");
            RenderImages(imgData);
        }

        private void BtnLoadData_WebRequest_Async_APM_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Debug("Web Request async APM. Clicked");
            var asyncRequestsAndStates = new Dictionary<Uri, WebRequestAsyncState>();
            foreach (var picUrl in _picUrls)
            {
                var uri = new Uri(picUrl);
                asyncRequestsAndStates.Add(uri, new WebRequestAsyncState { Completed = false, Data = new List<byte>() });

                var req = (HttpWebRequest) WebRequest.Create(uri);
                req.BeginGetResponse(ProcessResponse, (req, asyncRequestsAndStates));
                
                _logger.Debug("Web Request async APM. One started");
            }
        }

        private void ProcessResponse(IAsyncResult ar)
        {
            var (req, state) = ((HttpWebRequest, Dictionary<Uri, WebRequestAsyncState>)) ar.AsyncState;
            
            var webResponse = (HttpWebResponse) req.EndGetResponse(ar);
            var data = webResponse.GetBinaryData();

            state[req.RequestUri].Data = data.ToList();
            state[req.RequestUri].Completed = true;
            _logger.Debug("Web Request async APM. One finished");

            // all finished
            if (state.Values.All(x => x.Completed))
            {
                _logger.Debug("Web Request async APM. All finished");
                Dispatcher.BeginInvoke(
                    () => RenderImages(state.Select(x => x.Value.Data.ToArray()).ToList()),
                    DispatcherPriority.Render);
            }
        }

        private async void BtnLoadData_WebRequest_Async_TAP_OnClick(object sender, RoutedEventArgs e)
        {
            var responsesTasks = _picUrls
                .Select(url => WebRequest.Create(url).GetResponseAsync())
                .ToList();
            await Task.WhenAll(responsesTasks);
            var data = responsesTasks
                .Select(x => ((HttpWebResponse)x.Result).GetBinaryData())
                .ToList();
            RenderImages(data);
        }

        private async void BtnLoadData_WebRequest_Async_TAPPuppetTask_OnClick(object sender, RoutedEventArgs e)
        {
            var requests = new List<Task<byte[]>>();
            foreach (var picUrl in _picUrls)
            {
                var webRequest = (HttpWebRequest) WebRequest.Create(picUrl);
                requests.Add(webRequest.DownloadDataPuppetTask());
            }

            await Task.WhenAll(requests);
            
            RenderImages(requests.Select(x => x.Result).ToList());
        }
        
        private void RenderImages(List<byte[]> imgsData)
        {
            _logger.Debug("Render Images called.");
            ClearImages();
            foreach (var oneImgData in imgsData)
            {
                var image = new BitmapImage();
                using (MemoryStream imgByteStream = new MemoryStream(oneImgData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = imgByteStream;
                    image.EndInit();
                }
                image.Freeze();

                var imageControl = new Image
                {
                    Source = image,
                    Width = 200,
                    Height = 200,
                    Stretch = Stretch.Uniform
                };
                containerImgs.Children.Add(imageControl);
            }
        }
        
        private void BtnClearImages_OnClick(object sender, RoutedEventArgs e)
        {
            ClearImages();
        }

        private void ClearImages()
        {
            containerImgs.Children.Clear();
        }
    }
}