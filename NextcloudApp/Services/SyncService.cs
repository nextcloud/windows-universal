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
using System.Diagnostics;

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
            Debug.WriteLine("Sync folder " + info.Path + ":" + folder.Path);
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
            //List<Task> syncTasks = new List<Task>();
            List<IStorageItem> synced = new List<IStorageItem>();
            if(list!=null && list.Count > 0)
            {
                foreach(ResourceInfo subInfo in list)
                {
                    if(subInfo.IsDirectory())
                    {
                        IEnumerable<StorageFolder> localFoldersWithName = localFolders.Where(f => f.Name.Equals(subInfo.Name));
                        StorageFolder subFolder = localFoldersWithName.FirstOrDefault();
                        // Can localFoldersWithName be null?
                        if (subFolder == null)
                        {
                            var sid = SyncDbUtils.getSyncInfoDetail(subInfo, folderSyncInfo);
                            if (sid != null)
                            {
                                Debug.WriteLine("Sync folder (delete remotely) " + subInfo.Path);
                                if (await client.Delete(subInfo.Path))
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
                                Debug.WriteLine("Sync folder (create locally) " + subInfo.Path);
                                subFolder = await folder.CreateFolderAsync(subInfo.Name);
                                SyncInfoDetail syncInfoDetail = new SyncInfoDetail();
                                syncInfoDetail.Path = subInfo.Path;
                                syncInfoDetail.FilePath = subFolder.Path;
                                syncInfoDetail.FsiID = folderSyncInfo.Id;
                                SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                await SyncFolder(subInfo, subFolder);
                                // syncTasks.Add(SyncFolder(subInfo, subFolder));
                            }
                        } else {
                            synced.Add(subFolder);
                            await SyncFolder(subInfo, subFolder);
                            // syncTasks.Add(SyncFolder(subInfo, subFolder));
                        }
                    } else
                    {
                        IEnumerable<StorageFile> localFilessWithName = localFiles.Where(f => f.Name.Equals(subInfo.Name));
                        // Can localFilessWithName be null?
                        StorageFile subFile = localFilessWithName.FirstOrDefault();
                        if (subFile != null)
                        {
                            synced.Add(subFile);
                        }
                        await SyncFile(subInfo, subFile, info, folder);
                        //syncTasks.Add(SyncFile(subInfo, subFile, info, folder));
                    }
                }
            } 
            foreach(StorageFile file in localFiles)
            {
                if(!synced.Contains(file))
                {
                    await SyncFile(null, file, info, folder);
                    //syncTasks.Add(SyncFile(null, file, info, folder));
                }
            }
            foreach (StorageFolder localFolder in localFolders)
            {
                if (!synced.Contains(localFolder))
                {
                    var sid = SyncDbUtils.getSyncInfoDetail(localFolder, folderSyncInfo);
                    if (sid != null)
                    {
                        // Delete all sids and local folder
                        Debug.WriteLine("Sync folder (delete locally) " + localFolder.Path);
                        await localFolder.DeleteAsync();
                        SyncDbUtils.DeleteSyncInfoDetail(sid, true);
                    }
                    else
                    {
                        // Create sid and remotefolder
                        string newPath = info.Path + localFolder.Name;
                        Debug.WriteLine("Sync folder (create remotely) " + newPath);
                        if (await client.CreateDirectory(newPath))
                        {
                            ResourceInfo subInfo = await client.GetResourceInfo(info.Path, localFolder.Name);
                            SyncInfoDetail syncInfoDetail = new SyncInfoDetail();
                            syncInfoDetail.Path = subInfo.Path;
                            syncInfoDetail.FilePath = localFolder.Path;
                            syncInfoDetail.FsiID = folderSyncInfo.Id;
                            SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                            await SyncFolder(subInfo, localFolder);
                            //syncTasks.Add(SyncFolder(subInfo, localFolder));                                
                        }
                        else
                        {
                            // ERROR
                        }
                    }
                }
            }
            //Task.WaitAll(syncTasks.ToArray());
        }

        private async Task SyncFile(ResourceInfo info, StorageFile file, ResourceInfo parent, StorageFolder parentFolder)
        {
            SyncInfoDetail sid = null;
            if(info!=null)
            {
                Debug.WriteLine("Sync file " + info.Path + "/" + info.Name);
                sid = SyncDbUtils.getSyncInfoDetail(info, folderSyncInfo);
            } else if(file!=null)
            {
                Debug.WriteLine("Sync file " + file.Path);
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
                    string newPath = parent.Path + file.Name;
                    Debug.WriteLine("Sync file (Upload)" + newPath);
                    var _cts = new CancellationTokenSource();    
                    IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);

                    if (await client.Upload(newPath, await file.OpenReadAsync(), file.ContentType, _cts, progress))
                    {
                        ResourceInfo newInfo = await client.GetResourceInfo(parent.Path, file.Name);
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
                    Debug.WriteLine("Sync file (Download)" + localFile.Path);
                    var _cts = new CancellationTokenSource();
                    IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);
                    CachedFileManager.DeferUpdates(localFile);
                    IBuffer buffer = await client.Download(info.Path + "/" + info.Name, _cts, progress);
                    await FileIO.WriteBufferAsync(localFile, buffer);
                    var status = await CachedFileManager.CompleteUpdatesAsync(localFile);
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
                        Debug.WriteLine("Sync file (Delete locally)" + file.Path);
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
                        Debug.WriteLine("Sync file (Delete remotely) " + info.Path + "/" + info.Name);
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
                            Debug.WriteLine("Sync file (update locally) " + info.Path + "/" + info.Name);
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
                        Debug.WriteLine("Sync file (update remotely) " + info.Path + "/" + info.Name);
                        var _cts = new CancellationTokenSource();
                        IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);

                        if (await client.Upload(info.Path + "/" + info.Name, await file.OpenReadAsync(), file.ContentType, _cts, progress))
                        {
                            ResourceInfo newInfo = await client.GetResourceInfo(info.Path, info.Name);
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
