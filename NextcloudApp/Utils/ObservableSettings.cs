using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.ComponentModel;
using Windows.Storage;

namespace NextcloudApp.Utils
{
    /// <summary>
    /// Class for settings that is observable, strongly typed and provides default values.
    /// </summary>
    /// <remarks>See https://github.com/joseangelmt/ObservableSettings</remarks>
    public class ObservableSettings : INotifyPropertyChanged
    {
        private readonly ApplicationDataContainer applicationDataContainer;

        public ObservableSettings(ApplicationDataContainer settings)
        {
            this.applicationDataContainer = settings;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (applicationDataContainer.Values.ContainsKey(propertyName))
            {
                var currentValue = (T)applicationDataContainer.Values[propertyName];
                if (EqualityComparer<T>.Default.Equals(currentValue, value))
                    return false;
            }

            applicationDataContainer.Values[propertyName] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (applicationDataContainer.Values.ContainsKey(propertyName))
                return (T)applicationDataContainer.Values[propertyName];

            var attributes = GetType().GetTypeInfo().GetDeclaredProperty(propertyName).CustomAttributes.Where(ca => ca.AttributeType == typeof(DefaultSettingValueAttribute)).ToList();

            if (attributes.Count == 1)
                return (T)attributes[0].NamedArguments[0].TypedValue.Value;

            return default(T);
        }
    }
}
