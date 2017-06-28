namespace NextcloudApp.Utils
{
    public interface IRevertState
    {
        bool CanRevertState();
        void RevertState();
    }
}
