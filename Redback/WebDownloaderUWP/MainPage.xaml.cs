using Redback.Helpers;
using Redback.WebGraph;
using Windows.System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;

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
            var webTask = new SocketSiteGraph(url, _downloadFolder.Path);

            webTask.ObjectProcessed += WebTaskOnObjectProcessed;

            await webTask.Run();

            _searching = false;
        }

        private void WebTaskOnObjectProcessed(object sender, SocketSiteGraph.ObjectProcessedEventArgs args)
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
