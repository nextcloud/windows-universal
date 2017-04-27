using NextcloudApp.Constants;
using NextcloudApp.Services;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Models;

namespace NextcloudApp.ViewModels
{
    public class VerificationPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationSerive;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        private string nextPage;

        public VerificationPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationSerive = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);

            var pageParameters = PinStartPageParameters.Deserialize(e.Parameter) as PinStartPageParameters;
            if (pageParameters != null)
            {
                this.nextPage = pageParameters.PageTarget.ToString();
            }
            else if (e.Parameter is string)
            {
                this.nextPage = e.Parameter as string;
            }
            else
            {
                this.nextPage = PageToken.DirectoryList.ToString();
            }
        }

        public async void VerificationPageLoaded()
        {
            _navigationSerive.ClearHistory();

            // Workaround: When the app gets reactivated (on phone), the UserConsentVerifier immediately returns 'Canceled', but it works on the second call.
            // This seems to be a general problem on Windows Phone, you can also see this behavior on the OnDrive app.
            // For now, we mimic the behavior of the OneDrive app showing an additional dialog requesting authentication.
            var verificationResult = false;

            do
            {
                verificationResult = await VerificationService.RequestUserConsent();

                if (!verificationResult)
                {
                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString(ResourceConstants.DialogTitle_SignInRequired),
                        Content = new TextBlock
                        {
                            Text = _resourceLoader.GetString(ResourceConstants.SignInRequiredMessage),
                            TextWrapping = TextWrapping.WrapWholeWords,
                            Margin = new Thickness(0, 20, 0, 0)
                        },
                        PrimaryButtonText = _resourceLoader.GetString(ResourceConstants.TryAgain),
                        SecondaryButtonText = _resourceLoader.GetString(ResourceConstants.Cancel)
                    };

                    var dialogResult = await _dialogService.ShowAsync(dialog);

                    if (dialogResult == ContentDialogResult.Secondary)
                        App.Current.Exit();
                }

            } while (!verificationResult);

            if (!string.IsNullOrEmpty(this.nextPage))
                _navigationSerive.Navigate(this.nextPage, null);
            else
                App.Current.Exit();
        }
    }
}
