using Windows.ApplicationModel;
using Microsoft.Practices.Unity;
using Prism.Unity.Windows;
using Prism.Windows.AppModel;

namespace NextcloudApp.Services
{
    public class LocalizationService
    {
        private readonly IResourceLoader _resourceLoader;
        private static LocalizationService _instance;

        public LocalizationService()
        {
            if (DesignMode.DesignModeEnabled) return;
            var app = PrismUnityApplication.Current as App;
            if (app != null)
            {
                _resourceLoader = app.Container.Resolve<IResourceLoader>();
            }
        }

        public static LocalizationService Instance => _instance ?? (_instance = new LocalizationService());

        public string GetString(string key)
        {
            return _resourceLoader?.GetString(key);
        }

        public string GetString(string key, object arg0)
        {
            return string.Format(_resourceLoader?.GetString(key), arg0);
        }

        public string GetString(string key, object arg0, object arg1)
        {
            return string.Format(_resourceLoader?.GetString(key), arg0, arg1);
        }

        public string GetString(string key, object arg0, object arg1, object arg2)
        {
            return string.Format(_resourceLoader?.GetString(key), arg0, arg1, arg2);
        }
    }
}
