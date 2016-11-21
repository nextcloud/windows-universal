using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.Web.Http;
using NextcloudApp.Converter;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudClient.Exceptions;
using Prism.Windows.Navigation;
using NextcloudClient.Types;
using Prism.Windows.AppModel;

namespace NextcloudApp.ViewModels
{
    public class SingleFileDownloadPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private CancellationTokenSource _cts;
        private readonly BytesToHumanReadableConverter _converter;
        private readonly IResourceLoader _resourceLoader;
        private int _bytesDownloaded;
        private int _percentageDownloaded;
        private long _bytesTotal;
        private ResourceInfo _resourceInfo;
        private string _downloadingFileProgressText;
        private StorageFile _currentFile;
        private bool _isIndeterminate;

        public SingleFileDownloadPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader)
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

            var client = ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            _cts = new CancellationTokenSource();

            var parameters = SingleFileDownloadPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;
            if (resourceInfo == null)
            {
                return;
            }
            ResourceInfo = resourceInfo;

            if (ResourceInfo.ContentType == "dav/directory")
            {
                ResourceInfo.Name = ResourceInfo.Name + ".zip";
                ResourceInfo.ContentType = "application/zip";
            }
            
            var savePicker = new FileSavePicker();

            savePicker.FileTypeChoices.Add(ResourceInfo.ContentType, new List<string> { Path.GetExtension(ResourceInfo.Name) });
            savePicker.SuggestedFileName = ResourceInfo.Name;

            var localFile = await savePicker.PickSaveFileAsync();
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
                IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);
                Windows.Storage.Streams.IBuffer buffer;
                switch (ResourceInfo.ContentType)
                {
                    case "application/zip":
                        buffer = await client.DownloadDirectoryAsZip(ResourceInfo.Path, _cts, progress);
                        break;
                    default:
                        buffer = await client.Download(ResourceInfo.Path + "/" + ResourceInfo.Name, _cts, progress);
                        break;
                }
                await FileIO.WriteBufferAsync(localFile, buffer);
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

            _navigationService.GoBack();
        }

        private async void ProgressHandler(HttpProgress progressInfo)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (progressInfo.TotalBytesToReceive != null)
                {
                    BytesTotal = (long)progressInfo.TotalBytesToReceive;
                }
                BytesDownloaded = (int)progressInfo.BytesReceived;
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