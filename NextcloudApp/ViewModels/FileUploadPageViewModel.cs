using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.IO;

namespace NextcloudApp.ViewModels
{
    public class FileUploadPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private ResourceInfo _resourceInfo;
        private string _uploadingFilesTitle;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        private string _uploadingFileProgressText;
        private int _percentageUploaded;
        private int _bytesSend;
        private long _bytesTotal;
        private readonly BytesToHumanReadableConverter _converter;
        private CancellationTokenSource _cts;
        private StorageFile _currentFile;
        private bool _waitingForServerResponse;

        public FileUploadPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            _converter = new BytesToHumanReadableConverter();
        }

        public string UploadingFilesTitle
        {
            get { return _uploadingFilesTitle; }
            private set { SetProperty(ref _uploadingFilesTitle, value); }
        }

        public string UploadingFileProgressText
        {
            get { return _uploadingFileProgressText; }
            private set { SetProperty(ref _uploadingFileProgressText, value); }
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
            var parameters = FileUploadPageParameters.Deserialize(e.Parameter);
            var resourceInfo = parameters?.ResourceInfo;
            if (resourceInfo == null)
            {
                return;
            }
            ResourceInfo = resourceInfo;
            SuggestedStartLocation = parameters.PickerLocationId;
            UploadingFilesTitle = null;
            UploadingFileProgressText = null;
            var i = 0;

            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = SuggestedStartLocation
            };
            openPicker.FileTypeFilter.Add("*");
            var storageFiles = await openPicker.PickMultipleFilesAsync();

            foreach (var localFile in storageFiles)
            {
                _currentFile = localFile;

                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(localFile);

                if (storageFiles.Count > 1)
                {
                    UploadingFilesTitle = string.Format(_resourceLoader.GetString("UploadingFiles"), ++i, storageFiles.Count);
                }

                try
                {
                    var properties = await localFile.GetBasicPropertiesAsync();
                    BytesTotal = (long) properties.Size;

                    using (var stream = await localFile.OpenAsync(FileAccessMode.Read))
                    {
                        var targetStream = stream.AsStreamForRead();

                        IProgress<WebDavProgress> progress = new Progress<WebDavProgress>(ProgressHandler);
                        await client.Upload(ResourceInfo.Path + localFile.Name, targetStream, localFile.ContentType, progress, _cts.Token);
                    }
                }
                catch (ResponseError e2)
                {
                    ResponseErrorHandlerService.HandleException(e2);
                }

                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                await CachedFileManager.CompleteUpdatesAsync(localFile);

                UploadingFileProgressText = null;

            }
            _navigationService.GoBack();
        }

        private PickerLocationId SuggestedStartLocation { get; set; }

        private async void ProgressHandler(WebDavProgress progressInfo)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                BytesTotal = (long)progressInfo.TotalBytes;
                BytesSend = (int)progressInfo.Bytes;

                WaitingForServerResponse = BytesSend == BytesTotal;
            });
        }

        public bool WaitingForServerResponse
        {
            get { return _waitingForServerResponse; }
            private set { SetProperty(ref _waitingForServerResponse, value); }
        }

        public int PercentageUploaded
        {
            get { return _percentageUploaded; }
            private set { SetProperty(ref _percentageUploaded, value); }
        }

        public int BytesSend
        {
            get { return _bytesSend; }
            private set {
                if (SetProperty(ref _bytesSend, value))
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

        private void Update()
        {
            var percentage = (double)BytesSend / BytesTotal;
            PercentageUploaded = (int)(percentage * 100);

            UploadingFileProgressText = string.Format(
            _resourceLoader.GetString("UploadingFileProgress"),
            _converter.Convert((long)BytesSend, typeof(string), null,
                CultureInfo.CurrentCulture.ToString()),
            _converter.Convert(BytesTotal, typeof(string), null,
                CultureInfo.CurrentCulture.ToString())
            );
        }
    }
}