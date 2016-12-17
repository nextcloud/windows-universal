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
using NextcloudApp.Utils;
using NextcloudClient;
using System.Threading;
using Windows.Web.Http;
using Windows.Storage.Streams;

namespace NextcloudApp.Services
{
    public class SyncService
    {
        private bool overrideLocally;
        private bool overrideRemotely;
        private FolderSyncInfo folderSyncInfo;
        private NextcloudClient.NextcloudClient client;


        public SyncService(bool overrideLocally, bool overrideRemotely, FolderSyncInfo syncInfo)
        {
            this.overrideLocally = overrideLocally;
            this.overrideRemotely = overrideRemotely;
            this.folderSyncInfo = syncInfo;
            client = ClientService.GetClient();
            if (client == null)
            {
                // ERROR
                throw new Exception("Error creating webdav client");
            }
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
                            var sid = SyncDbUtils.getSyncInfoDetail(subInfo, folderSyncInfo);
                            if (sid != null)
                            {
                                if(await client.Delete(subInfo.Path))
                                {
                                    SyncDbUtils.DeleteSyncInfoDetail(sid, true);
                                }
                                else
                                {
                                    // Error
                                }
                            }
                            else
                            {
                                // Create sid and local folder
                                subFolder = await folder.CreateFolderAsync(subInfo.Name);
                                SyncInfoDetail syncInfoDetail = new SyncInfoDetail();
                                syncInfoDetail.Path = subInfo.Path;
                                syncInfoDetail.FilePath = subFolder.Path;
                                syncInfoDetail.FsiID = folderSyncInfo.Id;
                                SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                syncTasks.Add(SyncFolder(subInfo, subFolder));
                            }
                        }
                        synced.Add(subFolder);
                        syncTasks.Add(SyncFolder(subInfo, subFolder));
                        // Can localFoldersWithName be null?
                    } else
                    {
                        IEnumerable<StorageFile> localFilessWithName = localFiles.Where(f => f.Name.Equals(subInfo.Name));
                        // Can localFilessWithName be null?
                        StorageFile subFile = localFilessWithName.FirstOrDefault();
                        if (subFile != null)
                        {
                            synced.Add(subFile);
                        }
                        syncTasks.Add(SyncFile(subInfo, subFile, info, folder));
                    }
                }
            } else
            {
                foreach(StorageFile file in localFiles)
                {
                    if(!synced.Contains(file))
                    {
                        syncTasks.Add(SyncFile(null, file, info, folder));
                    }
                }
                foreach (StorageFolder localFolder in localFolders)
                {
                    if (!synced.Contains(localFolder))
                    {
                        var sid = SyncDbUtils.getSyncInfoDetail(localFolder, folderSyncInfo);
                        if(sid!=null)
                        {
                            // Delete all sids and local folder
                            await localFolder.DeleteAsync();
                            SyncDbUtils.DeleteSyncInfoDetail(sid, true);
                        } else
                        {
                            // Create sid and remotefolder
                            string newPath = info.Path + "/" + localFolder.Name;
                            if (await client.CreateDirectory(newPath))
                            {
                                ResourceInfo subInfo = await client.GetResourceInfo(newPath);
                                SyncInfoDetail syncInfoDetail = new SyncInfoDetail();
                                syncInfoDetail.Path = subInfo.Path;
                                syncInfoDetail.FilePath = localFolder.Path;
                                syncInfoDetail.FsiID = folderSyncInfo.Id;
                                SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                syncTasks.Add(SyncFolder(null, localFolder));
                            } else
                            {
                                // ERROR
                            }
                        }
                    }
                }
            }
            Task.WaitAll(syncTasks.ToArray());
        }

        private async Task SyncFile(ResourceInfo info, StorageFile file, ResourceInfo parent, StorageFolder parentFolder)
        {
            SyncInfoDetail sid = null;
            if(info!=null)
            {
                sid = SyncDbUtils.getSyncInfoDetail(info, folderSyncInfo);
            } else if(file!=null)
            {
                sid = SyncDbUtils.getSyncInfoDetail(file, folderSyncInfo);
            }
            if(sid==null)
            {
                if(file != null && info!=null)
                {
                    // Conflict
                } else if(file!=null)
                {
                    // Create sid and upload file
                    BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
                    DateTimeOffset currentModified = basicProperties.DateModified;
                    string newPath = info.Path + file.Name;
                    var _cts = new CancellationTokenSource();    
                    IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);

                    if (await client.Upload(newPath, await file.OpenReadAsync(), file.ContentType, _cts, progress))
                    {
                        ResourceInfo newInfo = await client.GetResourceInfo(newPath);
                        SyncInfoDetail syncInfoDetail = new SyncInfoDetail();
                        syncInfoDetail.Path = newInfo.Path;
                        syncInfoDetail.ETag = newInfo.ETag;
                        syncInfoDetail.DateModified = currentModified;
                        syncInfoDetail.FilePath = file.Path;
                        syncInfoDetail.FsiID = folderSyncInfo.Id;
                        SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                    } else
                    {
                        // ERROR
                    }
                }
                else if(info!=null)
                {
                    // Create sid and download file
                    StorageFile localFile = await parentFolder.CreateFileAsync(info.Name);
                    var _cts = new CancellationTokenSource();
                    IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);
                    IBuffer buffer = await client.Download(info.Path + "/" + info.Name, _cts, progress);
                    await localFile.OpenAsync(FileAccessMode.ReadWrite);
                    await FileIO.WriteBufferAsync(localFile, buffer);
                    BasicProperties basicProperties = await localFile.GetBasicPropertiesAsync();
                    DateTimeOffset currentModified = basicProperties.DateModified;
                    SyncInfoDetail syncInfoDetail = new SyncInfoDetail();
                    syncInfoDetail.Path = info.Path;
                    syncInfoDetail.ETag = info.ETag;
                    syncInfoDetail.DateModified = currentModified;
                    syncInfoDetail.FilePath = localFile.Path;
                    syncInfoDetail.FsiID = folderSyncInfo.Id;
                    SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                }
                
            } else
            {
                if (info == null)
                {
                    BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
                    DateTimeOffset currentModified = basicProperties.DateModified;
                    if (sid.DateModified.Equals(currentModified))
                    {
                        // Remove sid and local file
                        await file.DeleteAsync();
                        SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                    } else
                    {
                        // Conflict
                    }
                } else if(file == null)
                {
                    if(info.ETag.Equals(sid.ETag))
                    {
                        // Remove sid and remote file
                        await client.Delete(info.Path + "/" + info.Name);
                        SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                    }
                    else
                    {
                        // Conflict
                    }
                } else
                {
                    BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
                    DateTimeOffset currentModified = basicProperties.DateModified;
                    if (currentModified.Equals(sid.DateModified))
                    {
                        if(!info.ETag.Equals(sid.ETag))
                        {
                            // Update local file
                            var _cts = new CancellationTokenSource();
                            IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);
                            IBuffer buffer = await client.Download(info.Path + "/" + info.Name, _cts, progress);
                            await file.OpenAsync(FileAccessMode.ReadWrite);
                            await FileIO.WriteBufferAsync(file, buffer);
                            sid.ETag = info.ETag;
                            sid.DateModified = currentModified;
                            SyncDbUtils.SaveSyncInfoDetail(sid);
                        }
                    } else if(info.ETag.Equals(sid.ETag))
                    {
                        // update file on nextcloud
                        var _cts = new CancellationTokenSource();
                        IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);

                        if (await client.Upload(info.Path + "/" + info.Name, await file.OpenReadAsync(), file.ContentType, _cts, progress))
                        {
                            ResourceInfo newInfo = await client.GetResourceInfo(info.Path + "/" + info.Name);
                            sid.ETag = newInfo.ETag;
                            sid.DateModified = currentModified;
                            SyncDbUtils.SaveSyncInfoDetail(sid);
                        }
                        else
                        {
                            // ERROR
                        }
                    } else
                    {
                        // Conflict
                    }
                }
            }
        }
        private void ProgressHandler(HttpProgress progressInfo)
        {
            
        }
    }
}
