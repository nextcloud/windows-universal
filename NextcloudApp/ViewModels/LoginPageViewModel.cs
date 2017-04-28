using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Services;
using NextcloudClient.Exceptions;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using Windows.UI.Core;
using Windows.System;

namespace NextcloudApp.ViewModels
{
    public class LoginPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private string _serverAddress;
        private string _username;
        private string _password;
        private Color? _statusBarBackgroundColor;
        private Color? _statusBarForegroundColor;
        private double _keyboardHeight;
        private bool _isKeyboardVisible;
        private bool _isLoading;
        private readonly DialogService _dialogService;
        private readonly IResourceLoader _resourceLoader;
        private string _serverAddressGivenByUser;

        public string ServerAddress
        {
            get { return _serverAddress; }
            set { SetProperty(ref _serverAddress, value); }
        }

        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); }
        }

        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        public double KeyboardHeight
        {
            get { return _keyboardHeight; }
            set { SetProperty(ref _keyboardHeight, value); }
        }

        public bool IsKeyboardVisible
        {
            get { return _isKeyboardVisible; }
            set { SetProperty(ref _isKeyboardVisible, value); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public ICommand SaveSettingsCommand { get; private set; }

        public LoginPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;

            SaveSettingsCommand = new DelegateCommand(SaveSettings);
            CoreWindow.GetForCurrentThread().KeyDown += LoginPageViewModel_KeyDown;
        }

        private void LoginPageViewModel_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            // Login in on 'Enter'.
            switch (args.VirtualKey)
            {
                case VirtualKey.Enter:
                    SaveSettingsCommand.Execute(null);
                    break;
                default:
                    break;
            }
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> dictionary)
        {
            base.OnNavigatedTo(e, dictionary);

            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return;
            }
            var statusBar = StatusBar.GetForCurrentView();
            if (statusBar == null)
            {
                return;
            }

            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            _statusBarBackgroundColor = statusBar.BackgroundColor;
            _statusBarForegroundColor = statusBar.ForegroundColor;
            statusBar.BackgroundOpacity = 0;
            statusBar.BackgroundColor = Color.FromArgb(255, 0, 130, 201);
            statusBar.ForegroundColor = Colors.White;

            InputPane.GetForCurrentView().Showing += OnShowing;
            InputPane.GetForCurrentView().Hiding += OnHiding;
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);

            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return;
            }
            var statusBar = StatusBar.GetForCurrentView();
            if (statusBar == null)
            {
                return;
            }
            statusBar.BackgroundOpacity = 1;
            statusBar.BackgroundColor = _statusBarBackgroundColor;
            statusBar.ForegroundColor = _statusBarForegroundColor;

            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);

            InputPane.GetForCurrentView().Showing -= OnShowing;
            InputPane.GetForCurrentView().Hiding -= OnHiding;
        }

        private void OnShowing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            KeyboardHeight = args.OccludedRect.Height;
            IsKeyboardVisible = true;
        }

        private void OnHiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            KeyboardHeight = 0;
            IsKeyboardVisible = false;
        }

        public async void SaveSettings()
        {
            IsLoading = true;

            _serverAddressGivenByUser = ServerAddress + "";

            var serverAddressIsValid = await CheckAndFixServerAddress();
            if (!serverAddressIsValid)
            {
                IsLoading = false;
                return;
            }

            var serverIsUpAndRunning = await CheckServerStatus();
            var userLoginIsValid = await CheckUserLogin();

            IsLoading = false;

            if (!serverIsUpAndRunning)
            {
                return;
            }

            if (!userLoginIsValid)
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("AnErrorHasOccurred"),
                    Content = new TextBlock
                    {
                        Text = _resourceLoader.GetString("Auth_Unauthorized"),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = _resourceLoader.GetString("OK")
                };
                await _dialogService.ShowAsync(dialog);
                return;
            }

            SettingsService.Instance.LocalSettings.ServerAddress = ServerAddress;
            SettingsService.Instance.LocalSettings.Username = Username;
            
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(ServerAddress, Username, Password));

            _navigationService.Navigate(PageToken.DirectoryList.ToString(), null);
        }

        private async Task<bool> CheckAndFixServerAddress()
        {
            if (!ServerAddress.StartsWith("http"))
            {
                ServerAddress = string.Format("https://{0}", ServerAddress);
            }

            try
            {
                var response = await NextcloudClient.NextcloudClient.GetServerStatus(ServerAddress);
                if (response == null)
                {
                    ServerAddress = ServerAddress.Replace("https:", "http:");
                }
                else
                {
                    return true;
                }
            }
            catch (ResponseError e)
            {
                if (e.Message.Equals("The certificate authority is invalid or incorrect"))
                {
                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString("Attention_ExclamationMark"),
                        Content = new TextBlock
                        {
                            Text = _resourceLoader.GetString("TheCertificateAuthorityIsInvalidOrIncorrect_ConnectAnyway"),
                            TextWrapping = TextWrapping.WrapWholeWords,
                            Margin = new Thickness(0, 20, 0, 0)
                        },
                        PrimaryButtonText = _resourceLoader.GetString("Cancel"),
                        SecondaryButtonText = _resourceLoader.GetString("Connect2"),
                        SecondaryButtonCommand = new DelegateCommand(IgnoreServerCertificateErrors)
                    };
                    await _dialogService.ShowAsync(dialog);
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                await ShowServerAddressNotFoundMessage();
                return false;
            }

            if (SettingsService.Instance.LocalSettings.IgnoreServerCertificateErrors)
            {
                var response = await NextcloudClient.NextcloudClient.GetServerStatus(ServerAddress, true);
                if (response == null)
                {
                    ServerAddress = ServerAddress.Replace("https:", "http:");
                }
                else
                {
                    return true;
                }
            }

            try
            {
                var response = await NextcloudClient.NextcloudClient.GetServerStatus(ServerAddress);
                if (response == null)
                {
                    await ShowServerAddressNotFoundMessage();
                    return false;
                }
            }
            catch 
            {
                await ShowServerAddressNotFoundMessage();
                return false;
            }
            return true;
        }

        private void IgnoreServerCertificateErrors()
        {
            SettingsService.Instance.LocalSettings.IgnoreServerCertificateErrors = true;
        }

        private async Task ShowServerAddressNotFoundMessage()
        {
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("AnErrorHasOccurred"),
                Content = new TextBlock
                {
                    Text = string.Format(_resourceLoader.GetString("ServerWithGivenAddressIsNotReachable"), _serverAddressGivenByUser),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("OK")
            };
            await _dialogService.ShowAsync(dialog);
        }

        private async Task<bool> CheckServerStatus()
        {
            try
            {
                var status = await NextcloudClient.NextcloudClient.GetServerStatus(ServerAddress, SettingsService.Instance.LocalSettings.IgnoreServerCertificateErrors);
                if (status == null)
                {
                    await ShowServerAddressNotFoundMessage();
                    return false;
                }
                if (!status.Installed)
                {
                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString("AnErrorHasOccurred"),
                        Content = new TextBlock
                        {
                            Text = _resourceLoader.GetString("Auth_Unauthorized"),
                            TextWrapping = TextWrapping.WrapWholeWords,
                            Margin = new Thickness(0, 20, 0, 0)
                        },
                        PrimaryButtonText = _resourceLoader.GetString("OK")
                    };
                    await _dialogService.ShowAsync(dialog);
                    return false;
                }
                if (status.Maintenance)
                {
                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString("AnErrorHasOccurred"),
                        Content = new TextBlock
                        {
                            Text = _resourceLoader.GetString("Auth_MaintenanceEnabled"),
                            TextWrapping = TextWrapping.WrapWholeWords,
                            Margin = new Thickness(0, 20, 0, 0)
                        },
                        PrimaryButtonText = _resourceLoader.GetString("OK")
                    };
                    await _dialogService.ShowAsync(dialog);
                    return false;
                }
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
                return false;
            }
            return true;
        }

        private async Task<bool> CheckUserLogin()
        {
            try
            {
                return await NextcloudClient.NextcloudClient.CheckUserLogin(ServerAddress, Username, Password, SettingsService.Instance.LocalSettings.IgnoreServerCertificateErrors);
            }
            catch (ResponseError)
            {
                return false;
            }
        }
    }
}
