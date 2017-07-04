using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using Prism.Windows.AppModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Notifications;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace NextcloudApp.ViewModels
{
    public class SyncStatusPageViewModel : ViewModel
    {
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;

        public ObservableCollection<SyncHistory> SyncHistoryList { get; }

        public ObservableCollection<SyncInfoDetail> ConflictList { get; }
        public ObservableCollection<SyncInfoDetail> ErrorList { get; }
        public ObservableCollection<FolderSyncInfo> FolderSyncList { get; }
        
        public ICommand FixConflictByLocalCommand { get; }
        public ICommand FixConflictByRemoteCommand { get; }
        public ICommand FixConflictByKeepAsIsCommand { get; }
        public ICommand ClearSyncHistoryCommand { get; }
        public ICommand CheckAllCommand { get; }
        public ICommand UncheckAllCommand { get; }

        public SyncStatusPageViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                ConflictList = new ObservableCollection<SyncInfoDetail>
                {
                    new SyncInfoDetail
                    {
                        ConflictSolution = ConflictSolution.None,
                        ConflictType = ConflictType.BothNew,
                        Path = "/foo/bar/"
                    },
                    new SyncInfoDetail
                    {
                        ConflictSolution = ConflictSolution.None,
                        ConflictType = ConflictType.BothChanged,
                        Path = "/foo/bar/"
                    }
                };
            }
        }

        public SyncStatusPageViewModel(IResourceLoader resourceLoader, DialogService dialogService)
        {
            ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SyncAction);
            ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SyncConflictAction);
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;

            FixConflictByLocalCommand = new RelayCommand(FixConflictByLocal, CanExecuteFixConflict);
            FixConflictByRemoteCommand = new RelayCommand(FixConflictByRemote, CanExecuteFixConflict);
            FixConflictByKeepAsIsCommand = new RelayCommand(FixConflictByKeepAsIs, CanExecuteFixConflict);
            ClearSyncHistoryCommand = new RelayCommand(ClearSyncHistory);
            CheckAllCommand = new RelayCommand(CheckAll);
            UncheckAllCommand = new RelayCommand(UncheckAll);

            SyncHistoryList = new ObservableCollection<SyncHistory>();
            ConflictList = new ObservableCollection<SyncInfoDetail>();
            ErrorList = new ObservableCollection<SyncInfoDetail>();
            FolderSyncList = new ObservableCollection<FolderSyncInfo>();

            var history = SyncDbUtils.GetSyncHistory();
            history.ForEach(x => SyncHistoryList.Add(x));

            var conflicts = SyncDbUtils.GetConflicts();
            conflicts.ForEach(x => ConflictList.Add(x));

            var errors = SyncDbUtils.GetErrors();
            errors.ForEach(x => ErrorList.Add(x));
            var fsis = SyncDbUtils.GetAllFolderSyncInfos();
            fsis.ForEach(x => FolderSyncList.Add(x));
        }

        private void FixConflictByLocal(object parameter)
        {
            var listView = parameter as ListView;

            if (listView == null)
            {
                return;
            }

            var selectedList = new List<SyncInfoDetail>();

            foreach (SyncInfoDetail detail in listView.SelectedItems)
            {
                detail.ConflictSolution = ConflictSolution.PreferLocal;
                SyncDbUtils.SaveSyncInfoDetail(detail);
                selectedList.Add(detail);
            }

            selectedList.ForEach(x => ConflictList.Remove(x));
        }

        private void FixConflictByRemote(object parameter)
        {
            var listView = parameter as ListView;

            if (listView == null)
            {
                return;
            }

            var selectedList = new List<SyncInfoDetail>();

            foreach (SyncInfoDetail detail in listView.SelectedItems)
            {
                detail.ConflictSolution = ConflictSolution.PreferRemote;
                SyncDbUtils.SaveSyncInfoDetail(detail);
                selectedList.Add(detail);
            }

            selectedList.ForEach(x => ConflictList.Remove(x));
        }

        private async void FixConflictByKeepAsIs(object parameter)
        {
            var listView = parameter as ListView;

            if (listView == null)
            {
                return;
            }

            var selectedList = new List<SyncInfoDetail>();
            var usageHint = false;
            foreach (SyncInfoDetail detail in listView.SelectedItems)
            {
                if (detail.ConflictType == ConflictType.BothChanged ||
                    detail.ConflictType == ConflictType.BothNew)
                {
                    detail.ConflictSolution = ConflictSolution.KeepAsIs;
                    SyncDbUtils.SaveSyncInfoDetail(detail);
                    selectedList.Add(detail);
                } else
                {
                    usageHint = true;
                }
            }

            selectedList.ForEach(x => ConflictList.Remove(x));
            if (!usageHint)
            {
                return;
            }
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("SyncKeepAsIsHintTitle"),
                Content = new TextBlock
                {
                    Text = _resourceLoader.GetString("SyncKeepAsIsHintDesc"),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("OK")
            };
            await _dialogService.ShowAsync(dialog);
        }

        private bool CanExecuteFixConflict()
        {
            return ConflictList?.Count > 0;
        }

        private void ClearSyncHistory(object parameter)
        {
            SyncDbUtils.DeleteSyncHistory();
            SyncHistoryList.Clear();
            // ReSharper disable once ExplicitCallerInfoArgument
            RaisePropertyChanged(nameof(SyncHistoryList));
        }

        private void UncheckAll(object parameter)
        {
            var listView = parameter as ListView;

            listView?.SelectedItems.Clear();
        }

        private void CheckAll(object parameter)
        {
            var listView = parameter as ListView;

            listView?.SelectAll();
        }
    }
}
