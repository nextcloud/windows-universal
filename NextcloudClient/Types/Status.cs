using Newtonsoft.Json;

namespace NextcloudClient.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class Status
    {
        /// <summary>
        /// Gets or sets a value indicating whether the ownCloud instance was installed successfully; 
        /// true indicates a successful installation, and false indicates an unsuccessful installation.
        /// </summary>
        /// <value>
        ///   <c>true</c> if installed; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("installed")]
        public bool Installed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether maintenance mode to disable ownCloud is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if maintenance; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("maintenance")]
        public bool Maintenance { get; set; }

        [JsonProperty("needsDbUpgrade")]
        public bool NeedsDbUpgrade { get; set; }

        /// <summary>
        /// Gets or sets the current version number of your ownCloud installation.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the current version string of your ownCloud installation.
        /// </summary>
        /// <value>
        /// The version string.
        /// </value>
        [JsonProperty("versionstring")]
        public string VersionString { get; set; }

        /// <summary>
        /// Gets or sets the edition.
        /// </summary>
        /// <value>
        /// The edition.
        /// </value>
        [JsonProperty("edition")]
        public string Edition { get; set; }

        [JsonProperty("productname")]
        public string Productname { get; set; }
    }
}
