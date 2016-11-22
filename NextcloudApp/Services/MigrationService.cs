using Windows.Security.Credentials;
using Windows.Storage;

namespace NextcloudApp.Services
{
    class MigrationService
    {
        private static MigrationService _instance;

        private MigrationService()
        {
        }

        public static MigrationService Instance => _instance ?? (_instance = new MigrationService());

        public void StartMigration()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("Password"))
            {
                var vault = new PasswordVault();
                vault.Add(new PasswordCredential(
                    SettingsService.Instance.Settings.ServerAddress,
                    SettingsService.Instance.Settings.Username,
                    (string)localSettings.Values["Password"]
                ));
                localSettings.Values.Remove("Password");
            }
        }
    }
}
