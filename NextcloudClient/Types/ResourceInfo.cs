using System;
using Newtonsoft.Json;

namespace NextcloudClient.Types
{
    /// <summary>
    /// File or directory information
    /// </summary>
    public class ResourceInfo
    {
        /// <summary>
        /// Gets or sets the base name of the file without path
        /// </summary>
        /// <value>name of the file</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full path to the file without name and without trailing slash
        /// </summary>
        /// <value>path to the file</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes
        /// </summary>
        /// <value>size of the file in bytes</value>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the file content type
        /// </summary>
        /// <value>file etag</value>
        public string ETag { get; set; }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>file content type</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the last modified time
        /// </summary>
        /// <value>last modified time</value>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets the creation time
        /// </summary>
        /// <value>creation time</value>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the quota used in bytes.
        /// </summary>
        /// <value>The quota used in bytes.</value>
        public long QuotaUsed { get; set; }

        /// <summary>
        /// Gets or sets the quota available in bytes.
        /// </summary>
        /// <value>The quota available in bytes.</value>
        public long QuotaAvailable { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = "ResourceInfo {\n";
            sb += "\tName: " + Name + "\n";
            sb += "\tPath: " + Path + "\n";
            sb += "\tSize: " + Size + "\n";
            sb += "\tETag: " + ETag + "\n";
            sb += "\tContentType: " + ContentType + "\n";
            sb += "\tLastModified: " + LastModified + "\n";
            sb += "\tCreated: " + Created + "\n";
            sb += "\tQuotaUsed: " + QuotaUsed + "\n";
            sb += "\tQuotaAvailable: " + QuotaAvailable + "\n";
            sb += "}";
            return sb;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsDirectory
        {
            get
            {
                return ContentType == null ? false : ContentType.Equals("dav/directory");
            }
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        #region Equals

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as ResourceInfo;
            return this.Equals(other);
        }

        public bool Equals(ResourceInfo other)
        {
            if (other == null)
                return false;

            return this.GetHashCode() == other.GetHashCode();
        }

        public static bool operator ==(ResourceInfo a, ResourceInfo b)
        {
            if (System.Object.ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.GetHashCode() == b.GetHashCode();
        }

        public static bool operator !=(ResourceInfo a, ResourceInfo b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            var hashCode = this.ContentType.GetHashCode() ^ this.Created.GetHashCode() ^ this.LastModified.GetHashCode() ^ this.Name.GetHashCode()
                ^ this.Path.GetHashCode() ^ this.Size.GetHashCode();

            // Is null on directories
            if (this.ETag != null)
                hashCode ^= this.ETag.GetHashCode();

            return hashCode;
        }

        #endregion Equals
    }
}

