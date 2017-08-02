using Redback.Helpers;
using System.IO;
using System.Threading.Tasks;

namespace Redback.WebGraph.Actions
{
    public abstract class FileDownloader : BaseDownloader
    {
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
    }
}
