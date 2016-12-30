using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Microsoft.Practices.Unity;
using Prism.Unity.Windows;
using Prism.Windows.AppModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using Prism.Windows.Mvvm;

namespace NextcloudApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : PrismUnityApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
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
            var shell = Container.Resolve<AppShell>();
            shell.SetContentFrame(rootFrame);
            return shell;
        }

        protected override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            Container.RegisterInstance(new DialogService());
            Container.RegisterInstance<IResourceLoader>(new ResourceLoaderAdapter(new ResourceLoader()));

            var task = base.OnInitializeAsync(args);

            DeviceGestureService.GoBackRequested += DeviceGestureServiceOnGoBackRequested;

            // Just count total app starts
            SettingsService.Instance.LocalSettings.AppTotalRuns = SettingsService.Instance.LocalSettings.AppTotalRuns + 1;

            // Count app starts after last update
            var currentVersion =
                $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
            if (currentVersion == SettingsService.Instance.LocalSettings.AppRunsAfterLastUpdateVersion)
            {
                SettingsService.Instance.LocalSettings.AppRunsAfterLastUpdate = SettingsService.Instance.LocalSettings.AppRunsAfterLastUpdate + 1;
            }
            else
            {
                SettingsService.Instance.LocalSettings.AppRunsAfterLastUpdateVersion = currentVersion;
                SettingsService.Instance.LocalSettings.AppRunsAfterLastUpdate = 1;
            }

            MigrationService.Instance.StartMigration();

            return task;
        }

        protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            if (
                string.IsNullOrEmpty(SettingsService.Instance.LocalSettings.ServerAddress) ||
                string.IsNullOrEmpty(SettingsService.Instance.LocalSettings.Username)
            )
            {
                NavigationService.Navigate(PageTokens.Login.ToString(), null);
            }
            else
            {
                var vault = new PasswordVault();

                var credentialList = vault.FindAllByResource(SettingsService.Instance.LocalSettings.ServerAddress);
                var credential = credentialList.FirstOrDefault(item => item.UserName.Equals(SettingsService.Instance.LocalSettings.Username));

                if (credential != null)
                {
                    credential.RetrievePassword();
                    if (!string.IsNullOrEmpty(credential.Password))
                    {
                        if (SettingsService.Instance.LocalSettings.UseWindowsHello)
                        {
                            NavigationService.Navigate(PageTokens.Verification.ToString(),
                                PageTokens.DirectoryList.ToString());
                        }
                        else
                        {
                            NavigationService.Navigate(PageTokens.DirectoryList.ToString(), null);
                        }
                    }
                    else
                    {
                        NavigationService.Navigate(PageTokens.Login.ToString(), null);
                    }
                }
                else
                {
                    NavigationService.Navigate(PageTokens.Login.ToString(), null);
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();

            return Task.FromResult(true);
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
