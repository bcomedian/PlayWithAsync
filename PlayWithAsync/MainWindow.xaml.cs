using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using PlayWithAsync.Utils;

namespace PlayWithAsync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static readonly List<string> _faviconUrls = new List<string>
        {
            "http://www.google.com/favicon.ico",
            "http://www.bing.com/favicon.ico",
            "http://www.facebook.com/favicon.ico",
            "http://www.apple.com/favicon.ico",
            "https://www.cnn.com/favicon.ico",
            "http://www.wikipedia.org/favicon.ico",
            "http://www.youtube.com/favicon.ico",
            "http://www.yahoo.com/favicon.ico",
            "http://www.linkedin.com/favicon.ico",
            "http://www.microsoft.com/favicon.ico"
        };
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLoadSync_OnClick(object sender, RoutedEventArgs e)
        {
            WebClient webClient = new WebClient();
            var dataFromServer = _faviconUrls.Select(url => webClient.DownloadData(url));
            RenderFavicons(dataFromServer.ToList());
        }

        private void BtnLoadAsync_EAP_OnClick(object sender, RoutedEventArgs e)
        {
            var dataFromServer = new List<byte[]>();
            AsyncDownload(0, dataFromServer);
        }

        /// <summary>
        /// Event based asynchronous calls need to be done as recursive calls. It's nasty.
        /// </summary>
        private void AsyncDownload(int index, List<byte[]> result)
        {
            if (index == _faviconUrls.Count)
            {
                RenderFavicons(result);
                return;
            }

            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += (o, args) =>
            {
                result.Add(args.Result);
                AsyncDownload(++index, result);
            };
            webClient.DownloadDataAsync(new Uri(_faviconUrls.ElementAt(index)));
        }

        private async void BtnLoadAsync_TAP_OnClick(object sender, RoutedEventArgs e)
        {
            var data = new List<byte[]>();
            
            var webClient = new WebClient();
            foreach (var url in _faviconUrls)
            {
                data.Add(await webClient.DownloadDataTaskAsync(url));
            }
            RenderFavicons(data);
        }

        private async void BtnLoadAsync_TAPPuppetTask_OnClick(object sender, RoutedEventArgs e)
        {
            var dataFromServer = new List<byte[]>();
            
            var webClient = new WebClient();
            foreach (var faviconUrl in _faviconUrls)
            {
                dataFromServer.Add(await webClient.DownloadDataPuppetTask(faviconUrl));
            }
            RenderFavicons(dataFromServer);
        }
        
        private void RenderFavicons(List<byte[]> faviconsData)
        {
            stackPanelFavicons.Children.Clear();
            foreach (var oneFaviconData in faviconsData)
            {
                using (MemoryStream faviconByteStream = new MemoryStream(oneFaviconData))
                {
                    var imageControl = new System.Windows.Controls.Image();
                    imageControl.Source = (ImageSource) new ImageSourceConverter().ConvertFrom(faviconByteStream);
                    stackPanelFavicons.Children.Add(imageControl);
                }
            }
        }
        
        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
        {
            stackPanelFavicons.Children.Clear();
        }
    }
}