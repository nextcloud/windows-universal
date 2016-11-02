using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using NextcloudApp.Converter;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using Prism.Commands;
using Prism.Windows.Navigation;
using NextcloudClient.Types;
using Prism.Windows.AppModel;

namespace NextcloudApp.ViewModels
{
    public class FileInfoPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        private DirectoryService _directoryService;
        private ResourceInfo _resourceInfo;
        private string _fileExtension;
        private string _fileName;
        private string _fileSizeString;
        private int _selectedPathIndex = -1;
        private BitmapImage _thumbnail;
        public ICommand DownloadCommand { get; private set; }
        public ICommand DeleteResourceCommand { get; private set; }

        public FileInfoPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;

            Directory = DirectoryService.Instance;

            //DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            //dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(ShareImageHandler);
            DownloadCommand = new DelegateCommand(() =>
            {
                var parameters = new SingleFileDownloadPageParameters
                {
                    ResourceInfo = ResourceInfo
                };
                _navigationService.Navigate(PageTokens.SingleFileDownload.ToString(), parameters.Serialize());
            });
            DeleteResourceCommand = new DelegateCommand(DeleteResource);
        }
        
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            foreach(var path in Directory.PathStack)
            {
                PathStack.Add(path);
            }
            var parameters = FileInfoPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;
            if (resourceInfo == null)
            {
                return;
            }
            PathStack.Add(new PathInfo
            {
                ResourceInfo = resourceInfo
            });
            ResourceInfo = resourceInfo;
            FileExtension = Path.GetExtension(ResourceInfo.Name);
            FileName = Path.GetFileNameWithoutExtension(ResourceInfo.Name);
            var converter = new BytesToHumanReadableConverter();
            FileSizeString = LocalizationService.Instance.GetString(
                "FileSizeString",
                converter.Convert(ResourceInfo.Size, typeof(string), null, CultureInfo.CurrentCulture.ToString()),
                ResourceInfo.Size
            );
            DownloadPreviewImages();
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (!suspending)
            {
                Directory.StopDirectoryListing();
                Directory = null;
            }
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private async void DownloadPreviewImages()
        {
            var client = ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            switch (SettingsService.Instance.Settings.PreviewImageDownloadMode)
            {
                case PreviewImageDownloadMode.Always:
                    break;
                case PreviewImageDownloadMode.WiFiOnly:
                    var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                    // connectionProfile can be null (e.g. airplane mode)
                    if (connectionProfile == null || !connectionProfile.IsWlanConnectionProfile)
                    {
                        return;
                    }
                    break;
                case PreviewImageDownloadMode.Never:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            try
            {
                Stream stream = null;
                try
                {
                    stream = await client.GetThumbnail(ResourceInfo, 120, 120);
                }
                catch (ResponseError e)
                {
                    ResponseErrorHandlerService.HandleException(e);
                }

                if (stream == null)
                {
                    return;
                }
                var bitmap = new BitmapImage();
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);
                    memStream.Position = 0;
                    bitmap.SetSource(memStream.AsRandomAccessStream());
                }
                Thumbnail = bitmap;
            }
            catch (ResponseError)
            {
                Thumbnail = new BitmapImage
                {
                    UriSource = new Uri("ms-appx:///Assets/Images/ThumbnailNotFound.png")
                };
            }
        }

        public BitmapImage Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                if (_thumbnail == value)
                {
                    return;
                }
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        public string FileSizeString
        {
            get { return _fileSizeString; }
            private set { SetProperty(ref _fileSizeString, value); }
        }

        public string FileExtension
        {
            get { return _fileExtension; }
            private set { SetProperty(ref _fileExtension, value); }
        }

        public string FileName
        {
            get { return _fileName; }
            private set { SetProperty(ref _fileName, value); }
        }

        public ResourceInfo ResourceInfo
        {
            get { return _resourceInfo; }
            private set { SetProperty(ref _resourceInfo, value); }
        }

        public DirectoryService Directory
        {
            get { return _directoryService; }
            private set { SetProperty(ref _directoryService, value); }
        }

        public ObservableCollection<PathInfo> PathStack { get; } = new ObservableCollection<PathInfo>();

        public int SelectedPathIndex
        {
            get { return _selectedPathIndex; }
            set
            {
                if (!SetProperty(ref _selectedPathIndex, value))
                {
                    return;
                }

                if (_selectedPathIndex == PathStack.Count - 1)
                {
                    // file name was selected, just return
                    return;
                }

                while (Directory.PathStack.Count > 0 && Directory.PathStack.Count > _selectedPathIndex + 1)
                {
                    Directory.PathStack.RemoveAt(Directory.PathStack.Count - 1);
                }

                _navigationService.GoBack();
            }
        }

        private async void DeleteResource()
        {
            if (ResourceInfo == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(ResourceInfo.ContentType.Equals("dav/directory") ? "DeleteFolder" : "DeleteFile"),
                Content = new TextBlock()
                {
                    Text = string.Format(_resourceLoader.GetString(ResourceInfo.ContentType.Equals("dav/directory") ? "DeleteFolder_Description" : "DeleteFile_Description"), ResourceInfo.Name),
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
            var success = await DirectoryService.Instance.DeleteResource(ResourceInfo);
            HideProgressIndicator();
            if (success)
            {
                _navigationService.GoBack();
            }
        }
    }
}