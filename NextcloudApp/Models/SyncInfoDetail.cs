using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextcloudApp.Models
{
    public class SyncInfoDetail
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset DateModified { get; set; }
        public int FsiID { get; set; }
        public string Path { get; set; }
    }
}
