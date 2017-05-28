using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace NextcloudApp.Utils
{
    /// <summary>
    /// Class for settings that is observable, strongly typed and provides default values.
    /// </summary>
    /// <remarks>See https://github.com/joseangelmt/ObservableSettings</remarks>
    public class ObservableSettings : INotifyPropertyChanged
    {
        private readonly ApplicationDataContainer _applicationDataContainer;
        protected bool EnableRaisePropertyChanged = true;
        //private CoreDispatcher _dispatcher;

        public ObservableSettings(ApplicationDataContainer settings)
        {
            _applicationDataContainer = settings;
            //_dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected async Task<bool> Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (_applicationDataContainer.Values.ContainsKey(propertyName))
            {
                var currentValue = (T)_applicationDataContainer.Values[propertyName];
                if (EqualityComparer<T>.Default.Equals(currentValue, value))
                {
                    return false;
                }
            }

            _applicationDataContainer.Values[propertyName] = value;

            if (EnableRaisePropertyChanged)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

                //await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //{
                //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                //});

                //await Task.Factory.StartNew(
                //    () =>
                //    {
                //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                //    }, 
                //    CancellationToken.None, 
                //    TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler,
                //    TaskScheduler.Default
                //).ConfigureAwait(false);
            }

            return true;
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (_applicationDataContainer.Values.ContainsKey(propertyName))
            {
                return (T)_applicationDataContainer.Values[propertyName];
            }

            var attributes = GetType().GetTypeInfo().GetDeclaredProperty(propertyName).CustomAttributes.Where(ca => ca.AttributeType == typeof(DefaultSettingValueAttribute)).ToList();

            if (attributes.Count != 1) return default(T);
            var val =  attributes[0].NamedArguments[0].TypedValue.Value;

            if(val is T) return (T)val;

            return default(T);
        }
    }
}
