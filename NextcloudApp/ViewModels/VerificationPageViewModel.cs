using NextcloudApp.Services;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
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

            if (PinStartPageParameters.Deserialize(e.Parameter) is PinStartPageParameters pageParameters)
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

            // Workaround: When the app gets reactivated(on phone), the UserConsentVerifier immediately returns 'Canceled', but it works on the second call.
            // This seems to be a general problem on Windows Phone, you can also see this behavior on the OneDrive app.
            // For now, we do the verification in a loop to "jump over" failed first verification.
            var verificationResult = false;

            for (int i = 0; i < 2; i++)
            {
                verificationResult = await VerificationService.RequestUserConsent();

                if (verificationResult)
                    break;
            }           

            if (!verificationResult)
                App.Current.Exit();
            else if (!string.IsNullOrEmpty(this.nextPage))
                _navigationSerive.Navigate(this.nextPage, null);
            else
                App.Current.Exit();
        }
    }
}