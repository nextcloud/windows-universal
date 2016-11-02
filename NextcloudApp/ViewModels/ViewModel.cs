using NextcloudApp.Services;
using NextcloudApp.Utils;
using Prism.Windows.Mvvm;

namespace NextcloudApp.ViewModels
{
    public class ViewModel : ViewModelBase, IRevertState
    {
        internal void ShowProgressIndicator()
        {
            StatusBarService.Instance.ShowProgressIndicator();
        }

        internal void HideProgressIndicator()
        {
            StatusBarService.Instance.HideProgressIndicator();
        }

        public virtual bool CanRevertState()
        {
            return false;
        }

        public virtual void RevertState()
        {
        }
    }
}
