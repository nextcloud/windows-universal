using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class PinStartPageParameters : FileInfoPageParameters
    {
        public PageToken PageTarget { get; set; }
    }
}
