using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Types;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;

namespace NextcloudApp.ViewModels
{
    public class DirectoryListPageViewModel : ViewModel
    {
        private Settings _settngs;
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
        public ICommand RefreshCommand { get; private set; }
        public ICommand CreateDirectoryCommand { get; private set; }
        public ICommand UploadFilesCommand { get; private set; }
        public ICommand UploadPhotosCommand { get; private set; }
        public ICommand DeleteResourceCommand { get; private set; }
        public ICommand RenameResourceCommand { get; private set; }
        public ICommand PinResourceCommand { get; private set; }

        public DirectoryListPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            _tileService = TileService.Instance;
            Settings = SettingsService.Instance.Settings;
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
            UploadFilesCommand = new DelegateCommand(UploadFiles);
            UploadPhotosCommand = new DelegateCommand(UploadPhotos);
            DeleteResourceCommand = new RelayCommand(DeleteResource);
            RenameResourceCommand = new RelayCommand(RenameResource);
            PinResourceCommand = new RelayCommand(PinResource);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;

            if (e.Parameter != null)
            {
                var resourceInfo = ResourceInfo.Deserialize(e.Parameter as string);
                OpenFolder(resourceInfo);
            }
        }

        private async void OpenFolder(ResourceInfo resourceInfo)
        {
            _navigationService.RemoveAllPages(PageTokens.DirectoryList.ToString());
            ShowProgressIndicator();
            try
            {
                if (!await Directory.TryOpenPath(resourceInfo))
                {
                    var dialog = new MessageDialog(_resourceLoader.GetString("Folder_NotFound"));
                    await _dialogService.ShowAsync(dialog);
                    App.Current.Exit();
                }
                SelectedPathIndex = Directory.PathStack.Count - 1;
            }
            finally
            {
                HideProgressIndicator();
            }
        }
        
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

        private async void DeleteResource(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;
            if (resourceInfo == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(resourceInfo.ContentType.Equals("dav/directory") ? "DeleteFolder" : "DeleteFile"),
                Content = new TextBlock()
                {
                    Text = string.Format(_resourceLoader.GetString(resourceInfo.ContentType.Equals("dav/directory") ? "DeleteFolder_Description" : "DeleteFile_Description"), resourceInfo.Name),
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
            await Directory.DeleteResource(resourceInfo);
            await _tileService.RemovePinnedObject(resourceInfo);
            HideProgressIndicator();
        }

        private void PinResource(object parameter)
        {
            var resourceInfo = parameter as ResourceInfo;
            if (resourceInfo == null)
            {
                return;
            }
            _tileService.CreatePinnedObject(resourceInfo);
        }

        private void UploadFiles()
        {
            var parameters = new FileUploadPageParameters
            {
                ResourceInfo = Directory.PathStack.Count > 0
                    ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo
                    : new ResourceInfo()
            };
            _navigationService.Navigate(PageTokens.FileUpload.ToString(), parameters.Serialize());
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
            _navigationService.Navigate(PageTokens.FileUpload.ToString(), parameters.Serialize());
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
                return;
            }
        }

        public DirectoryService Directory
        {
            get { return _directoryService; }
            private set { SetProperty(ref _directoryService, value); }
        }

        public Settings Settings
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
                if (value.IsDirectory())
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
                    _navigationService.Navigate(PageTokens.FileInfo.ToString(), parameters.Serialize());
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