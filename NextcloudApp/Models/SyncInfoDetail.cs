using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextcloudApp.Models
{
    public class SyncInfoDetail : IEquatable<SyncInfoDetail>
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset DateModified { get; set; }
        public int FsiID { get; set; }
        public string Path { get; set; }
        public string FilePath { get; set; }
        public string Error { get; internal set; }

        public SyncInfoDetail()
        {
        }

        public SyncInfoDetail(FolderSyncInfo fsi)
        {
            this.FsiID = fsi.Id;
        }

        public bool Equals(SyncInfoDetail other)
        {
            return Id != 0 && other.Id == Id;
        }
        
        public string ToString()
        {
            return "Path: " + detail.Path + " - " +
                "FilePath: " + detail.FilePath + " - " +
                "ETag: " + detail.ETag + " - " +
                "Modified: " + detail.DateModified.ToString("u") + " - " +
                "Error: " + detail.Error;
        }
    }
}
