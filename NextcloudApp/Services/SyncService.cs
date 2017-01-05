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
        private FolderSyncInfo folderSyncInfo;
        private StorageFolder baseFolder;
        private ResourceInfo resourceInfo;
        private NextcloudClient.NextcloudClient client;
        private List<SyncInfoDetail> sidList;
        
        public SyncService(StorageFolder startFolder, ResourceInfo resourceInfo, FolderSyncInfo syncInfo)
        {
            this.baseFolder = startFolder;
            this.folderSyncInfo = syncInfo;
            this.resourceInfo = resourceInfo;
            sidList = new List<SyncInfoDetail>();
            client = ClientService.GetClient();
            if (client == null)
            {
                // ERROR
                throw new Exception("Error creating webdav client");
            }
        }

        public async Task<bool> StartSync()
        {
            bool success = true;            
            List<SyncInfoDetail> oldList = SyncDbUtils.GetAllSyncInfoDetails(folderSyncInfo);            
            Debug.WriteLine("Sid List before Sync: ");
            foreach (SyncInfoDetail detail in oldList)
            {
                Debug.WriteLine("Detail: " + detail.ToString());
            }
            SyncInfoDetail sid = SyncDbUtils.GetSyncInfoDetail(resourceInfo, folderSyncInfo);
            try
            {
                if (sid == null)
                {
                    sid = new SyncInfoDetail(folderSyncInfo);
                    sid.Path = resourceInfo.Path;
                    sid.FilePath = baseFolder.Path;
                    SyncDbUtils.SaveSyncInfoDetail(sid);
                }
                else
                {
                    sidList.Remove(sid);
                    sid.Error = null;
                }
                success = await SyncFolder(resourceInfo, baseFolder);
                foreach (SyncInfoDetail detail in oldList)
                {
                    if(!sidList.Contains(detail))
                    {
                        // The items left here must be deleted both remotely and locally so the sid is obsolete.
                        SyncDbUtils.DeleteSyncInfoDetail(detail, false);
                    }
                }
            } catch (Exception e)
            {
                sid.Error = e.Message;
                success = false;
            }
            SyncDbUtils.SaveSyncInfoDetail(sid);
            List<SyncInfoDetail> newSidList = SyncDbUtils.GetAllSyncInfoDetails(folderSyncInfo);
            Debug.WriteLine("Sid List after Sync: ");
            foreach (SyncInfoDetail detail in newSidList)
            {
                Debug.WriteLine("Detail: " + detail.ToString());
            }
            return success;
        }


        /// <summary>
        /// Folder Synchronization
        /// </summary>
        /// <param name="resourceInfo">webdav Resource to sync</param>
        /// <param name="folder">Target folder</param>
        private async Task<bool> SyncFolder(ResourceInfo info, StorageFolder folder)
        {
            SyncInfoDetail sid = SyncDbUtils.GetSyncInfoDetail(info, folderSyncInfo);
            sid.Error = null;
            bool success = true;
            try
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
                if (list != null && list.Count > 0)
                {
                    foreach (ResourceInfo subInfo in list)
                    {
                        if (subInfo.IsDirectory())
                        {
                            IEnumerable<StorageFolder> localFoldersWithName = localFolders.Where(f => f.Name.Equals(subInfo.Name));
                            StorageFolder subFolder = localFoldersWithName.FirstOrDefault();
                            // Can localFoldersWithName be null?
                            if (subFolder == null)
                            {
                                var subSid = SyncDbUtils.GetSyncInfoDetail(subInfo, folderSyncInfo);
                                if (subSid != null)
                                {
                                    Debug.WriteLine("Sync folder (delete remotely) " + subInfo.Path);
                                    if (await client.Delete(subInfo.Path))
                                    {
                                        SyncDbUtils.DeleteSyncInfoDetail(subSid, true);
                                    }
                                    else
                                    {
                                        sid.Error = "Deletion of " + subInfo.Path + " on nextcloud failed.";
                                        // Error could be overridden by other errors
                                        success = false;
                                    }
                                }
                                else
                                {
                                    // Create sid and local folder
                                    Debug.WriteLine("Sync folder (create locally) " + subInfo.Path);
                                    subFolder = await folder.CreateFolderAsync(subInfo.Name);
                                    SyncInfoDetail syncInfoDetail = new SyncInfoDetail(folderSyncInfo);
                                    syncInfoDetail.Path = subInfo.Path;
                                    syncInfoDetail.FilePath = subFolder.Path;
                                    SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                    success = success && await SyncFolder(subInfo, subFolder);
                                    // syncTasks.Add(SyncFolder(subInfo, subFolder));
                                }
                            }
                            else
                            {
                                var subSid = SyncDbUtils.GetSyncInfoDetail(subInfo, folderSyncInfo);
                                if(subSid == null)
                                {
                                    // Both new
                                    Debug.WriteLine("Sync folder (create both) " + subInfo.Path);
                                    SyncInfoDetail syncInfoDetail = new SyncInfoDetail(folderSyncInfo);
                                    syncInfoDetail.Path = subInfo.Path;
                                    syncInfoDetail.FilePath = subFolder.Path;
                                    SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                }
                                synced.Add(subFolder);
                                success = success && await SyncFolder(subInfo, subFolder);
                                // syncTasks.Add(SyncFolder(subInfo, subFolder));
                            }
                        }
                        else
                        {
                            IEnumerable<StorageFile> localFilessWithName = localFiles.Where(f => f.Name.Equals(subInfo.Name));
                            // Can localFilessWithName be null?
                            StorageFile subFile = localFilessWithName.FirstOrDefault();
                            if (subFile != null)
                            {
                                synced.Add(subFile);
                            }
                            success = success && await SyncFile(subInfo, subFile, info, folder);
                            //syncTasks.Add(SyncFile(subInfo, subFile, info, folder));
                        }
                    }
                }
                foreach (StorageFile file in localFiles)
                {
                    if (!synced.Contains(file))
                    {
                        success = success && await SyncFile(null, file, info, folder);
                        //syncTasks.Add(SyncFile(null, file, info, folder));
                    }
                }
                foreach (StorageFolder localFolder in localFolders)
                {
                    if (!synced.Contains(localFolder))
                    {
                        var subSid = SyncDbUtils.GetSyncInfoDetail(localFolder, folderSyncInfo);
                        if (subSid != null)
                        {
                            // Delete all sids and local folder
                            Debug.WriteLine("Sync folder (delete locally) " + localFolder.Path);
                            await localFolder.DeleteAsync();
                            SyncDbUtils.DeleteSyncInfoDetail(subSid, true);
                        }
                        else
                        {
                            // Create sid and remotefolder
                            string newPath = info.Path + localFolder.Name;
                            Debug.WriteLine("Sync folder (create remotely) " + newPath);
                            if (await client.CreateDirectory(newPath))
                            {
                                ResourceInfo subInfo = await client.GetResourceInfo(info.Path, localFolder.Name);
                                SyncInfoDetail syncInfoDetail = new SyncInfoDetail(folderSyncInfo);
                                syncInfoDetail.Path = subInfo.Path;
                                syncInfoDetail.FilePath = localFolder.Path;
                                SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                success = success && await SyncFolder(subInfo, localFolder);
                                //syncTasks.Add(SyncFolder(subInfo, localFolder));                                
                            }
                            else
                            {
                                sid.Error = "Could not create directory on nextcloud: " + newPath;
                                success = false;
                            }
                        }
                    }
                }
                //Task.WaitAll(syncTasks.ToArray());
            }
            catch (Exception e)
            {
                sid.Error = e.Message;
                success = false;
            }
            sidList.Add(sid);
            SyncDbUtils.SaveSyncInfoDetail(sid);
            return success;
        }

        private async Task<bool> SyncFile(ResourceInfo info, StorageFile file, ResourceInfo parent, StorageFolder parentFolder)
        {
            SyncInfoDetail sid = null;
            bool success = true;
            if (info != null)
            {
                sid = SyncDbUtils.GetSyncInfoDetail(info, folderSyncInfo);
            } else if (file != null)
            {
                sid = SyncDbUtils.GetSyncInfoDetail(file, folderSyncInfo);
            } 
            if (sid == null)
            {
                sid = new SyncInfoDetail(folderSyncInfo);
            }
            sid.Error = null;
            try {
                DateTimeOffset currentModified;
                if(file != null)
                {
                    BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
                    currentModified = basicProperties.DateModified;
                }

                if (sid.Path == null || sid.FilePath == null)
                {
                    if (file != null && info != null)
                    {
                        sid.Path = info.Path + "/" + info.Name;
                        sid.FilePath = file.Path;
                        sid.ETag = info.ETag;
                        sid.DateModified = currentModified;
                        sid.Error = "File has been created remotely and locally - which is the correct one?";
                        Debug.WriteLine("Sync file: Conflict (both new) " + info.Path + "/" + info.Name);
                    } else if (file != null)
                    {
                        // Create sid and upload file
                        string newPath = parent.Path + file.Name;
                        Debug.WriteLine("Sync file (Upload)" + newPath);
                        var _cts = new CancellationTokenSource();
                        IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);
                        sid.DateModified = currentModified;
                        sid.FilePath = file.Path;
                        if (await client.Upload(newPath, await file.OpenReadAsync(), file.ContentType, _cts, progress))
                        {
                            ResourceInfo newInfo = await client.GetResourceInfo(parent.Path, file.Name);
                            sid.Path = newInfo.Path + "/" + newInfo.Name;
                            sid.ETag = newInfo.ETag;
                        } else
                        {
                            sid.Error = "Error while uploading File to nextcloud.";
                        }
                    }
                    else if (info != null)
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
                        currentModified = basicProperties.DateModified;
                        sid.Path = info.Path + "/" + info.Name;
                        sid.ETag = info.ETag;
                        sid.DateModified = currentModified;
                        sid.FilePath = localFile.Path;
                    }
                } else
                {
                    if (info == null)
                    {
                        if (sid.DateModified.Equals(currentModified))
                        {
                            Debug.WriteLine("Sync file (Delete locally) " + sid.Path);
                            // Remove sid and local file
                            await file.DeleteAsync();
                            SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                        } else
                        {
                            Debug.WriteLine("Sync file: Conflict (Deleted remotely, but changed locally) " + sid.Path);
                            sid.Error = "Conflict: Deleted file remotely but changed locally. Which do you prefer?";
                        }
                    } else if (file == null)
                    {
                        if (info.ETag.Equals(sid.ETag))
                        {
                            Debug.WriteLine("Sync file (Delete remotely) " + sid.Path);
                            // Remove sid and remote file
                            await client.Delete(info.Path + "/" + info.Name);
                            SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                        }
                        else
                        {
                            // Conflict
                            Debug.WriteLine("Sync file: Conflict (Deleted locally, but changed remotely) " + sid.Path);
                            sid.Error = "Conflict: Deleted file locally but changed remotely. Which do you prefer?";
                        }
                    } else
                    {
                        if (currentModified.Equals(sid.DateModified))
                        {
                            if (!info.ETag.Equals(sid.ETag))
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
                            }
                        } else if (info.ETag.Equals(sid.ETag))
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
                            }
                            else
                            {
                                sid.Error = "Error while uploading file to nextcloud";
                            }
                        } else
                        {
                            Debug.WriteLine("Sync file: Conflict (Changed remotely and locally) " + sid.Path);
                            sid.Error = "Conflict: File changed locally and remotely. Which do you prefer?";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                sid.Error = e.Message;
                success = false;
            }            
            Debug.WriteLine("Synced file " + sid.ToString());
            sidList.Add(sid);
            SyncDbUtils.SaveSyncInfoDetail(sid);
            return success;
        }
                       
        private void ProgressHandler(HttpProgress progressInfo)
        {
            
        }
    }
}
