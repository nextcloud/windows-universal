using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.Web.Http;
using NextcloudApp.Converter;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudClient.Exceptions;
using Prism.Windows.Navigation;
using NextcloudClient.Types;
using Prism.Unity.Windows;
using Prism.Windows.AppModel;

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
            if (suspending)
            {
                return;
            }

            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            if (_currentFile != null)
            {
                await CachedFileManager.CompleteUpdatesAsync(_currentFile);
            }

            _cts?.Cancel();
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

            IReadOnlyList<StorageFile> storageFiles = null;

            ActivationKind = parameters.ActivationKind;

            if (parameters.FileTokens != null && parameters.FileTokens.Any())
            {
                storageFiles = new List<StorageFile>();
                foreach (var token in parameters.FileTokens)
                {
                    ((List<StorageFile>) storageFiles).Add(
                        await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token));
                }
                StorageApplicationPermissions.FutureAccessList.Clear();
            }

            if (storageFiles == null || !storageFiles.Any())
            {
                SuggestedStartLocation = parameters.PickerLocationId;

                var openPicker = new FileOpenPicker
                {
                    SuggestedStartLocation = SuggestedStartLocation
                };
                openPicker.FileTypeFilter.Add("*");
                storageFiles = await openPicker.PickMultipleFilesAsync();
            }

            UploadingFilesTitle = null;
            UploadingFileProgressText = null;

            var i = 0;
            foreach (var localFile in storageFiles)
            {
                _currentFile = localFile;

                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(localFile);

                if (storageFiles.Count > 1)
                {
                    UploadingFilesTitle = string.Format(_resourceLoader.GetString("UploadingFiles"), ++i,
                        storageFiles.Count);
                }

                try
                {
                    var properties = await localFile.GetBasicPropertiesAsync();
                    BytesTotal = (long) properties.Size;

                    // this moves the OpenReadAsync off of the UI thread and works fine...
                    var stream =
                        await
                            Task.Factory.StartNew(async () => await localFile.OpenReadAsync(), CancellationToken.None,
                                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler,
                                TaskScheduler.Default).ConfigureAwait(false);

                    IProgress<HttpProgress> progress = new Progress<HttpProgress>(ProgressHandler);
                    await
                        client.Upload(ResourceInfo.Path + localFile.Name, stream.Result, localFile.ContentType, _cts,
                            progress);
                }
                catch (ResponseError e2)
                {
                    ResponseErrorHandlerService.HandleException(e2);
                }

                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                await CachedFileManager.CompleteUpdatesAsync(localFile);

                await CoreApplication.Views.First().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UploadingFileProgressText = null;
                });

            }
            if (ActivationKind == ActivationKind.ShareTarget)
            {
                PrismUnityApplication.Current.Exit();
            }
            else
            {
                await CoreApplication.Views.First().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    _navigationService.GoBack();
                });
            }
        }

        public ActivationKind ActivationKind { get; private set; }

        private PickerLocationId SuggestedStartLocation { get; set; }

        private async void ProgressHandler(HttpProgress progressInfo)
        {
            await CoreApplication.Views.First().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (progressInfo.TotalBytesToSend != null)
                {
                    BytesTotal = (long)progressInfo.TotalBytesToSend;
                }
                BytesSend = (int)progressInfo.BytesSent;

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

        private async void Update()
        {
            var percentage = (double) BytesSend/BytesTotal;
            PercentageUploaded = (int) (percentage*100);

            UploadingFileProgressText = string.Format(
                _resourceLoader.GetString("UploadingFileProgress"),
                _converter.Convert((long) BytesSend, typeof(string), null,
                    CultureInfo.CurrentCulture.ToString()),
                _converter.Convert(BytesTotal, typeof(string), null,
                    CultureInfo.CurrentCulture.ToString())
                );
        }
    }
}