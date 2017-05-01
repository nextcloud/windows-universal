using NextcloudApp.Utils;

namespace NextcloudApp.Models
{
    public class ThemeItem
    {
        public string Name { get; set; }
        public Theme Value { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj as Theme? == Value;
        }

        protected bool Equals(ThemeItem other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name?.GetHashCode() ?? 0)) ^ (int)Value;
            }
        }
    }
}
