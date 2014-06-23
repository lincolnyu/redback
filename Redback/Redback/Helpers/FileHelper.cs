using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Redback.Helpers
{
    public static class FileHelper
    {
        #region Methods

        public static async Task<IStorageFolder> GetOrCreateSubfolderAsync(this IStorageFolder folder, string subfolderName)
        {
            IStorageFolder subfolder;

            try
            {
                subfolder = await folder.GetFolderAsync(subfolderName);
            }
            catch (FileNotFoundException)
            {
                subfolder = null;
            }

            return subfolder ?? (await folder.CreateFolderAsync(subfolderName));
        }

        public static async Task<IStorageFile> CreateNewFileAsync(this IStorageFolder folder, string fileName)
        {
            try
            {
                var file = await folder.GetFileAsync(fileName);
                await file.DeleteAsync();
            }
            catch (FileNotFoundException)
            {
            }

            return await folder.CreateFileAsync(fileName);
        }

        public static async Task<IStorageFile> GetOrCreateFileAsync(this IStorageFolder folder, string fileName)
        {
            IStorageFile file;

            try
            {
                file = await folder.GetFileAsync(fileName);
            }
            catch (FileNotFoundException)
            {
                file = null;
            }

            return file ?? (await folder.CreateFileAsync(fileName));
        }

        /// <summary>
        ///  Gets or creates folder at the specified path which is guarateed by the caller to be within a accessible location
        /// </summary>
        /// <param name="path">The path where the folder is to be created</param>
        /// <returns>The folder created at the specified path</returns>
        public static async Task<IStorageFolder> GetOrCreateFolderAsync(this string path)
        {
            path = path.TrimEnd('\\');
            var segs = path.Split('\\');

            var sbPath = new StringBuilder();
            IStorageFolder folder = null;

            var folderGot = false;
            foreach (var seg in segs)
            {
                if (!folderGot)
                {
                    sbPath.Append(seg);
                    sbPath.Append('\\');
                    var p = sbPath.ToString();

                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(p);
                        folderGot = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        folder = null;
                    }
                }
                else
                {
                    folder = await folder.GetOrCreateSubfolderAsync(seg);
                }
            }
            return folder;
        }

        public static async Task DeleteIfExists(this IStorageFile f)
        {
            try
            {
                await f.DeleteAsync();
            }
            catch (FileNotFoundException)
            {                
            }
        }

        #endregion
    }
}
