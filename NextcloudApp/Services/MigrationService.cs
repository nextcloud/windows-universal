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
            if (!localSettings.Values.ContainsKey("Password"))
            {
                return;
            }
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(
                SettingsService.Instance.LocalSettings.ServerAddress,
                SettingsService.Instance.LocalSettings.Username,
                (string)localSettings.Values["Password"]
            ));
            localSettings.Values.Remove("Password");
        }
    }
}
