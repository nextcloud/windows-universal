using System;

namespace NextcloudApp.Utils
{
    /// <summary>
    /// Attribute class to provide default values for ObservableSettings.
    /// </summary>
    /// <remarks>See https://github.com/joseangelmt/ObservableSettings</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DefaultSettingValueAttribute : Attribute
    {
        public DefaultSettingValueAttribute()
        {
        }

        public DefaultSettingValueAttribute(object value)
        {
            Value = value;
        }

        public object Value { get; set; }
    }
}
