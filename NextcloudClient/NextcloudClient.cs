using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Newtonsoft.Json;
using NextcloudClient.Exceptions;
using NextcloudClient.Types;
using DecaTec.WebDav;
using Windows.Security.Credentials;
using Windows.Security.Cryptography.Certificates;

namespace NextcloudClient
{
    /// <summary>
    ///     Nextcloud OCS and DAV access client
    /// </summary>
    public class NextcloudClient : IDisposable
    {
        #region PRIVATE PROPERTIES

        /// <summary>
        ///     WebDavNet instance.
        /// </summary>
        private readonly WebDavSession _dav;

        /// <summary>
        ///     Nextcloud Base URL.
        /// </summary>
        private readonly string _url;

        /// <summary>
        ///     The client
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// The HTTP base protocol filter
        /// </summary>
        private readonly HttpBaseProtocolFilter _httpBaseProtocolFilter;

        /// <summary>
        ///     Nextcloud WebDAV access path.
        /// </summary>
        private const string Davpath = "remote.php/webdav";

        /// <summary>
        ///     Nextcloud OCS API access path.
        /// </summary>
        private const string Ocspath = "ocs/v1.php";

        /// <summary>
        ///     OCS Share API path.
        /// </summary>
        private const string OcsServiceShare = "apps/files_sharing/api/v1";

        private const string OcsServiceData = "privatedata";

        /// <summary>
        ///     OCS Provisioning API path.
        /// </summary>
        private const string OcsServiceCloud = "cloud";

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        ///     Initializes a new instance of the <see cref="NextcloudClient" /> class.
        /// </summary>
        /// <param name="url">Nextcloud instance URL.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="password">Password.</param>
        public NextcloudClient(string url, string userId, string password)
            : this(url, new PasswordCredential(url, userId, password))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NextcloudClient"/> class.
        /// </summary>
        /// <param name="url">Nextcloud instance URL.</param>
        /// <param name="passwordCredential">The password credential.</param>
        public NextcloudClient(string url, PasswordCredential passwordCredential) 
            : this(url, new HttpBaseProtocolFilter() { ServerCredential = passwordCredential })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NextcloudClient"/> class.
        /// </summary>
        /// <param name="url">Nextcloud instance URL.</param>
        /// <param name="httpBaseProtocolFilter">The HTTP base protocol filter.</param>
        public NextcloudClient(string url, HttpBaseProtocolFilter httpBaseProtocolFilter)
        {
            if (url == null)
            {
                return;
            }

            // In case URL has a trailing slash remove it
            if (url.EndsWith("/"))
            {
                url = url.TrimEnd('/');
            }

            _url = url;
            _httpBaseProtocolFilter = httpBaseProtocolFilter;

            // Disable the UI mode, we will handle password entry in the app
            _httpBaseProtocolFilter.AllowUI = false;

            _client = new HttpClient(_httpBaseProtocolFilter);
            _client.DefaultRequestHeaders["Pragma"] = "no-cache";

            var encoded =
                Convert.ToBase64String(
                    Encoding.GetEncoding("ISO-8859-1").GetBytes(
                        _httpBaseProtocolFilter.ServerCredential.UserName + ":" +
                        _httpBaseProtocolFilter.ServerCredential.Password
                    ));
            _client.DefaultRequestHeaders["Authorization"] = "Basic " + encoded;

            _dav = new WebDavSession(_url, new System.Net.NetworkCredential(_httpBaseProtocolFilter.ServerCredential.UserName, _httpBaseProtocolFilter.ServerCredential.Password))
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        #endregion

        #region Settings

        /// <summary>
        /// Gets or sets a value indicating whether to ignore server certificate errors.
        /// Be careful, setting this to <c>true</c> will allow MITM attacks!
        /// </summary>
        /// <value>
        /// <c>true</c> if server certificate errors are ignored; otherwise, <c>false</c>.
        /// </value>
        public bool IgnoreServerCertificateErrors
        {
            get { return _httpBaseProtocolFilter.IgnorableServerCertificateErrors.Count > 0; }
            set {
                if (value)
                {
                    // Specify the certificate errors which should be ignored.
                    // It is recommended to only ignore expired or untrusted certificate errors.
                    // When an invalid certificate is used by the WebDAV server and these errors are not ignored, 
                    // an exception will be thrown when trying to access WebDAV resources.
                    _httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                    _httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                    _httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                }
                else
                {
                    _httpBaseProtocolFilter.IgnorableServerCertificateErrors.Clear();
                }
            }
        }

        #endregion

        #region DAV

        /// <summary>
        ///     List the specified remote path.
        /// </summary>
        /// <param name="path">remote Path.</param>
        /// <returns>List of Resources.</returns>
        public async Task<List<ResourceInfo>> List(string path)
        {
            var resources = new List<ResourceInfo>();
            var test = GetDavUri(path);
            var result = await _dav.ListAsync(GetDavUri(path));

            var baseUri = new Uri(_url);
            baseUri = new Uri(baseUri, baseUri.AbsolutePath + (baseUri.AbsolutePath.EndsWith("/") ? "" : "/") + Davpath);

            foreach (var item in result)
            {
                var res = new ResourceInfo
                {
                    ContentType = item.IsCollection ? "dav/directory" : item.ContentType,
                    Created = item.CreationDate,
                    ETag = item.ETag,
                    LastModified = item.LastModified,
                    Name = System.Net.WebUtility.UrlDecode(item.Name),
                    QuotaAvailable = item.QuotaAvailableBytes,
                    QuotaUsed = item.QuotaUsedBytes,
                    Size = item.ContentLength != 0 ? item.ContentLength : item.QuotaUsedBytes,
                    Path = System.Net.WebUtility.UrlDecode(item.Uri.AbsoluteUri.Replace(baseUri.AbsoluteUri, ""))
                };
                if (!res.ContentType.Equals("dav/directory"))
                {
                    // if resource not a directory, remove the file name from remote path.
                    res.Path = res.Path.Replace("/" + res.Name, "");
                }
                resources.Add(res);
            }

            return resources;
        }

        /// <summary>
        ///     Gets the resource info for the remote path.
        /// </summary>
        /// <returns>The resource info.</returns>
        /// <param name="path">remote Path.</param>
        /// <param name="name">name of resource to get</param>
        public async Task<ResourceInfo> GetResourceInfo(string path, string name)
        {
            var baseUri = new Uri(_url);
            baseUri = new Uri(baseUri, baseUri.AbsolutePath + (baseUri.AbsolutePath.EndsWith("/") ? "" : "/") + Davpath);

            var result = await _dav.ListAsync(GetDavUri(path));

            if (result.Count <= 0)
            {
                return null;
            }
            foreach (var item in result)
            {
                if (item.Name.Equals(name))
                {
                    var res = new ResourceInfo
                    {
                        ContentType = item.IsCollection ? "dav/directory" : item.ContentType,
                        Created = item.CreationDate,
                        ETag = item.ETag,
                        LastModified = item.LastModified,
                        Name = System.Net.WebUtility.UrlDecode(item.Name),
                        QuotaAvailable = item.QuotaAvailableBytes,
                        QuotaUsed = item.QuotaUsedBytes,
                        Size = item.ContentLength,
                        Path = item.Uri.AbsolutePath.Replace(baseUri.AbsolutePath, "")
                    };
                    if (!res.ContentType.Equals("dav/directory"))
                    {
                        // if resource not a directory, remove the file name from remote path.
                        res.Path = res.Path.Replace("/" + res.Name, "");
                    }
                    return res;
                }
            }
            return null;
        }


        /// <summary>
        ///     Finds remote outgoing shares.
        /// </summary>
        /// <returns>List of shares.</returns>
        public async Task<List<ResourceInfo>> GetSharesView(string viewname)
        {
            var param = new Tuple<string, string>("shared_with_me", "false");
            if (viewname == "sharesIn") param = new Tuple<string, string>("shared_with_me", "true");
            var shares = await GetShares(param);

            List<ResourceInfo> sharesList = new List<ResourceInfo>();

            foreach (var item in shares)
            {
                try
                {
                    var itemShare = await GetResourceInfoByPath(item.Path);

                    sharesList.Add(itemShare);
                }
                catch (ResponseError e)
                {
                    throw e;
                }
            }

            return sharesList;
        }

        /// <summary>
        ///     Finds user favorites.
        /// </summary>
        /// <returns>List of favorites.</returns>
        public async Task<List<ResourceInfo>> GetFavorites()
        {
            var url = new UrlBuilder(_url + "/remote.php/webdav");

            // See: https://docs.nextcloud.com/server/12/developer_manual/client_apis/WebDAV/index.html#listing-favorites
            // Also, for Props see: https://docs.nextcloud.com/server/12/developer_manual/client_apis/WebDAV/index.html
            var content = "<?xml version=\"1.0\"?>"
                + "<oc:filter-files  xmlns:d=\"DAV:\" xmlns:oc=\"http://owncloud.org/ns\" xmlns:nc=\"http://nextcloud.org/ns\">"
                    + "<d:prop>"
                        + "<oc:favorite />"
                    + "</d:prop>"
                    + "<oc:filter-rules>"
                        + "<oc:favorite>1</oc:favorite>"
                    + "</oc:filter-rules>"
                + "</oc:filter-files>";

            var request = new HttpRequestMessage(new HttpMethod("REPORT"), url.ToUri())
            {
                Content = new HttpStringContent(content, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/xml")
            };

            var response = await _client.SendRequestAsync(request);

            var xDoc = XDocument.Parse(response.Content.ToString());

            List<ResourceInfo> favoritesList = new List<ResourceInfo>();

            foreach (XElement element in xDoc.Descendants())
            {
                if (element.ToString().IndexOf("d:href") > -1 && element.ToString().IndexOf("d:response") < 0 )
                {
                    var favoritePath = element.ToString().Replace("<d:href xmlns:d=\"DAV:\">/remote.php/webdav", "").Replace("</d:href>", "");

                    try
                    {
                        var itemFav = await GetResourceInfoByPath(Uri.UnescapeDataString(favoritePath));

                        favoritesList.Add(itemFav);
                    }
                    catch (ResponseError e)
                    {
                        throw e;
                    }
                }
            }

            return favoritesList;
        }

        /// <summary>
        ///     Finds resource info for item by searching its parent.
        /// </summary>
        /// <returns>Resource Info if given item.</returns>
        /// <param name="Path">Path to the Item.</param>
        private async Task<ResourceInfo> GetResourceInfoByPath(string Path)
        {

            var targetPath = "/" + Path.Split('/')[Path.Split('/').Length - 1];
            var parentPath = Path.Replace(targetPath, "/");
            var itemName = targetPath.Replace("/", "");

            var parentResource = await List(parentPath);
            var itemResource = new ResourceInfo();

            foreach (var item in parentResource)
            {
                if (item.Name == itemName)
                {
                    itemResource = item;
                }
            }

            return itemResource;
        }

        /// <summary>
        ///     Download the specified file.
        /// </summary>
        /// <param name="path">File remote Path.</param>
        /// <param name="localStream"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns>File contents.</returns>
        public async Task<bool> Download(string path, Stream localStream, IProgress<WebDavProgress> progress, CancellationToken cancellationToken)
        {
            return await _dav.DownloadFileWithProgressAsync(GetDavUri(path), localStream, progress, cancellationToken);
        }

        /// <summary>
        /// </summary>
        /// <param name="file"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task<Stream> GetThumbnail(ResourceInfo file, int width, int height)
        {
            if (!file.ContentType.StartsWith(@"image/"))
            {
                return null;
            }

            var uri = new Uri(_url + "/index.php/apps/files/api/v1/thumbnail/" + width + "/" + height +
                            (string.IsNullOrEmpty(file.Path) ? "/" : Uri.EscapeDataString(file.Path).Replace("%2F", "/") + "/") +
                            file.Name);
            //See: https://github.com/nextcloud/android/pull/37/files#diff-05dcd4530a2e437ac9592620ca97647eR288
            _client.DefaultRequestHeaders["Cookie"] = "nc_sameSiteCookielax=true;nc_sameSiteCookiestrict=true";

            var response = await _client.GetAsync(uri);

            _client.DefaultRequestHeaders.Remove("Cookie");

            if (response != null)
            {
                return (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'GetThumbnail'" + Environment.NewLine + uri);

            return null;
        }

        /// <summary>
        ///     Upload the specified file to the specified path.
        /// </summary>
        /// <param name="path">remote Path.</param>
        /// <param name="stream"></param>
        /// <param name="contentType">File content type.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns><c>true</c>, if upload successful, <c>false</c> otherwise.</returns>
        public async Task<bool> Upload(string path, Stream stream, string contentType, IProgress<WebDavProgress> progress, CancellationToken cancellationToken)
        {
            return await _dav.UploadFileWithProgressAsync(GetDavUri(path), stream, contentType, progress, cancellationToken);
        }

        /// <summary>
        ///     Checks if the specified remote path exists.
        /// </summary>
        /// <param name="path">remote Path.</param>
        /// <returns><c>true</c>, if remote path exists, <c>false</c> otherwise.</returns>
        public async Task<bool> Exists(string path)
        {
            return await _dav.ExistsAsync(GetDavUri(path));
        }

        /// <summary>
        ///     Creates a new directory at remote path.
        /// </summary>
        /// <returns><c>true</c>, if directory was created, <c>false</c> otherwise.</returns>
        /// <param name="path">remote Path.</param>
        public async Task<bool> CreateDirectory(string path)
        {
            return await _dav.CreateDirectoryAsync(GetDavUri(path));
        }

        /// <summary>
        ///     Delete resource at the specified remote path.
        /// </summary>
        /// <param name="path">remote Path.</param>
        /// <returns><c>true</c>, if resource was deleted, <c>false</c> otherwise.</returns>
        public async Task<bool> Delete(string path)
        {
            return await _dav.DeleteAsync(GetDavUri(path));
        }

        /// <summary>
        ///     Copy the specified source to destination.
        /// </summary>
        /// <param name="source">Source resoure path.</param>
        /// <param name="destination">Destination resource path.</param>
        /// <returns><c>true</c>, if resource was copied, <c>false</c> otherwise.</returns>
        public async Task<bool> Copy(string source, string destination)
        {
            return await _dav.CopyAsync(GetDavUri(source), GetDavUri(destination));
        }

        /// <summary>
        ///     Move the specified source and destination.
        /// </summary>
        /// <param name="source">Source resource path.</param>
        /// <param name="destination">Destination resource path.</param>
        /// <returns><c>true</c>, if resource was moved, <c>false</c> otherwise.</returns>
        public async Task<bool> Move(string source, string destination)
        {
            return await _dav.MoveAsync(GetDavUri(source), GetDavUri(destination));
        }

        /// <summary>
        ///     Downloads a remote directory as zip.
        /// </summary>
        /// <param name="path">File remote Path.</param>
        /// <param name="localStream"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns>File contents.</returns>
        //public async Task<IBuffer> Download(string path, CancellationTokenSource cts, IProgress<HttpProgress> progress)
        public async Task<bool> DownloadDirectoryAsZip(string path, Stream localStream, IProgress<WebDavProgress> progress, CancellationToken cancellationToken)
        {
            return await _dav.DownloadFileWithProgressAsync(GetDavUriZip(path), localStream, progress, cancellationToken);
        }

        #endregion

        #region Nextcloud

        #region Remote Shares

        /// <summary>
        /// Gets the server status.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <returns></returns>
        /// <exception cref="ResponseError">The certificate authority is invalid or incorrect
        /// or
        /// The remote server returned an error: (401) Unauthorized. - 401
        /// or</exception>
        public static async Task<Status> GetServerStatus(string serverUrl)
        {
            return await GetServerStatus(serverUrl, false);
        }

        /// <summary>
        /// Gets the server status.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <param name="ignoreServerCertificateErrors">if set to <c>true</c> [ignore server certificate errors].</param>
        /// <returns></returns>
        /// <exception cref="ResponseError">The certificate authority is invalid or incorrect
        /// or
        /// The remote server returned an error: (401) Unauthorized. - 401
        /// or</exception>
        public static async Task<Status> GetServerStatus(string serverUrl, bool ignoreServerCertificateErrors)
        {
            // In case the URL has no trailing slash, add it
            if ((serverUrl != null) && !serverUrl.EndsWith("/"))
            {
                serverUrl = serverUrl + "/";
            }

            var url = new Uri(new Uri(serverUrl), "status.php");

            var httpBaseProtocolFilter = new HttpBaseProtocolFilter
            {
                AllowUI = false,
                AllowAutoRedirect = false
            };

            if (ignoreServerCertificateErrors)
            {
                // Specify the certificate errors which should be ignored.
                // It is recommended to only ignore expired or untrusted certificate errors.
                // When an invalid certificate is used by the WebDAV server and these errors are not ignored, 
                // an exception will be thrown when trying to access WebDAV resources.
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            }

            var client = new HttpClient(httpBaseProtocolFilter);

            client.DefaultRequestHeaders["Pragma"] = "no-cache";

            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(url);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("The certificate authority is invalid or incorrect"))
                {
                    throw new ResponseError("The certificate authority is invalid or incorrect");
                }
            }

            if (response == null)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new ResponseError("The remote server returned an error: (401) Unauthorized.", "401");
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                throw new ResponseError(response.ReasonPhrase);
            }

            try
            {
                return JsonConvert.DeserializeObject<Status>(content);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Checks the user login.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static async Task<bool> CheckUserLogin(string serverUrl, string userId, string password)
        {
            return await CheckUserLogin(serverUrl, userId, password, false);
        }

        /// <summary>
        /// Checks the user login.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="password">The password.</param>
        /// <param name="ignoreServerCertificateErrors">if set to <c>true</c> [ignore server certificate errors].</param>
        /// <returns></returns>
        public static async Task<bool> CheckUserLogin(string serverUrl, string userId, string password, bool ignoreServerCertificateErrors)
        {
            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            // This method is also called on app reset.
            // Only using a HEAD request doesn't seem to work, because in subsequent calls (with wrong user/password), the server always returns HTTP 200 (OK).
            // So we're using an API call here.
            if (!serverUrl.EndsWith("/"))
            {
                serverUrl = serverUrl + "/";
            }

            var httpBaseProtocolFilter = new HttpBaseProtocolFilter
            {
                AllowUI = false,
                AllowAutoRedirect = false,
                ServerCredential = new PasswordCredential(serverUrl, userId, password)
            };

            if (ignoreServerCertificateErrors)
            {
                // Specify the certificate errors which should be ignored.
                // It is recommended to only ignore expired or untrusted certificate errors.
                // When an invalid certificate is used by the WebDAV server and these errors are not ignored, 
                // an exception will be thrown when trying to access WebDAV resources.
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            }

            var client = new NextcloudClient(serverUrl, httpBaseProtocolFilter);

            User user = null;

            try
            {
                user = await client.GetUserAttributes(userId);
            }
            catch
            {
                // ignored
            }

            return user != null;    
        }

        /// <summary>
        ///     List all remote shares.
        /// </summary>
        /// <returns>List of remote shares.</returns>
        public async Task<object> ListOpenRemoteShare()
        {
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "remote_shares")
                );

            //Debug.Assert(response.StatusCode == HttpStatusCode.OK);

            // TODO: Parse response
            return response;
        }

        /// <summary>
        ///     List all remote shares.
        /// </summary>
        /// <returns>List of remote shares.</returns>
        public async Task<object> ListShare()
        {
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "shares")
                );

            //Debug.Assert(response.StatusCode == HttpStatusCode.OK);

            // TODO: Parse response
            return response;
        }

        /// <summary>
        ///     Accepts a remote share
        /// </summary>
        /// <returns><c>true</c>, if remote share was accepted, <c>false</c> otherwise.</returns>
        /// <param name="shareId">Share identifier.</param>
        public async Task<bool> AcceptRemoteShare(int shareId)
        {
            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "remote_shares") + "/" + shareId
                );

            var responseObj = JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Declines a remote share.
        /// </summary>
        /// <returns><c>true</c>, if remote share was declined, <c>false</c> otherwise.</returns>
        /// <param name="shareId">Share identifier.</param>
        public async Task<bool> DeclineRemoteShare(int shareId)
        {
            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "remote_shares") + "/" + shareId
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        #endregion

        #region Shares

        /// <summary>
        ///     Unshares a file or directory.
        /// </summary>
        /// <returns><c>true</c>, if share was deleted, <c>false</c> otherwise.</returns>
        /// <param name="shareId">Share identifier.</param>
        public async Task<bool> DeleteShare(int shareId)
        {
            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "remote_shares") + "/" + shareId
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Updates a given share. NOTE: Only one of the update parameters can be specified at once.
        /// </summary>
        /// <returns><c>true</c>, if share was updated, <c>false</c> otherwise.</returns>
        /// <param name="shareId">Share identifier.</param>
        /// <param name="perms">(optional) update permissions.</param>
        /// <param name="password">(optional) updated password for public link Share.</param>
        /// <param name="publicUpload">(optional) If set to <c>true</c> enables public upload for public shares.</param>
        public async Task<bool> UpdateShare(int shareId, int perms = -1, string password = null,
            OcsBoolParam publicUpload = OcsBoolParam.None)
        {
            if ((perms == Convert.ToInt32(OcsPermission.None)) && (password == null) &&
                (publicUpload == OcsBoolParam.None))
            {
                return false;
            }

            //var parameters = new List<KeyValuePair<string, string>>();
            var parameters = new Dictionary<string, string>();

            if (perms != Convert.ToInt32(OcsPermission.None))
            {
                parameters.Add("permissions", Convert.ToInt32(perms).ToString());
            }
            if (password != null)
            {
                parameters.Add("password", password);
            }
            switch (publicUpload)
            {
                case OcsBoolParam.True:
                    parameters.Add("publicUpload", "true");
                    break;
                case OcsBoolParam.False:
                    parameters.Add("publicUpload", "false");
                    break;
                case OcsBoolParam.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(publicUpload), publicUpload, null);
            }

            var response = await DoApiRequest(
                "PUT",
                "/" + GetOcsPath(OcsServiceShare, "shares") + "/" + shareId,
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Shares a remote file with link.
        /// </summary>
        /// <returns>instance of PublicShare with the share info.</returns>
        /// <param name="path">path to the remote file to share.</param>
        /// <param name="perms">(optional) permission of the shared object.</param>
        /// <param name="password">(optional) sets a password.</param>
        /// <param name="publicUpload">(optional) allows users to upload files or folders.</param>
        public async Task<PublicShare> ShareWithLink(string path, int perms = -1, string password = null,
            OcsBoolParam publicUpload = OcsBoolParam.None)
        {
            var parameters = new Dictionary<string, string>
            {
                {"shareType", Convert.ToInt32(OcsShareType.Link).ToString()},
                {"path", path}
            };

            if (perms != Convert.ToInt32(OcsPermission.None))
            {
                parameters.Add("permissions", Convert.ToInt32(perms).ToString());
            }
            if (password != null)
            {
                parameters.Add("password", password);
            }
            switch (publicUpload)
            {
                case OcsBoolParam.True:
                    parameters.Add("publicUpload", "true");
                    break;
                case OcsBoolParam.False:
                    parameters.Add("publicUpload", "false");
                    break;
                case OcsBoolParam.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(publicUpload), publicUpload, null);
            }

            var response = await DoApiRequest("POST", "/" + GetOcsPath(OcsServiceShare, "shares"));

            var responseStr = response;

            var share = new PublicShare
            {
                ShareId = Convert.ToInt32(GetFromData(responseStr, "id")),
                Url = GetFromData(responseStr, "url"),
                Token = GetFromData(responseStr, "token"),
                TargetPath = path,
                Perms = perms > -1 ? perms : Convert.ToInt32(OcsPermission.Read)
            };

            return share;
        }

        /// <summary>
        ///     Shares a remote file with specified user.
        /// </summary>
        /// <returns>instance of UserShare with the share info.</returns>
        /// <param name="path">path to the remote file to share.</param>
        /// <param name="username">name of the user whom we want to share a file/folder.</param>
        /// <param name="perms">permissions of the shared object.</param>
        /// <param name="remoteUser">Remote user.</param>
        public async Task<object> ShareWithUser(string path, string username, int perms = -1,
            OcsBoolParam remoteUser = OcsBoolParam.None)
        {
            if ((perms == -1) || (perms > Convert.ToInt32(OcsPermission.All)) || string.IsNullOrEmpty(username))
            {
                return null;
            }

            var parameters = new Dictionary<string, string>
            {
                {"path", path},
                {"shareWith", username},
                {
                    "shareType", remoteUser == OcsBoolParam.True
                        ? Convert.ToInt32(OcsShareType.Remote).ToString()
                        : Convert.ToInt32(OcsShareType.User).ToString()
                },
                {
                    "permissions", perms != Convert.ToInt32(OcsPermission.None)
                        ? perms.ToString()
                        : Convert.ToInt32(OcsPermission.Read).ToString()
                }
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "shares"),
                parameters
                );

            var responseStr = response;

            var share = new UserShare
            {
                ShareId = Convert.ToInt32(GetFromData(responseStr, "id")),
                TargetPath = path,
                Perms = perms,
                SharedWith = username
            };

            return share;
        }

        /// <summary>
        ///     Shares a remote file with specified group.
        /// </summary>
        /// <returns>instance of GroupShare with the share info.</returns>
        /// <param name="path">path to the remote file to share.</param>
        /// <param name="groupName">name of the group whom we want to share a file/folder.</param>
        /// <param name="perms">permissions of the shared object.</param>
        public async Task<GroupShare> ShareWithGroup(string path, string groupName, int perms = -1)
        {
            if ((perms == -1) || (perms > Convert.ToInt32(OcsPermission.All)) || string.IsNullOrEmpty(groupName))
            {
                return null;
            }

            var parameters = new Dictionary<string, string>
            {
                {"shareType", Convert.ToInt32(OcsShareType.Group).ToString()},
                {"path", path},
                {
                    "permissions", perms != Convert.ToInt32(OcsPermission.None)
                        ? perms.ToString()
                        : Convert.ToInt32(OcsPermission.Read).ToString()
                },
                {"shareWith", groupName}
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "shares"),
                parameters
                );

            var responseStr = response;

            var share = new GroupShare
            {
                ShareId = Convert.ToInt32(GetFromData(responseStr, "id")),
                TargetPath = path,
                Perms = perms,
                SharedWith = groupName
            };

            return share;
        }

        /// <summary>
        ///     Checks whether a path is already shared.
        /// </summary>
        /// <returns><c>true</c> if this instance is shared the specified path; otherwise, <c>false</c>.</returns>
        /// <param name="path">path to the share to be checked.</param>
        public async Task<bool> IsShared(string path)
        {
            var result = await GetShares(new Tuple<string, string>("path", path));
            return result.Count > 0;
        }

        /// <summary>
        ///     Gets all shares for the current user when <c>path</c> is not set, otherwise it gets shares for the specific file or
        ///     folder
        /// </summary>
        /// <returns>array of shares or empty array if the operation failed.</returns>
        /// <param name="path">(optional) path to the share to be checked.</param>
        /// <param name="reshares">(optional) returns not only the shares from	the current user but all shares from the given file.</param>
        /// <param name="subfiles">(optional) returns all shares within	a folder, given that path defines a folder.</param>
        public async Task<List<Share>> GetShares(Tuple<string, string> tParam, OcsBoolParam reshares = OcsBoolParam.None,
            OcsBoolParam subfiles = OcsBoolParam.None)
        {
            var parameters = new Dictionary<string, string>();

            if (tParam != null)
            {
                parameters.Add(tParam.Item1, tParam.Item2);
            }

            switch (reshares)
            {
                case OcsBoolParam.True:
                    parameters.Add("reshares", "true");
                    break;
                case OcsBoolParam.False:
                    parameters.Add("reshares", "false");
                    break;
                case OcsBoolParam.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reshares), reshares, null);
            }
            switch (subfiles)
            {
                case OcsBoolParam.True:
                    parameters.Add("subfiles", "true");
                    break;
                case OcsBoolParam.False:
                    parameters.Add("subfiles", "false");
                    break;
                case OcsBoolParam.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(subfiles), subfiles, null);
            }

            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "shares"),
                parameters
                );

            var responseStr = response;

            return GetShareList(responseStr);
        }

        #endregion

        #region Users

        /// <summary>
        ///     Create a new user with an initial password via provisioning API.
        /// </summary>
        /// <returns><c>true</c>, if user was created, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to be created.</param>
        /// <param name="initialPassword">password for user being created.</param>
        public async Task<bool> CreateUser(string username, string initialPassword)
        {
            var parameters = new Dictionary<string, string>
            {
                {"userid", username},
                {"password", initialPassword}
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "users"),
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Deletes a user via provisioning API.
        /// </summary>
        /// <returns><c>true</c>, if user was deleted, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to be deleted.</param>
        public async Task<bool> DeleteUser(string username)
        {
            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null) return false;
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Checks a user via provisioning API.
        /// </summary>
        /// <returns><c>true</c>, if exists was usered, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to be checked.</param>
        public async Task<bool> UserExists(string username)
        {
            var result = await SearchUsers(username);
            return result.Contains(username);
        }

        /// <summary>
        ///     Searches for users via provisioning API.
        /// </summary>
        /// <returns>list of users.</returns>
        /// <param name="username">name of user to be searched for.</param>
        public async Task<List<string>> SearchUsers(string username)
        {
            var parameters = new Dictionary<string, string>
            {
                {"search", username}
            };

            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "users"),
                parameters
                );

            var responseStr = response;

            return GetDataElements(responseStr);
        }

        /// <summary>
        ///     Gets the user's attributes.
        /// </summary>
        /// <returns>The user attributes.</returns>
        /// <param name="username">Username.</param>
        public async Task<User> GetUserAttributes(string username)
        {            
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceCloud, "users") + "/" + username
                );

            var responseStr = response;

            return GetUser(responseStr);
        }

        /// <summary>
        ///     Gets the user's avatar.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public async Task<Uri> GetUserAvatarUrl(string username, int size)
        {
            var url = new Uri(_url + "/index.php/avatar/" + username + "/" + size);
            var client = new HttpClient(new HttpBaseProtocolFilter { AllowUI = false });

            client.DefaultRequestHeaders["Pragma"] = "no-cache";

            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendRequestAsync(request);

            if (response.StatusCode != HttpStatusCode.Ok)
            {
                return null;
            }

            if (!response.Headers.ContainsKey("Content-Length"))
            {
                response = await client.GetAsync(url);
                if (response != null)
                {
                    var stream = (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
                    using (var memStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memStream);
                        return memStream.Length > 100 ? url : null;
                    }
                }
            }
            try
            {
                if (response != null)
                {
                    var length = long.Parse(response.Headers["Content-Length"]);
                    if (length > 0)
                    {
                        return url;
                    }
                }
            }
            catch
            {
                return null;
            }
            return url;
        }

        /// <summary>
        ///     Sets a user attribute. See
        ///     https://doc.Nextcloud.com/server/7.0EE/admin_manual/configuration_auth_backends/user_provisioning_api.html#users-edituser
        ///     for reference.
        /// </summary>
        /// <returns><c>true</c>, if user attribute was set, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to modify.</param>
        /// <param name="key">key of the attribute to set.</param>
        /// <param name="value">value to set.</param>
        public async Task<bool> SetUserAttribute(string username, string key, string value)
        {
            var parameters = new Dictionary<string, string>
            {
                {"key", key},
                {"value", value}
            };

            var response = await DoApiRequest(
                "PUT",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username,
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Adds a user to a group.
        /// </summary>
        /// <returns><c>true</c>, if user was added to group, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to be added.</param>
        /// <param name="groupName">name of group user is to be added to.</param>
        public async Task<bool> AddUserToGroup(string username, string groupName)
        {
            var parameters = new Dictionary<string, string>
            {
                {"groupid", groupName}
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username + "/groups",
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Get a list of groups associated to a user.
        /// </summary>
        /// <returns>list of groups.</returns>
        /// <param name="username">name of user to list groups.</param>
        public async Task<List<string>> GetUserGroups(string username)
        {
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username + "/groups"
                );

            var responseStr = response;

            return GetDataElements(responseStr);
        }

        /// <summary>
        ///     Check if a user is in a group.
        /// </summary>
        /// <returns><c>true</c>, if user is in group, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user.</param>
        /// <param name="groupName">name of group.</param>
        public async Task<bool> IsUserInGroup(string username, string groupName)
        {
            var groups = await GetUserGroups(username);
            return groups.Contains(groupName);
        }

        /// <summary>
        ///     Removes a user from a group.
        /// </summary>
        /// <returns><c>true</c>, if user was removed from group, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to be removed.</param>
        /// <param name="groupName">name of group user is to be removed from.</param>
        public async Task<bool> RemoveUserFromGroup(string username, string groupName)
        {
            var parameters = new Dictionary<string, string>
            {
                {"groupid", groupName}
            };

            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username + "/groups",
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Adds a user to a subadmin group.
        /// </summary>
        /// <returns><c>true</c>, if user was added to sub admin group, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user to be added to subadmin group.</param>
        /// <param name="groupName">name of subadmin group.</param>
        public async Task<bool> AddUserToSubAdminGroup(string username, string groupName)
        {
            var parameters = new Dictionary<string, string>
            {
                {"groupid", groupName}
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username + "/subadmins",
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Get a list of subadmin groups associated to a user.
        /// </summary>
        /// <returns>list of subadmin groups.</returns>
        /// <param name="username">name of user.</param>
        public async Task<List<string>> GetUserSubAdminGroups(string username)
        {
            string responseStr = null;

            try
            {
                var response = await DoApiRequest(
                    "GET",
                    "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username + "/subadmins"
                    );

                responseStr = response;
            }
            catch (OcsResponseError ocserr)
            {
                if (ocserr.StatusCode.Equals("102")) // empty response results in a OCS 102 Error
                {
                    return new List<string>();
                }
            }

            return GetDataElements(responseStr);
        }

        /// <summary>
        ///     Check if a user is in a subadmin group.
        /// </summary>
        /// <returns><c>true</c>, if user is in sub admin group, <c>false</c> otherwise.</returns>
        /// <param name="username">name of user.</param>
        /// <param name="groupNname">name of subadmin group.</param>
        public async Task<bool> IsUserInSubAdminGroup(string username, string groupNname)
        {
            var groups = await GetUserSubAdminGroups(username);
            return groups.Contains(groupNname);
        }

        /// <summary>
        ///     Removes the user from sub admin group.
        /// </summary>
        /// <returns><c>true</c>, if user from sub admin group was removed, <c>false</c> otherwise.</returns>
        /// <param name="username">Username.</param>
        /// <param name="groupName">Group name.</param>
        public async Task<bool> RemoveUserFromSubAdminGroup(string username, string groupName)
        {
            var parameters = new Dictionary<string, string>
            {
                {"groupid", groupName}
            };

            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "users") + "/" + username + "/subadmins",
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        #endregion

        #region Groups

        /// <summary>
        ///     Create a new group via provisioning API.
        /// </summary>
        /// <returns><c>true</c>, if group was created, <c>false</c> otherwise.</returns>
        /// <param name="groupName">name of group to be created.</param>
        public async Task<bool> CreateGroup(string groupName)
        {
            var parameters = new Dictionary<string, string>
            {
                {"groupid", groupName}
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "groups"),
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Deletes the group.
        /// </summary>
        /// <returns><c>true</c>, if group was deleted, <c>false</c> otherwise.</returns>
        /// <param name="groupName">Group name.</param>
        public async Task<bool> DeleteGroup(string groupName)
        {
            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "groups") + "/" + groupName
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Checks a group via provisioning API.
        /// </summary>
        /// <returns><c>true</c>, if group exists, <c>false</c> otherwise.</returns>
        /// <param name="groupName">name of group to be checked.</param>
        public async Task<bool> GroupExists(string groupName)
        {
            var parameters = new Dictionary<string, string>
            {
                {"search", groupName}
            };

            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "groups"),
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        #endregion

        #region Config

        /// <summary>
        ///     Returns Nextcloud config information.
        /// </summary>
        /// <returns>The config.</returns>
        public async Task<Config> GetConfig()
        {
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath("", "config")
                );

            var responseStr = response;

            var cfg = new Config
            {
                Contact = GetFromData(responseStr, "contact"),
                Host = GetFromData(responseStr, "host"),
                Ssl = GetFromData(responseStr, "ssl"),
                Version = GetFromData(responseStr, "version"),
                website = GetFromData(responseStr, "website")
            };

            return cfg;
        }

        #endregion

        #region Application attributes

        /// <summary>
        ///     Returns an application attribute
        /// </summary>
        /// <returns>App Attribute List.</returns>
        /// <param name="app">application id.</param>
        /// <param name="key">attribute key or None to retrieve all values for the given application.</param>
        public async Task<List<AppAttribute>> GetAttribute(string app = "", string key = "")
        {
            var path = "getattribute";
            if (!app.Equals(""))
            {
                path += "/" + app;
                if (!key.Equals(""))
                {
                    path += "/" + System.Net.WebUtility.UrlEncode(key);
                }
            }

            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceData, path)
                );

            var responseStr = response;

            return GetAttributeList(responseStr);
        }

        /// <summary>
        ///     Sets an application attribute.
        /// </summary>
        /// <returns><c>true</c>, if attribute was set, <c>false</c> otherwise.</returns>
        /// <param name="app">application id.</param>
        /// <param name="key">key of the attribute to set.</param>
        /// <param name="value">value to set.</param>
        public async Task<bool> SetAttribute(string app, string key, string value)
        {
            var path = "setattribute" + "/" + app + "/" + System.Net.WebUtility.UrlEncode(key);

            var parameters = new Dictionary<string, string>
            {
                {"value", value}
            };

            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceData, path),
                parameters
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Deletes an application attribute.
        /// </summary>
        /// <returns><c>true</c>, if attribute was deleted, <c>false</c> otherwise.</returns>
        /// <param name="app">application id.</param>
        /// <param name="key">key of the attribute to delete.</param>
        public async Task<bool> DeleteAttribute(string app, string key)
        {
            var path = "deleteattribute" + "/" + app + "/" + System.Net.WebUtility.UrlEncode(key);

            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceData, path)
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        #endregion

        #region Apps

        /// <summary>
        ///     List all enabled apps through the provisioning api.
        /// </summary>
        /// <returns>a list of apps and their enabled state.</returns>
        public async Task<List<string>> GetApps()
        {
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "apps")
                );

            var responseStr = response;

            return GetDataElements(responseStr);
        }

        /// <summary>
        ///     Gets information about the specified app.
        /// </summary>
        /// <returns>App information.</returns>
        /// <param name="appName">App name.</param>
        public async Task<AppInfo> GetApp(string appName)
        {
            var response = await DoApiRequest(
                "GET",
                "/" + GetOcsPath(OcsServiceShare, "apps") + "/" + appName
                );

            var responseStr = response;

            return GetAppInfo(responseStr);
        }

        /// <summary>
        ///     Enable an app through provisioning_api.
        /// </summary>
        /// <returns><c>true</c>, if app was enabled, <c>false</c> otherwise.</returns>
        /// <param name="appName">Name of app to be enabled.</param>
        public async Task<bool> EnableApp(string appName)
        {
            var response = await DoApiRequest(
                "POST",
                "/" + GetOcsPath(OcsServiceShare, "apps") + "/" + appName
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        /// <summary>
        ///     Disable an app through provisioning_api
        /// </summary>
        /// <returns><c>true</c>, if app was disabled, <c>false</c> otherwise.</returns>
        /// <param name="appName">Name of app to be disabled.</param>
        public async Task<bool> DisableApp(string appName)
        {
            var response = await DoApiRequest(
                "DELETE",
                "/" + GetOcsPath(OcsServiceShare, "apps") + "/" + appName
                );

            var responseObj =
                JsonConvert.DeserializeObject<OCS>(response);

            if (responseObj == null)
            {
                return false;
            }
            if (responseObj.Meta.StatusCode == 100)
            {
                return true;
            }
            throw new OcsResponseError(responseObj.Meta.Message, responseObj.Meta.StatusCode.ToString());
        }

        #endregion

        #endregion

        #region Url Handling

        private async Task<string> DoApiRequest(string method, string path, Dictionary<string, string> parameters = null)
        {
            var url = new UrlBuilder(_url + "/" + Ocspath + path);
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            url.AddQueryParameter(parameter.Key, parameter.Value);
                        }
                    }
                    _client.DefaultRequestHeaders["OCS-APIREQUEST"] = "true";
                    response = await _client.GetAsync(url.ToUri());
                    _client.DefaultRequestHeaders.Remove("OCS-APIREQUEST");
                    break;

                default:
                    var content2 = new HttpFormUrlEncodedContent(parameters);
                    content2.Headers["OCS-APIREQUEST"] = true.ToString();
                    response = await _client.PostAsync(url.ToUri(), content2);
                    break;
            }

            CheckOcsStatus(response);

            return response.Content.ToString();
        }

        /// <summary>
        ///     Gets the DAV request URI.
        /// </summary>
        /// <returns>The DAV URI.</returns>
        /// <param name="path">remote Path.</param>
        private Uri GetDavUri(string path)
        {
            return new Uri(_url + "/" + Davpath + Uri.EscapeDataString(path).Replace("%2F", "/"));
        }

        /// <summary>
        ///     Gets the DAV request URI.
        /// </summary>
        /// <returns>The DAV URI.</returns>
        /// <param name="path">remote Path.</param>
        private Uri GetDavUriZip(string path)
        {
            var pathArry = path.Split('/');
            var files = pathArry[pathArry.Length - 2 ];
            path = path.Substring(0, path.Length - (files.Length + 1) );

            var url = new UrlBuilder(_url + "/index.php/apps/files/ajax/download.php");
            var parameters = new Dictionary<string, string>
            {
                {"dir", path},
                {"files", files}
            };
            foreach (var parameter in parameters)
            {
                url.AddQueryParameter(parameter.Key, parameter.Value);
            }
            return url.ToUri();
        }

        /// <summary>
        ///     Gets the remote path for OCS API.
        /// </summary>
        /// <returns>The ocs path.</returns>
        /// <param name="service">Service.</param>
        /// <param name="action">Action.</param>
        private string GetOcsPath(string service, string action)
        {
            var slash = !service.Equals("") ? "/" : "";
            return service + slash + action;
        }

        #endregion

        #region OCS Response parsing

        /// <summary>
        ///     Get element value from OCS Meta.
        /// </summary>
        /// <returns>Element value.</returns>
        /// <param name="response">XML OCS response.</param>
        /// <param name="elementName">XML Element name.</param>
        private static string GetFromMeta(string response, string elementName)
        {
            var xdoc = XDocument.Parse(response);

            return (from data in xdoc.Descendants(XName.Get("meta"))
                select data.Element(XName.Get(elementName))
                into node
                where node != null
                select node.Value).FirstOrDefault();
        }

        /// <summary>
        ///     Get element value from OCS Data.
        /// </summary>
        /// <returns>Element value.</returns>
        /// <param name="response">XML OCS response.</param>
        /// <param name="elementName">XML Element name.</param>
        private string GetFromData(string response, string elementName)
        {
            var xdoc = XDocument.Parse(response);

            return (from data in xdoc.Descendants(XName.Get("data"))
                select data.Element(XName.Get(elementName))
                into node
                where node != null
                select node.Value).FirstOrDefault();
        }

        /// <summary>
        ///     Gets the data element values.
        /// </summary>
        /// <returns>The data elements.</returns>
        /// <param name="response">XML OCS Response.</param>
        private List<string> GetDataElements(string response)
        {
            var xdoc = XDocument.Parse(response);

            return
                (from data in xdoc.Descendants(XName.Get("data"))
                    from node in data.Descendants(XName.Get("element"))
                    select node.Value).ToList();
        }

        /// <summary>
        ///     Gets the share list from a OCS Data response.
        /// </summary>
        /// <returns>The share list.</returns>
        /// <param name="response">XML OCS Response.</param>
        private List<Share> GetShareList(string response)
        {
            var shares = new List<Share>();
            var xdoc = XDocument.Parse(response);

            foreach (var data in xdoc.Descendants(XName.Get("element")))
            {
                var node = data.Element(XName.Get("share_type"));
                if (node == null)
                {
                    continue;
                }

                #region Share Type

                var shareType = Convert.ToInt32(node.Value);
                Share share;
                if (shareType == Convert.ToInt32(OcsShareType.Link))
                {
                    share = new PublicShare();
                }
                else if (shareType == Convert.ToInt32(OcsShareType.User))
                {
                    share = new UserShare();
                }
                else if (shareType == Convert.ToInt32(OcsShareType.Group))
                {
                    share = new GroupShare();
                }
                else
                {
                    share = new Share();
                }
                share.AdvancedProperties = new AdvancedShareProperties();

                #endregion

                #region General Properties

                node = data.Element(XName.Get("id"));
                if (node != null)
                {
                    share.ShareId = Convert.ToInt32(node.Value);
                }

                node = data.Element(XName.Get("file_target"));
                if (node != null)
                {
                    share.TargetPath = node.Value;
                }

                node = data.Element(XName.Get("path"));
                if (node != null)
                {
                    share.Path = node.Value;
                }

                node = data.Element(XName.Get("permissions"));
                if (node != null)
                {
                    share.Perms = Convert.ToInt32(node.Value);
                }

                #endregion

                #region Advanced Properties

                node = data.Element(XName.Get("item_type"));
                if (node != null)
                {
                    share.AdvancedProperties.ItemType = node.Value;
                }

                node = data.Element(XName.Get("item_source"));
                if (node != null)
                {
                    share.AdvancedProperties.ItemSource = node.Value;
                }

                node = data.Element(XName.Get("parent"));
                if (node != null)
                {
                    share.AdvancedProperties.Parent = node.Value;
                }

                node = data.Element(XName.Get("file_source"));
                if (node != null)
                {
                    share.AdvancedProperties.FileSource = node.Value;
                }

                node = data.Element(XName.Get("stime"));
                if (node != null)
                {
                    share.AdvancedProperties.STime = node.Value;
                }

                node = data.Element(XName.Get("expiration"));
                if (node != null)
                {
                    share.AdvancedProperties.Expiration = node.Value;
                }

                node = data.Element(XName.Get("mail_send"));
                if (node != null)
                {
                    share.AdvancedProperties.MailSend = node.Value;
                }

                node = data.Element(XName.Get("uid_owner"));
                if (node != null)
                {
                    share.AdvancedProperties.Owner = node.Value;
                }

                node = data.Element(XName.Get("storage_id"));
                if (node != null)
                {
                    share.AdvancedProperties.StorageId = node.Value;
                }

                node = data.Element(XName.Get("storage"));
                if (node != null)
                {
                    share.AdvancedProperties.Storage = node.Value;
                }

                node = data.Element(XName.Get("file_parent"));
                if (node != null)
                {
                    share.AdvancedProperties.FileParent = node.Value;
                }

                node = data.Element(XName.Get("share_with_displayname"));
                if (node != null)
                {
                    share.AdvancedProperties.ShareWithDisplayname = node.Value;
                }

                node = data.Element(XName.Get("displayname_owner"));
                if (node != null)
                {
                    share.AdvancedProperties.DisplaynameOwner = node.Value;
                }

                #endregion

                #region ShareType specific

                if (shareType == Convert.ToInt32(OcsShareType.Link))
                {
                    node = data.Element(XName.Get("url"));
                    if (node != null)
                    {
                        ((PublicShare) share).Url = node.Value;
                    }

                    node = data.Element(XName.Get("token"));
                    if (node != null)
                    {
                        ((PublicShare) share).Token = node.Value;
                    }
                }
                else if (shareType == Convert.ToInt32(OcsShareType.User))
                {
                    node = data.Element(XName.Get("share_with"));
                    if (node != null)
                    {
                        ((UserShare) share).SharedWith = node.Value;
                    }
                }
                else if (shareType == Convert.ToInt32(OcsShareType.Group))
                {
                    node = data.Element(XName.Get("share_with"));
                    if (node != null)
                    {
                        ((GroupShare) share).SharedWith = node.Value;
                    }
                }

                #endregion

                shares.Add(share);
            }

            return shares;
        }

        /// <summary>
        ///     Checks the validity of the OCS Request. If invalid a exception is thrown.
        /// </summary>
        /// <param name="response">OCS Response.</param>
        /// <exception cref="ResponseError">Empty response</exception>
        /// <exception cref="OcsResponseError"></exception>
        private static void CheckOcsStatus(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new ResponseError("The remote server returned an error: (401) Unauthorized.", "401");
            }
            if (response.Content == null)
            {
                throw new ResponseError(response.ReasonPhrase);
            }
            var ocsStatus = GetFromMeta(response.Content.ToString(), "statuscode");
            if (ocsStatus == null)
            {
                throw new ResponseError("Empty response");
            }
            if (!ocsStatus.Equals("100"))
            {
                throw new OcsResponseError(GetFromMeta(response.Content.ToString(), "message"), ocsStatus);
            }
        }

        /// <summary>
        ///     Returns a list of application attributes.
        /// </summary>
        /// <param name="response">XML OCS Response.</param>
        /// <returns>
        ///     List of application attributes.
        /// </returns>
        private List<AppAttribute> GetAttributeList(string response)
        {
            var result = new List<AppAttribute>();
            var xdoc = XDocument.Parse(response);

            foreach (var data in xdoc.Descendants(XName.Get("data")))
            {
                foreach (var element in data.Descendants(XName.Get("element")))
                {
                    var attr = new AppAttribute();

                    var node = element.Element(XName.Get("app"));
                    if (node != null)
                    {
                        attr.App = node.Value;
                    }

                    node = element.Element(XName.Get("key"));
                    if (node != null)
                    {
                        attr.Key = node.Value;
                    }

                    node = element.Element(XName.Get("value"));
                    if (node != null)
                    {
                        attr.value = node.Value;
                    }

                    result.Add(attr);
                }
            }

            return result;
        }

        /// <summary>
        ///     Gets the user attributes from a OCS XML Response.
        /// </summary>
        /// <returns>The user attributes.</returns>
        /// <param name="response">OCS XML Response.</param>
        private User GetUser(string response)
        {
            var user = new User();
            var xdoc = XDocument.Parse(response);

            var data = xdoc.Descendants(XName.Get("data")).FirstOrDefault();
            if (data == null)
            {
                return user;
            }
            var node = data.Element(XName.Get("displayname"));
            if (node != null)
            {
                user.DisplayName = node.Value;
            }

            node = data.Element(XName.Get("email"));
            if (node != null)
            {
                user.EMail = node.Value;
            }

            node = data.Element(XName.Get("enabled"));
            if (node != null)
            {
                user.Enabled = node.Value.Equals("true");
            }

            var quota = new Quota();
            user.Quota = quota;

            var element = data.Descendants(XName.Get("quota")).FirstOrDefault();
            if (element == null)
            {
                return user;
            }

            node = element.Element(XName.Get("free"));
            if (node != null)
            {
                quota.Free = Convert.ToInt64(node.Value);
            }

            node = element.Element(XName.Get("used"));
            if (node != null)
            {
                quota.Used = Convert.ToInt64(node.Value);
            }

            node = element.Element(XName.Get("total"));
            if (node != null)
            {
                quota.Total = Convert.ToInt64(node.Value);
            }

            node = element.Element(XName.Get("relative"));
            if (node != null)
            {
                quota.Relative = Convert.ToDouble(node.Value);
            }

            node = element.Element(XName.Get("quota"));
            if (node != null)
            {
                quota.QuotaValue = Convert.ToInt64(node.Value);
            }

            return user;
        }

        /// <summary>
        /// Gets the application information.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private AppInfo GetAppInfo(string response)
        {
            var app = new AppInfo();
            var xdoc = XDocument.Parse(response);

            foreach (var data in xdoc.Descendants(XName.Get("data")))
            {
                var node = data.Element(XName.Get("id"));
                if (node != null)
                {
                    app.Id = node.Value;
                }

                node = data.Element(XName.Get("name"));
                if (node != null)
                {
                    app.Name = node.Value;
                }

                node = data.Element(XName.Get("description"));
                if (node != null)
                {
                    app.Description = node.Value;
                }

                node = data.Element(XName.Get("licence"));
                if (node != null)
                {
                    app.Licence = node.Value;
                }

                node = data.Element(XName.Get("author"));
                if (node != null)
                {
                    app.Author = node.Value;
                }

                node = data.Element(XName.Get("requiremin"));
                if (node != null)
                {
                    app.RequireMin = node.Value;
                }

                node = data.Element(XName.Get("shipped"));
                if (node != null)
                {
                    app.Shipped = node.Value.Equals("true");
                }

                node = data.Element(XName.Get("standalone"));
                app.Standalone = node != null;

                node = data.Element(XName.Get("default_enable"));
                app.DefaultEnable = node != null;

                node = data.Element(XName.Get("types"));
                if (node != null)
                {
                    app.Types = XmlElementsToList(node);
                }

                node = data.Element(XName.Get("remote"));
                if (node != null)
                {
                    app.Remote = XmlElementsToDict(node);
                }

                node = data.Element(XName.Get("documentation"));
                if (node != null)
                {
                    app.Documentation = XmlElementsToDict(node);
                }

                node = data.Element(XName.Get("info"));
                if (node != null)
                {
                    app.Info = XmlElementsToDict(node);
                }

                node = data.Element(XName.Get("public"));
                if (node != null)
                {
                    app.Public = XmlElementsToDict(node);
                }
            }

            return app;
        }

        /// <summary>
        ///     Returns the elements of a XML Element as a List.
        /// </summary>
        /// <returns>The elements as list.</returns>
        /// <param name="element">XML Element.</param>
        private List<string> XmlElementsToList(XContainer element)
        {
            return element.Descendants(XName.Get("element")).Select(node => node.Value).ToList();
        }

        /// <summary>
        ///     Returns the elements of a XML Element as a Dictionary.
        /// </summary>
        /// <returns>The elements as dictionary.</returns>
        /// <param name="element">XML Element.</param>
        private Dictionary<string, string> XmlElementsToDict(XContainer element)
        {
            return element.Descendants().ToDictionary(node => node.Name.ToString(), node => node.Value);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dav?.Dispose();
            }
        }

        #endregion IDisposable
    }
}