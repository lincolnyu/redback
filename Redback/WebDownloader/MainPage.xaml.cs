using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Redback.Helpers;
using Redback.WebGraph;
using Redback.WebGraph.Actions;

namespace WebDownloader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Fields

        private IStorageFolder _downloadFolder;

        #endregion

        #region Methods

        public MainPage()
        {
            InitializeComponent();

            TxtAppDataFolder.Text = ApplicationData.Current.LocalFolder.Path;
        }

        private async void TestDownload()
        {
            Uri uri;
            Uri.TryCreate("http://www.bing.com", UriKind.Absolute, out uri);

            var folder = await ApplicationData.Current.LocalFolder.GetOrCreateSubfolderAsync("haha");
            var file = await folder.GetOrCreateFileAsync("baidu_homtpage");

            DownloadOperation dlo = null;
            var downloader = new BackgroundDownloader();
            await Task.Run(() => dlo = downloader.CreateDownload(uri, file));

            await dlo.StartAsync();

            var resp = dlo.GetResponseInformation();
            string ct;
            resp.Headers.TryGetValue("Content-Type", out ct);
        }

        private async void TestDownload2()
        {
            var webTask = new SiteGraph("http//www.bing.com", ApplicationData.Current.LocalFolder.Path);

            var msd = new MySocketDownloader();
            msd.Owner = webTask;
            msd.Url = "http://www.bing.com";
            msd.LocalDirectory = Path.Combine(ApplicationData.Current.LocalFolder.Path, "haha2");
            msd.LocalFileName = "bing_page";
            msd.Perform();
        }

        private async void BtnGoOnClick(object sender, RoutedEventArgs e)
        {
            //TestDownload();

            TestDownload2();
            return;


            var d = ApplicationData.Current;
            var storage = d.LocalFolder;

            _downloadFolder = await storage.GetOrCreateSubfolderAsync("Downloads");

            var url = TxtUrl.Text;
            var webTask = new SiteGraph(url, _downloadFolder.Path);

            webTask.ObjectProcessed += WebTaskOnObjectProcessed;

            webTask.Run();
        }

        private void WebTaskOnObjectProcessed(object sender, SiteGraph.ObjectProcessedEventArgs args)
        {
            var hasUrl = args.Object as IHasUrl;
            var url = "";
            if (hasUrl != null)
            {
                url = hasUrl.Url;
            }
            var isAction = args.Object is BaseAction;
            var msg = isAction
                ? string.Format("Performing action with url '{0}'", url)
                : string.Format("Analyzing node with url '{0}'", url);
// ReSharper disable once PossibleNullReferenceException
            LstTasks.Items.Add(msg);
        }

        #endregion
    }
}
