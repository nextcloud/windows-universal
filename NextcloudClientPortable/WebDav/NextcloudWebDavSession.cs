using DecaTec.WebDav;
using NextcloudClient.WebDav.WebDavArtifacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NextcloudClient.WebDav
{
    public class NextcloudWebDavSession : WebDavSession
    {
        #region Constructor

        public NextcloudWebDavSession(NetworkCredential networkCredential)
            : this(new HttpClientHandler() { PreAuthenticate = true, Credentials = networkCredential })
        {
        }

        public NextcloudWebDavSession(string baseUrl, NetworkCredential networkCredential)
            : this(new Uri(baseUrl), new HttpClientHandler() { PreAuthenticate = true, Credentials = networkCredential })
        {
        }
      
        public NextcloudWebDavSession(Uri baseUri, NetworkCredential networkCredential)
            : this(baseUri, new HttpClientHandler() { PreAuthenticate = true, Credentials = networkCredential })
        {
        }

        public NextcloudWebDavSession(HttpMessageHandler httpMessageHandler)
            : this(string.Empty, httpMessageHandler)
        {
        }

        public NextcloudWebDavSession(string baseUrl, HttpMessageHandler httpMessageHandler)
            : this(string.IsNullOrEmpty(baseUrl) ? null : new Uri(baseUrl), httpMessageHandler)
        {
        }

        public NextcloudWebDavSession(Uri baseUri, HttpMessageHandler httpMessageHandler) : base(baseUri, CreateWebDavClient(httpMessageHandler))
        {
        }

        #endregion Constructor

        private NextcloudWebDavClient NextCloudWebDavClient
        {
            get
            {
                return (NextcloudWebDavClient)this.webDavClient;
            }
        }

        public new async Task<IList<NextcloudWebDavSessionListItem>> ListAsync(Uri uri)
        {
            return await ListAsync(uri, NextcloudPropFind.CreatePropFindAllProp());
        }

        public new async Task<IList<NextcloudWebDavSessionListItem>> ListAsync(string url)
        {
            return await ListAsync(UriHelper.CreateUriFromUrl(url));
        }

        public async Task<IList<NextcloudWebDavSessionListItem>> ListAsync(string url, NextcloudPropFind propFind)
        {
            return await ListAsync(UriHelper.CreateUriFromUrl(url), propFind);
        }

        public async Task<IList<NextcloudWebDavSessionListItem>> ListAsync(Uri uri, NextcloudPropFind propFind)
        {
            if (propFind == null)
                throw new ArgumentException("Argument propFind must not be null.");

            uri = UriHelper.CombineUri(this.BaseUri, uri, true);
            var response = await this.NextCloudWebDavClient.PropFindAsync(uri, WebDavDepthHeaderValue.One, propFind);

            // Remember the original port to include it in the hrefs later.
            var port = UriHelper.GetPort(uri);

            if (response.StatusCode != WebDavStatusCode.MultiStatus)
                throw new WebDavException($"Error while executing ListAsync (wrong response status code). Expected status code: 207 (MultiStatus); actual status code: {(int)response.StatusCode} ({response.StatusCode})");

            var multistatus = await NextcloudWebDavResponseContentParser.ParseMultistatusResponseContentAsync(response.Content);

            var itemList = new List<NextcloudWebDavSessionListItem>();

            foreach (var responseItem in multistatus.Response)
            {
                var webDavSessionItem = new NextcloudWebDavSessionListItem();

                Uri href = null;

                if (!string.IsNullOrEmpty(responseItem.Href))
                {
                    if (UriHelper.TryCreateUriFromUrl(responseItem.Href, out href))
                    {
                        var fullQualifiedUri = UriHelper.CombineUri(uri, href, true);
                        fullQualifiedUri = UriHelper.SetPort(fullQualifiedUri, port);
                        webDavSessionItem.Uri = fullQualifiedUri;
                    }
                }

                // Skip the folder which contents were requested, only add children.
                if (href != null && WebUtility.UrlDecode(UriHelper.RemovePort(uri).ToString().Trim('/')).EndsWith(WebUtility.UrlDecode(UriHelper.RemovePort(href).ToString().Trim('/')), StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var item in responseItem.Items)
                {
                    var propStat = item as NextcloudPropstat;

                    // Do not items where no properties could be found.
                    if (propStat == null || propStat.Status.ToLower().Contains("404 not found"))
                        continue;

                    var prop = propStat.Prop;

                    webDavSessionItem.Checksums = prop.Checksums;
                    webDavSessionItem.CommentsCount = prop.CommentsCount;
                    webDavSessionItem.CommentsHref = prop.CommentsHref;
                    webDavSessionItem.CommentsUnread = prop.CommentsUnread;
                    webDavSessionItem.ContentLength = prop.GetContentLength;
                    webDavSessionItem.ContentType = prop.GetContentType;
                    webDavSessionItem.ETag = prop.GetEtag;
                    webDavSessionItem.FileId = prop.FileId;
                    webDavSessionItem.HasPreview = prop.HasPreview;
                    webDavSessionItem.IsFavorite = prop.IsFavorite;
                    webDavSessionItem.LastModified = prop.GetLastModified;
                    webDavSessionItem.OwnerDisplayName = prop.OwnerDisplayName;
                    webDavSessionItem.OwnerId = prop.OwnerId;
                    webDavSessionItem.ShareTypes = prop.ShareTypes;
                    webDavSessionItem.Size = prop.Size;
                    webDavSessionItem.IsFolder = prop.IsFolder;

                    // RFC4331
                    webDavSessionItem.QuotaAvailableBytes = prop.QuotaAvailableBytes;
                    webDavSessionItem.QuotaUsedBytes = prop.QuotaUsedBytes;

                    //// Make sure that the IsDirectory property is set if it's a directory.
                    if (prop.IsFolder.HasValue && prop.IsFolder.Value)
                        webDavSessionItem.IsFolder = prop.IsFolder.Value;
                    else if (prop.ResourceType != null && prop.ResourceType.Collection != null)
                        webDavSessionItem.IsFolder = true;

                    // Make sure that the name property is set.
                    // Naming priority:
                    // 1. displayname (only if it doesn't contain raw unicode, otherwise there are problems with non western characters)
                    // 2. name
                    // 3. (part of) URI.
                    //if (!TextHelper.StringContainsRawUnicode(prop.DisplayName))
                    //    webDavSessionItem.Name = prop.DisplayName;

                    //if (string.IsNullOrEmpty(webDavSessionItem.Name))
                    //    webDavSessionItem.Name = prop.Name;

                    if (string.IsNullOrEmpty(webDavSessionItem.Name) && href != null)
                        webDavSessionItem.Name = WebUtility.UrlDecode(href.ToString().Split('/').Last(x => !string.IsNullOrEmpty(x)));
                }

                itemList.Add(webDavSessionItem);
            }

            return itemList;
        }

        #region Private methods

        private static NextcloudWebDavClient CreateWebDavClient(HttpMessageHandler messageHandler)
        {
            return new NextcloudWebDavClient(messageHandler, false);
        }

        #endregion Private methods
    }
}
