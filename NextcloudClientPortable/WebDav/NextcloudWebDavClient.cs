using DecaTec.WebDav;
using NextcloudClient.WebDav.WebDavArtifacts;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav
{
    public class NextcloudWebDavClient : WebDavClient
    {

        private static readonly XmlSerializer PropFindSerializer = new XmlSerializer(typeof(NextcloudPropFind));

        #region Constructor

        public NextcloudWebDavClient()
            : base()
        {
            SetDefaultRequestHeaders();
        }

        public NextcloudWebDavClient(HttpMessageHandler httpMessageHandler)
            : base(httpMessageHandler)
        {
            SetDefaultRequestHeaders();
        }

        public NextcloudWebDavClient(HttpMessageHandler httpMessageHandler, bool disposeHandler)
            : base(httpMessageHandler, disposeHandler)
        {
            SetDefaultRequestHeaders();
        }

        #endregion Constructor    

        public async Task<WebDavResponseMessage> PropFindAsync(string requestUrl, WebDavDepthHeaderValue depth, NextcloudPropFind propfind)
        {
            return await PropFindAsync(UriHelper.CreateUriFromUrl(requestUrl), depth, propfind, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(Uri requestUri, WebDavDepthHeaderValue depth, NextcloudPropFind propfind)
        {
            return await PropFindAsync(requestUri, depth, propfind, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(string requestUrl, WebDavDepthHeaderValue depth, NextcloudPropFind propfind, HttpCompletionOption completionOption)
        {
            return await PropFindAsync(UriHelper.CreateUriFromUrl(requestUrl), depth, propfind, completionOption, CancellationToken.None);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(Uri requestUri, WebDavDepthHeaderValue depth, NextcloudPropFind propfind, HttpCompletionOption completionOption)
        {
            return await PropFindAsync(requestUri, depth, propfind, completionOption, CancellationToken.None);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(string requestUrl, WebDavDepthHeaderValue depth, NextcloudPropFind propfind, CancellationToken cancellationToken)
        {
            return await PropFindAsync(UriHelper.CreateUriFromUrl(requestUrl), depth, propfind, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(Uri requestUri, WebDavDepthHeaderValue depth, NextcloudPropFind propfind, CancellationToken cancellationToken)
        {
            return await PropFindAsync(requestUri, depth, propfind, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(string requestUrl, WebDavDepthHeaderValue depth, NextcloudPropFind propfind, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return await PropFindAsync(UriHelper.CreateUriFromUrl(requestUrl), depth, propfind, completionOption, cancellationToken);
        }

        public async Task<WebDavResponseMessage> PropFindAsync(Uri requestUri, WebDavDepthHeaderValue depth, NextcloudPropFind propfind, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            string requestContentString = string.Empty;

            if (propfind != null)
                requestContentString = WebDavHelper.GetUtf8EncodedXmlWebDavRequestString(PropFindSerializer, propfind);

            return await PropFindAsync(requestUri, depth, requestContentString, completionOption, cancellationToken);
        }
    }
}
