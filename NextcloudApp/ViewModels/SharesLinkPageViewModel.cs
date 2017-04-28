using System.Collections.Generic;
using System.Linq.Expressions;
using System.Windows.Input;
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

namespace NextcloudApp.ViewModels
{
    public class SharesLinkPageViewModel : DirectoryListPageViewModel
    {
        private TileService _tileService;
        private ResourceInfo _selectedFileOrFolder;
        private readonly INavigationService _navigationService;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        private bool _isNavigatingBack;

        public SharesLinkPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
            : base(navigationService, resourceLoader, dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            _tileService = TileService.Instance;

        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;

            if (e.Parameter != null)
            {
                var parameter = FileInfoPageParameters.Deserialize(e.Parameter);
                SelectedFileOrFolder = parameter?.ResourceInfo;
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

        public override ResourceInfo SelectedFileOrFolder
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
                    var parameters = new FileInfoPageParameters
                    {
                        ResourceInfo = value
                    };

                    if (parameters.ResourceInfo != null)
                    {
                        Directory.PathStack.Clear();

                        Directory.PathStack.Add(new PathInfo
                        {
                            ResourceInfo = new ResourceInfo()
                            {
                                Name = "Nextcloud",
                                Path = "/"
                            },
                            IsRoot = true
                        });

                        string[] pathSplit = value.Path.Split('/');
                        foreach (string pathPart in pathSplit)
                        {
                            if (pathPart.Length > 0)
                            {
                                Directory.PathStack.Add(new PathInfo
                                {
                                    ResourceInfo = new ResourceInfo()
                                    {
                                        Name = pathPart,
                                        Path = "/" + ((Directory.PathStack[Directory.PathStack.Count - 1]).ResourceInfo.Path + "/" + pathPart).TrimStart('/')
                                    },
                                    IsRoot = false
                                });
                            }
                        }
                    }
                    _navigationService.Navigate(PageToken.DirectoryList.ToString(), null);
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
        
        private async void StartDirectoryListing()
        {
            ShowProgressIndicator();

            await Directory.StartDirectoryListing(null, "sharesLink");

            HideProgressIndicator();
            RaisePropertyChanged(nameof(DirectoryListPageViewModel.StatusBarText));
        }
    }
}