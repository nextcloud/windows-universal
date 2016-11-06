using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace WebDavClient
{
    /// <summary>
    ///     Base manager class for handling all WebDav purpose.
    /// </summary>
    public class WebDavClient
    {
        #region PRIVATE PROPERTIES

        private readonly HttpClient _client;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavClient" /> class.
        /// </summary>
        public WebDavClient() : this(null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavClient" /> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        public WebDavClient(WebDavCredential credential)
        {
            _client = new HttpClient(new HttpBaseProtocolFilter { AllowUI = false });
            _client.DefaultRequestHeaders["Pragma"] = "no-cache";
            Credential = credential;
            if (Credential == null)
            {
                return;
            }
            var encoded =
                Convert.ToBase64String(
                    Encoding.GetEncoding("ISO-8859-1").GetBytes(Credential.UserName + ":" + Credential.Password));
            _client.DefaultRequestHeaders["Authorization"] = "Basic " + encoded;
        }

        #endregion

        #region PUBLIC PROPERTIES

        /// <summary>
        ///     Gets or sets the credential.
        /// </summary>
        /// <value>The credential.</value>
        public WebDavCredential Credential { get; }

        /// <summary>
        ///     Gets or sets the timeout.
        /// </summary>
        /// <value>The timeout.</value>
        public int Timeout { get; set; }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        ///     Deletes the resource behind the specified Url.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <returns>></returns>
        public Task<bool> Delete(string url)
        {
            return Delete(new Uri(url));
        }

        /// <summary>
        ///     Deletes the resource behind the specified Uri.
        /// </summary>
        /// <param name="uri">The Uri.</param>
        /// <returns>></returns>
        public async Task<bool> Delete(Uri uri)
        {
            var response = await _client.DeleteAsync(uri);
            if (response != null)
            {
                return response.StatusCode == HttpStatusCode.NoContent;
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'Delete'" + Environment.NewLine + uri);

            return false;
        }

        /// <summary>
        ///     Creates a directory on the given Url address.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <returns></returns>
        public Task<bool> CreateDirectory(string url)
        {
            return CreateDirectory(new Uri(url));
        }

        /// <summary>
        ///     Creates a directory on the given Uri.
        /// </summary>
        /// <param name="uri">The Uri.</param>
        /// <returns></returns>
        public async Task<bool> CreateDirectory(Uri uri)
        {
            var request = new HttpRequestMessage(new HttpMethod("MKCOL"), uri);
            var response = await _client.SendRequestAsync(request);
            if (response != null)
            {
                return response.StatusCode == HttpStatusCode.Created;
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'CreateDirectory'" + Environment.NewLine + uri);

            return false;
        }

        /// <summary>
        ///     Copies the resource of the specified source Url to the target Url.
        /// </summary>
        /// <param name="sourceUrl">The source Url.</param>
        /// <param name="targetUrl">The target Url.</param>
        /// <returns></returns>
        public Task<bool> Copy(string sourceUrl, string targetUrl)
        {
            return Copy(new Uri(sourceUrl), new Uri(targetUrl));
        }

        /// <summary>
        ///     Copies the resource of the specified source Uri to the target Uri.
        /// </summary>
        /// <param name="sourceUri">The source Uri.</param>
        /// <param name="targetUri">The target Uri.</param>
        /// <returns></returns>
        public async Task<bool> Copy(Uri sourceUri, Uri targetUri)
        {
            var request = new HttpRequestMessage(new HttpMethod("COPY"), sourceUri);
            request.Headers["Destination"] = targetUri.ToString();

            // TODO: Handle overwrite
            //request.Headers["Overwrite"] = "F"; // No Overwrite

            var response = await _client.SendRequestAsync(request);
            if (response != null)
            {
                return
                    response.StatusCode == HttpStatusCode.Created ||
                    response.StatusCode == HttpStatusCode.NoContent;
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'Copy'" + Environment.NewLine + sourceUri);

            return false;
        }

        /// <summary>
        ///     Moves the resource of the specified source Url to the target Url.
        /// </summary>
        /// <param name="sourceUrl">The source Url.</param>
        /// <param name="targetUrl">The target Url.</param>
        /// <returns></returns>
        public Task<bool> Move(string sourceUrl, string targetUrl)
        {
            return Move(new Uri(sourceUrl), new Uri(targetUrl));
        }

        /// <summary>
        ///     Moves the resource of the specified source Uri to the target Uri.
        /// </summary>
        /// <param name="sourceUri">The source Uri.</param>
        /// <param name="targetUri">The target Uri.</param>
        /// <returns></returns>
        public async Task<bool> Move(Uri sourceUri, Uri targetUri)
        {
            var request = new HttpRequestMessage(new HttpMethod("MOVE"), sourceUri);
            request.Headers["Destination"] = targetUri.ToString();

            // TODO: Handle overwrite take a look at http://www.ietf.org/rfc/rfc2518.txt first
            //request.Headers["Overwrite"] = "F"; // No Overwrite
            //request.Headers["Overwrite"] = "T"; // ???

            var response = await _client.SendRequestAsync(request);
            if (response != null)
            {
                return
                    response.StatusCode == HttpStatusCode.Created ||
                    response.StatusCode == HttpStatusCode.NoContent;
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'Move'" + Environment.NewLine + sourceUri);

            return false;
        }

        /// <summary>
        ///     Checks if a resource exists on the specified Url.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <returns></returns>
        public Task<bool> Exists(string url)
        {
            return Exists(new Uri(url));
        }

        /// <summary>
        ///     Checks if a resource exists on the specified Uri.
        /// </summary>
        /// <param name="uri">The Uri.</param>
        /// <returns></returns>
        public async Task<bool> Exists(Uri uri)
        {
            var request = new HttpRequestMessage(new HttpMethod("HEAD"), uri);
            var response = await _client.SendRequestAsync(request);
            if (response != null)
            {
                return response.StatusCode == HttpStatusCode.Ok;
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'Exists'" + Environment.NewLine + uri);

            return false;
        }

        /// <summary>
        ///     Lists all resources on the specified Url.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <returns></returns>
        public Task<List<WebDavResource>> List(string url)
        {
            return List(new Uri(url));
        }

        /// <summary>
        ///     Lists all resources on the specified Uri.
        /// </summary>
        /// <param name="uri">The Uri.</param>
        /// <returns></returns>
        public async Task<List<WebDavResource>> List(Uri uri)
        {
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), uri);

            // Retrieve only the requested folder
            request.Headers["Depth"] = "1";

            var response = await _client.SendRequestAsync(request);
            if (response != null)
            {
                return ExtractResources(await response.Content.ReadAsInputStreamAsync(), uri.AbsolutePath,
                    $"{uri.Scheme}://{uri.Host}");
            }

            // TODO: Errorhandling
            Debug.WriteLine("Empty WebResponse @'List'" + Environment.NewLine + uri);

            return new List<WebDavResource>();
        }

        /// <summary>
        ///     Uploads the file.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <param name="stream"></param>
        /// <param name="contentType">The files content type.</param>
        /// <param name="cts"></param>
        /// <param name="progress"></param>
        /// <returns>true on success, false on error</returns>
        public Task<HttpResponseMessage> UploadFile(string url, IRandomAccessStream stream, string contentType,
            CancellationTokenSource cts, IProgress<HttpProgress> progress)
        {
            return UploadFile(new Uri(url), stream, contentType, cts, progress);
        }

        /// <summary>
        ///     Uploads the file.
        /// </summary>
        /// <param name="uri">The Uri.</param>
        /// <param name="stream"></param>
        /// <param name="contentType">The files content type.</param>
        /// <param name="cts"></param>
        /// <param name="progress"></param>
        /// <returns>true on success, false on error</returns>
        public async Task<HttpResponseMessage> UploadFile(Uri uri, IRandomAccessStream stream, string contentType,
            CancellationTokenSource cts, IProgress<HttpProgress> progress)
        {
            var inputStream = stream.GetInputStreamAt(0);
            var streamContent = new HttpStreamContent(inputStream);
            streamContent.Headers["Content-Type"] = contentType;
            streamContent.Headers["Content-Length"] = stream.Size.ToString();

            //var requestContent = new HttpMultipartContent {streamContent};

            var response = await _client.PutAsync(uri, streamContent).AsTask(cts.Token, progress);

            return response;
        }

        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <param name="cts">The CTS.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>
        /// File contents.
        /// </returns>
        public async Task<IBuffer> DownloadFile(string url, CancellationTokenSource cts, IProgress<HttpProgress> progress)
        {
            return await DownloadFile(new Uri(url), cts, progress);
        }

        /// <summary>
        ///     Downloads the file.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="cts">The CTS.</param>
        /// <param name="progress">The progress.</param>
        /// <returns></returns>
        public async Task<IBuffer> DownloadFile(Uri uri, CancellationTokenSource cts, IProgress<HttpProgress> progress)
        {
            return await _client.GetBufferAsync(uri).AsTask(cts.Token, progress);
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        ///     Extracts the resources.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="rootpath">The rootpath.</param>
        /// <param name="fullpath">The fullpath.</param>
        /// <returns></returns>
        private static List<WebDavResource> ExtractResources(IInputStream stream, string rootpath, string fullpath)
        {
            var webDavResources = new List<WebDavResource>();

            try
            {
                TextReader treader = new StreamReader(stream.AsStreamForRead());
                var xml = treader.ReadToEnd();
                treader.Dispose();

                //Debug.WriteLine(xml);

                var xdoc = XDocument.Parse(xml);

                foreach (var element in xdoc.Descendants(XName.Get("response", "DAV:")))
                {
                    var resource = new WebDavResource();

                    // Do not add hidden files
                    // Hidden files cannot be downloaded from the IIs
                    // For further information see http://support.microsoft.com/kb/216803/

                    var prop = element.Descendants(XName.Get("prop", "DAV:")).FirstOrDefault();

                    var node = prop.Element(XName.Get("ishidden", "DAV:"));
                    if ((node != null) && (node.Value == "1"))
                    {
                        continue;
                    }

                    node = prop.Element(XName.Get("displayname", "DAV:"));
                    if (node != null)
                    {
                        resource.Name = node.Value;
                    }

                    node = element.Element(XName.Get("href", "DAV:"));
                    if (node != null)
                    {
                        Uri href;

                        if (Uri.TryCreate(node.Value, UriKind.Absolute, out href))
                        {
                            resource.Uri = href;
                        }
                        else if (Uri.TryCreate($"{fullpath}{node.Value}", UriKind.Absolute, out href))
                        {
                            resource.Uri = href;
                        }
                    }

                    node = prop.Element(XName.Get("getcontentlength", "DAV:"));
                    if (node != null)
                    {
                        resource.Size = int.Parse(node.Value, CultureInfo.CurrentCulture);
                    }

                    node = prop.Element(XName.Get("creationdate", "DAV:"));
                    if (node != null)
                    {
                        resource.Created = DateTime.Parse(node.Value, CultureInfo.CurrentCulture);
                    }

                    node = prop.Element(XName.Get("getlastmodified", "DAV:"));
                    if (node != null)
                    {
                        resource.Modified = DateTime.Parse(node.Value, CultureInfo.CurrentCulture);
                    }

                    // Check if the resource is a collection
                    var xElement = prop.Element(XName.Get("resourcetype", "DAV:"));
                    if (xElement != null)
                    {
                        node = xElement.Element(XName.Get("collection", "DAV:"));
                    }
                    resource.IsDirectory = node != null;

                    node = prop.Element(XName.Get("getcontenttype", "DAV:"));
                    if (node != null)
                    {
                        resource.ContentType = node.Value;
                    }

                    node = prop.Element(XName.Get("getetag", "DAV:"));
                    if (node != null)
                    {
                        resource.Etag = node.Value;
                    }

                    node = prop.Element(XName.Get("quota-used-bytes", "DAV:"));
                    if (node != null)
                    {
                        resource.QuotaUsed = long.Parse(node.Value);
                    }

                    node = prop.Element(XName.Get("quota-available-bytes", "DAV:"));
                    if (node != null)
                    {
                        resource.QutoaAvailable = long.Parse(node.Value);
                    }

                    if (resource.Name == null && resource.Uri != null)
                    {
                        resource.Name = resource.IsDirectory
                            ? resource.Uri.Segments.Last().TrimEnd('/')
                            : resource.Uri.Segments.Last();

                        if (resource.Uri.AbsolutePath.Equals(rootpath))
                        {
                            resource.Name = "/";
                        }
                    }

                    webDavResources.Add(resource);
                }
            }
            catch (Exception e)
            {
                // TODO: Implement better error handling
                Debug.WriteLine(e.Message);

                webDavResources = new List<WebDavResource>();
            }

            return webDavResources;
        }

        #endregion
    }
}