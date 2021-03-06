﻿// Socket is duplicated left only for research / reference
//#define USE_SOCKET_IMPLEMENTATION

using Redback.Helpers;
using Redback.WebGraph;
using Windows.System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;
using Redback.UrlManagement;
using Redback.WebGraph.Actions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WebDownloaderUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Fields

        private IStorageFolder _downloadFolder;

        private bool _searching = false;

        #endregion

        #region Methods

        public MainPage()
        {
            InitializeComponent();

            TxtAppDataFolder.Text = ApplicationData.Current.LocalFolder.Path;
        }

        private async void TxtUrlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                await Start();
            }
        }

        private async void BtnGoOnClick(object sender, RoutedEventArgs e)
        {
            await Start();
        }

        private async Task Start()
        {
            if (_searching)
            {
                return;
            }
            _searching = true;

            var d = ApplicationData.Current;
            var storage = d.LocalFolder;

            _downloadFolder = await storage.GetOrCreateSubfolderAsync("Downloads");

            var url = TxtUrl.Text;
#if USE_SOCKET_IMPLEMENTATION
            var manager = DownloadHelper.CreateManager<SocketSiteGraph, UrlPool, HostRegulator, SocketDownloader>
                (url, _downloadFolder.Path);
#else
            var manager = DownloadHelper.CreateManager<HttpSiteGraph, UrlPool, HostRegulator, HttpDownloader>(
                url, _downloadFolder.Path);
#endif
            manager.Graph.ObjectProcessed += WebTaskOnObjectProcessed;

            await manager.Initialize();
            await manager.Graph.Run();

            _searching = false;
        }

        private void WebTaskOnObjectProcessed(object sender, BaseSiteGraph.ObjectProcessedEventArgs args)
        {
            var hasUrl = args.Object as IHasUrl;
            var url = "";
            if (hasUrl != null)
            {
                url = hasUrl.Url;
            }
            var isAction = args.Object is BaseAction;
            string msg;

            if (args.Successful)
            {
                msg = isAction
                    ? string.Format("Succeeded in performing action with url '{0}'", url)
                    : string.Format("Succeeded in analyzing node with url '{0}'", url);
            }
            else
            {
                msg = isAction
                    ? string.Format("Failed to perform action with url '{0}', error being '{1}'", url, args.ErrorMessage)
                    : string.Format("Failed to analyze node with url '{0}', error being '{1}'", url, args.ErrorMessage);
            }
            // ReSharper disable once PossibleNullReferenceException
            LstTasks.Items.Add(msg);
        }

        #endregion
    }
}
