using Windows.Storage;
using Windows.UI.Xaml;
using Redback.Helpers;
using Redback.WebGraph;

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

        private async void BtnGoOnClick(object sender, RoutedEventArgs e)
        {
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
