using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Redback.Helpers;
using Redback.WebGraph.Nodes;

namespace Redback.WebGraph.Actions
{
    public class FrameworkImplementedDownloader : FileDownloader
    {
        #region Properties

        public IStorageFile File
        {
            get; private set;
        }

        public DownloadOperation DownloadOperation
        {
            get; private set;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Performs the download
        /// </summary>
        /// <remarks>
        ///  So far this can't be used for retrieving page content directly
        /// </remarks>
        public async override Task Perform()
        {
            Uri.TryCreate(Url, UriKind.Absolute, out Uri uri);
            var folder = await LocalDirectory.GetOrCreateFolderAsync();
            File = await folder.GetOrCreateFileAsync(LocalFileName);

            var downloader = new BackgroundDownloader();
            await Task.Run(() => DownloadOperation = downloader.CreateDownload(uri, File));

            await DownloadOperation.StartAsync();

            // retrieves data if it's a page
            var resp = DownloadOperation.GetResponseInformation();
            var headers = resp.Headers;
            // contentType may be in such format as 'text/html; charset=utf-8'
            if (headers.TryGetValue("Content-Type", out string contentType) && contentType.Contains("text/html"))
            {
                // it's a page
                var s = await File.OpenStreamForReadAsync();
                using (var sr = new StreamReader(s))
                {
                    var page = sr.ReadToEnd();
                    TargetNode = new SimplePageParser((owner, source, level, url) =>
                        new FrameworkImplementedDownloader
                        {
                            Owner = owner,
                            SourceNode = source,
                            Level = level,
                            Url = url,
                        })
                    {
                        Owner = Owner,
                        Url = Url,
                        InducingAction = this,
                        Level = Level + 1,
                        Page = page
                    };
                    Owner.AddObject(TargetNode);
                }
            }
        }

        #endregion
    }
}
