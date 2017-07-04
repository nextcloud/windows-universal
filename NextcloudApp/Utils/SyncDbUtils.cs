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
                    // Activate Tracing
                    TraceListener = new DebugTraceListener()
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
            var models = (from p in DbConnection.Table<FolderSyncInfo>() select p).ToList();
            return models;
        }

        public static List<SyncInfoDetail> GetErrors()
        {
            IEnumerable<SyncInfoDetail> sidList = from detail in DbConnection.Table<SyncInfoDetail>() where detail.Error != null select detail;
            return sidList.ToList();
        }

        public static FolderSyncInfo GetFolderSyncInfoByPath(string path)
        {
            return (from fsi in DbConnection.Table<FolderSyncInfo>() where fsi.Path == path select fsi).FirstOrDefault();
        }

        public static int GetErrorConflictCount(FolderSyncInfo folderSyncInfo)
        {
            return (from sid in DbConnection.Table<SyncInfoDetail>() where sid.FsiId == folderSyncInfo.Id && (sid.ConflictType != ConflictType.None || sid.Error != null) select sid).Count();
        }

        public static FolderSyncInfo GetFolderSyncInfoBySubPath(string path)
        {
            var infos = from fsi in DbConnection.Table<FolderSyncInfo>() select fsi;
            return (from info in infos let index = path.IndexOf(info.Path, StringComparison.Ordinal) where index == 0 && path.Substring(info.Path.Length - 1, 1).Equals("/") select info).FirstOrDefault();
        }

        internal static void UnlockFolderSyncInfo(FolderSyncInfo folderSyncInfo)
        {
            lock (FsiLock)
            {
                var m = (from fsi in DbConnection.Table<FolderSyncInfo>() where fsi.Id == folderSyncInfo.Id select fsi).FirstOrDefault();
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
                var m = (from fsi in DbConnection.Table<FolderSyncInfo>() where fsi.Id == folderSyncInfo.Id select fsi).FirstOrDefault();
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
            IEnumerable<FolderSyncInfo> list = from fsi in DbConnection.Table<FolderSyncInfo>() where fsi.Active select fsi;
            return list.ToList();
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

            var sid = (from detail in DbConnection.Table<SyncInfoDetail>() where detail.Path == fullPath && detail.FsiId == fsi.Id select detail).FirstOrDefault();
            return sid;
        }

        public static SyncInfoDetail IsResourceInfoSynced(ResourceInfo info)
        {
            var fullPath = info.Path;

            if (!info.IsDirectory)
            {
                fullPath = $"{info.Path}/{info.Name}";
            }

            var sid = (from detail in DbConnection.Table<SyncInfoDetail>() where detail.Path == fullPath select detail).FirstOrDefault();
            return sid;
        }

        public static SyncInfoDetail GetSyncInfoDetail(IStorageItem file, FolderSyncInfo fsi)
        {
            var sid = (from detail in DbConnection.Table<SyncInfoDetail>() where detail.FilePath == file.Path && detail.FsiId == fsi.Id select detail).FirstOrDefault();
            return sid;
        }

        public static List<SyncInfoDetail> GetAllSyncInfoDetails(FolderSyncInfo fsi)
        {
            IEnumerable<SyncInfoDetail> sidList = from detail in DbConnection.Table<SyncInfoDetail>() where detail.FsiId == fsi.Id select detail;
            return sidList.ToList();
        }

        public static List<SyncInfoDetail> GetConflicts()
        {
            IEnumerable<SyncInfoDetail> sidList = from detail in DbConnection.Table<SyncInfoDetail>() where detail.ConflictType != ConflictType.None select detail;
            return sidList.ToList();
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
            var historyList = (from detail in DbConnection.Table<SyncHistory>() select detail).Take(500);
            return historyList.OrderByDescending(x => x.SyncDate).ToList();
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