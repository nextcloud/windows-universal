using System;
using Windows.ApplicationModel.Resources;

namespace NextcloudApp.Models
{

    public enum ConflictType
    {
        None,
        BothNew,
        BothChanged,
        LocalDelRemoteChange,
        RemoteDelLocalChange
    }

    public class SyncConflict
    {
        public static string GetConflictMessage(ConflictType type)
        {
            var resourceLoader = new ResourceLoader();
            string conflictMessage;

            switch (type)
            {
                case ConflictType.None: return "";
                case ConflictType.BothNew:
                    conflictMessage = resourceLoader.GetString("SyncConflictBothNew");
                    break;
                case ConflictType.BothChanged:
                    conflictMessage = resourceLoader.GetString("SyncConflictBothChanged");
                    break;
                case ConflictType.LocalDelRemoteChange:
                    conflictMessage = resourceLoader.GetString("SyncConflictLocalDel");
                    break;
                case ConflictType.RemoteDelLocalChange:
                    conflictMessage = resourceLoader.GetString("SyncConflictRemoteDel");
                    break;
                default:
                    conflictMessage = "Unknown";
                    break;
            }
            return $"{resourceLoader.GetString("SyncConflictPrefix")} {conflictMessage}";
        }
    }
}
