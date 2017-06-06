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
using System.Threading.Tasks;

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

        public ICommand GroupByNameAscendingCommand { get; }
        public ICommand GroupByNameDescendingCommand { get; }
        public ICommand GroupByDateAscendingCommand { get; }
        public ICommand GroupByDateDescendingCommand { get; }
        public ICommand GroupBySizeAscendingCommand { get; }
        public ICommand GroupBySizeDescendingCommand { get; }
        public ICommand GroupByTypeAscendingCommand { get; }
        public ICommand GroupByTypeDescendingCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateDirectoryCommand { get; }
        public ICommand SelectToggleCommand { get; }
        public object CancelFolderSelectionCommand { get; }
        public object MoveToSelectedFolderCommand { get; }

        public MoveFileOrFolderPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
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

            GroupByTypeAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByTypeAscending();
            });

            GroupByTypeDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByTypeDescending();
            });

            SelectedFileOrFolder = null;

            RefreshCommand = new DelegateCommand(async () =>
            {
                ShowProgressIndicator();
                await Directory.Refresh();
                HideProgressIndicator();
            });

            SelectToggleCommand = new DelegateCommand(() =>
            {
                Directory.ToggleSelectionMode();
            });

            CreateDirectoryCommand = new DelegateCommand(CreateDirectory);
            MoveToSelectedFolderCommand = new DelegateCommand(MoveToSelectedFolder);
            CancelFolderSelectionCommand = new DelegateCommand(CancelFolderSelection);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            base.OnNavigatedTo(e, viewModelState);
            ResourceInfo = null;
            ResourceInfos = null;
            var parameters = MoveFileOrFolderPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;
            var resourceInfos = parameters?.ResourceInfos;

            if (resourceInfo != null)
            {
                ResourceInfo = resourceInfo;
            }
            else if (resourceInfos != null)
            {
                ResourceInfos = resourceInfos;
            }
            else
            {
                return;
            }

            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;
            SelectedFileOrFolder = null;
        }

        public ResourceInfo ResourceInfo { get; private set; }

        public List<ResourceInfo> ResourceInfos { get; private set; }

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

            if (ResourceInfo != null)
            {
                await Move(ResourceInfo, currentFolderResourceInfo);
            }
            else if (ResourceInfos != null)
            {
                foreach (var resInfo in ResourceInfos)
                {
                    await Move(resInfo, currentFolderResourceInfo);
                }
            }

            HideProgressIndicator();
            _navigationService.GoBack();
        }

        private async Task Move(ResourceInfo resInfo, ResourceInfo currentFolderResourceInfo)
        {
            var oldPath = string.IsNullOrEmpty(resInfo.Path) ? "/" : resInfo.Path;
            oldPath = oldPath.TrimEnd('/');

            if (!resInfo.ContentType.Equals("dav/directory"))
            {
                oldPath = oldPath + "/" + resInfo.Name;
            }

            var newPath = string.IsNullOrEmpty(currentFolderResourceInfo.Path) ? "/" : currentFolderResourceInfo.Path;
            newPath = newPath.TrimEnd('/');
            newPath = newPath + "/" + resInfo.Name;

            await Directory.Move(oldPath, newPath);
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
            get => _directoryService;
            private set => SetProperty(ref _directoryService, value);
        }

        public LocalSettings Settings
        {
            get => _settings;
            private set => SetProperty(ref _settings, value);
        }

        public ResourceInfo SelectedFileOrFolder
        {
            get => _selectedFileOrFolder;
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

            if (ResourceInfo != null)
            {
                Directory.RemoveResourceInfos = new List<ResourceInfo> { ResourceInfo };
            }
            else if (ResourceInfos != null)
            {
                Directory.RemoveResourceInfos = ResourceInfos;
            }
               
            // The folder to move should not be set as target.
            await Directory.StartDirectoryListing(ResourceInfo);
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
    }
}