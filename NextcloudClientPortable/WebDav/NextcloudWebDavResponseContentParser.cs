using DecaTec.WebDav;
using DecaTec.WebDav.WebDavArtifacts;
using NextcloudClient.WebDav.WebDavArtifacts;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav
{
    public static class NextcloudWebDavResponseContentParser
    {
        private static readonly XmlSerializer MultistatusSerializer = new XmlSerializer(typeof(NextcloudMultistatus));
        private static readonly XmlSerializer PropSerializer = new XmlSerializer(typeof(NextcloudProp));

        public static async Task<NextcloudMultistatus> ParseMultistatusResponseContentAsync(HttpContent content)
        {
            if (content == null)
                return null;

            try
            {
                var contentStream = await content.ReadAsStreamAsync();
                var multistatus = (NextcloudMultistatus)MultistatusSerializer.Deserialize(contentStream);
                return multistatus;
            }
            catch (Exception ex)
            {
                throw new WebDavException("Failed to parse a multistatus response", ex);
            }
        }

        public static NextcloudMultistatus ParseMultistatusResponseContentString(string stringContent)
        {
            if (string.IsNullOrEmpty(stringContent))
                return null;

            try
            {
                var contentStream = new StringReader(stringContent);
                var multistatus = (NextcloudMultistatus)MultistatusSerializer.Deserialize(contentStream);
                return multistatus;
            }
            catch (Exception ex)
            {
                throw new WebDavException("Failed to parse a multistatus response", ex);
            }
        }

        public static async Task<NextcloudProp> ParsePropResponseContentAsync(HttpContent content)
        {
            if (content == null)
                return null;

            try
            {
                var contentStream = await content.ReadAsStreamAsync();
                var prop = (NextcloudProp)PropSerializer.Deserialize(contentStream);
                return prop;
            }
            catch (Exception ex)
            {
                throw new WebDavException("Failed to parse a WebDAV Prop", ex);
            }
        }

        public static NextcloudProp ParsePropResponseContentString(string stringContent)
        {
            if (string.IsNullOrEmpty(stringContent))
                return null;

            try
            {
                var contentStream = new StringReader(stringContent);
                var prop = (NextcloudProp)PropSerializer.Deserialize(contentStream);
                return prop;
            }
            catch (Exception ex)
            {
                throw new WebDavException("Failed to parse a WebDAV Prop", ex);
            }
        }
    }
}
