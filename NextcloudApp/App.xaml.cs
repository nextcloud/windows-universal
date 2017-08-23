using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Microsoft.Practices.Unity;
using Prism.Windows.AppModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using NextcloudClient.Types;
using Prism.Windows.Mvvm;
using Microsoft.QueryStringDotNET;
using Windows.UI.Notifications;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace NextcloudApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            InitializeComponent();
        }

        public IActivatedEventArgs ActivatedEventArgs { get; private set; }

        private async void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            var exceptionStackTrace = args.Exception.StackTrace;
            var
                exceptionHashCode = string.IsNullOrEmpty(exceptionStackTrace)
                    ? args.Exception.GetHashCode().ToString()
                    : exceptionStackTrace.GetHashCode().ToString();
            await
                ExceptionReportService.Handle(args.Exception.GetType().ToString(), args.Exception.Message,
                    exceptionStackTrace, args.Exception.InnerException.GetType().ToString(), exceptionHashCode);
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exceptionStackTrace = string.Empty;
            try
            {
                exceptionStackTrace = args.Exception.StackTrace + "";
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            var exceptionMessage = args.Message;
            var exceptionType = string.Empty;
            var innerExceptionType = string.Empty;
            var exceptionHashCode = string.Empty;
            if (args.Exception != null)
            {
                // Tasks will throw a canceled exception if they get canceled
                // We don't care, but avoid closing the app
                if (args.Exception.GetType() == typeof(TaskCanceledException))
                {
                    args.Handled = true;
                    return;
                }
                if (args.Exception.GetType() == typeof(OperationCanceledException))
                {
                    args.Handled = true;
                    return;
                }
                if (args.Exception.GetType() == typeof(FileNotFoundException))
                {
                    args.Handled = true;
                    return;
                }
                // Temporary Workaround for WP10
                if (args.Exception.GetType() == typeof(ArgumentException))
                {
                    args.Handled = true;
                    return;
                }
                if (args.Exception.GetType() == typeof(ResponseError))
                {
                    args.Handled = true;
                    ResponseErrorHandlerService.HandleException((ResponseError)args.Exception);
                    return;
                }
                // 0x8000000B, E_BOUNDS, System.Exception, OutOfBoundsException
                if ((uint)args.Exception.HResult == 0x80004004)
                {
                    args.Handled = true;
                    return;
                }
                // 0x80072EE7, ERROR_WINHTTP_NAME_NOT_RESOLVED, The server name or address could not be resolved
                if ((uint)args.Exception.HResult == 0x80072EE7)
                {
                    args.Handled = true;
                    var resourceLoader = Container.Resolve<IResourceLoader>();
                    var dialogService = Container.Resolve<DialogService>();
                    var dialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("AnErrorHasOccurred"),
                        Content = new TextBlock
                        {
                            Text = resourceLoader.GetString("ServerNameOrAddressCouldNotBeResolved"),
                            TextWrapping = TextWrapping.WrapWholeWords,
                            Margin = new Thickness(0, 20, 0, 0)
                        },
                        PrimaryButtonText = resourceLoader.GetString("OK")
                    };
                    await dialogService.ShowAsync(dialog);
                    return;
                }
                exceptionType = args.Exception.GetType().ToString();
                if (args.Exception.InnerException != null)
                {
                    innerExceptionType = args.Exception.InnerException.GetType().ToString();
                }
                exceptionHashCode = string.IsNullOrEmpty(exceptionStackTrace)
                    ? args.Exception.GetHashCode().ToString()
                    : exceptionStackTrace.GetHashCode().ToString();
            }
            if (args.Handled)
            {
                return;
            }
            args.Handled = true;
            await
                ExceptionReportService.Handle(exceptionType, exceptionMessage, exceptionStackTrace,
                    innerExceptionType, exceptionHashCode);
        }

        protected override UIElement CreateShell(Frame rootFrame)
        {
            ThemeManager.Instance.Initialize();
            var shell = Container.Resolve<AppShell>();
            shell.SetContentFrame(rootFrame);
            return shell;
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            SettingsService.Default.Value.Disposed();

            // show a simple loading page without any dependencies, to avoid te system killing our app
            var frame = new Frame();
            frame.Navigate(typeof(ShareTarget), null);
            Window.Current.Content = frame;
            Window.Current.Activate();

            base.OnShareTargetActivated(args);

            OnShareTargetActivatedsyncAsync(args);
        }

        private async void OnShareTargetActivatedsyncAsync(ShareTargetActivatedEventArgs args)
        {
            /*
             * If the app get's launched in a share pane, Window.Current will be null,
             * even if the app is suspended. If we "just" initialize the windows again, 
             * we will get a lot of thread conflicts, because some classes are marshalled
             * for a different thread.
             * 
             * To avoid this, we have to escape from the share pane view (see below)
             */

            // get the shared items and create a token for later access
            var sorageItems = await args.ShareOperation.Data.GetStorageItemsAsync();
            StorageApplicationPermissions.FutureAccessList.Clear();

            // launch the app again via protocol link, this will avoid the issue explained above
            var options = new LauncherOptions()
            {
                TargetApplicationPackageFamilyName = Package.Current.Id.FamilyName,
                DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseNone
            };

            var storageFiles = sorageItems.Where(storageItem => storageItem.IsOfType(StorageItemTypes.File));

            var inputData = new ValueSet
            {
                { "FileTokens", storageFiles.Select(storageFile => StorageApplicationPermissions.FutureAccessList.Add(storageFile)).ToArray() }
            };
            var uri = new Uri("nextcloud:///share");

            // we processed all files, so we are redy to release them
            args.ShareOperation.ReportDataRetrieved();

#pragma warning disable 4014
            Task.Delay(300).ContinueWith(async t => await Launcher.LaunchUriAsync(uri, options, inputData));
#pragma warning restore 4014
            
            // we are done, report back
            args.ShareOperation.ReportCompleted();
        }

        //TODO: Find out, why this is not working on WP10
        //SEE: https://github.com/nextcloud/windows-universal/issues/32
        //protected override void OnFileSavePickerActivated(FileSavePickerActivatedEventArgs args)
        //{
        //    base.OnFileSavePickerActivated(args);
        //    OnActivated(args);
        //}
        //protected override void OnCachedFileUpdaterActivated(CachedFileUpdaterActivatedEventArgs args)
        //{
        //    base.OnCachedFileUpdaterActivated(args);
        //    OnActivated(args);
        //}

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);
            OnActivated(args);
        }

        protected override async Task OnActivateApplicationAsync(IActivatedEventArgs args)
        {
            ActivatedEventArgs = args;
            await base.OnActivateApplicationAsync(args);

            // Remove unnecessary notifications whenever the app is used.
            ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SyncAction);

            // Handle toast activation
            var eventArgs = args as ToastNotificationActivatedEventArgs;
            if (eventArgs != null)
            {
                var toastActivationArgs = eventArgs;
                // Parse the query string
                var query = QueryString.Parse(toastActivationArgs.Argument);
                // See what action is being requested 
                switch (query["action"])
                {
                    // Nothing to do here
                    case ToastNotificationService.SyncAction:
                        NavigationService.Navigate(PageToken.DirectoryList.ToString(), null);
                        break;
                    // Open Status Page
                    case ToastNotificationService.SyncConflictAction:
                        ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SyncConflictAction);
                        NavigationService.Navigate(PageToken.SyncStatus.ToString(), null);
                        break;
                }
            }
            else switch (args.Kind)
            {
                case ActivationKind.Protocol:
                    var protocolArgs = args as ProtocolActivatedEventArgs;

                    if (protocolArgs != null && protocolArgs.Uri.AbsolutePath == "/share")
                    {
                        var pageParameters = new ShareTargetPageParameters()
                        {
                            ActivationKind = ActivationKind.ShareTarget,
                            FileTokens = new List<string>()
                        };

                        if (protocolArgs.Data.ContainsKey("FileTokens"))
                        {
                            var tokens = protocolArgs.Data["FileTokens"] as string[];
                            if (tokens != null)
                            {
                                foreach (var token in tokens)
                                {
                                    pageParameters.FileTokens.Add(token);
                                }
                            }
                        }

                        CheckSettingsAndContinue(PageToken.ShareTarget, pageParameters);
                    }
                    break;
                case ActivationKind.FileSavePicker:
                case ActivationKind.CachedFileUpdater:
                    CheckSettingsAndContinue(PageToken.FileSavePicker, null);
                    break;
                case ActivationKind.File:
                    if (args is FileActivatedEventArgs activatedEventArgs)
                    {
                        var sorageItems = activatedEventArgs.Files;
                        var pageParameters = new ShareTargetPageParameters()
                        {
                            //ShareOperation = activatedEventArgs.ShareOperation,
                            ActivationKind = ActivationKind.ShareTarget,
                            FileTokens = new List<string>()
                        };
                        StorageApplicationPermissions.FutureAccessList.Clear();
                        foreach (var storageItem in sorageItems)
                        {
                            var token = StorageApplicationPermissions.FutureAccessList.Add(storageItem);
                            pageParameters.FileTokens.Add(token);
                        }
                        CheckSettingsAndContinue(PageToken.ShareTarget, pageParameters);
                    }
                    break;
            }
        }

        protected override Task OnSuspendingApplicationAsync()
        {
            var task = base.OnSuspendingApplicationAsync();
            // Stop Background Sync Tasks
            var activeSyncs = SyncDbUtils.GetActiveSyncInfos();
            foreach (var fsi in activeSyncs)
            {
                ToastNotificationService.ShowSyncSuspendedNotification(fsi);
                SyncDbUtils.UnlockFolderSyncInfo(fsi);
            }
            return task;
        }

        protected override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            Container.RegisterInstance(new DialogService());
            Container.RegisterInstance<IResourceLoader>(new ResourceLoaderAdapter(new ResourceLoader()));
            var task = base.OnInitializeAsync(args);
            DeviceGestureService.GoBackRequested += DeviceGestureServiceOnGoBackRequested;
            // Just count total app starts
            SettingsService.Default.Value.LocalSettings.AppTotalRuns = SettingsService.Default.Value.LocalSettings.AppTotalRuns + 1;
            // Count app starts after last update
            var currentVersion =
                $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
            if (currentVersion == SettingsService.Default.Value.LocalSettings.AppRunsAfterLastUpdateVersion)
            {
                SettingsService.Default.Value.LocalSettings.AppRunsAfterLastUpdate = SettingsService.Default.Value.LocalSettings.AppRunsAfterLastUpdate + 1;
            }
            else
            {
                SettingsService.Default.Value.LocalSettings.AppRunsAfterLastUpdateVersion = currentVersion;
                SettingsService.Default.Value.LocalSettings.AppRunsAfterLastUpdate = 1;
                SettingsService.Default.Value.LocalSettings.ShowUpdateMessage = true;
            }
            MigrationService.Instance.StartMigration();
            return task;
        }

        protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            // Ensure the current window is active
            Window.Current.Activate();

            // Remove unnecessary notifications whenever the app is used.
            ToastNotificationManager.History.RemoveGroup(ToastNotificationService.SyncAction);
            PinStartPageParameters pageParameters = null;
            if (!string.IsNullOrEmpty(args?.Arguments))
            {
                var tmpResourceInfo = JsonConvert.DeserializeObject<ResourceInfo>(args.Arguments);
                if (tmpResourceInfo != null)
                {
                    pageParameters = new PinStartPageParameters()
                    {
                        ResourceInfo = tmpResourceInfo,
                        PageTarget = tmpResourceInfo.IsDirectory ? PageToken.DirectoryList : PageToken.FileInfo
                    };
                }
            }
            if (SettingsService.Default.Value.LocalSettings.UseWindowsHello)
            {
                CheckSettingsAndContinue(PageToken.Verification, pageParameters);
            }
            else
            {
                IPageParameters resourceInfoPageParameters = null;
                if (pageParameters?.PageTarget == PageToken.DirectoryList)
                {
                    resourceInfoPageParameters = new DirectoryListPageParameters
                    {
                        ResourceInfo = pageParameters?.ResourceInfo
                    };
                }
                else if (pageParameters?.PageTarget == PageToken.FileInfo)
                {
                    resourceInfoPageParameters = new FileInfoPageParameters
                    {
                        ResourceInfo = pageParameters?.ResourceInfo
                    };
                }
                CheckSettingsAndContinue(pageParameters?.PageTarget ?? PageToken.DirectoryList, resourceInfoPageParameters);
            }
            return Task.FromResult(true);
        }

        private void CheckSettingsAndContinue(PageToken requestedPage, IPageParameters pageParameters)
        {
            if (
                string.IsNullOrEmpty(SettingsService.Default.Value.LocalSettings.ServerAddress) ||
                string.IsNullOrEmpty(SettingsService.Default.Value.LocalSettings.Username)
            )
            {
                NavigationService.Navigate(PageToken.Login.ToString(), null);
            }
            else
            {
                var vault = new PasswordVault();
                IReadOnlyList<PasswordCredential> credentialList = null;
                try
                {
                    credentialList = vault.FindAllByResource(SettingsService.Default.Value.LocalSettings.ServerAddress);
                }
                catch
                {
                    // ignored
                }
                var credential = credentialList?.FirstOrDefault(item => item.UserName.Equals(SettingsService.Default.Value.LocalSettings.Username));
                if (credential != null)
                {
                    credential.RetrievePassword();
                    if (!string.IsNullOrEmpty(credential.Password))
                    {
                        NavigationService.Navigate(requestedPage.ToString(), pageParameters?.Serialize());
                    }
                    else
                    {
                        NavigationService.Navigate(
                            PageToken.Login.ToString(),
                            null);
                    }
                }
                else
                {
                    NavigationService.Navigate(
                        PageToken.Login.ToString(),
                        null);
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        private void DeviceGestureServiceOnGoBackRequested(object sender, DeviceGestureEventArgs e)
        {
            var appShell = (AppShell)Window.Current.Content;
            var contentFrame = (Frame)appShell.GetContentFrame();
            var page = (SessionStateAwarePage)contentFrame.Content;
            var revertable = page?.DataContext as IRevertState;
            if (revertable == null || !revertable.CanRevertState())
            {
                return;
            }
            e.Handled = true;
            e.Cancel = true;
            revertable.RevertState();
        }
    }
}