using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using PlayWithAsync.Utils;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NLog;

namespace PlayWithAsync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        
        private List<byte[]> _apmImgData = new ();
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
            foreach (var picUrl in _picUrls)
            {
                _logger.Debug("Web Request async APM. Start");
                var req = (HttpWebRequest) WebRequest.Create(new Uri(picUrl));
                req.BeginGetResponse(ProcessEndResponse, req);
            }

            while (_apmImgData.Count != _picUrls.Count)
            { }
            
            RenderImages(_apmImgData);
        }

        private void ProcessEndResponse(IAsyncResult ar)
        {
            var req = (HttpWebRequest) ar.AsyncState;
            var response = (HttpWebResponse) req.EndGetResponse(ar);
            using var binaryReader = new BinaryReader(response.GetResponseStream());
            var data = binaryReader.ReadBytes((int)response.ContentLength);
            lock (_apmImgData)
            {
                _apmImgData.Add(data);
            }
            _logger.Debug("Web Request async APM. Finish");
        }

        private async void BtnLoadData_WebRequest_Async_TAP_OnClick(object sender, RoutedEventArgs e)
        { }

        private async void BtnLoadData_WebRequest_Async_TAPPuppetTask_OnClick(object sender, RoutedEventArgs e)
        { }
        
        private void RenderImages(List<byte[]> imgsData)
        {
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