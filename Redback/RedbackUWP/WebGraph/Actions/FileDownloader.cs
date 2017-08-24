using System.IO;
using System.Threading.Tasks;
using Redback.Helpers;
using System.Collections.Generic;

namespace Redback.WebGraph.Actions
{
    public abstract class FileDownloader : BaseDownloader
    {
        public delegate byte[] GetDataDelegate();

        public string LocalDirectory { get; set; }

        public string LocalFileName { get; set; }

        public override async Task SaveAsync(string content)
        {
            var folder = await LocalDirectory.GetOrCreateFolderAsync();
            var file = await folder.CreateNewFileAsync(LocalFileName);
            using (var fs = await file.OpenStreamForWriteAsync())
            {
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(content);
                }
            }
        }

        protected async Task SaveDataAsync(byte[] data, int start = 0, int? len = null)
        {
            var folder = await LocalDirectory.GetOrCreateFolderAsync();
            var file = await folder.CreateNewFileAsync(LocalFileName);
            using (var outputStream = await file.OpenStreamForWriteAsync())
            {
                if (len == null)
                {
                    len = data.Length - start;
                }
                await outputStream.WriteAsync(data, start, len.Value);
                await outputStream.FlushAsync();
            }
        }

        protected async Task SaveDataAsync(GetDataDelegate getdata)
        {
            var folder = await LocalDirectory.GetOrCreateFolderAsync();
            var file = await folder.CreateNewFileAsync(LocalFileName);
            using (var outputStream = await file.OpenStreamForWriteAsync())
            {
                while (true)
                {
                    var data = getdata();
                    if (data == null || data.Length == 0)
                    {
                        break;
                    }
                    await outputStream.WriteAsync(data, 0, data.Length);
                }
                await outputStream.FlushAsync();
            }
        }
    }
}
