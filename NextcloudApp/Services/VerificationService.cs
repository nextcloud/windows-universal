using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;
using Microsoft.Practices.Unity;
using Prism.Windows.AppModel;
using NextcloudApp.Constants;

namespace NextcloudApp.Services
{
    /// <summary>
    /// Class for user verification.
    /// </summary>
    public static class VerificationService
    {
        /// <summary>
        /// Checks the availability of the UserConsentVerifier.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static async Task<bool> CheckAvailabilityAsync()
        {
            var available = await UserConsentVerifier.CheckAvailabilityAsync();
            return available == UserConsentVerifierAvailability.Available;
        }

        /// <summary>
        /// Request for user's consent.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static async Task<bool> RequestUserConsent()
        {
            return await RequestUserConsent(string.Empty);
        }

        /// <summary>
        /// Request for user's consent with prompt given.
        /// </summary>
        /// <param name="prompt">The prompt which is shown during verification.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static async Task<bool> RequestUserConsent(string prompt)
        {
            // If verification is not available, always return true.
            if (!await CheckAvailabilityAsync())
                return true;

            var app = Application.Current as App;

            if (app == null)
                return false;

            var resourceLoader = app.Container.Resolve<IResourceLoader>();

            if (string.IsNullOrEmpty(prompt))
            {
                prompt = resourceLoader.GetString(ResourceConstants.VerificationService_Prompt);
            }

            try
            {
                var consentResult = await UserConsentVerifier.RequestVerificationAsync(prompt);
                return consentResult == UserConsentVerificationResult.Verified;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
