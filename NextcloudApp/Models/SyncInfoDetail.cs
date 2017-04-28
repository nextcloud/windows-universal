using SQLite.Net.Attributes;
using System;

namespace NextcloudApp.Models
{
    public class SyncInfoDetail : IEquatable<SyncInfoDetail>
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public int FsiID { get; set; }
        public string Path { get; set; }
        public string FilePath { get; set; }
        public string Error { get; internal set; }
        public ConflictSolution ConflictSolution { get; set; }
        public ConflictType ConflictType { get; set; }

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
        
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string datemodified = DateModified.HasValue ? DateModified.Value.ToString("u") : "";
            return "Path: " + Path + " - " +
                "FilePath: " + FilePath + " - " +
                "ETag: " + ETag + " - " +
                "Modified: " + datemodified + " - " +
                "Error: " + Error;
        }
    }
}
