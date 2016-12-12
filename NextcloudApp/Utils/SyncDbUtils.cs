namespace NextcloudApp.Utils
{
    using Models;
    using SQLite.Net;
    using SQLite.Net.Platform.WinRT;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Windows.Storage;

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
    }
}