using System;
using System.Threading;
using NextcloudApp.Models;
using Windows.Security.Credentials;

namespace NextcloudApp.Services
{
    /// <summary>
    /// Service for accessing the app's settings.
    /// </summary>
    public class SettingsService
    {
        public static Lazy<SettingsService> Default = new Lazy<SettingsService>(() => new SettingsService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the settings which are stored on the local device only.
        /// </summary>
        public LocalSettings LocalSettings => LocalSettings.Default.Value;

        /// <summary>
        /// Gets the settings which are stored in the roaming profile and are synchronized between devices.
        /// </summary>
        public RoamingSettings RoamingSettings => RoamingSettings.Default.Value;

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
            // afiedler: We should ask the user if he also wants to reset the roaming settings
            //TODO
        }

        /// <summary>
        /// Disposeds this instance.
        /// </summary>
        public void Disposed()
        {
            LocalSettings.Dispose();
            RoamingSettings.Dispose();
            if (Default.IsValueCreated)
            {
                Default = new Lazy<SettingsService>(() => new SettingsService(), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    }
}
