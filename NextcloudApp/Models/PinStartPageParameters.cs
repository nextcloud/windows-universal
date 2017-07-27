using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class PinStartPageParameters : PageParameters<PinStartPageParameters>
    {
        public PageToken PageTarget { get; set; }

        public ResourceInfo ResourceInfo { get; set; }
    }
}
