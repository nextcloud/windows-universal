using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using Prism.Windows.Navigation;
using Prism.Windows.AppModel;
using Prism.Commands;
using Windows.UI.Xaml.Controls;
using Windows.UI.Notifications;

namespace NextcloudApp.ViewModels
{
    public class SyncConflictPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        public ObservableCollection<SyncInfoDetail> ConflictList { get; private set; }
        public ObservableCollection<SyncInfoDetail> ErrorList { get; private set; }
        public ICommand FixConflictByLocalCommand { get; private set; }
        public ICommand FixConflictByRemoteCommand { get; private set; }
        public SyncConflictPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SYNCACTION);
            ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SYNCONFLICTACTION);
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            FixConflictByLocalCommand = new RelayCommand(FixConflictByLocal);
            FixConflictByRemoteCommand = new RelayCommand(FixConflictByRemote);
            ConflictList = new ObservableCollection<SyncInfoDetail>();
            ErrorList = new ObservableCollection<SyncInfoDetail>();
            List<SyncInfoDetail> conflicts = SyncDbUtils.GetConflicts();
            conflicts.ForEach(x => ConflictList.Add(x));
            List<SyncInfoDetail> errors = SyncDbUtils.GetErrors();
            errors.ForEach(x => ErrorList.Add(x));
        }
        

        private void FixConflictByLocal(object parameter)
        {
            ListView listView = parameter as ListView;
            if (listView == null)
            {
                return;
            }
            var selectedList = new List<SyncInfoDetail>();
            foreach (SyncInfoDetail detail in listView.SelectedItems)
            {
                detail.ConflictSolution = ConflictSolution.PREFER_LOCAL;
                SyncDbUtils.SaveSyncInfoDetail(detail);
                selectedList.Add(detail);
            }
            selectedList.ForEach(x => ConflictList.Remove(x));
        }

        private void FixConflictByRemote(object parameter)
        {
            ListView listView = parameter as ListView;
            if (listView == null)
            {
                return;
            }
            var selectedList = new List<SyncInfoDetail>();
            foreach (SyncInfoDetail detail in listView.SelectedItems)
            {
                detail.ConflictSolution = ConflictSolution.PREFER_REMOTE;
                SyncDbUtils.SaveSyncInfoDetail(detail);
                selectedList.Add(detail);
            }
            selectedList.ForEach(x=>ConflictList.Remove(x));
        }

    }
}
