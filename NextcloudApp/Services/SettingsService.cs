using NextcloudApp.Models;
using Windows.Security.Credentials;

namespace NextcloudApp.Services
{
    /// <summary>
    /// Service for accessing the app's settings.
    /// </summary>
    public class SettingsService
    {        
        private static SettingsService _instance;

        public static SettingsService Instance => _instance ?? (_instance = new SettingsService());

        /// <summary>
        /// Gets the settings which are stored on the local device only.
        /// </summary>
        public LocalSettings LocalSettings
        {
            get;
        } = new LocalSettings();

        /// <summary>
        /// Gets the settings which are stored in the roaming profile and are synchronized between devices.
        /// </summary>
        public RoamingSettings RoamingSettings
        {
            get;
        } = new RoamingSettings();

        private SettingsService()
        {
            
        }     

        public void Reset()
        {
            var vault = new PasswordVault();
            var credentialList = vault.FindAllByResource(LocalSettings.ServerAddress);

            foreach (var credential in credentialList)
            {
                vault.Remove(credential);
            }

            LocalSettings.Reset();

            // Should we reset the roaming settings here, these would be reset on all devices?
        }
    }
}
