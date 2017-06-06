using NextcloudApp.Utils;

namespace NextcloudApp.Models
{
    public class PreviewImageDownloadModeItem
    {
        public PreviewImageDownloadModeItem()
        {
        }

        public PreviewImageDownloadModeItem(string name, PreviewImageDownloadMode value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public PreviewImageDownloadMode Value { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj as PreviewImageDownloadMode? == Value;
        }

        protected bool Equals(PreviewImageDownloadModeItem other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name?.GetHashCode() ?? 0)*397) ^ (int) Value;
            }
        }
    }
}
