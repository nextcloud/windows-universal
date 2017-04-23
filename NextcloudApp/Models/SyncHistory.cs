using SQLite.Net.Attributes;
using System;

namespace NextcloudApp.Models
{
    public class SyncHistory : IEquatable<SyncHistory>
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Path { get; set; }
        public string Error { get; internal set; }
        public DateTime SyncDate { get; set; }
        public ConflictType ConflictType { get; set; }

        public SyncHistory()
        {
        }

        public bool Equals(SyncHistory other)
        {
            return Id != 0 && other.Id == Id;
        }

        public override string ToString()
        {
            return $"{Path}\t{SyncDate}";
        }
    }
}
