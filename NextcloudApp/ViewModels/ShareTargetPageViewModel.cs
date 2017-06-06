using System.Collections.Generic;
using System.Windows.Input;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudClient.Types;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System.Threading.Tasks;
using System;
using Windows.UI.Core;
using Prism.Unity.Windows;

namespace NextcloudApp.ViewModels
{
    public class ShareTargetPageViewModel : ViewModel
    {
        private LocalSettings _settngs;
        private DirectoryService _directoryService;
        private ResourceInfo _selectedFileOrFolder;
        private int _selectedPathIndex = -1;
        private readonly INavigationService _navigationService;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        private bool _isNavigatingBack;
        private CoreDispatcher _dispatcher;

        public ICommand GroupByNameAscendingCommand { get; private set; }
        public ICommand GroupByNameDescendingCommand { get; private set; }
        public ICommand GroupByDateAscendingCommand { get; private set; }
        public ICommand GroupByDateDescendingCommand { get; private set; }
        public ICommand GroupBySizeAscendingCommand { get; private set; }
        public ICommand GroupBySizeDescendingCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand CreateDirectoryCommand { get; private set; }
        public object CancelFolderSelectionCommand { get; private set; }
        public object StartUploadCommand { get; private set; }

        public ShareTargetPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            Settings = SettingsService.Instance.LocalSettings;
            GroupByNameAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByNameAscending();
            });
            GroupByNameDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByNameDescending();
            });
            GroupByDateAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByDateAscending();
            });
            GroupByDateDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByDateDescending();
            });
            GroupBySizeAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupBySizeAscending();
            });
            GroupBySizeDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupBySizeDescending();
            });

            SelectedFileOrFolder = null;

            RefreshCommand = new DelegateCommand(async () =>
            {
                ShowProgressIndicator();
                await Directory.Refresh();
                HideProgressIndicator();
            });
            CreateDirectoryCommand = new DelegateCommand(CreateDirectory);
            StartUploadCommand = new DelegateCommand(StartUpload);
            CancelFolderSelectionCommand = new DelegateCommand(CancelFolderSelection);
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            
            var parameters = ShareTargetPageParameters.Deserialize(e.Parameter);
            var fileTokens = parameters?.FileTokens;
            if (fileTokens == null)
            {
                return;
            }
            ActivationKind = parameters.ActivationKind;
            FileTokens = fileTokens;
            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;
        }

        public ActivationKind ActivationKind { get; private set; }

        public List<string> FileTokens { get; private set; }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _isNavigatingBack = true;
            if (!suspending)
            {
                Directory.StopDirectoryListing();
                Directory = null;
                _selectedFileOrFolder = null;
            }
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private void CancelFolderSelection()
        {
            PrismUnityApplication.Current.Exit();
        }

        private void StartUpload()
        {
            var parameters = new FileUploadPageParameters
            {
                ActivationKind = ActivationKind,
                ResourceInfo = Directory.PathStack.Count > 0
                    ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo
                    : new ResourceInfo(),
                FileTokens = FileTokens
            };
            _navigationService.Navigate(PageToken.FileUpload.ToString(), parameters.Serialize());
        }

        private async void CreateDirectory()
        {
            while (true)
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("CreateNewFolder"),
                    Content = new TextBox()
                    {
                        Header = _resourceLoader.GetString("FolderName"),
                        PlaceholderText = _resourceLoader.GetString("NewFolder"),
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = _resourceLoader.GetString("Create"),
                    SecondaryButtonText = _resourceLoader.GetString("Cancel")
                };
                var dialogResult = await _dialogService.ShowAsync(dialog);
                if (dialogResult != ContentDialogResult.Primary)
                {
                    return;
                }
                var textBox = dialog.Content as TextBox;
                if (textBox == null)
                {
                    return;
                }
                var folderName = textBox.Text;
                if (string.IsNullOrEmpty(folderName))
                {
                    folderName = _resourceLoader.GetString("NewFolder");
                }
                ShowProgressIndicator();
                var success = await Directory.CreateDirectory(folderName);
                HideProgressIndicator();
                if (success)
                {
                    SelectedFileOrFolder = null;
                    return;
                }

                dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("CanNotCreateFolder"),
                    Content = new TextBlock
                    {
                        Text = _resourceLoader.GetString("SpecifyDifferentName"),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = _resourceLoader.GetString("Retry"),
                    SecondaryButtonText = _resourceLoader.GetString("Cancel")
                };
                dialogResult = await _dialogService.ShowAsync(dialog);
                if (dialogResult != ContentDialogResult.Primary)
                {
                    return;
                }
            }
        }

        public DirectoryService Directory
        {
            get { return _directoryService; }
            private set { SetProperty(ref _directoryService, value); }
        }

        public LocalSettings Settings
        {
            get { return _settngs; }
            private set { SetProperty(ref _settngs, value); }
        }

        public ResourceInfo SelectedFileOrFolder
        {
            get { return _selectedFileOrFolder; }
            set
            {
                if (_isNavigatingBack)
                {
                    return;
                }
                try
                {
                    if (!SetProperty(ref _selectedFileOrFolder, value))
                    {
                        return;
                    }
                }
                catch
                {
                    _selectedPathIndex = -1;
                    return;
                }

                if (value == null)
                {
                    return;
                }

                if (Directory?.PathStack == null)
                {
                    return;
                }

                if (Directory.IsSorting)
                {
                    return;
                }
                if (value.IsDirectory)
                {
                    Directory.PathStack.Add(new PathInfo
                    {
                        ResourceInfo = value
                    });
                    SelectedPathIndex = Directory.PathStack.Count - 1;
                }
            }
        }

        public int SelectedPathIndex
        {
            get { return _selectedPathIndex; }
            set
            {
                try
                {
                    if (!SetProperty(ref _selectedPathIndex, value))
                    {
                        return;
                    }
                }
                catch
                {
                    _selectedPathIndex = -1;
                    return;
                }

                if (Directory?.PathStack == null)
                {
                    return;
                }

                while (Directory.PathStack.Count > 0 && Directory.PathStack.Count > _selectedPathIndex + 1)
                {
                    Directory.PathStack.RemoveAt(Directory.PathStack.Count - 1);
                }

                StartDirectoryListing();
            }
        }

        private async void StartDirectoryListing()
        {
            ShowProgressIndicator();

            await Directory.StartDirectoryListing();

            HideProgressIndicator();

            SelectedFileOrFolder = null;
        }

        public override bool CanRevertState()
        {
            return SelectedPathIndex > 0;
        }

        public override void RevertState()
        {
            SelectedPathIndex--;
        }
        private async Task OnUiThread(Action action)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }
    }
}