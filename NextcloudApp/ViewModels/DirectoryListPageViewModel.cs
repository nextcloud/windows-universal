using System.Collections.Generic;
using System.Windows.Input;
using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Types;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace NextcloudApp.ViewModels
{
    public class DirectoryListPageViewModel : ViewModel
    {
        private LocalSettings _settings;
        private DirectoryService _directoryService;
        private TileService _tileService;
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
        public ICommand GroupByTypeAscendingCommand { get; private set; }
        public ICommand GroupByTypeDescendingCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand CreateDirectoryCommand { get; private set; }
        public ICommand UploadFilesCommand { get; private set; }
        public ICommand UploadPhotosCommand { get; private set; }
        public ICommand DownloadResourceCommand { get; private set; }
        public ICommand DownloadSelectedCommand { get; private set; }
        public ICommand DeleteResourceCommand { get; private set; }
        public ICommand DeleteSelectedCommand { get; private set; }
        public ICommand RenameResourceCommand { get; private set; }
        public ICommand MoveResourceCommand { get; private set; }
        public ICommand SynchronizeFolderCommand { get; private set; }
        public ICommand StopSynchronizeFolderCommand { get; private set; }
        public ICommand MoveSelectedCommand { get; private set; }
        public ICommand PinToStartCommand { get; private set; }
        public ICommand SelectToggleCommand { get; private set; }

        public DirectoryListPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            _tileService = TileService.Instance;

            /**
             * Contains the User Settings ie. Server-Address and Username
             */
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
            UploadFilesCommand = new DelegateCommand(UploadFiles);
            UploadPhotosCommand = new DelegateCommand(UploadPhotos);
            DownloadResourceCommand = new RelayCommand(DownloadResource);
            DownloadSelectedCommand = new RelayCommand(DownloadSelected);
            DeleteResourceCommand = new RelayCommand(DeleteResource);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected);
            RenameResourceCommand = new RelayCommand(RenameResource);
            MoveResourceCommand = new RelayCommand(MoveResource);
            SynchronizeFolderCommand = new RelayCommand(SynchronizeFolder);
            StopSynchronizeFolderCommand = new RelayCommand(StopSynchronizeFolder);
            MoveSelectedCommand = new RelayCommand(MoveSelected);
            //PinToStartCommand = new DelegateCommand<object>(PinToStart, CanPinToStart);
            PinToStartCommand = new DelegateCommand<object>(PinToStart);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _isNavigatingBack = true;

            if (!suspending)
            {
                _isNavigatingBack = true;
                if (Directory != null) Directory.StopDirectoryListing();
                Directory = null;
                _selectedFileOrFolder = null;
            }
            else
                _isNavigatingBack = false;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private void DownloadResource(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;

            if (resourceInfo == null)
            {
                return;
            }

            var parameters = new FileDownloadPageParameters
            {
                ResourceInfo = resourceInfo
            };

            _navigationService.Navigate(PageToken.FileDownload.ToString(), parameters.Serialize());
        }

        private void DownloadSelected(object parameter)
        {
            if (parameter is ListView listView)
            {
                var selectedItems = new List<ResourceInfo>();

                foreach (var selectedItem in listView.SelectedItems)
                {
                    if (selectedItem is ResourceInfo resourceInfo)
                    {
                        selectedItems.Add(resourceInfo);
                    }
                }

                var parameters = new FileDownloadPageParameters
                {
                    ResourceInfos = selectedItems
                };

                if (selectedItems.Count == 1)
                {
                    parameters = new FileDownloadPageParameters
                    {
                        ResourceInfo = selectedItems[0]
                    };
                }

                Directory.ToggleSelectionMode();
                _navigationService.Navigate(PageToken.FileDownload.ToString(), parameters.Serialize());
            }
        }

        private void MoveResource(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;

            if (resourceInfo == null)
            {
                return;
            }

            var parameters = new MoveFileOrFolderPageParameters
            {
                ResourceInfo = resourceInfo
            };

            _navigationService.Navigate(PageToken.MoveFileOrFolder.ToString(), parameters.Serialize());
        }

        private async void SynchronizeFolder(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;

            if (resourceInfo == null)
            {
                return;
            }

            var syncInfo = SyncDbUtils.GetFolderSyncInfoByPath(resourceInfo.Path);
            StorageFolder folder;

            try
            {
                Task<ContentDialogResult> firstRunDialog = null;
                if (syncInfo == null)
                {
                    // try to Get parent or initialize
                    syncInfo = SyncDbUtils.GetFolderSyncInfoBySubPath(resourceInfo.Path);

                    if (syncInfo == null)
                    {
                        // Initial Sync
                        syncInfo = new FolderSyncInfo()
                        {
                            Path = resourceInfo.Path
                        };

                        var folderPicker = new FolderPicker()
                        {
                            SuggestedStartLocation = PickerLocationId.Desktop
                        };

                        folderPicker.FileTypeFilter.Add(".txt");
                        StorageFolder newFolder = await folderPicker.PickSingleFolderAsync();

                        if (newFolder == null)
                        {
                            return;
                        }

                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(syncInfo.AccessListKey, newFolder);
                        IReadOnlyList<IStorageItem> subElements = await newFolder.GetItemsAsync();
                        NextcloudClient.NextcloudClient client = await ClientService.GetClient();
                        var remoteElements = await client.List(resourceInfo.Path);

                        if (subElements.Count > 0 && remoteElements.Count > 0)
                        {
                            var dialogNotEmpty = new ContentDialog
                            {
                                Title = _resourceLoader.GetString("SyncFoldersNotEmptyWarning"),
                                Content = new TextBlock()
                                {
                                    Text = _resourceLoader.GetString("SyncFoldersNotEmptyWarningDetail"),
                                    TextWrapping = TextWrapping.WrapWholeWords,
                                    Margin = new Thickness(0, 20, 0, 0)
                                },
                                PrimaryButtonText = _resourceLoader.GetString("OK"),
                                SecondaryButtonText = _resourceLoader.GetString("Cancel")
                            };

                            var dialogResult = await _dialogService.ShowAsync(dialogNotEmpty);

                            if (dialogResult != ContentDialogResult.Primary)
                            {
                                return;
                            }
                        }

                        folder = newFolder;
                        SyncDbUtils.SaveFolderSyncInfo(syncInfo);
                        StartDirectoryListing(); // This is just to update the menu flyout - maybe there is a better way

                        var dialog = new ContentDialog
                        {
                            Title = _resourceLoader.GetString("SyncStarted"),
                            Content = new TextBlock()
                            {
                                Text = _resourceLoader.GetString("SyncStartedDetail"),
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Margin = new Thickness(0, 20, 0, 0)
                            },
                            PrimaryButtonText = _resourceLoader.GetString("OK")
                        };
                        firstRunDialog = _dialogService.ShowAsync(dialog);
                    }
                    else
                    {
                        string subPath = resourceInfo.Path.Substring(syncInfo.Path.Length);
                        StorageFolder tempFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(syncInfo.AccessListKey);
                        foreach (string foldername in subPath.Split('/'))
                        {
                            if (foldername.Length > 0)
                                tempFolder = await tempFolder.GetFolderAsync(foldername);
                        }
                        folder = tempFolder;
                    }
                }
                else
                {
                    folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(syncInfo.AccessListKey);
                    // TODO catch exceptions
                }

                SyncService service = new SyncService(folder, resourceInfo, syncInfo);
                await service.StartSync();

                if (firstRunDialog != null)
                {
                    await firstRunDialog;
                }
            }
            catch (Exception e)
            {
                // ERROR Maybe AccessList timed out.
                Debug.WriteLine(e.Message);
            }
        }
        private void StopSynchronizeFolder(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;

            if (resourceInfo == null)
            {
                return;
            }

            var syncInfo = SyncDbUtils.GetFolderSyncInfoByPath(resourceInfo.Path);

            if (syncInfo != null)
            {
                // If there exists an entry for this path - stop sync command has been triggered.
                SyncDbUtils.DeleteFolderSyncInfo(syncInfo);
                StartDirectoryListing(); // This is just to update the menu flyout - maybe there is a better way
            }
        }

        private void MoveSelected(object parameter)
        {
            if (parameter is ListView listView)
            {
                var selectedItems = new List<ResourceInfo>();

                foreach (var selectedItem in listView.SelectedItems)
                {
                    if (selectedItem is ResourceInfo resourceInfo)
                    {
                        selectedItems.Add(resourceInfo);
                    }
                }

                var parameters = new MoveFileOrFolderPageParameters
                {
                    ResourceInfos = selectedItems
                };

                if (selectedItems.Count == 1)
                {
                    parameters = new MoveFileOrFolderPageParameters
                    {
                        ResourceInfo = selectedItems[0]
                    };
                }

                Directory.ToggleSelectionMode();
                _navigationService.Navigate(PageToken.MoveFileOrFolder.ToString(), parameters.Serialize());
            }
        }

        private async void DeleteResource(object parameter)
        {
            if (parameter as ResourceInfo == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString((parameter as ResourceInfo).ContentType.Equals("dav/directory") ? "DeleteFolder" : "DeleteFile"),
                Content = new TextBlock()
                {
                    Text = string.Format(_resourceLoader.GetString((parameter as ResourceInfo).ContentType.Equals("dav/directory") ? "DeleteFolder_Description" : "DeleteFile_Description"), (parameter as ResourceInfo).Name),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("Yes"),
                SecondaryButtonText = _resourceLoader.GetString("No")
            };

            var dialogResult = await _dialogService.ShowAsync(dialog);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            ShowProgressIndicator();
            await Directory.DeleteResource(parameter as ResourceInfo);
            HideProgressIndicator();
            SelectedFileOrFolder = null;
            RaisePropertyChanged(nameof(StatusBarText));
        }

        private async void DeleteSelected(object parameter)
        {
            if (parameter is ListView listView)
            {
                var selectedItems = new List<ResourceInfo>();
                foreach (var selectedItem in listView.SelectedItems)
                {
                    if (selectedItem is ResourceInfo resourceInfo)
                    {
                        selectedItems.Add(resourceInfo);
                    }
                }
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("DeleteFiles"),
                    Content = new TextBlock()
                    {
                        Text = string.Format(_resourceLoader.GetString("DeleteFiles_Description"), selectedItems.Count),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = _resourceLoader.GetString("Yes"),
                    SecondaryButtonText = _resourceLoader.GetString("No")
                };

                var dialogResult = await _dialogService.ShowAsync(dialog);

                if (dialogResult != ContentDialogResult.Primary)
                {
                    return;
                }

                Directory.ToggleSelectionMode();
                ShowProgressIndicator();
                await Directory.DeleteSelected(selectedItems);
                HideProgressIndicator();
                SelectedFileOrFolder = null;
                RaisePropertyChanged(nameof(StatusBarText));
            }
        }

        private void PinToStart(object parameter)
        {
            if (!(parameter is ResourceInfo))
                return;

            var resourceInfo = parameter as ResourceInfo;
            _tileService.CreatePinnedObject(resourceInfo);
            SelectedFileOrFolder = null;
        }

        private bool CanPinToStart(object parameter)
        {
            if (parameter is ResourceInfo)
            {
                var resourceInfo = parameter as ResourceInfo;
                return _tileService.IsTilePinned(resourceInfo);
            }

            return false;
        }

        private void UploadFiles()
        {
            var parameters = new FileUploadPageParameters
            {
                ResourceInfo = Directory.PathStack.Count > 0
                    ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo
                    : new ResourceInfo()
            };

            _navigationService.Navigate(PageToken.FileUpload.ToString(), parameters.Serialize());
        }

        private void UploadPhotos()
        {
            var parameters = new FileUploadPageParameters
            {
                ResourceInfo = Directory.PathStack.Count > 0
                    ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo
                    : new ResourceInfo(),
                PickerLocationId = PickerLocationId.PicturesLibrary
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
                    RaisePropertyChanged(nameof(StatusBarText));
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
                    SelectedFileOrFolder = null;
                    RaisePropertyChanged(nameof(StatusBarText));
                    return;
                }
            }
        }

        private async void RenameResource(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;

            if (resourceInfo == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Rename"),
                Content = new TextBox()
                {
                    Header = _resourceLoader.GetString("ChooseANewName"),
                    Text = resourceInfo.Name,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("Ok"),
                SecondaryButtonText = _resourceLoader.GetString("Cancel")
            };

            var dialogResult = await _dialogService.ShowAsync(dialog);

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            var textBox = dialog.Content as TextBox;
            var newName = textBox?.Text;

            if (string.IsNullOrEmpty(newName))
            {
                return;
            }

            ShowProgressIndicator();
            var success = await Directory.Rename(resourceInfo.Name, newName);
            HideProgressIndicator();

            if (success)
            {
                SelectedFileOrFolder = null;
                return;
            }
        }

        public DirectoryService Directory
        {
            get { return _directoryService; }
            set { SetProperty(ref _directoryService, value); }
        }

        public LocalSettings Settings
        {
            get { return _settings; }
            private set { SetProperty(ref _settings, value); }
        }

        public virtual ResourceInfo SelectedFileOrFolder
        {
            get { return _selectedFileOrFolder; }
            set
            {
                if (Directory != null && Directory.IsSelecting)
                {
                    return;
                }

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
                else
                {
                    var parameters = new FileInfoPageParameters
                    {
                        ResourceInfo = value
                    };
                    _navigationService.Navigate(PageToken.FileInfo.ToString(), parameters.Serialize());
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
            RaisePropertyChanged(nameof(StatusBarText));
        }


        public override bool CanRevertState()
        {
            return SelectedPathIndex > 0;
        }

        public override void RevertState()
        {
            SelectedPathIndex--;
        }

        public string StatusBarText
        {
            get
            {
                var folderCount = Directory.FilesAndFolders.Where(x => x.IsDirectory).Count();
                var fileCount = Directory.FilesAndFolders.Where(x => !x.IsDirectory).Count();
                return String.Format(_resourceLoader.GetString("DirectoryListStatusBarText"), fileCount + folderCount, folderCount, fileCount);                
            }
        }
    }
}