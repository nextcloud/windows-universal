using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NextcloudApp.Utils
{
    public static class StringUtils
    {
        public static bool IsNullOrEmpty(this string self)
        {
            if (string.IsNullOrEmpty(self)) return true;
            return string.IsNullOrEmpty(self.Trim());
        }

        public static string ToBase64(this string self)
        {
            var bytes = Encoding.UTF8.GetBytes(self);
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string self)
        {
            var bytes = Convert.FromBase64String(self);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
