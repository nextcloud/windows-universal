using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class MoveFileOrFolderPageParameters : PageParameters<MoveFileOrFolderPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
    }
}
