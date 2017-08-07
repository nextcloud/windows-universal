using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class DirectoryListPageParameters : PageParameters<DirectoryListPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
    }
}
