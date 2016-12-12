using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextcloudClient.Types;
using Windows.Storage;
using NextcloudClient.Exceptions;
using NextcloudApp.Models;
using Windows.Storage.FileProperties;

namespace NextcloudApp.Services
{
    public class SyncService
    {
        private bool overrideLocally;
        private bool overrideRemotely;
        private FolderSyncInfo folderSyncInfo;

        public SyncService(bool overrideLocally, bool overrideRemotely, FolderSyncInfo syncInfo)
        {
            this.overrideLocally = overrideLocally;
            this.overrideRemotely = overrideRemotely;
            this.folderSyncInfo = syncInfo;
        }

        /// <summary>
        /// Initialize Synchronization
        /// </summary>
        /// <param name="resourceInfo">webdav Resource to sync</param>
        /// <param name="folder">Target folder</param>
        public async Task SyncFolder(ResourceInfo info, StorageFolder folder)
        {
            IReadOnlyList<StorageFile> localFiles = await folder.GetFilesAsync();
            IReadOnlyList<StorageFolder> localFolders = await folder.GetFoldersAsync();
            var client = ClientService.GetClient();
            if (client == null)
            {
                return;
            }
            List<ResourceInfo> list = null;
            try
            {
                list = await client.List(info.Path);
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
            }
            List<Task> syncTasks = new List<Task>();
            List<IStorageItem> synced = new List<IStorageItem>();
            if(list!=null && list.Count > 0)
            {
                foreach(ResourceInfo subInfo in list)
                {
                    if(subInfo.IsDirectory())
                    {
                        IEnumerable<StorageFolder> localFoldersWithName = localFolders.Where(f => f.Name.Equals(subInfo.Name));
                        StorageFolder subFolder = localFoldersWithName.FirstOrDefault();
                        if(subFolder == null)
                        {
                            // create locally or delete remotely?
                            subFolder = await folder.CreateFolderAsync(subInfo.Name);
                        }
                        synced.Add(subFolder);
                        syncTasks.Add(SyncFolder(subInfo, subFolder));
                        // Can localFoldersWithName be null?
                    } else
                    {
                        IEnumerable<StorageFile> localFilessWithName = localFiles.Where(f => f.Name.Equals(subInfo.Name));
                        // Can localFilessWithName be null?
                        StorageFile subFile = localFilessWithName.FirstOrDefault();
                        if (subFile == null)
                        {
                            // create locally or delete remotely?
                            subFile = await folder.CreateFileAsync(subInfo.Name);
                        }
                        synced.Add(subFile);
                        syncTasks.Add(SyncFile(subInfo, subFile));
                    }
                }
            } else
            {
                foreach(StorageFile file in localFiles)
                {
                    if(!synced.Contains(file))
                    {
                        // TODO Upload remotely or delete locally
                    }
                }
                foreach (StorageFolder localFolder in localFolders)
                {
                    if (!synced.Contains(localFolder))
                    {
                        // TODO Upload remotely or delete locally
                    }
                }
            }
            Task.WaitAll(syncTasks.ToArray());
        }

        private async Task SyncFile(ResourceInfo info, StorageFile file)
        {
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            DateTimeOffset currentModified = basicProperties.DateModified;
            string currentEtag = info.ETag;
            // TODO read SyncInfoDetail from DB and compare ETag and DateModified
        }
    }
}
