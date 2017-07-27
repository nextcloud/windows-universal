using System.Collections.Generic;
using NextcloudApp.Models;
using NextcloudApp.Services;
using Prism.Unity.Windows;
using Prism.Windows.Navigation;

namespace NextcloudApp.ViewModels
{
    public class VerificationPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationSerive;
        private string _nextPage;
        private string _nextPageParameters;

        public VerificationPageViewModel(INavigationService navigationService)
        {
            _navigationSerive = navigationService;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);

            var pinStartPageParameters = PinStartPageParameters.Deserialize(e.Parameter);
            if (pinStartPageParameters is PinStartPageParameters)
            {
                _nextPage = pinStartPageParameters.PageTarget.ToString();
                _nextPageParameters = new FileInfoPageParameters
                {
                    ResourceInfo = pinStartPageParameters.ResourceInfo
                }.Serialize();
            }
            else if (e.Parameter is string)
            {
                _nextPage = (string) e.Parameter;
            }
            else
            {
                _nextPage = PageToken.DirectoryList.ToString();
            }
        }

        public async void VerificationPageLoaded()
        {
            _navigationSerive.ClearHistory();

            // Workaround: When the app gets reactivated(on phone), the UserConsentVerifier immediately returns 'Canceled', but it works on the second call.
            // This seems to be a general problem on Windows Phone, you can also see this behavior on the OneDrive app.
            // For now, we do the verification in a loop to "jump over" failed first verification.
            var verificationResult = false;

            for (var i = 0; i < 2; i++)
            {
                verificationResult = await VerificationService.RequestUserConsent();

                if (verificationResult)
                    break;
            }

            if (!verificationResult)
            {
                PrismUnityApplication.Current.Exit();
            }
            else if (!string.IsNullOrEmpty(_nextPage))
            {
                _navigationSerive.Navigate(_nextPage, _nextPageParameters);
            }
            else
            {
                PrismUnityApplication.Current.Exit();
            }
        }
    }
}