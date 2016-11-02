using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class SingleFileDownloadPageParameters : PageParameters<SingleFileDownloadPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
    }
}
