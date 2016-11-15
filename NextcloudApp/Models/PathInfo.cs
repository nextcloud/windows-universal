using Newtonsoft.Json;
using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class PathInfo
    {
        public ResourceInfo ResourceInfo { get; set; }

        public bool IsRoot { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static PathInfo Deserialize(object json)
        {
            var parameters = json as string;
            return string.IsNullOrEmpty(parameters)
                ? null
                : JsonConvert.DeserializeObject<PathInfo>(parameters);
        }
    }
}
