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

namespace PlayWithAsync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly List<string> _picUrls = new ()
        {
            "https://media.gettyimages.com/photos/entrance-to-uncompahgre-national-forest-colorado-circa-1962-picture-id168642438?s=2048x2048",
            "https://media.gettyimages.com/photos/jackson-lake-snake-river-and-mount-moran-in-the-grand-teton-national-picture-id492841003?s=2048x2048",
            "https://media.gettyimages.com/photos/35mm-film-photo-shows-an-automobile-making-its-way-down-an-empty-dirt-picture-id1315492753?s=2048x2048",
            "https://media.gettyimages.com/photos/white-sands-national-monument-new-mexico-usa-circa-1960-picture-id532273617?s=2048x2048"
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLoadData_WebClient_Sync_OnClick(object sender, RoutedEventArgs e)
        {
            WebClient webClient = new WebClient();
            var dataFromServer = _picUrls.Select(url => webClient.DownloadData(url));
            RenderImages(dataFromServer.ToList());
        }

        private void BtnLoadData_WebClient_Async_EAP_OnClick(object sender, RoutedEventArgs e)
        {
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
                AsyncDownloadData(++index, result);
            };

            webClient.DownloadDataCompleted += downloadDataCompletedHandler; 
            webClient.DownloadDataAsync(new Uri(_picUrls.ElementAt(index)));
        }

        private async void BtnLoadData_WebClient_Async_TAP_OnClick(object sender, RoutedEventArgs e)
        {
            var data = new List<byte[]>();
            
            var webClient = new WebClient();
            foreach (var url in _picUrls)
            {
                data.Add(await webClient.DownloadDataTaskAsync(url));
            }
            RenderImages(data);
        }

        private async void BtnLoadData_WebClient_Async_TAPPuppetTask_OnClick(object sender, RoutedEventArgs e)
        {
            var dataFromServer = new List<byte[]>();
            
            var webClient = new WebClient();
            foreach (var faviconUrl in _picUrls)
            {
                dataFromServer.Add(await webClient.DownloadDataPuppetTask(faviconUrl));
            }
            RenderImages(dataFromServer);
        }
        
        private void BtnLoadData_WebRequest_Sync_OnClick(object sender, RoutedEventArgs e)
        { }

        private void BtnLoadData_WebRequest_Async_APM_OnClick(object sender, RoutedEventArgs e)
        { }

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