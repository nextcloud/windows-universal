//using Windows.ApplicationModel.Activation;
//using Windows.Foundation;
//using Windows.Storage;
//using Windows.Storage.AccessCache;
//using Windows.Storage.Pickers.Provider;
//using Windows.Storage.Provider;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Navigation;
//using NextcloudApp.Models;
//using NextcloudApp.Services;
//using NextcloudClient.Types;
//using Prism.Commands;
//using Prism.Unity.Windows;
//using Prism.Windows.Navigation;
//using Prism.Windows.AppModel;

namespace NextcloudApp.ViewModels
{
    public class FileSavePickerPageViewModel : ViewModel
    {
        //TODO: Find out, why this is not working on WP10
        //SEE: https://github.com/nextcloud/windows-universal/issues/32

        //private LocalSettings _settngs;
        //private DirectoryService _directoryService;
        //private ResourceInfo _selectedFileOrFolder;
        //private int _selectedPathIndex = -1;
        //private readonly INavigationService _navigationService;
        //private readonly IResourceLoader _resourceLoader;
        //private readonly DialogService _dialogService;
        //private bool _isNavigatingBack;
        //private FileSavePickerUI _fileSavePickerUI;

        //public ICommand GroupByNameAscendingCommand { get; private set; }
        //public ICommand GroupByNameDescendingCommand { get; private set; }
        //public ICommand GroupByDateAscendingCommand { get; private set; }
        //public ICommand GroupByDateDescendingCommand { get; private set; }
        //public ICommand GroupBySizeAscendingCommand { get; private set; }
        //public ICommand GroupBySizeDescendingCommand { get; private set; }
        //public ICommand RefreshCommand { get; private set; }
        //public ICommand CreateDirectoryCommand { get; private set; }
        //public object CancelFolderSelectionCommand { get; private set; }
        //public object StartUploadCommand { get; private set; }

        //public FileSavePickerPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        //{
        //    _navigationService = navigationService;
        //    _resourceLoader = resourceLoader;
        //    _dialogService = dialogService;
        //    Settings = SettingsService.Instance.LocalSettings;
        //    GroupByNameAscendingCommand = new DelegateCommand(() =>
        //    {
        //        Directory.GroupByNameAscending();
        //        SelectedFileOrFolder = null;
        //    });
        //    GroupByNameDescendingCommand = new DelegateCommand(() =>
        //    {
        //        Directory.GroupByNameDescending();
        //        SelectedFileOrFolder = null;
        //    });
        //    GroupByDateAscendingCommand = new DelegateCommand(() =>
        //    {
        //        Directory.GroupByDateAscending();
        //        SelectedFileOrFolder = null;
        //    });
        //    GroupByDateDescendingCommand = new DelegateCommand(() =>
        //    {
        //        Directory.GroupByDateDescending();
        //        SelectedFileOrFolder = null;
        //    });
        //    GroupBySizeAscendingCommand = new DelegateCommand(() =>
        //    {
        //        Directory.GroupBySizeAscending();
        //        SelectedFileOrFolder = null;
        //    });
        //    GroupBySizeDescendingCommand = new DelegateCommand(() =>
        //    {
        //        Directory.GroupBySizeDescending();
        //        SelectedFileOrFolder = null;
        //    });
        //    RefreshCommand = new DelegateCommand(async () =>
        //    {
        //        ShowProgressIndicator();
        //        await Directory.Refresh();
        //        HideProgressIndicator();
        //    });
        //    CreateDirectoryCommand = new DelegateCommand(CreateDirectory);
        //    StartUploadCommand = new DelegateCommand(StartUpload);
        //    CancelFolderSelectionCommand = new DelegateCommand(CancelFolderSelection);
        //}

        //public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        //{
        //    base.OnNavigatedTo(e, viewModelState);

        //    var app = PrismUnityApplication.Current as App;
        //    var fileSavePickerActivatedEventArgs = app?.ActivatedEventArgs as FileSavePickerActivatedEventArgs;
        //    if (fileSavePickerActivatedEventArgs != null)
        //    {
        //        _fileSavePickerUI = fileSavePickerActivatedEventArgs.FileSavePickerUI;
        //        _fileSavePickerUI.TargetFileRequested += new TypedEventHandler<FileSavePickerUI, TargetFileRequestedEventArgs>(OnTargetFileRequested);
        //    }

        //    var cachedFileUpdaterActivatedEventArgs = app?.ActivatedEventArgs as CachedFileUpdaterActivatedEventArgs;
        //    if (cachedFileUpdaterActivatedEventArgs != null)
        //    {
        //        //cachedFileUpdaterActivatedEventArgs.FileSavePickerUI.TargetFileRequested += OnTargetFileRequested;
        //    }

        //    //var parameters = ShareTargetPageParameters.Deserialize(e.Parameter);

        //    //ActivationKind = parameters.ActivationKind;

        //    Directory = DirectoryService.Instance;
        //    StartDirectoryListing();
        //    _isNavigatingBack = false;
        //}

        //public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState,
        //    bool suspending)
        //{
        //    base.OnNavigatingFrom(e, viewModelState, suspending);
        //    _isNavigatingBack = true;
        //    if (!suspending)
        //    {
        //        Directory.StopDirectoryListing();
        //        Directory = null;
        //        _selectedFileOrFolder = null;
        //    }
        //    if (_fileSavePickerUI != null)
        //    {
        //        _fileSavePickerUI.TargetFileRequested -=
        //            new TypedEventHandler<FileSavePickerUI, TargetFileRequestedEventArgs>(OnTargetFileRequested);
        //        _fileSavePickerUI = null;
        //    }
        //}

        //private async void OnTargetFileRequested(FileSavePickerUI sender, TargetFileRequestedEventArgs args)
        //{
        //    // Requesting a deferral allows the app to call another asynchronous method and complete the request at a later time 
        //    var deferral = args.Request.GetDeferral();

        //    // Create a temporary file
        //    var storageItem = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(sender.FileName, CreationCollisionOption.GenerateUniqueName);
        //    //var token = StorageApplicationPermissions.FutureAccessList.Add(storageItem);

        //    // See: http://www.jonathanantoine.com/2013/03/25/win8-the-cached-file-updater-contract-or-how-to-make-more-useful-the-file-save-picker-contract/
        //    CachedFileUpdater.SetUpdateInformation(storageItem, "testttt", ReadActivationMode.NotNeeded, WriteActivationMode.AfterWrite, CachedFileOptions.RequireUpdateOnAccess);

        //    args.Request.TargetFile = storageItem;

        //    // Complete the deferral to let the Picker know the request is finished 
        //    deferral.Complete();
        //}

        //void OnCachedFileUpdaterUIFileUpdateRequested(CachedFileUpdaterUI sender, FileUpdateRequestedEventArgs args)
        //{
        //    var deferral = args.Request.GetDeferral();

        //    var theContentId = args.Request.ContentId;
        //    var theTargetFile = args.Request.File;

        //    //Do something to the file

        //    //If the local file have to be updated, call do this :
        //    //StorageFile upToDateFile=null;
        //    //fill upToDateFile with the correct data
        //    //args.Request.UpdateLocalFile(upToDateFile);

        //    args.Request.Status = FileUpdateStatus.Complete;
        //    deferral.Complete();
        //}

        //public ActivationKind ActivationKind { get; private set; }

        //public List<string> FileTokens { get; private set; }

        //private void CancelFolderSelection()
        //{
        //    _navigationService.GoBack();
        //}

        //private void StartUpload()
        //{
        //    var parameters = new FileUploadPageParameters
        //    {
        //        ActivationKind = ActivationKind,
        //        ResourceInfo = Directory.PathStack.Count > 0
        //            ? Directory.PathStack[Directory.PathStack.Count - 1].ResourceInfo
        //            : new ResourceInfo(),
        //        FileTokens = FileTokens
        //    };
        //    _navigationService.Navigate(PageToken.FileUpload.ToString(), parameters.Serialize());
        //}

        //private async void CreateDirectory()
        //{
        //    while (true)
        //    {
        //        var dialog = new ContentDialog
        //        {
        //            Title = _resourceLoader.GetString("CreateNewFolder"),
        //            Content = new TextBox()
        //            {
        //                Header = _resourceLoader.GetString("FolderName"),
        //                PlaceholderText = _resourceLoader.GetString("NewFolder"),
        //                Margin = new Thickness(0, 20, 0, 0)
        //            },
        //            PrimaryButtonText = _resourceLoader.GetString("Create"),
        //            SecondaryButtonText = _resourceLoader.GetString("Cancel")
        //        };
        //        var dialogResult = await _dialogService.ShowAsync(dialog);
        //        if (dialogResult != ContentDialogResult.Primary)
        //        {
        //            return;
        //        }
        //        var textBox = dialog.Content as TextBox;
        //        if (textBox == null)
        //        {
        //            return;
        //        }
        //        var folderName = textBox.Text;
        //        if (string.IsNullOrEmpty(folderName))
        //        {
        //            folderName = _resourceLoader.GetString("NewFolder");
        //        }
        //        ShowProgressIndicator();
        //        var success = await Directory.CreateDirectory(folderName);
        //        HideProgressIndicator();
        //        if (success)
        //        {
        //            return;
        //        }

        //        dialog = new ContentDialog
        //        {
        //            Title = _resourceLoader.GetString("CanNotCreateFolder"),
        //            Content = new TextBlock
        //            {
        //                Text = _resourceLoader.GetString("SpecifyDifferentName"),
        //                TextWrapping = TextWrapping.WrapWholeWords,
        //                Margin = new Thickness(0, 20, 0, 0)
        //            },
        //            PrimaryButtonText = _resourceLoader.GetString("Retry"),
        //            SecondaryButtonText = _resourceLoader.GetString("Cancel")
        //        };
        //        dialogResult = await _dialogService.ShowAsync(dialog);
        //        if (dialogResult != ContentDialogResult.Primary)
        //        {
        //            return;
        //        }
        //    }
        //}

        //public DirectoryService Directory
        //{
        //    get { return _directoryService; }
        //    private set { SetProperty(ref _directoryService, value); }
        //}

        //public LocalSettings Settings
        //{
        //    get { return _settngs; }
        //    private set { SetProperty(ref _settngs, value); }
        //}

        //public ResourceInfo SelectedFileOrFolder
        //{
        //    get { return _selectedFileOrFolder; }
        //    set
        //    {
        //        if (_isNavigatingBack)
        //        {
        //            return;
        //        }
        //        try
        //        {
        //            if (!SetProperty(ref _selectedFileOrFolder, value))
        //            {
        //                return;
        //            }
        //        }
        //        catch
        //        {
        //            _selectedPathIndex = -1;
        //            return;
        //        }

        //        if (value == null)
        //        {
        //            return;
        //        }

        //        if (Directory?.PathStack == null)
        //        {
        //            return;
        //        }

        //        if (Directory.IsSorting)
        //        {
        //            return;
        //        }
        //        if (value.IsDirectory())
        //        {
        //            Directory.PathStack.Add(new PathInfo
        //            {
        //                ResourceInfo = value
        //            });
        //            SelectedPathIndex = Directory.PathStack.Count - 1;
        //        }
        //    }
        //}

        //public int SelectedPathIndex
        //{
        //    get { return _selectedPathIndex; }
        //    set
        //    {
        //        try
        //        {
        //            if (!SetProperty(ref _selectedPathIndex, value))
        //            {
        //                return;
        //            }
        //        }
        //        catch
        //        {
        //            _selectedPathIndex = -1;
        //            return;
        //        }

        //        if (Directory?.PathStack == null)
        //        {
        //            return;
        //        }

        //        while (Directory.PathStack.Count > 0 && Directory.PathStack.Count > _selectedPathIndex + 1)
        //        {
        //            Directory.PathStack.RemoveAt(Directory.PathStack.Count - 1);
        //        }

        //        StartDirectoryListing();
        //    }
        //}

        //private async void StartDirectoryListing()
        //{
        //    ShowProgressIndicator();

        //    await Directory.StartDirectoryListing();

        //    HideProgressIndicator();
        //}


        //public override bool CanRevertState()
        //{
        //    return SelectedPathIndex > 0;
        //}

        //public override void RevertState()
        //{
        //    SelectedPathIndex--;
        //}
    }
}