using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using NextcloudClient.Types;
using NextcloudClient.Exceptions;
using NextcloudApp.Models;
using NextcloudApp.Utils;
using System.Threading;
using System.Diagnostics;
using DecaTec.WebDav;
using Windows.Storage;
using NextcloudApp.Constants;
using Prism.Windows.AppModel;

namespace NextcloudApp.Services
{
    public class SyncService
    {
        private readonly FolderSyncInfo _folderSyncInfo;
        private readonly StorageFolder _baseFolder;
        private readonly ResourceInfo _resourceInfo;
        private NextcloudClient.NextcloudClient _client;
        private readonly List<SyncInfoDetail> _sidList;
        private readonly IResourceLoader _resourceLoader;

        public SyncService(StorageFolder startFolder, ResourceInfo resourceInfo, FolderSyncInfo syncInfo, IResourceLoader resourceLoader)
        {
            _baseFolder = startFolder;
            _folderSyncInfo = syncInfo;
            _resourceInfo = resourceInfo;
            _resourceLoader = resourceLoader;
            _sidList = new List<SyncInfoDetail>();
        }

        public async Task<bool> StartSync()
        {
            if (!SyncDbUtils.LockFolderSyncInfo(_folderSyncInfo))
            {
                return false;
            }

            try
            {
                _client = await ClientService.GetClient();

                if (_client == null)
                {
                    // ERROR
                    throw new NullReferenceException(_resourceLoader.GetString(ResourceConstants.SyncService_Error_CannotCreateClient));
                }

                var changedCount = 0;
                var oldList = SyncDbUtils.GetAllSyncInfoDetails(_folderSyncInfo);
                Debug.WriteLine("Sid List before Sync: ");

                foreach (var detail in oldList)
                {
                    Debug.WriteLine("Detail: " + detail);
                }

                var sid = SyncDbUtils.GetSyncInfoDetail(_resourceInfo, _folderSyncInfo);
                var errorCount = 0;

                try
                {
                    if (sid == null)
                    {
                        sid = new SyncInfoDetail(_folderSyncInfo)
                        {
                            Path = _resourceInfo.Path,
                            FilePath = _baseFolder.Path,
                        };

                        SyncDbUtils.SaveSyncInfoDetail(sid);
                    }
                    else
                    {
                        _sidList.Remove(sid);
                        sid.Error = null;
                    }

                    changedCount = await SyncFolder(_resourceInfo, _baseFolder);

                    foreach (var detail in oldList)
                    {
                        if (_sidList.Contains(detail) || !detail.FilePath.StartsWith(_baseFolder.Path))
                        {
                            continue;
                        }
                        // The items left here must have been deleted both remotely and locally so the sid is obsolete.
                        SyncDbUtils.DeleteSyncInfoDetail(detail, false);
                        changedCount++;
                    }
                    errorCount = SyncDbUtils.GetErrorConflictCount(_folderSyncInfo);
                }
                catch (Exception e)
                {
                    if (sid != null)
                    {
                        sid.Error = e.Message;
                    }
                    errorCount++;
                }

                SyncDbUtils.SaveSyncInfoDetail(sid);
                var newSidList = SyncDbUtils.GetAllSyncInfoDetails(_folderSyncInfo);
                Debug.WriteLine("Sid List after Sync: ");

                foreach (var detail in newSidList)
                {
                    Debug.WriteLine("Detail: " + detail);
                }

                ToastNotificationService.ShowSyncFinishedNotification(_folderSyncInfo.Path, changedCount, errorCount);
                return errorCount == 0;
            }
            finally
            {
                SyncDbUtils.UnlockFolderSyncInfo(_folderSyncInfo);
            }
        }

        /// <summary>
        /// Folder Synchronization
        /// </summary>
        /// <param name="resourceInfo">webdav Resource to sync</param>
        /// <param name="folder">Target folder</param>
        private async Task<int> SyncFolder(ResourceInfo resourceInfo, StorageFolder folder)
        {
            var sid = SyncDbUtils.GetSyncInfoDetail(resourceInfo, _folderSyncInfo);
            sid.Error = null;
            var changesCount = 0;
            try
            {
                Debug.WriteLine("Sync folder " + resourceInfo.Path + ":" + folder.Path);
                var localFiles = await folder.GetFilesAsync();
                var localFolders = await folder.GetFoldersAsync();

                List<ResourceInfo> list = null;
                try
                {
                    list = await _client.List(resourceInfo.Path);
                }
                catch (ResponseError e)
                {
                    ResponseErrorHandlerService.HandleException(e);
                }

                var synced = new List<IStorageItem>();

                if (list != null && list.Count > 0)
                {
                    foreach (var subInfo in list)
                    {
                        if (subInfo.IsDirectory)
                        {
                            var localFoldersWithName = localFolders.Where(f => f.Name.Equals(subInfo.Name));
                            var subFolder = localFoldersWithName.FirstOrDefault();
                            // Can localFoldersWithName be null?
                            if (subFolder == null)
                            {
                                var subSid = SyncDbUtils.GetSyncInfoDetail(subInfo, _folderSyncInfo);
                                if (subSid != null)
                                {
                                    Debug.WriteLine("Sync folder (delete remotely) " + subInfo.Path);
                                    if (await _client.Delete(subInfo.Path))
                                    {
                                        SyncDbUtils.DeleteSyncInfoDetail(subSid, true);
                                    }
                                    else
                                    {
                                        sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_DeleteFolderRemotely), subInfo.Path);
                                        // Error could be overridden by other errors
                                    }
                                }
                                else
                                {
                                    // Create sid and local folder
                                    Debug.WriteLine("Sync folder (create locally) " + subInfo.Path);
                                    subFolder = await folder.CreateFolderAsync(subInfo.Name);

                                    var syncInfoDetail = new SyncInfoDetail(_folderSyncInfo)
                                    {
                                        Path = subInfo.Path,
                                        FilePath = subFolder.Path
                                    };

                                    SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                    changesCount = changesCount + await SyncFolder(subInfo, subFolder);
                                    // syncTasks.Add(SyncFolder(subInfo, subFolder));
                                }
                            }
                            else
                            {
                                var subSid = SyncDbUtils.GetSyncInfoDetail(subInfo, _folderSyncInfo);
                                if (subSid == null)
                                {
                                    // Both new
                                    Debug.WriteLine("Sync folder (create both) " + subInfo.Path);

                                    var syncInfoDetail = new SyncInfoDetail(_folderSyncInfo)
                                    {
                                        Path = subInfo.Path,
                                        FilePath = subFolder.Path
                                    };

                                    SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                                }
                                synced.Add(subFolder);
                                changesCount = changesCount + await SyncFolder(subInfo, subFolder);
                                // syncTasks.Add(SyncFolder(subInfo, subFolder));
                            }
                        }
                        else
                        {
                            var localFilessWithName = localFiles.Where(f => f.Name.Equals(subInfo.Name));
                            // Can localFilessWithName be null?
                            var subFile = localFilessWithName.FirstOrDefault();
                            if (subFile != null)
                            {
                                synced.Add(subFile);
                            }
                            changesCount = changesCount + await SyncFile(subInfo, subFile, resourceInfo, folder);
                            //syncTasks.Add(SyncFile(subInfo, subFile, info, folder));
                        }
                    }
                }
                foreach (var file in localFiles)
                {
                    if (!synced.Contains(file))
                    {
                        changesCount = changesCount + await SyncFile(null, file, resourceInfo, folder);
                        //syncTasks.Add(SyncFile(null, file, info, folder));
                    }
                }
                foreach (var localFolder in localFolders)
                {
                    if (synced.Contains(localFolder))
                    {
                        continue;
                    }
                    var subSid = SyncDbUtils.GetSyncInfoDetail(localFolder, _folderSyncInfo);
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
                        var newPath = resourceInfo.Path + localFolder.Name;
                        Debug.WriteLine("Sync folder (create remotely) " + newPath);

                        if (await _client.CreateDirectory(newPath))
                        {
                            var subInfo = await _client.GetResourceInfo(resourceInfo.Path, localFolder.Name);

                            var syncInfoDetail = new SyncInfoDetail(_folderSyncInfo)
                            {
                                Path = subInfo.Path,
                                FilePath = localFolder.Path
                            };

                            SyncDbUtils.SaveSyncInfoDetail(syncInfoDetail);
                            changesCount = changesCount + await SyncFolder(subInfo, localFolder);
                            //syncTasks.Add(SyncFolder(subInfo, localFolder));                                
                        }
                        else
                        {
                            sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_CreateFolderRemotely), newPath);
                        }
                    }
                }
                //Task.WaitAll(syncTasks.ToArray());
            }
            catch (Exception e)
            {
                sid.Error = e.Message;
            }
            _sidList.Add(sid);
            SyncDbUtils.SaveSyncInfoDetail(sid);
            SyncDbUtils.SaveSyncHistory(sid);
            return changesCount;
        }

        private async Task<int> SyncFile(ResourceInfo info, StorageFile file, ResourceInfo parent, StorageFolder parentFolder)
        {
            SyncInfoDetail sid = null;
            var changed = false;
            var deleted = false;
            if (info != null)
            {
                sid = SyncDbUtils.GetSyncInfoDetail(info, _folderSyncInfo);
            }
            else if (file != null)
            {
                sid = SyncDbUtils.GetSyncInfoDetail(file, _folderSyncInfo);
            }
            if (sid == null)
            {
                sid = new SyncInfoDetail(_folderSyncInfo);
            }
            sid.Error = null;
            sid.ConflictType = ConflictType.None;

            try
            {
                DateTimeOffset currentModified;
                if (file != null)
                {
                    var basicProperties = await file.GetBasicPropertiesAsync();
                    currentModified = basicProperties.DateModified;
                }

                if (sid.Path == null && sid.FilePath == null)
                {
                    if (file != null && info != null)
                    {
                        sid.Path = info.Path + "/" + info.Name;
                        sid.FilePath = file.Path;
                        sid.ConflictType = ConflictType.BothNew;
                        Debug.WriteLine("Sync file: Conflict (both new) " + info.Path + "/" + info.Name);
                    }
                    else if (file != null)
                    {
                        // Create sid and upload file
                        var newPath = parent.Path + file.Name;
                        Debug.WriteLine("Sync file (Upload)" + newPath);
                        sid.DateModified = currentModified;
                        sid.FilePath = file.Path;
                        if (await UploadFile(file, newPath))
                        {
                            var newInfo = await _client.GetResourceInfo(parent.Path, file.Name);
                            sid.Path = newInfo.Path + "/" + newInfo.Name;
                            sid.ETag = newInfo.ETag;
                            changed = true;
                        }
                        else
                        {
                            sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_UploadFile), file.Name);
                        }
                    }
                    else if (info != null)
                    {
                        // Create sid and download file
                        var localFile = await parentFolder.CreateFileAsync(info.Name);
                        Debug.WriteLine("Sync file (Download)" + localFile.Path);
                        if (await DownloadFile(localFile, info.Path + "/" + info.Name))
                        {
                            var basicProperties = await localFile.GetBasicPropertiesAsync();
                            currentModified = basicProperties.DateModified;
                            sid.Path = info.Path + "/" + info.Name;
                            sid.ETag = info.ETag;
                            sid.DateModified = currentModified;
                            sid.FilePath = localFile.Path;
                            changed = true;
                        }
                        else
                        {
                            sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_DownloadFile), info.Name);
                        }
                    }
                }
                else
                {
                    if (info == null)
                    {
                        if (sid.DateModified != null && sid.DateModified.Value.Equals(currentModified))
                        {
                            Debug.WriteLine("Sync file (Delete locally) " + sid.Path);
                            // Remove sid and local file
                            if (file != null)
                            {
                                await file.DeleteAsync();
                            }
                            SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                            changed = true;
                            deleted = true;
                        }
                        else
                        {
                            switch (sid.ConflictSolution)
                            {
                                case ConflictSolution.PreferLocal:
                                    if (file != null)
                                    {
                                        var newPath = parent.Path + file.Name;
                                        Debug.WriteLine("Sync file (Upload)" + newPath);
                                        sid.DateModified = currentModified;
                                        sid.FilePath = file.Path;
                                        if (await UploadFile(file, newPath))
                                        {
                                            var newInfo = await _client.GetResourceInfo(parent.Path, file.Name);
                                            sid.Path = newInfo.Path + "/" + newInfo.Name;
                                            sid.ETag = newInfo.ETag;
                                            sid.ConflictSolution = ConflictSolution.None;
                                            changed = true;
                                        }
                                        else
                                        {
                                            sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_UploadFile), file.Name);
                                        }
                                    }
                                    break;
                                case ConflictSolution.PreferRemote:
                                    Debug.WriteLine("Sync file (Delete locally) " + sid.Path);
                                    // Remove sid and local file
                                    if (file != null)
                                    {
                                        await file.DeleteAsync();
                                    }
                                    SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                                    deleted = true;
                                    changed = true;
                                    break;
                                case ConflictSolution.None:
                                    break;
                                default:
                                    Debug.WriteLine("Sync file: Conflict (Deleted remotely, but changed locally) " + sid.Path);
                                    sid.ConflictType = ConflictType.RemoteDelLocalChange;
                                    break;
                            }
                        }
                    }
                    else if (file == null)
                    {
                        if (sid.ETag == null || info.ETag.Equals(sid.ETag))
                        {
                            Debug.WriteLine("Sync file (Delete remotely) " + sid.Path);
                            // Remove sid and remote file
                            await _client.Delete(info.Path + "/" + info.Name);
                            SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                            deleted = true;
                            changed = true;
                        }
                        else
                        {
                            switch (sid.ConflictSolution)
                            {
                                case ConflictSolution.PreferLocal:
                                    Debug.WriteLine("Sync file (Delete remotely) " + sid.Path);
                                    // Remove sid and remote file
                                    await _client.Delete(info.Path + "/" + info.Name);
                                    SyncDbUtils.DeleteSyncInfoDetail(sid, false);
                                    deleted = true;
                                    changed = true;
                                    break;
                                case ConflictSolution.PreferRemote:
                                    // Update local file
                                    var localFile = await parentFolder.CreateFileAsync(info.Name);
                                    Debug.WriteLine("Sync file (Download)" + localFile.Path);
                                    if (await DownloadFile(localFile, info.Path + "/" + info.Name))
                                    {
                                        var basicProperties = await localFile.GetBasicPropertiesAsync();
                                        currentModified = basicProperties.DateModified;
                                        sid.ETag = info.ETag;
                                        sid.DateModified = currentModified;
                                        sid.ConflictSolution = ConflictSolution.None;
                                        changed = true;
                                    }
                                    else
                                    {
                                        sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_DownloadFile), info.Name);
                                    }
                                    break;
                                case ConflictSolution.None:
                                    break;
                                default:
                                    // Conflict
                                    Debug.WriteLine("Sync file: Conflict (Deleted locally, but changed remotely) " + sid.Path);
                                    sid.ConflictType = ConflictType.LocalDelRemoteChange;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (currentModified.Equals(sid.DateModified))
                        {
                            if (!info.ETag.Equals(sid.ETag))
                            {
                                // Update local file
                                Debug.WriteLine("Sync file (update locally) " + info.Path + "/" + info.Name);
                                if (await DownloadFile(file, info.Path + "/" + info.Name))
                                {
                                    sid.ETag = info.ETag;
                                    sid.DateModified = currentModified;
                                    changed = true;
                                }
                                else
                                {
                                    sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_DownloadFile), info.Name);
                                }
                            }
                        }
                        else if (info.ETag.Equals(sid.ETag))
                        {
                            // update file on nextcloud
                            Debug.WriteLine("Sync file (update remotely) " + info.Path + "/" + info.Name);

                            if (await UploadFile(file, info.Path + "/" + info.Name))
                            {
                                var newInfo = await _client.GetResourceInfo(info.Path, info.Name);
                                sid.ETag = newInfo.ETag;
                                sid.DateModified = currentModified;
                                changed = true;
                            }
                            else
                            {
                                sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_UploadFile), info.Name);
                            }
                        }
                        else
                        {
                            switch (sid.ConflictSolution)
                            {
                                case ConflictSolution.PreferLocal:
                                    // update file on nextcloud
                                    Debug.WriteLine("Sync file (update remotely) " + info.Path + "/" + info.Name);

                                    if (await UploadFile(file, info.Path + "/" + info.Name))
                                    {
                                        var newInfo = await _client.GetResourceInfo(info.Path, info.Name);
                                        sid.ETag = newInfo.ETag;
                                        sid.DateModified = currentModified;
                                        sid.ConflictSolution = ConflictSolution.None;
                                        changed = true;
                                    }
                                    else
                                    {
                                        sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_UploadFile), info.Name);
                                    }
                                    break;
                                case ConflictSolution.PreferRemote:
                                    // Update local file
                                    Debug.WriteLine("Sync file (update locally) " + info.Path + "/" + info.Name);
                                    if (await DownloadFile(file, info.Path + "/" + info.Name))
                                    {
                                        sid.ETag = info.ETag;
                                        sid.DateModified = currentModified;
                                        sid.ConflictSolution = ConflictSolution.None;
                                        changed = true;
                                    }
                                    else
                                    {
                                        sid.Error = string.Format(_resourceLoader.GetString(ResourceConstants.SyncService_Error_DownloadFile), info.Name);
                                    }
                                    break;
                                case ConflictSolution.None:
                                    break;
                                default:
                                    // Conflict
                                    if (sid.ETag == null && !sid.DateModified.HasValue)
                                    {
                                        sid.ConflictType = ConflictType.BothNew;
                                        Debug.WriteLine("Sync file: Conflict (both new) " + info.Path + "/" + info.Name);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Sync file: Conflict (Changed remotely and locally) " + sid.Path);
                                        sid.ConflictType = ConflictType.BothChanged;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: do not write only the raw exception message in the sid.Error.
                sid.Error = e.Message;
            }
            Debug.WriteLine("Synced file " + sid);
            _sidList.Add(sid);
            SyncDbUtils.SaveSyncHistory(sid);
            if (!deleted)
            {
                SyncDbUtils.SaveSyncInfoDetail(sid);
            }
            return changed ? 1 : 0;
        }

        private static void ProgressHandler(WebDavProgress progressInfo)
        {
            // progress
        }

        private async Task<bool> UploadFile(IStorageFile localFile, string path)
        {
            var result = false;
            var cts = new CancellationTokenSource();
            CachedFileManager.DeferUpdates(localFile);
            try
            {
                using (var stream = await localFile.OpenAsync(FileAccessMode.Read))
                {
                    var targetStream = stream.AsStreamForRead();

                    IProgress<WebDavProgress> progress = new Progress<WebDavProgress>(ProgressHandler);
                    result = await _client.Upload(path, targetStream, localFile.ContentType, progress, cts.Token);
                }
            }
            catch (ResponseError e2)
            {
                ResponseErrorHandlerService.HandleException(e2);
            }

            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            await CachedFileManager.CompleteUpdatesAsync(localFile);
            return result;
        }

        private async Task<bool> DownloadFile(IStorageFile localFile, string path)
        {
            bool result;
            CachedFileManager.DeferUpdates(localFile);
            var cts = new CancellationTokenSource();
            IProgress<WebDavProgress> progress = new Progress<WebDavProgress>(ProgressHandler);

            using (var randomAccessStream = await localFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var targetStream = randomAccessStream.AsStreamForWrite();

                result = await _client.Download(path, targetStream, progress, cts.Token);
            }

            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            await CachedFileManager.CompleteUpdatesAsync(localFile);
            return result;
        }
    }
}
