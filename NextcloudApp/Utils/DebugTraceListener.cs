using SQLite.Net;
using System.Diagnostics;

namespace NextcloudApp.Utils
{
    /// <summary>
    /// Writes SQLite.NET trace to the debug window.
    /// </summary>
    public class DebugTraceListener : ITraceListener
    {
        public void Receive(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
