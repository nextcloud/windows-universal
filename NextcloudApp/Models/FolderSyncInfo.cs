using NextcloudClient.Types;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextcloudApp.Models
{
    public class FolderSyncInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Path { get; set; }
        public bool Active { get; set; }
        public string AccessListKey
        {
            get
            {
                return Path.Replace('/', '_');
            }
        }
    }
}
