using System.Collections.Generic;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudClient.Types;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;

namespace NextcloudApp.ViewModels
{
    public class MoveFileOrFolderPageViewModel : ViewModel
    {
        private LocalSettings _settings;
        private DirectoryService _directoryService;
        private ResourceInfo _selectedFileOrFolder;
        private int _selectedPathIndex = -1;
        private readonly INavigationService _navigationService;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        private bool _isNavigatingBack;

        public ICommand GroupByNameAscendingCommand { get; private set; }
        public ICommand GroupByNameDescendingCommand { get; private set; }
        public ICommand GroupByDateAscendingCommand { get; private set; }
        public ICommand GroupByDateDescendingCommand { get; private set; }
        public ICommand GroupBySizeAscendingCommand { get; private set; }
        public ICommand GroupBySizeDescendingCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand CreateDirectoryCommand { get; private set; }
        public object CancelFolderSelectionCommand { get; private set; }
        public object MoveToSelectedFolderCommand { get; private set; }

        public MoveFileOrFolderPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            Settings = SettingsService.Instance.LocalSettings;
            GroupByNameAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByNameAscending();
                SelectedFileOrFolder = null;
            });
            GroupByNameDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByNameDescending();
                SelectedFileOrFolder = null;
            });
            GroupByDateAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByDateAscending();
                SelectedFileOrFolder = null;
            });
            GroupByDateDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByDateDescending();
                SelectedFileOrFolder = null;
            });
            GroupBySizeAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupBySizeAscending();
                SelectedFileOrFolder = null;
            });
            GroupBySizeDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupBySizeDescending();
                SelectedFileOrFolder = null;
            });
            RefreshCommand = new DelegateCommand(async () =>
            {
                ShowProgressIndicator();
                await Directory.Refresh();
                HideProgressIndicator();
            });
            CreateDirectoryCommand = new DelegateCommand(CreateDirectory);
            MoveToSelectedFolderCommand = new DelegateCommand(MoveToSelectedFolder);
            CancelFolderSelectionCommand = new DelegateCommand(CancelFolderSelection);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            base.OnNavigatedTo(e, viewModelState);
            var parameters = MoveFileOrFolderPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;
            if (resourceInfo == null)
            {
                return;
            }
            ResourceInfo = resourceInfo;
            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;
        }

        public ResourceInfo ResourceInfo { get; private set; }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _isNavigatingBack = true;

            if (!suspending)
            {
                Directory.StopDirectoryListing();
                Directory = null;
                _selectedFileOrFolder = null;
            }
            else
                _isNavigatingBack = false;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private void CancelFolderSelection()
        {
            _navigationService.GoBack();
        }

        private async void MoveToSelectedFolder()
        {
            ShowProgressIndicator();
            var currentFolderResourceInfo = Directory.PathStack.Count > 0
                ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo
                : new ResourceInfo();

            var oldPath = string.IsNullOrEmpty(ResourceInfo.Path) ? "/" : ResourceInfo.Path;
            oldPath = oldPath.TrimEnd('/');

            if (!ResourceInfo.ContentType.Equals("dav/directory"))
            {
                oldPath = oldPath + "/" + ResourceInfo.Name;
            }

            var newPath = string.IsNullOrEmpty(currentFolderResourceInfo.Path) ? "/" : currentFolderResourceInfo.Path;
            newPath = newPath.TrimEnd('/');
            newPath = newPath + "/" + ResourceInfo.Name;

            var success = await Directory.Move(oldPath, newPath);

            HideProgressIndicator();

            if (success)
            {
                _navigationService.GoBack();
            }
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
            get { return _settings; }
            private set { SetProperty(ref _settings, value); }
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
                if (value.IsDirectory())
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

            // The folder to move should not be set as target.
            await Directory.StartDirectoryListing(ResourceInfo);

            HideProgressIndicator();
        }


        public override bool CanRevertState()
        {
            return SelectedPathIndex > 0;
        }

        public override void RevertState()
        {
            SelectedPathIndex--;
        }
    }
}