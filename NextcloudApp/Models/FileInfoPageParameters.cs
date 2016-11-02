using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class FileInfoPageParameters : PageParameters<FileInfoPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
    }
}
