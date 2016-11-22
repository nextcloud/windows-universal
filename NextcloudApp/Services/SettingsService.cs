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
        private readonly ApplicationDataContainer _localSettings;

        private SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
            var appTotalRuns = _localSettings.Values["AppTotalRuns"];
            if (appTotalRuns == null)
            {
                Settings.AppTotalRuns = 0;
            }
            else
            {
                Settings.AppTotalRuns = (int) appTotalRuns;
            }
            Settings.AppRunsAfterLastUpdateVersion = (string)_localSettings.Values["AppRunsAfterLastUpdateVersion"];
            var appRunsAfterLastUpdate = _localSettings.Values["AppRunsAfterLastUpdate"];
            if (appRunsAfterLastUpdate == null)
            {
                Settings.AppRunsAfterLastUpdate = 0;
            }
            else
            {
                Settings.AppRunsAfterLastUpdate = (int)appRunsAfterLastUpdate;
            }
            Settings.ServerAddress = (string)_localSettings.Values["ServerAddress"];
            Settings.Username = (string)_localSettings.Values["Username"];
            var showFileAndFolderGroupingHeader = _localSettings.Values["ShowFileAndFolderGroupingHeader"];
            if (showFileAndFolderGroupingHeader == null)
            {
                Settings.ShowFileAndFolderGroupingHeader = true;
            }
            else
            {
                Settings.ShowFileAndFolderGroupingHeader =
                    (bool)_localSettings.Values["ShowFileAndFolderGroupingHeader"];
            }

            var previewImageDownloadMode = (string)_localSettings.Values["PreviewImageDownloadMode"];
            Settings.PreviewImageDownloadMode = previewImageDownloadMode == null ? PreviewImageDownloadMode.Always : JsonConvert.DeserializeObject<PreviewImageDownloadMode>(previewImageDownloadMode);

            Settings.PropertyChanged += SettingsOnPropertyChanged;
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var newValue = sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
            if (e.PropertyName == "PreviewImageDownloadMode")
            {
                _localSettings.Values["PreviewImageDownloadMode"] =
                    JsonConvert.SerializeObject(newValue);
            }
            else
            {
                _localSettings.Values[e.PropertyName] = newValue;
            }
        }

        public static SettingsService Instance => _instance ?? (_instance = new SettingsService());

        public Settings Settings { get; } = new Settings();

        public void Reset()
        {
            _localSettings.Values["serverAddress"] = null;
            _localSettings.Values["username"] = null;
            _localSettings.Values["password"] = null;
            _localSettings.Values["ShowFileAndFolderGroupingHeader"] = true;
            _localSettings.Values["PreviewImageDownloadMode"] =
                JsonConvert.SerializeObject(PreviewImageDownloadMode.Always);
        }
    }
}
