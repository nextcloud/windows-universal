using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class IsGlyphToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            var glyph = (string)value;
            if (!string.IsNullOrEmpty(glyph) && glyph.Length == 1)
            {
                return invert ? Visibility.Collapsed : Visibility.Visible;
            }
            return invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
