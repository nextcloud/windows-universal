using Newtonsoft.Json;

namespace NextcloudApp.Models
{
    public class PageParameters<T> : IPageParameters where T : class
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static T Deserialize(object json)
        {
            var parameters = json as string;
            return string.IsNullOrEmpty(parameters)
                ? null
                : JsonConvert.DeserializeObject<T>(parameters);
        }
    }
}
