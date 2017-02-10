using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class FileDownloadPageParameters : PageParameters<FileDownloadPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
    }
}
