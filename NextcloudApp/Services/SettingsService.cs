using System.ComponentModel;
using System.Reflection;
using Windows.Storage;
using Newtonsoft.Json;
using NextcloudApp.Models;
using NextcloudApp.Utils;

namespace NextcloudApp.Services
{
    public class SettingsService
    {        
        private static SettingsService _instance;

        public static SettingsService Instance => _instance ?? (_instance = new SettingsService());

        public Settings Settings
        {
            get;
        } = new Settings();

        private SettingsService()
        {
            
        }     

        public void Reset()
        {
            // #TODO: Clear PasswordVault.
            Settings.ServerAddress = null;
            Settings.Username = null;
            Settings.ShowFileAndFolderGroupingHeader = true;
            Settings.PreviewImageDownloadMode = PreviewImageDownloadMode.Always;
        }
    }
}
