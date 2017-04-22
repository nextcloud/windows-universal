using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using NextcloudApp.Converter;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudClient.Exceptions;
using Prism.Windows.Navigation;
using NextcloudClient.Types;
using Prism.Windows.AppModel;
using DecaTec.WebDav;
using Windows.Storage.AccessCache;
using System.Threading.Tasks;

namespace NextcloudApp.ViewModels
{
    public class FileDownloadPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private CancellationTokenSource _cts;
        private readonly BytesToHumanReadableConverter _converter;
        private readonly IResourceLoader _resourceLoader;
        private int _bytesDownloaded;
        private int _percentageDownloaded;
        private long _bytesTotal;
        private ResourceInfo _resourceInfo;
        private List<ResourceInfo> _resourceInfos;
        private string _downloadingFileProgressText;
        private StorageFile _currentFile;
        private bool _isIndeterminate;

        public FileDownloadPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _converter = new BytesToHumanReadableConverter();
        }

        public override async void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);
            if (!suspending)
            {
                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                if (_currentFile != null)
                {
                    await CachedFileManager.CompleteUpdatesAsync(_currentFile);
                }
                _cts?.Cancel();
            }
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);

            var client = await ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            _cts = new CancellationTokenSource();

            var parameters = FileDownloadPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;
            var resourceInfos = parameters?.ResourceInfos;

            if (resourceInfo != null)
            {
                ResourceInfo = resourceInfo;

                await Download(resourceInfo, client, null);
            }
            else if (resourceInfos != null)
            {
                ResourceInfos = resourceInfos;

                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add(".zip");
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                }
                else
                {
                    return;
                }

                foreach (var resInfo in ResourceInfos)
                {
                    await Download(resInfo, client, folder);
                }
            }
            else
            {
                return;
            }

            _navigationService.GoBack();
        }

        private async Task Download(ResourceInfo resourceInfo, NextcloudClient.NextcloudClient client, StorageFolder folder)
        {
            if (resourceInfo.ContentType == "dav/directory")
            {
                ResourceInfo = new ResourceInfo
                {
                    Name = resourceInfo.Name + ".zip",
                    ContentType = "application/zip",
                    Path = resourceInfo.Path
                };
            }
            else
            {
                ResourceInfo = resourceInfo;
            }

            var savePicker = new FileSavePicker();

            savePicker.FileTypeChoices.Add(ResourceInfo.ContentType, new List<string> { Path.GetExtension(ResourceInfo.Name) });
            savePicker.SuggestedFileName = ResourceInfo.Name;

            StorageFile localFile;
            if (folder != null)
            {
                try
                {
                    localFile = await folder.CreateFileAsync(
                        savePicker.SuggestedFileName,
                        CreationCollisionOption.GenerateUniqueName);
                }
                catch (FileNotFoundException ex)
                {
                    //this.textBlock.Text = "Error " + ex;
                    return;
                }
            }
            else
            {
                localFile = await savePicker.PickSaveFileAsync();
            }
            if (localFile == null)
            {
                return;
            }

            _currentFile = localFile;

            // Prevent updates to the remote version of the file until
            // we finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(localFile);

            try
            {
                IProgress<WebDavProgress> progress = new Progress<WebDavProgress>(ProgressHandler);

                using (var randomAccessStream = await localFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    Stream targetStream = randomAccessStream.AsStreamForWrite();

                    switch (resourceInfo.ContentType)
                    {
                        case "dav/directory":
                            await client.DownloadDirectoryAsZip(ResourceInfo.Path, targetStream, progress, _cts.Token);
                            break;
                        default:
                            await client.Download(ResourceInfo.Path + "/" + ResourceInfo.Name, targetStream, progress, _cts.Token);
                            break;
                    }
                }
            }
            catch (ResponseError e2)
            {
                ResponseErrorHandlerService.HandleException(e2);
            }

            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            var status = await CachedFileManager.CompleteUpdatesAsync(localFile);
            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                //this.textBlock.Text = "Path " + file.Name + " was saved.";
            }
            else
            {
                //this.textBlock.Text = "Path " + file.Name + " couldn't be saved.";
            }
            return;
        }

        private async void ProgressHandler(WebDavProgress progressInfo)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                BytesTotal = (long)progressInfo.TotalBytes;
                BytesDownloaded = (int)progressInfo.Bytes;
            });
        }

        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            private set { SetProperty(ref _isIndeterminate, value); }
        }

        public int PercentageDownloaded
        {
            get { return _percentageDownloaded; }
            private set { SetProperty(ref _percentageDownloaded, value); }
        }

        public int BytesDownloaded
        {
            get { return _bytesDownloaded; }
            private set
            {
                if (SetProperty(ref _bytesDownloaded, value))
                {
                    Update();
                }
            }
        }

        public long BytesTotal
        {
            get { return _bytesTotal; }
            private set
            {
                if (SetProperty(ref _bytesTotal, value))
                {
                    Update();
                }
            }
        }

        public ResourceInfo ResourceInfo
        {
            get { return _resourceInfo; }
            private set { SetProperty(ref _resourceInfo, value); }
        }

        public List<ResourceInfo> ResourceInfos
        {
            get { return _resourceInfos; }
            private set { SetProperty(ref _resourceInfos, value); }
        }

        public string DownloadingFileProgressText
        {
            get { return _downloadingFileProgressText; }
            private set { SetProperty(ref _downloadingFileProgressText, value); }
        }

        private void Update()
        {
            if (BytesTotal == 0)
            {
                IsIndeterminate = true;
                DownloadingFileProgressText = string.Format(
                    _resourceLoader.GetString("DownloadingFileProgressIndeterminate"),
                    _converter.Convert((long)BytesDownloaded, typeof(string), null, CultureInfo.CurrentCulture.ToString())
                );
                return;
            }

            var percentage = (double)BytesDownloaded / BytesTotal;
            PercentageDownloaded = (int)(percentage * 100);

            IsIndeterminate = false;
            DownloadingFileProgressText = string.Format(
                _resourceLoader.GetString("DownloadingFileProgress"),
                _converter.Convert((long)BytesDownloaded, typeof(string), null, CultureInfo.CurrentCulture.ToString()),
                _converter.Convert(BytesTotal, typeof(string), null, CultureInfo.CurrentCulture.ToString())
            );
        }
    }
}
