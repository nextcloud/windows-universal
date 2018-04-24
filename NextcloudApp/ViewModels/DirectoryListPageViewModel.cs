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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media;

namespace NextcloudApp.ViewModels
{
    public class DirectoryListPageViewModel : ViewModel
    {
        private LocalSettings _settings;
        private DirectoryService _directoryService;
        private readonly TileService _tileService;
        private ResourceInfo _selectedFileOrFolder;
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
        public ICommand UploadFilesCommand { get; }
        public ICommand UploadPhotosCommand { get; }
        public ICommand DownloadResourceCommand { get; }
        public ICommand DownloadSelectedCommand { get; }
        public ICommand DeleteResourceCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand RenameResourceCommand { get; }
        public ICommand MoveResourceCommand { get; }
        public ICommand SynchronizeFolderCommand { get; }
        public ICommand SynchronizeThisFolderCommand { get; }
        public ICommand StopSynchronizeFolderCommand { get; }
        public ICommand StopSynchronizeThisFolderCommand { get; }
        public ICommand MoveSelectedCommand { get; }
        public ICommand PinToStartCommand { get; }
        public ICommand SelectToggleCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }

        public DirectoryListPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            _tileService = TileService.Instance;

            /**
             * Contains the User Settings ie. Server-Address and Username
             */
            Settings = SettingsService.Default.Value.LocalSettings;

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

            GroupByTypeAscendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByTypeAscending();
                SelectedFileOrFolder = null;
            });

            GroupByTypeDescendingCommand = new DelegateCommand(() =>
            {
                Directory.GroupByTypeDescending();
                SelectedFileOrFolder = null;
            });

            SelectedFileOrFolder = null;

            RefreshCommand = new DelegateCommand(async () =>
            {
                ShowProgressIndicator();
                await Directory.Refresh();
                HideProgressIndicator();
                SelectedFileOrFolder = null;
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
            SynchronizeThisFolderCommand = new RelayCommand(SynchronizeThisFolder);
            StopSynchronizeFolderCommand = new RelayCommand(StopSynchronizeFolder);
            StopSynchronizeThisFolderCommand = new RelayCommand(StopSynchronizeThisFolder);
            MoveSelectedCommand = new RelayCommand(MoveSelected);
            PinToStartCommand = new DelegateCommand<object>(PinToStart, CanPinToStart);
            ToggleFavoriteCommand = new RelayCommand(ToggleFavorite);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            Directory = DirectoryService.Instance;

            var parameters = DirectoryListPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;

            if (resourceInfo != null)
            {
                Directory.RebuildPathStackFromResourceInfo(resourceInfo);
            }

            _isNavigatingBack = false;
            StartDirectoryListing();
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _isNavigatingBack = true;

            if (!suspending)
            {
                _isNavigatingBack = true;
                Directory?.StopDirectoryListing();
                Directory = null;
                _selectedFileOrFolder = null;
            }
            else
            {
                _isNavigatingBack = false;
            }

            Directory = null;

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

            await SychronizeFolder(resourceInfo);
        }

        private async void SynchronizeThisFolder(object parameter)
        {
            var resourceInfo = Directory.PathStack.Count > 0 ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo : null;

            if (resourceInfo == null)
            {
                return;
            }

            await SychronizeFolder(resourceInfo);
            await Directory.StartDirectoryListing();
            SelectedFileOrFolder = null;
        }

        private async Task SychronizeFolder(ResourceInfo resourceInfo)
        {
            if (resourceInfo == null)
            {
                return;
            }

            var syncInfo = SyncDbUtils.GetFolderSyncInfoByPath(resourceInfo.Path);

            try
            {
                Task<ContentDialogResult> firstRunDialog = null;
                StorageFolder folder;
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
                        var newFolder = await folderPicker.PickSingleFolderAsync();

                        if (newFolder == null)
                        {
                            return;
                        }

                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(syncInfo.AccessListKey, newFolder);
                        var subElements = await newFolder.GetItemsAsync();
                        var client = await ClientService.GetClient();
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
                            Content = new TextBlock
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
                        var subPath = resourceInfo.Path.Substring(syncInfo.Path.Length);
                        var tempFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(syncInfo.AccessListKey);
                        foreach (var foldername in subPath.Split('/'))
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

                var service = new SyncService(folder, resourceInfo, syncInfo, _resourceLoader);
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

            StopSynchronizeFolder(resourceInfo);
        }

        private void StopSynchronizeThisFolder(object parameter)
        {
            var resourceInfo = Directory.PathStack.Count > 0 ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo : null;

            if (resourceInfo == null)
            {
                return;
            }

            StopSynchronizeFolder(resourceInfo);
        }      

        private void StopSynchronizeFolder(ResourceInfo resourceInfo)
        {
            if (resourceInfo == null)
            {
                return;
            }

            var syncInfo = SyncDbUtils.GetFolderSyncInfoByPath(resourceInfo.Path);

            if (syncInfo == null)
            {
                return;
            }
            // If there exists an entry for this path - stop sync command has been triggered.
            SyncDbUtils.DeleteFolderSyncInfo(syncInfo);
            StartDirectoryListing(); // This is just to update the menu flyout - maybe there is a better way
        }

        private void MoveSelected(object parameter)
        {
            if (!(parameter is ListView listView))
            {
                return;
            }
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

        private async void DeleteResource(object parameter)
        {
            if (!(parameter is ResourceInfo))
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(((ResourceInfo) parameter).ContentType.Equals("dav/directory") ? "DeleteFolder" : "DeleteFile"),
                Content = new TextBlock
                {
                    Text = string.Format(_resourceLoader.GetString(((ResourceInfo) parameter).ContentType.Equals("dav/directory") ? "DeleteFolder_Description" : "DeleteFile_Description"), ((ResourceInfo) parameter).Name),
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
            await Directory.DeleteResource((ResourceInfo) parameter);
            HideProgressIndicator();
            SelectedFileOrFolder = null;
        }

        private async void DeleteSelected(object parameter)
        {
            if (!(parameter is ListView listView))
            {
                return;
            }
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
        }

        private void PinToStart(object parameter)
        {
            if (!(parameter is ResourceInfo))
                return;

            var resourceInfo = (ResourceInfo) parameter;
            _tileService.CreatePinnedObject(resourceInfo);
            SelectedFileOrFolder = null;
        }

        private bool CanPinToStart(object parameter)
        {
            if (!(parameter is ResourceInfo))
            {
                return false;
            }
            var resourceInfo = (ResourceInfo) parameter;
            return !_tileService.IsTilePinned(resourceInfo);
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

        private async void ToggleFavorite(object parameter)
        {
            var res = parameter as ResourceInfo;

            if (res == null)
            {
                return;
            }

            ShowProgressIndicator();
            await Directory.ToggleFavorite(res);            
            await Directory.Refresh();
            HideProgressIndicator();
            SelectedFileOrFolder = null;
        }

        private async void CreateDirectory()
        {
            while (true)
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("CreateNewFolder"),
                    Content = new TextBox
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

                if (dialogResult == ContentDialogResult.Primary)
                {
                    continue;
                }
                SelectedFileOrFolder = null;
                return;
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
                Content = new TextBox
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

            if (!success)
            {
                return;
            }
            SelectedFileOrFolder = null;
        }

        public DirectoryService Directory
        {
            get => _directoryService;
            set => SetProperty(ref _directoryService, value);
        }

        public LocalSettings Settings
        {
            get => _settings;
            private set => SetProperty(ref _settings, value);
        }

        public virtual ResourceInfo SelectedFileOrFolder
        {
            get => _selectedFileOrFolder;
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
                }
                else
                {
                    //if (ListView != null && ListView.SelectionMode == ListViewSelectionMode.Single)
                    //{
                    //    var el = (FrameworkElement)ListView.ContainerFromIndex(0);
                    //    if (el == null)
                    //    {
                    //        return;
                    //    }
                    //    var img = FindElementByName<Image>(el, "Thumbnail");
                    //    if (img == null)
                    //    {
                    //        return;
                    //    }
                    //    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("image", img);
                    //}
                    
                    var parameters = new FileInfoPageParameters
                    {
                        ResourceInfo = value
                    };
                    _navigationService.Navigate(PageToken.FileInfo.ToString(), parameters.Serialize());
                }
            }
        }

        /// <summary>
        /// Extension method for a FrameworkElement that searches for a child element by type and name.
        /// </summary>
        /// <typeparam name="T">The type of the child element to search for.</typeparam>
        /// <param name="element">The parent framework element.</param>
        /// <param name="sChildName">The name of the child element to search for.</param>
        /// <returns>The matching child element, or null if none found.</returns>
        public static T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            Debug.WriteLine("[FindElementByName] ==> element [{0}] sChildName [{1}] T [{2}]", element, sChildName, typeof(T).ToString());

            T childElement = null;

            //
            // Spin through immediate children of the starting element.
            //
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                // Get next child element.
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                Debug.WriteLine("Found child [{0}]", child);

                // Do we have a child?
                if (child == null)
                    continue;

                // Is child of desired type and name?
                if (child is T && child.Name.Equals(sChildName))
                {
                    // Bingo! We found a match.
                    childElement = (T)child;
                    Debug.WriteLine("Found matching element [{0}]", childElement);
                    break;
                } // if

                // Recurse and search through this child's descendants.
                childElement = FindElementByName<T>(child, sChildName);

                // Did we find a matching child?
                if (childElement != null)
                    break;
            } // for

            Debug.WriteLine("[FindElementByName] <== childElement [{0}]", childElement);
            return childElement;
        }

        public ListView ListView { get; internal set; }

        private async void StartDirectoryListing()
        {
            ShowProgressIndicator();

            await Directory.StartDirectoryListing();

            HideProgressIndicator();
            SelectedFileOrFolder = null;
        }
        
        public override bool CanRevertState()
        {
            return Directory.PathStack.Count > 1;
        }

        public override void RevertState()
        {
            Directory.PathStack.RemoveAt(Directory.PathStack.Count - 1);
        }
    }
}