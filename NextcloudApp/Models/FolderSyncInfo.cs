using SQLite.Net.Attributes;

namespace NextcloudApp.Models
{
    public class FolderSyncInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Path { get; set; }
        public bool Active { get; set; }
        public string AccessListKey => Path.Replace('/', '_');
    }
}
