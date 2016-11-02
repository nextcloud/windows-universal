using System;

namespace WebDavClient
{
    /// <summary>
    ///     Description of WebDavResource.
    /// </summary>
    public class WebDavResource
    {
        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public long Size { get; set; }

        /// <summary>
        ///     Gets or sets the Uri.
        /// </summary>
        /// <value>The Uri.</value>
        public Uri Uri { get; set; }

        /// <summary>
        ///     Gets or sets the creation date.
        /// </summary>
        /// <value>The created.</value>
        public DateTime Created { get; set; }

        /// <summary>
        ///     Gets or sets the modification date.
        /// </summary>
        /// <value>The modified.</value>
        public DateTime Modified { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this resource is a directory.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this resource is a directory; otherwise, <c>false</c>.
        /// </value>
        public bool IsDirectory { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the contenttype of a file.
        /// </summary>
        /// <value>The file content type.</value>
        public string ContentType { get; set; }

        /// <summary>
        ///     Gets or sets the etag.
        /// </summary>
        /// <value>The etag.</value>
        public string Etag { get; set; }

        /// <summary>
        ///     Gets or sets the quota used in bytes.
        /// </summary>
        /// <value>The quota used.</value>
        public long QuotaUsed { get; set; }

        /// <summary>
        ///     Gets or sets the qutoa available in bytes.
        /// </summary>
        /// <value>The qutoa available.</value>
        public long QutoaAvailable { get; set; }
    }
}