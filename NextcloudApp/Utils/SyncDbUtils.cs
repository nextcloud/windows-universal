namespace NextcloudApp.Utils
{
    using Models;
    using NextcloudClient.Types;
    using SQLite.Net;
    using SQLite.Net.Platform.WinRT;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Windows.Storage;
    using System;
    using Windows.ApplicationModel.Resources;

    internal static class SyncDbUtils
    {
        private static string dbPath = string.Empty;
        private static string DbPath
        {
            get
            {
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "SyncStorage.sqlite");
                }

                return dbPath;
            }
        }

        private static SQLiteConnection DbConnection
        {
            get
            {
                var db = new SQLiteConnection(new SQLitePlatformWinRT(), DbPath);
                // Activate Tracing
                db.TraceListener = new DebugTraceListener();
                // Init tables
                db.CreateTable<FolderSyncInfo>();
                db.CreateTable<SyncInfoDetail>();
                return db;
            }
        }

        public static void DeleteFolderSyncInfo(FolderSyncInfo folderSyncInfo)
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                // Object model:
                db.Delete(folderSyncInfo);
                db.Execute("DELETE FROM SyncInfoDetail WHERE FsiID = ?", folderSyncInfo.Id);
            }
        }

        public static List<FolderSyncInfo> GetAllFolderSyncInfos()
        {
            List<FolderSyncInfo> models;

            // Create a new connection
            using (var db = DbConnection)
            {
                models = (from p in db.Table<FolderSyncInfo>()
                          select p).ToList();
            }

            return models;
        }

        public static List<SyncInfoDetail> GetErrors()
        {
            using (var db = DbConnection)
            {
                IEnumerable<SyncInfoDetail> sidList = (from detail in db.Table<SyncInfoDetail>()
                                                       where detail.Error != null
                                                       select detail);
                return sidList.ToList();
            }
        }

        public static FolderSyncInfo GetFolderSyncInfoByPath(string Path)
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                FolderSyncInfo m = (from fsi in db.Table<FolderSyncInfo>()
                            where fsi.Path == Path
                                    select fsi).FirstOrDefault();
                return m;
            }
        }

        public static int GetErrorConflictCount(FolderSyncInfo folderSyncInfo)
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                return (from sid in db.Table<SyncInfoDetail>()
                                    where sid.FsiID == folderSyncInfo.Id 
                                    && (sid.ConflictType != ConflictType.NONE 
                                    || sid.Error != null)
                        select sid).Count();
            }
        }

        public static FolderSyncInfo GetFolderSyncInfoBySubPath(string Path)
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                IEnumerable<FolderSyncInfo> infos = from fsi in db.Table<FolderSyncInfo>()
                                    select fsi;
                foreach(var info in infos)
                {
                    int index = Path.IndexOf(info.Path);
                    if(index == 0 && Path.Substring(info.Path.Length-1, 1).Equals("/"))
                    {
                        return info;
                    }
                }
                return null;
            }
        }

        public static void SaveFolderSyncInfo(FolderSyncInfo fsi)
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                if (fsi.Id == 0)
                {
                    // New
                    db.Insert(fsi);
                }
                else
                {
                    // Update
                    db.Update(fsi);
                }
            }
        }

        public static SyncInfoDetail GetSyncInfoDetail(ResourceInfo info, FolderSyncInfo fsi)
        {
            // Create a new connection
            string fullPath = info.Path;
            if(!info.IsDirectory())
            {
                fullPath = info.Path + "/" + info.Name;
            }

            using (var db = DbConnection)
            {
                SyncInfoDetail sid = (from detail in db.Table<SyncInfoDetail>()
                                      where detail.Path == fullPath && detail.FsiID == fsi.Id
                                      select detail).FirstOrDefault();
                return sid;
            }
        }

        public static SyncInfoDetail GetSyncInfoDetail(IStorageItem file, FolderSyncInfo fsi)
        {
            // Create a new connection
            using (var db = DbConnection)
            {
                SyncInfoDetail sid = (from detail in db.Table<SyncInfoDetail>()
                                      where detail.FilePath == file.Path && detail.FsiID == fsi.Id
                                      select detail).FirstOrDefault();
                return sid;
            }
        }

        public static List<SyncInfoDetail> GetAllSyncInfoDetails(FolderSyncInfo fsi)
        {
            using (var db = DbConnection)
            {
                IEnumerable<SyncInfoDetail> sidList = (from detail in db.Table<SyncInfoDetail>()
                                      where detail.FsiID == fsi.Id
                                      select detail);
                return sidList.ToList();
            }
        }

        public static List<SyncInfoDetail> GetConflicts()
        {
            using (var db = DbConnection)
            {
                IEnumerable<SyncInfoDetail> sidList = (from detail in db.Table<SyncInfoDetail>()
                                                       where detail.ConflictType != ConflictType.NONE
                                                       select detail);
                return sidList.ToList();
            }
        }

        public static void DeleteSyncInfoDetail(SyncInfoDetail sid, bool isFolder)
        {
            using (var db = DbConnection)
            {
                if (isFolder)
                {
                    // Including subpaths
                    db.Execute("DELETE FROM SyncInfoDetail WHERE Path LIKE '?%' AND FsiID = ?", sid.Path, sid.FsiID);
                } else
                {
                    db.Delete(sid);
                }
            }
        }

        public static void SaveSyncInfoDetail(SyncInfoDetail sid)
        {
            using (var db = DbConnection)
            {
                if (sid.Id == 0)
                {
                    // New
                    db.Insert(sid);
                }
                else
                {
                    // Update
                    db.Update(sid);
                }
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
            else
            {
                return GetFolderSyncInfoByPath(info.Path) != null;
            }
        }
    }
}