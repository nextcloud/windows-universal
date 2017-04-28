using NextcloudClient.Types;
using System.Collections.Generic;

namespace NextcloudApp.Models
{
    public class MoveFileOrFolderPageParameters : PageParameters<MoveFileOrFolderPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
        public List<ResourceInfo> ResourceInfos { get; set; }
    }
}
