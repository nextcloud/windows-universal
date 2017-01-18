using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer.ShareTarget;

namespace NextcloudApp.Models
{
    public class ShareTargetPageParameters : PageParameters<ShareTargetPageParameters>
    {
        public ShareOperation ShareOperation { get; set; }
        public List<string> FileTokens { get; set; }
    }
}
