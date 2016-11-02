using Windows.Storage.Pickers;
using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class FileUploadPageParameters : PageParameters<FileUploadPageParameters>
    {
        public ResourceInfo ResourceInfo { get; set; }
        public PickerLocationId PickerLocationId { get; set; }
    }
}
