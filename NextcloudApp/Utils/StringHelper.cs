using System;
using System.Text;

namespace NextcloudApp.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// To the base64.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string ToBase64(this string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }
    }
}
