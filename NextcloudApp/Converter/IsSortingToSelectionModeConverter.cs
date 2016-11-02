using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class IsSortingToSelectionModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            if (value is bool && (bool)value)
            {
                return !invert ? ListViewSelectionMode.None : ListViewSelectionMode.Single;
            }
            return !invert ? ListViewSelectionMode.Single : ListViewSelectionMode.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            if (value is ListViewSelectionMode && (ListViewSelectionMode)value == ListViewSelectionMode.None)
            {
                return !invert;
            }
            return invert;
        }
    }
}
