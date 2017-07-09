using NextcloudClient.Types;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using System;
using NextcloudApp.Models;

namespace NextcloudApp.Utils
{
    internal static class SyncDbUtils
    {
        private static string _dbPath = string.Empty;
        private static readonly object FsiLock = new object();
        private static SQLiteConnection _dbConnection;

        private static string DbPath
        {
            get
            {
                if (string.IsNullOrEmpty(_dbPath))
                {
                    _dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "SyncStorage.sqlite");
                }

                return _dbPath;
            }
        }

        private static SQLiteConnection DbConnection
        {
            get
            {
                if (_dbConnection != null)
                {
                    return _dbConnection;
                }

                _dbConnection = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath)
                {
#if DEBUG
                    // Activate Tracing
                    TraceListener = new DebugTraceListener()
#endif
                };

                // Init tables
                _dbConnection.CreateTable<FolderSyncInfo>();
                _dbConnection.CreateTable<SyncInfoDetail>();
                _dbConnection.CreateTable<SyncHistory>();
                return _dbConnection;
            }
            set => _dbConnection = value;
        }

        public static void DeleteFolderSyncInfo(FolderSyncInfo folderSyncInfo)
        {
            // Object model:
            DbConnection.Delete(folderSyncInfo);
            DbConnection.Execute("DELETE FROM SyncInfoDetail WHERE FsiID = ?", folderSyncInfo.Id);
        }

        public static List<FolderSyncInfo> GetAllFolderSyncInfos()
        {
            return DbConnection.Table<FolderSyncInfo>().ToList();
        }

        public static List<SyncInfoDetail> GetErrors()
        {
            return DbConnection.Table<SyncInfoDetail>().Where(detail => detail.Error != null).ToList();
        }

        public static FolderSyncInfo GetFolderSyncInfoByPath(string path)
        {
            return DbConnection.Table<FolderSyncInfo>().FirstOrDefault(fsi => fsi.Path == path);
        }

        public static int GetErrorConflictCount(FolderSyncInfo folderSyncInfo)
        {
            return DbConnection.Table<SyncInfoDetail>().Count(sid => sid.FsiId == folderSyncInfo.Id && (sid.ConflictType != ConflictType.None || sid.Error != null));
        }

        public static FolderSyncInfo GetFolderSyncInfoBySubPath(string path)
        {
            return DbConnection.Table<FolderSyncInfo>().FirstOrDefault(info => path.IndexOf(info.Path, StringComparison.Ordinal) == 0 && path.Substring(info.Path.Length - 1, 1).Equals("/"));
        }

        internal static void UnlockFolderSyncInfo(FolderSyncInfo folderSyncInfo)
        {
            lock (FsiLock)
            {
                var m = DbConnection.Table<FolderSyncInfo>().FirstOrDefault(fsi => fsi.Id == folderSyncInfo.Id);
                if (m == null || !m.Active)
                {
                    return;
                }
                m.Active = false;
                DbConnection.Update(m);
            }
        }

        internal static bool LockFolderSyncInfo(FolderSyncInfo folderSyncInfo)
        {
            lock (FsiLock)
            {
                var m = DbConnection.Table<FolderSyncInfo>().FirstOrDefault(fsi => fsi.Id == folderSyncInfo.Id);
                if (m == null || m.Active)
                {
                    return false;
                }
                m.Active = true;
                DbConnection.Update(m);
                return true;
            }
        }

        internal static List<FolderSyncInfo> GetActiveSyncInfos()
        {
            return DbConnection.Table<FolderSyncInfo>().Where(fsi => fsi.Active).ToList();
        }

        public static void SaveFolderSyncInfo(FolderSyncInfo fsi)
        {
            if (fsi.Id == 0)
            {
                // New
                DbConnection.Insert(fsi);
            }
            else
            {
                // Update
                DbConnection.Update(fsi);
            }
        }

        public static SyncInfoDetail GetSyncInfoDetail(ResourceInfo info, FolderSyncInfo fsi)
        {
            var fullPath = info.Path;

            if (!info.IsDirectory)
            {
                fullPath = $"{info.Path}/{info.Name}";
            }

            return DbConnection.Table<SyncInfoDetail>().FirstOrDefault(detail => detail.Path == fullPath && detail.FsiId == fsi.Id);
        }

        public static SyncInfoDetail IsResourceInfoSynced(ResourceInfo info)
        {
            var fullPath = info.Path;

            if (!info.IsDirectory)
            {
                fullPath = $"{info.Path}/{info.Name}";
            }
            
            return DbConnection.Table<SyncInfoDetail>().FirstOrDefault(detail => detail.Path == fullPath);
        }

        public static SyncInfoDetail GetSyncInfoDetail(IStorageItem file, FolderSyncInfo fsi)
        {
            return DbConnection.Table<SyncInfoDetail>().FirstOrDefault(detail => detail.FilePath == file.Path && detail.FsiId == fsi.Id);
        }

        public static List<SyncInfoDetail> GetAllSyncInfoDetails(FolderSyncInfo fsi)
        {
            return DbConnection.Table<SyncInfoDetail>().Where(detail => detail.FsiId == fsi.Id).ToList();
        }

        public static List<SyncInfoDetail> GetConflicts()
        {
            return DbConnection.Table<SyncInfoDetail>().Where(detail => detail.ConflictType != ConflictType.None).ToList();
        }

        public static void DeleteSyncInfoDetail(SyncInfoDetail sid, bool isFolder)
        {
            if (isFolder)
            {
                // Including subpaths
                DbConnection.Execute("DELETE FROM SyncInfoDetail WHERE Path LIKE '?%' AND FsiID = ?", sid.Path, sid.FsiId);
            }
            else
            {
                DbConnection.Delete(sid);
            }
        }

        public static void SaveSyncInfoDetail(SyncInfoDetail sid)
        {
            if (sid.Id == 0)
            {
                // New
                DbConnection.Insert(sid);
            }
            else
            {
                // Update
                DbConnection.Update(sid);
            }
        }

        /// <summary>
        /// Is this resource synced with a folder on the device
        /// </summary>
        public static bool IsSynced(ResourceInfo info)
        {
            if (info == null || info.Path == null)
            {
                return false;
            }
            return GetFolderSyncInfoByPath(info.Path) != null;
        }

        #region SyncHistory

        public static void SaveSyncHistory(SyncInfoDetail sid)
        {
            var syncHistory = new SyncHistory()
            {
                ConflictType = sid.ConflictType,
                Error = sid.Error,
                Path = sid.Path,
                SyncDate = DateTime.Now
            };

            DbConnection.Insert(syncHistory);
        }

        public static void DeleteSyncHistory()
        {
            DbConnection.DeleteAll<SyncHistory>();
        }

        public static List<SyncHistory> GetSyncHistory()
        {
            // Only first 500 entries
            return DbConnection.Table<SyncHistory>().Take(500).OrderByDescending(x => x.SyncDate).ToList();
        }

        #endregion SyncHistory

        public static void Reset()
        {
            DbConnection.DeleteAll<FolderSyncInfo>();
            DbConnection.DeleteAll<SyncInfoDetail>();
            DbConnection.DeleteAll<SyncHistory>();
        }

        public static void Dispose()
        {
            DbConnection.Dispose();
            DbConnection = null;
        }
    }
}