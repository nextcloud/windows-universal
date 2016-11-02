using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextcloudApp.Utils
{
    public interface IRevertState
    {
        bool CanRevertState();
        void RevertState();
    }
}
