using System;
using Windows.ApplicationModel.Resources;

namespace NextcloudApp.Models
{

    public enum ConflictType
    {
        NONE, BOTHNEW, BOTH_CHANGED, LOCALDEL_REMOTECHANGE, REMOTEDEL_LOCALCHANGE
    }

    public class SyncConflict
    {
        public static String GetConflictMessage(ConflictType type)
        {
            var _resourceLoader = new ResourceLoader();
            string conflictMessage;

            switch (type)
            {
                case ConflictType.NONE: return "";
                case ConflictType.BOTHNEW:
                    conflictMessage = _resourceLoader.GetString("SyncConflictBothNew");
                    break;
                case ConflictType.BOTH_CHANGED:
                    conflictMessage = _resourceLoader.GetString("SyncConflictBothChanged");
                    break;
                case ConflictType.LOCALDEL_REMOTECHANGE:
                    conflictMessage = _resourceLoader.GetString("SyncConflictLocalDel");
                    break;
                case ConflictType.REMOTEDEL_LOCALCHANGE:
                    conflictMessage = _resourceLoader.GetString("SyncConflictRemoteDel");
                    break;
                default:
                    conflictMessage = "Unknown";
                    break;
            }
            return $"{_resourceLoader.GetString("SyncConflictPrefix")} {conflictMessage}";
        }
    }
}
