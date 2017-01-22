namespace NextcloudApp.Models
{
    public enum SyncConflict
    {
        BOTH_NEW, BOTH_CHANGED, REMOTEDEL_LOCALCHANGE, LOCALDEL_REMOTECHANGE
    }
}
